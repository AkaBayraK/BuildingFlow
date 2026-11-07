using System.Net.Http.Headers;
using System.Text.Json;
using BuildingFlow.Application;

namespace BuildingFlow.Api;

// Assistant:Speech points at a local speech-to-text server: an OpenAI compatible /v1/audio/transcriptions endpoint or
// whisper.cpp's server (Path=/inference). The optional key comes only from env/user-secrets. Nothing is sent to a browser vendor.
public sealed class AssistantSpeechOptions
{
    public string? BaseUrl { get; init; }
    public string Path { get; init; } = "/v1/audio/transcriptions";
    public string? Model { get; init; }
    public string? ApiKey { get; init; }
    public int TimeoutSeconds { get; init; } = 60;
    public bool Enabled => Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" && Path.StartsWith('/');
    public static AssistantSpeechOptions From(IConfiguration configuration)
    {
        var section = configuration.GetSection("Assistant:Speech");
        return new()
        {
            BaseUrl = section["BaseUrl"], Path = string.IsNullOrWhiteSpace(section["Path"]) ? "/v1/audio/transcriptions" : section["Path"]!, Model = section["Model"], ApiKey = section["ApiKey"],
            TimeoutSeconds = Math.Clamp(section.GetValue("TimeoutSeconds", 60), 5, 180)
        };
    }
}

public sealed class LocalSpeechTranscriber(IHttpClientFactory clients, AssistantSpeechOptions options) : ISpeechTranscriber
{
    public const string ClientName = "assistant-speech";
    public bool Enabled => options.Enabled;

    public async Task<string> Transcribe(byte[] audio, string contentType, string language, CancellationToken cancellation)
    {
        if (!options.Enabled) throw new SpeechUnavailableException();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        try
        {
            using var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(audio); file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(file, "file", "speech." + contentType.Split('/')[1]);
            form.Add(new StringContent(language), "language");
            form.Add(new StringContent("json"), "response_format");
            if (!string.IsNullOrWhiteSpace(options.Model)) form.Add(new StringContent(options.Model), "model");
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(options.BaseUrl!), options.Path)) { Content = form };
            if (!string.IsNullOrWhiteSpace(options.ApiKey)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            using var response = await clients.CreateClient(ClientName).SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode) throw new SpeechUnavailableException();
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            return body.RootElement.GetProperty("text").GetString() ?? "";
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException && !cancellation.IsCancellationRequested)
        {
            throw new SpeechUnavailableException();
        }
    }
}
