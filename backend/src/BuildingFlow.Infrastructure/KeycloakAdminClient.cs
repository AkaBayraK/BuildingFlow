using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingFlow.Application;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;

public sealed class KeycloakAdminOptions
{
    public string BaseUrl { get; init; } = "";
    public string Realm { get; init; } = "buildingflow";
    public string ClientId { get; init; } = "buildingflow-admin-api";
    public string ClientSecret { get; init; } = "";
    public bool Configured => Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && ClientSecret.Length >= 16;
    public static KeycloakAdminOptions From(IConfiguration config) => new()
    {
        BaseUrl = (config["KeycloakAdmin:BaseUrl"] ?? "").TrimEnd('/'), Realm = config["KeycloakAdmin:Realm"] ?? "buildingflow",
        ClientId = config["KeycloakAdmin:ClientId"] ?? "buildingflow-admin-api", ClientSecret = config["KeycloakAdmin:ClientSecret"] ?? ""
    };
}

// Talks to the Keycloak Admin REST API with the least-privilege service account (view/query/manage-users only).
public sealed class KeycloakAdminClient(KeycloakAdminOptions options, HttpClient http, TimeProvider clock) : IKeycloakAdmin
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    private readonly SemaphoreSlim tokenLock = new(1, 1);
    private string? accessToken;
    private DateTimeOffset expiresAt;

    private string Admin(string path) => $"{options.BaseUrl}/admin/realms/{Uri.EscapeDataString(options.Realm)}/{path}";
    private static string Id(string id) => Uri.EscapeDataString(id);

    public async Task<(IReadOnlyList<KeycloakUser> Users, int Total)> SearchAsync(string? search, int first, int max, CancellationToken token)
    {
        var query = search is null ? "" : "search=" + Uri.EscapeDataString(search) + "&";
        var users = await Read<List<UserRepresentation>>(HttpMethod.Get, Admin($"users?{query}first={first}&max={max}&briefRepresentation=true"), null, token) ?? [];
        var total = await Read<int>(HttpMethod.Get, Admin($"users/count{(search is null ? "" : "?search=" + Uri.EscapeDataString(search))}"), null, token);
        return (users.Select(Map).ToArray(), total);
    }

    public async Task<KeycloakUser?> GetAsync(string id, CancellationToken token)
    {
        using var response = await Send(HttpMethod.Get, Admin($"users/{Id(id)}"), null, token);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        Ensure(response);
        return Map((await response.Content.ReadFromJsonAsync<UserRepresentation>(Json, token))!);
    }

    public async Task<IReadOnlyList<string>> RealmRolesAsync(string id, CancellationToken token) =>
        (await Read<List<RoleRepresentation>>(HttpMethod.Get, Admin($"users/{Id(id)}/role-mappings/realm"), null, token) ?? []).Select(r => r.Name).ToArray();

    public async Task<string> CreateAsync(KeycloakUserDraft draft, CancellationToken token)
    {
        using var response = await Send(HttpMethod.Post, Admin("users"), new UserRepresentation(null, draft.Username, draft.Email, draft.FirstName, draft.LastName, draft.Enabled, false, null), token);
        if (response.StatusCode == HttpStatusCode.Conflict) throw new IdentityConflictException();
        Ensure(response);
        var location = response.Headers.Location?.ToString() ?? throw new IdentityUnavailableException();
        return location[(location.LastIndexOf('/') + 1)..];
    }

    public async Task UpdateAsync(string id, KeycloakUserDraft draft, CancellationToken token)
    {
        using var response = await Send(HttpMethod.Put, Admin($"users/{Id(id)}"), new UserRepresentation(null, null, draft.Email, draft.FirstName, draft.LastName, draft.Enabled, null, null), token);
        if (response.StatusCode == HttpStatusCode.Conflict) throw new HierarchyConflictException(ErrorCatalog.UserExists.Create());
        Ensure(response);
    }

    // Role definitions need view-realm; the user-scoped "available" and current mappings only need manage/view-users.
    public async Task ChangeRealmRolesAsync(string id, IReadOnlyCollection<string> add, IReadOnlyCollection<string> remove, CancellationToken token)
    {
        async Task<RoleRepresentation[]> Pick(string path, IReadOnlyCollection<string> names)
        {
            var roles = await Read<List<RoleRepresentation>>(HttpMethod.Get, Admin($"users/{Id(id)}/role-mappings/realm{path}"), null, token) ?? [];
            var picked = roles.Where(r => names.Contains(r.Name)).ToArray();
            if (picked.Length != names.Count) throw new IdentityUnavailableException();
            return picked;
        }
        if (add.Count > 0) { using var response = await Send(HttpMethod.Post, Admin($"users/{Id(id)}/role-mappings/realm"), await Pick("/available", add), token); Ensure(response); }
        if (remove.Count > 0) { using var response = await Send(HttpMethod.Delete, Admin($"users/{Id(id)}/role-mappings/realm"), await Pick("", remove), token); Ensure(response); }
    }

    public async Task SetPasswordAsync(string id, string password, bool temporary, CancellationToken token)
    {
        using var response = await Send(HttpMethod.Put, Admin($"users/{Id(id)}/reset-password"), new { type = "password", value = password, temporary }, token);
        if (response.StatusCode == HttpStatusCode.BadRequest) throw new RequestValidationException(new() { [temporary ? "temporaryPassword" : "password"] = [ErrorCatalog.UserPasswordWeak.Create()] });
        Ensure(response);
    }

    public async Task SendActionsEmailAsync(string id, IReadOnlyCollection<string> actions, CancellationToken token)
    {
        using var response = await Send(HttpMethod.Put, Admin($"users/{Id(id)}/execute-actions-email?lifespan=86400"), actions, token);
        Ensure(response);
    }

    public async Task DeleteAsync(string id, CancellationToken token)
    {
        using var response = await Send(HttpMethod.Delete, Admin($"users/{Id(id)}"), null, token);
        if (response.StatusCode != HttpStatusCode.NotFound) Ensure(response);
    }

    private async Task<T?> Read<T>(HttpMethod method, string url, object? body, CancellationToken token)
    {
        using var response = await Send(method, url, body, token);
        if (response.StatusCode == HttpStatusCode.NotFound) return default;
        Ensure(response);
        return await response.Content.ReadFromJsonAsync<T>(Json, token);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string url, object? body, CancellationToken token)
    {
        if (!options.Configured) throw new IdentityUnavailableException();
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await Token(attempt > 0, token));
            if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);
            HttpResponseMessage response;
            try { response = await http.SendAsync(request, token); }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException && !token.IsCancellationRequested) { throw new IdentityUnavailableException(); }
            if (response.StatusCode != HttpStatusCode.Unauthorized || attempt > 0) return response;
            response.Dispose();
        }
    }

    private async Task<string> Token(bool refresh, CancellationToken token)
    {
        await tokenLock.WaitAsync(token);
        try
        {
            if (!refresh && accessToken is not null && clock.GetUtcNow() < expiresAt) return accessToken;
            using var content = new FormUrlEncodedContent(new Dictionary<string, string> {
                ["grant_type"] = "client_credentials", ["client_id"] = options.ClientId, ["client_secret"] = options.ClientSecret });
            HttpResponseMessage response;
            try { response = await http.PostAsync($"{options.BaseUrl}/realms/{Uri.EscapeDataString(options.Realm)}/protocol/openid-connect/token", content, token); }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException && !token.IsCancellationRequested) { throw new IdentityUnavailableException(); }
            using (response)
            {
                if (!response.IsSuccessStatusCode) throw new IdentityUnavailableException();
                var result = await response.Content.ReadFromJsonAsync<TokenResponse>(Json, token) ?? throw new IdentityUnavailableException();
                accessToken = result.AccessToken;
                expiresAt = clock.GetUtcNow().AddSeconds(Math.Max(5, result.ExpiresIn - 30));
                return accessToken;
            }
        }
        finally { tokenLock.Release(); }
    }

    private static void Ensure(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized || (int)response.StatusCode >= 500 || !response.IsSuccessStatusCode) throw new IdentityUnavailableException();
    }

    private static KeycloakUser Map(UserRepresentation u) => new(u.Id!, u.Username!, u.Email, u.FirstName, u.LastName, u.Enabled ?? false, u.EmailVerified ?? false,
        u.CreatedTimestamp is { } ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null);

    private sealed record UserRepresentation(string? Id, string? Username, string? Email, string? FirstName, string? LastName, bool? Enabled, bool? EmailVerified, long? CreatedTimestamp);
    private sealed record RoleRepresentation(string? Id, string Name);
    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken, [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
