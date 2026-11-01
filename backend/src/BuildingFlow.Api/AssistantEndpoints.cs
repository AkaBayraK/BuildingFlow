using System.Net.Http.Headers;
using System.Text.Json;
using BuildingFlow.Application;

namespace BuildingFlow.Api;

// Assistant:AkaAi is separate from QaCenter:AkaAi so each feature has its own client and quota. Secrets come only from
// environment variables or user-secrets; without them the assistant runs in rule-based mode instead of closing.
public sealed class AssistantAkaAiOptions
{
    public string? BaseUrl { get; init; }
    public string? TokenUrl { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? Model { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
    public bool Enabled => Absolute(BaseUrl) && Absolute(TokenUrl) && !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
    private static bool Absolute(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    public static AssistantAkaAiOptions From(IConfiguration configuration)
    {
        var section = configuration.GetSection("Assistant:AkaAi");
        return new()
        {
            BaseUrl = section["BaseUrl"], TokenUrl = section["TokenUrl"], ClientId = section["ClientId"], ClientSecret = section["ClientSecret"], Model = section["Model"],
            TimeoutSeconds = Math.Clamp(section.GetValue("TimeoutSeconds", 30), 5, 120)
        };
    }
}

// Prompts and replies are never logged; any transport or shape failure becomes "unavailable" and the service answers by rules.
public sealed class AkaAiAssistantModel(IHttpClientFactory clients, AssistantAkaAiOptions options, TimeProvider clock) : IAssistantModel
{
    public const string ClientName = "assistant-akaai";
    private (string Token, DateTimeOffset Expires)? token;
    public bool Enabled => options.Enabled;

    public async Task<string> Complete(string actorRef, string system, string user, CancellationToken cancellation)
    {
        if (!options.Enabled) throw new AssistantModelUnavailableException();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        try
        {
            var http = clients.CreateClient(ClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(options.BaseUrl!), "/api/v1/integrations/chat/completions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await Token(http, timeout.Token));
            request.Headers.Add("X-Actor-Ref", actorRef);
            request.Headers.Add("X-Request-Id", "bf-assistant-" + Guid.NewGuid().ToString("N"));
            request.Content = JsonContent.Create(new { model = options.Model, messages = new[] { new { role = "system", content = system }, new { role = "user", content = user } } });
            using var response = await http.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode) throw new AssistantModelUnavailableException();
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            return body.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException && !cancellation.IsCancellationRequested)
        {
            throw new AssistantModelUnavailableException();
        }
    }

    private async Task<string> Token(HttpClient http, CancellationToken cancellation)
    {
        if (token is { } cached && cached.Expires > clock.GetUtcNow()) return cached.Token;
        using var response = await http.PostAsync(options.TokenUrl, new FormUrlEncodedContent([new("grant_type", "client_credentials"), new("client_id", options.ClientId!), new("client_secret", options.ClientSecret!)]), cancellation);
        if (!response.IsSuccessStatusCode) throw new AssistantModelUnavailableException();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
        var value = body.RootElement.GetProperty("access_token").GetString()!;
        var lifetime = body.RootElement.TryGetProperty("expires_in", out var expires) && expires.TryGetInt32(out var seconds) ? seconds : 60;
        token = (value, clock.GetUtcNow().AddSeconds(Math.Max(0, lifetime - 30)));
        return value;
    }
}

internal static class AssistantEndpoints
{
    public static IServiceCollection AddAssistant(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(AssistantAkaAiOptions.From(configuration));
        services.AddSingleton(new AssistantSettings(Math.Clamp(configuration.GetValue("Assistant:DailyRequestLimit", AssistantSettings.DefaultLimit), 1, 1000)));
        services.AddSingleton<IAssistantModel, AkaAiAssistantModel>();
        services.AddHttpClient(AkaAiAssistantModel.ClientName, client => client.Timeout = TimeSpan.FromSeconds(150))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        services.AddScoped<AssistantService>();
        // The resident assistant stays off until the KVKK review, consent type and notice text are in place (FAZ 3 gate).
        services.AddSingleton(new ResidentAssistantSettings(configuration.GetValue("Assistant:Resident:Enabled", false),
            Math.Clamp(configuration.GetValue("Assistant:Resident:DailyRequestLimit", ResidentAssistantSettings.DefaultLimit), 1, 100)));
        services.AddScoped<ResidentAssistantService>();
        return services;
    }

    public static void MapAssistantEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/assistant").RequireAuthorization("CanUseAssistant").WithTags("Assistant");
        root.MapGet("/status", async (AssistantService service, HttpResponse response, CancellationToken token) => NoStore(response, Results.Ok(await service.Status(token))));
        root.MapGet("/prompts", async (AssistantService service, CancellationToken token) => Results.Ok(await service.ActivePrompts(token)));
        root.MapPost("/chat", async (AssistantChatRequest request, AssistantService service, HttpResponse response, CancellationToken token) =>
        {
            try { return NoStore(response, await service.Chat(request, token) is { } answer ? Results.Ok(answer) : Results.NotFound()); }
            catch (AssistantBudgetException) { return Budget(); }
        });
        // Drafts are returned to the caller only; nothing is saved or published here.
        root.MapPost("/announcement-draft", async (AssistantDraftRequest request, AssistantService service, HttpResponse response, CancellationToken token) =>
        {
            try { return NoStore(response, await service.DraftAnnouncement(request, token) is { } draft ? Results.Ok(draft) : Results.NotFound()); }
            catch (AssistantBudgetException) { return Budget(); }
        });
        // Residents: any signed-in account; the service checks the flag, consent and the caller's own unit context on every call.
        var resident = app.MapGroup("/api/v1/mobile/assistant").RequireAuthorization().WithTags("Mobile assistant");
        resident.MapGet("/status", async (ResidentAssistantService service, HttpResponse response, CancellationToken token) => NoStore(response, Results.Ok(await service.Status(token))));
        resident.MapGet("/prompts", async (ResidentAssistantService service, CancellationToken token) => Results.Ok(await service.Prompts(token)));
        resident.MapPost("/chat", async (ResidentChatRequest request, ResidentAssistantService service, HttpResponse response, CancellationToken token) =>
        {
            try { return NoStore(response, Results.Ok(await service.Chat(request, token))); }
            catch (AssistantBudgetException) { return Budget(); }
        });
        var admin = app.MapGroup("/api/v1/assistant/admin/prompts").RequireAuthorization("CanManageAssistantPrompts").WithTags("Assistant");
        admin.MapGet("", async (AssistantService service, CancellationToken token) => Results.Ok(await service.ManagedPrompts(token)));
        admin.MapPost("", async (SaveAssistantPromptRequest request, AssistantService service, CancellationToken token) =>
        {
            var prompt = await service.CreatePrompt(request, token);
            return Results.Created($"/api/v1/assistant/admin/prompts/{prompt.Id}", prompt);
        });
        admin.MapPut("/{id:guid}", async (Guid id, SaveAssistantPromptRequest request, AssistantService service, CancellationToken token) =>
            await service.UpdatePrompt(id, request, token) is { } prompt ? Results.Ok(prompt) : Results.NotFound());
        admin.MapDelete("/{id:guid}", async (Guid id, long version, AssistantService service, CancellationToken token) =>
            await service.DeletePrompt(id, version, token) ? Results.NoContent() : Results.NotFound());
    }

    private static IResult Budget()
    {
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = StatusCodes.Status429TooManyRequests };
        ApiProblems.Apply(problem, ErrorCatalog.AssistantBudget.Create());
        return Results.Problem(problem);
    }

    private static IResult NoStore(HttpResponse response, IResult result)
    {
        response.Headers.CacheControl = "no-store";
        return result;
    }
}
