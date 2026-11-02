using System.Diagnostics;
using System.Text.Json;
using BuildingFlow.Application;

namespace BuildingFlow.Api;

// AI Durumu: TraceFlow "AI Durumu" ekranının BuildingFlow karşılığı. Altyapı bileşenleri Sistem durumu ekranındadır; burada
// yalnız asistan/AkaAI hattı izlenir. Model çağrısı yapılmaz; yanıta token, secret, tam adres veya AkaAI hata gövdesi girmez.
// Açıklamalar dil bağımsız kodlardır (Detail), metni ön yüz sözlüğü üretir.
public sealed record AiStatusComponent(string Id, string Type, string Status, long? ResponseTimeMs, DateTimeOffset CheckedAt, string Detail, int? Count = null);
public sealed record AiStatusResponse(string OverallStatus, string Mode, string? Model, string? ModelProvider, string Endpoint, string? Locality, string? ConsoleUrl,
    int ActivePromptCount, int ManagementDailyLimit, bool ResidentEnabled, int ResidentDailyLimit, bool QaCenterAiEnabled,
    int TotalComponents, int UpComponents, int DegradedComponents, int DownComponents, int UnknownComponents, int DisabledComponents,
    DateTimeOffset LastCheckedAt, IReadOnlyList<AiStatusComponent> Components);

public sealed record AkaAiProbeResult(string Status, string Detail, string? ModelName, string? Locality, string? ProviderCode, long ResponseTimeMs, DateTimeOffset CheckedAt);

// /readyz ve servis hesabıyla /api/v1/integrations/models okunur. Sonuç 30 sn saklanır; ekranın otomatik yenilemesi AkaAI oran sınırını tüketmez.
public sealed class AkaAiHealthProbe(IHttpClientFactory clients, AssistantAkaAiOptions options, TimeProvider clock)
{
    public const string ClientName = "assistant-akaai-status";
    public static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private readonly SemaphoreSlim gate = new(1, 1);
    private AkaAiProbeResult? cached;

    public async Task<AkaAiProbeResult> Check(CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            if (cached is { } fresh && clock.GetUtcNow() - fresh.CheckedAt < CacheTtl) return fresh;
            var watch = Stopwatch.StartNew();
            var (status, detail, model, locality, provider) = await Probe(token);
            return cached = new(status, detail, model, locality, provider, watch.ElapsedMilliseconds, clock.GetUtcNow());
        }
        finally { gate.Release(); }
    }

    private async Task<(string, string, string?, string?, string?)> Probe(CancellationToken token)
    {
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var root)) return ("DOWN", "INVALID_ADDRESS", null, null, null);
        var http = clients.CreateClient(ClientName);
        try
        {
            using (var ready = await http.GetAsync(new Uri(root, "/readyz"), HttpCompletionOption.ResponseHeadersRead, token))
                if (!ready.IsSuccessStatusCode) return ("DOWN", "NOT_READY", null, null, null);
            string access;
            try { access = await AccessToken(http, token); }
            catch (Exception error) when (error is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException) { return ("DOWN", "TOKEN_FAILED", null, null, null); }
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(root, "/api/v1/integrations/models"));
            request.Headers.Authorization = new("Bearer", access);
            using var response = await http.SendAsync(request, token);
            var code = (int)response.StatusCode;
            if (code is 401 or 403) return ("DOWN", "REJECTED", null, null, null);
            if (code == 429 || code >= 500) return ("DEGRADED", "MODELS_UNAVAILABLE", null, null, null);
            if (code is < 200 or >= 300) return ("DOWN", "UNEXPECTED_RESPONSE", null, null, null);
            try { return Selected(JsonDocument.Parse(await response.Content.ReadAsStringAsync(token)).RootElement.GetProperty("data")); }
            catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException) { return ("DEGRADED", "MODELS_UNREADABLE", null, null, null); }
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException && !token.IsCancellationRequested) { return ("DOWN", "UNREACHABLE", null, null, null); }
    }

    private (string, string, string?, string?, string?) Selected(JsonElement models)
    {
        var configured = string.IsNullOrWhiteSpace(options.Model) ? null : options.Model.Trim();
        foreach (var model in models.EnumerateArray())
        {
            var id = model.TryGetProperty("id", out var value) ? value.GetString() : null;
            var matches = configured is not null ? configured == id : model.TryGetProperty("x_akaai_default", out var fallback) && fallback.ValueKind == JsonValueKind.True;
            if (matches) return ("UP", configured is null ? "DEFAULT_MODEL_READY" : "MODEL_READY", id, Text(model, "x_akaai_locality"), Text(model, "owned_by"));
        }
        return ("DEGRADED", configured is null ? "DEFAULT_MODEL_MISSING" : "MODEL_MISSING", configured, null, null);
    }

    private static string? Text(JsonElement model, string name) => model.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private async Task<string> AccessToken(HttpClient http, CancellationToken token)
    {
        using var response = await http.PostAsync(options.TokenUrl, new FormUrlEncodedContent([new("grant_type", "client_credentials"), new("client_id", options.ClientId!), new("client_secret", options.ClientSecret!)]), token);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        return body.RootElement.GetProperty("access_token").GetString() ?? throw new InvalidOperationException();
    }
}

public sealed class AiStatusService(IServiceScopeFactory scopes, IServiceProvider services, AssistantAkaAiOptions options, AssistantSettings settings,
    ResidentAssistantSettings resident, AkaAiHealthProbe probe, TimeProvider clock)
{
    public async Task<AiStatusResponse> CheckAsync(CancellationToken token)
    {
        var configured = options.Enabled;
        var result = configured ? await probe.Check(token) : null;
        var (promptCount, prompts) = await Prompts(token);
        var qa = QaCenterAi();
        AiStatusComponent[] components =
        [
            Component("assistant-configuration", "CONFIGURATION", configured ? "UP" : "DISABLED", null, configured ? "CONFIGURED" : "RULES_MODE"),
            result is null ? Component("akaai-model", "MODEL", "DISABLED", null, "NOT_CHECKED") : new("akaai-model", "MODEL", result.Status, result.ResponseTimeMs, result.CheckedAt, result.Detail),
            prompts,
            Component("resident-assistant", "SERVICE", resident.Enabled ? "UP" : "DISABLED", null, resident.Enabled ? "ENABLED" : "CONSENT_GATE"),
            Component("qa-center-ai", "CONFIGURATION", qa ? "UP" : "DISABLED", null, qa ? "CONFIGURED" : "NOT_CONFIGURED")
        ];
        int Count(string status) => components.Count(c => c.Status == status);
        int down = Count("DOWN"), degraded = Count("DEGRADED"), unknown = Count("UNKNOWN");
        var overall = !configured ? "DISABLED" : down > 0 ? "DOWN" : degraded > 0 ? "DEGRADED" : unknown > 0 ? "UNKNOWN" : "UP";
        var model = result?.ModelName ?? (string.IsNullOrWhiteSpace(options.Model) ? null : options.Model.Trim());
        return new(overall, configured ? AssistantService.SourceModel : AssistantService.SourceRules, configured ? model : null, result?.ProviderCode,
            configured ? SafeEndpoint(options.BaseUrl) : "-", result?.Locality is "LOCAL" or "EXTERNAL" ? result.Locality : null, ConsoleUrl(),
            promptCount, settings.DailyLimit, resident.Enabled, resident.DailyLimit, qa,
            components.Length, Count("UP"), degraded, down, unknown, Count("DISABLED"), clock.GetUtcNow(), components);
    }

    private AiStatusComponent Component(string id, string type, string status, long? elapsed, string detail, int? count = null) =>
        new(id, type, status, elapsed, clock.GetUtcNow(), detail, count);

    private async Task<(int, AiStatusComponent)> Prompts(CancellationToken token)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            using var scope = scopes.CreateScope();
            var count = (await scope.ServiceProvider.GetRequiredService<IAssistantStore>().Prompts(false, token)).Count;
            return (count, Component("prompt-templates", "PROMPT", count > 0 ? "UP" : "DEGRADED", watch.ElapsedMilliseconds, count > 0 ? "PROMPTS_READY" : "NO_PROMPTS", count));
        }
        catch (Exception) when (!token.IsCancellationRequested)
        {
            return (0, Component("prompt-templates", "PROMPT", "DOWN", watch.ElapsedMilliseconds, "PROMPTS_UNREADABLE"));
        }
    }

    // QaCenter:AkaAi yalnız Test Merkezi açıkken kaydedilir; kapalıysa bileşen pasif görünür.
    private bool QaCenterAi() => services.GetService<QaCenterOptions>() is { Enabled: true } && services.GetService<QaAkaAiOptions>() is { Enabled: true };

    private string? ConsoleUrl() =>
        Uri.TryCreate(options.ConsoleUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0
            ? uri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/models" : null;

    private static string SafeEndpoint(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}" : "-";
}

internal static class AiStatusEndpoints
{
    public static IServiceCollection AddAiStatus(this IServiceCollection services)
    {
        services.AddHttpClient(AkaAiHealthProbe.ClientName, client => client.Timeout = TimeSpan.FromSeconds(5))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false, ConnectTimeout = TimeSpan.FromSeconds(3) });
        services.AddSingleton<AkaAiHealthProbe>();
        return services.AddSingleton<AiStatusService>();
    }

    // Yönetim > AI Durumu ekranının kaynağı; Sistem durumu ile aynı yönetim rollerine açıktır.
    public static void MapAiStatusEndpoints(this WebApplication app) =>
        app.MapGet("/api/v1/system/ai-status", async (AiStatusService service, HttpResponse response, CancellationToken token) =>
        {
            response.Headers.CacheControl = "no-store";
            return Results.Ok(await service.CheckAsync(token));
        }).RequireAuthorization("CanManageHierarchy").WithTags("System");
}
