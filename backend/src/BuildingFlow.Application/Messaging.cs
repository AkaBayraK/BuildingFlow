using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BuildingFlow.Application;

// These commands are transport contracts. Authorization and transactional enqueue belong to the calling use case.
public interface IMessageCommand
{
    Guid MessageId { get; }
    Guid PropertyId { get; }
    Guid CorrelationId { get; }
    DateTimeOffset OccurredAt { get; }
    string Locale { get; }
}

public sealed record EmailRequested(Guid MessageId, Guid PropertyId, Guid CorrelationId,
    DateTimeOffset OccurredAt, string Locale, string Recipient, string Subject, string Body) : IMessageCommand;
public sealed record SmsRequested(Guid MessageId, Guid PropertyId, Guid CorrelationId,
    DateTimeOffset OccurredAt, string Locale, string Recipient, string Body) : IMessageCommand;
public sealed record NotificationRequested(Guid MessageId, Guid PropertyId, Guid CorrelationId,
    DateTimeOffset OccurredAt, string Locale, string RecipientSubject, string Title, string Body) : IMessageCommand;

public sealed record MessageEnvelope(int SchemaVersion, string Type, Guid MessageId, Guid PropertyId,
    Guid CorrelationId, DateTimeOffset OccurredAt, JsonElement Payload);

// Enqueue only adds to the caller's unit of work; the caller's SaveChanges/transaction commits it with the business write.
public interface IMessageOutbox { void Enqueue(IMessageCommand command); }

public enum MessageHandlingOutcome { Delivered, Skipped }
public interface IMessageHandler
{
    string Type { get; }
    string Consumer { get; }
    Task<MessageHandlingOutcome> HandleAsync(IMessageCommand command, CancellationToken cancellationToken);
}

public sealed class InvalidMessageException() : Exception("Messaging contract is invalid.");
public sealed class MessagePublicationException() : Exception("Message publication was not confirmed.");

public static class MessageContracts
{
    public const int SchemaVersion = 1;
    public const int MaxBytes = 65536;
    public const string Email = "email.requested.v1";
    public const string Sms = "sms.requested.v1";
    public const string Notification = "notification.requested.v1";
    public static IReadOnlyList<string> Types { get; } = Array.AsReadOnly(new[] { Email, Sms, Notification });
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 16
    };

    public static string TypeOf(IMessageCommand command) => command switch {
        EmailRequested => Email, SmsRequested => Sms, NotificationRequested => Notification,
        _ => throw new InvalidMessageException()
    };

    public static void Validate(IMessageCommand command)
    {
        if (command is null || command.MessageId == Guid.Empty || command.PropertyId == Guid.Empty ||
            command.CorrelationId == Guid.Empty || command.OccurredAt == default || command.OccurredAt.Offset != TimeSpan.Zero ||
            command.Locale is not ("tr" or "en")) throw new InvalidMessageException();
        switch (command) {
            case EmailRequested email:
                Text(email.Recipient, 254, true); Text(email.Subject, 200, true); Text(email.Body, 10000);
                if (!MailAddress.TryCreate(email.Recipient, out var address) || address.Address != email.Recipient || email.Recipient != email.Recipient.Trim()) throw new InvalidMessageException();
                break;
            case SmsRequested sms:
                Text(sms.Recipient, 16, true); Text(sms.Body, 1000);
                if (!Regex.IsMatch(sms.Recipient, @"^\+[1-9][0-9]{7,14}$", RegexOptions.CultureInvariant)) throw new InvalidMessageException();
                break;
            case NotificationRequested notification:
                Text(notification.RecipientSubject, 200, true); Text(notification.Title, 200, true); Text(notification.Body, 2000);
                if (notification.RecipientSubject != notification.RecipientSubject.Trim()) throw new InvalidMessageException();
                break;
            default: throw new InvalidMessageException();
        }
    }
    private static void Text(string value, int max, bool singleLine = false)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > max || value.Any(c => char.IsControl(c) && (singleLine || c is not ('\r' or '\n' or '\t')))) throw new InvalidMessageException();
    }

    public static byte[] Encode(IMessageCommand command)
    {
        Validate(command);
        var payload = JsonSerializer.SerializeToElement(command, command.GetType(), Json);
        var envelope = new MessageEnvelope(SchemaVersion, TypeOf(command), command.MessageId, command.PropertyId, command.CorrelationId, command.OccurredAt, payload);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, Json);
        if (bytes.Length > MaxBytes) throw new InvalidMessageException();
        return bytes;
    }

    public static IMessageCommand Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is < 1 or > MaxBytes) throw new InvalidMessageException();
        try {
            var envelope = JsonSerializer.Deserialize<MessageEnvelope>(bytes, Json) ?? throw new InvalidMessageException();
            if (envelope.SchemaVersion != SchemaVersion) throw new InvalidMessageException();
            IMessageCommand? command = envelope.Type switch {
                Email => envelope.Payload.Deserialize<EmailRequested>(Json),
                Sms => envelope.Payload.Deserialize<SmsRequested>(Json),
                Notification => envelope.Payload.Deserialize<NotificationRequested>(Json),
                _ => throw new InvalidMessageException()
            };
            if (command is null) throw new InvalidMessageException();
            Validate(command);
            if (command.MessageId != envelope.MessageId || command.PropertyId != envelope.PropertyId ||
                command.CorrelationId != envelope.CorrelationId || command.OccurredAt != envelope.OccurredAt) throw new InvalidMessageException();
            return command;
        } catch (JsonException) { throw new InvalidMessageException(); }
    }
}
