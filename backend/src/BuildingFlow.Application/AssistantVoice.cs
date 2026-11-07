namespace BuildingFlow.Application;

// Sesli yazışma: speech is transcribed on the server by a configurable local provider; the text only fills the chat box and is
// sent through /chat by the person. Audio and transcript are never stored or logged. Management roles only.
public sealed record AssistantVoiceStatus(bool Enabled, string? Reason, int MaxSeconds, long MaxBytes, IReadOnlyList<string> AcceptedTypes);
public sealed record AssistantTranscript(string Text, AssistantQuota Quota);
public sealed record AssistantVoiceLimits(int MaxSeconds, long MaxBytes)
{
    public const int DefaultSeconds = 60;
    public const long DefaultBytes = 10 * 1024 * 1024;
}
public interface ISpeechTranscriber
{
    bool Enabled { get; }
    Task<string> Transcribe(byte[] audio, string contentType, string language, CancellationToken token);
}
public sealed class SpeechUnavailableException() : Exception("Speech provider unavailable.");
public sealed class VoiceInvalidException() : Exception("Invalid voice recording.");

public sealed class AssistantVoiceService(IAssistantStore store, ISpeechTranscriber transcriber, AssistantSettings settings, AssistantVoiceLimits limits,
    IPropertyService properties, ICurrentUserService user, TimeProvider clock)
{
    // Accepted MIME types and the leading bytes each must carry; the declared type alone is never trusted.
    public static readonly IReadOnlyList<string> AcceptedTypes = ["audio/webm", "audio/ogg", "audio/mp4", "audio/wav", "audio/mpeg"];
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    private string Manager()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !AssistantTools.Managers.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return user.UserId;
    }

    public AssistantVoiceStatus Status()
    {
        Manager();
        return new(transcriber.Enabled, transcriber.Enabled ? null : "PROVIDER_NOT_CONFIGURED", limits.MaxSeconds, limits.MaxBytes, AcceptedTypes);
    }

    public static string? BaseType(string? contentType) => contentType?.Split(';')[0].Trim().ToLowerInvariant() switch
    {
        "audio/x-wav" or "audio/wave" => "audio/wav",
        "audio/mp3" => "audio/mpeg",
        "video/webm" => "audio/webm",
        var type when type is not null && AcceptedTypes.Contains(type) => type,
        _ => null
    };

    public static bool Signature(string type, ReadOnlySpan<byte> b) => type switch
    {
        "audio/webm" => b.Length >= 4 && b[0] == 0x1A && b[1] == 0x45 && b[2] == 0xDF && b[3] == 0xA3,
        "audio/ogg" => b.Length >= 4 && b[..4].SequenceEqual("OggS"u8),
        "audio/mp4" => b.Length >= 8 && b[4..8].SequenceEqual("ftyp"u8),
        "audio/wav" => b.Length >= 12 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WAVE"u8),
        "audio/mpeg" => b.Length >= 3 && (b[..3].SequenceEqual("ID3"u8) || b[0] == 0xFF && (b[1] & 0xE0) == 0xE0),
        _ => false
    };

    // Audited as FREE_TEXT with tool TRANSCRIBE and source SPEECH, so voice shares the management quota without a schema change.
    public async Task<AssistantTranscript?> Transcribe(Guid property, string? contentType, byte[] audio, int? durationMs, string? locale, CancellationToken token)
    {
        var actor = Manager();
        var type = BaseType(contentType); var language = locale ?? "tr";
        if (property == Guid.Empty || type is null || audio.Length is 0 || audio.LongLength > limits.MaxBytes || durationMs is null or < 1 || durationMs > limits.MaxSeconds * 1000
            || language is not ("tr" or "en") || !Signature(type, audio))
            throw new VoiceInvalidException();
        if (!transcriber.Enabled) throw new SpeechUnavailableException();
        if (await properties.GetAsync(property, token) is not { IsEnabled: true }) return null;
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Zone).DateTime);
        var audit = await store.Reserve(actor, property, day, settings.DailyLimit, "FREE_TEXT", null, AssistantService.ManagementKinds, token) ?? throw new AssistantBudgetException();
        var started = clock.GetTimestamp(); var outcome = "FAILED";
        try
        {
            var text = (await transcriber.Transcribe(audio, type, language, token)).Trim();
            text = new string(text.Where(c => !char.IsControl(c) || c is '\n').ToArray());
            if (text.Length > AssistantService.MaxMessageLength) text = text[..AssistantService.MaxMessageLength];
            outcome = text.Length == 0 ? "UNSUPPORTED" : "ANSWERED";
            var used = await store.Used(actor, day, AssistantService.ManagementKinds, token);
            return new(text, new(settings.DailyLimit, used, Math.Max(0, settings.DailyLimit - used)));
        }
        finally
        {
            await store.Finish(audit, "TRANSCRIBE", "SPEECH", outcome, (int)clock.GetElapsedTime(started).TotalMilliseconds, CancellationToken.None);
        }
    }
}
