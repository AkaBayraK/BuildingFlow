namespace BuildingFlow.Domain;

public enum OutboxStatus { PENDING, PUBLISHED }
public enum InboxStatus { PROCESSED, SKIPPED }

public sealed class OutboxMessage : BaseEntity
{
    public const int MaxPayloadLength = 65536;
    private OutboxMessage() { }
    public OutboxMessage(Guid messageId, Guid propertyId, Guid correlationId, string type, DateTimeOffset occurredAt, string payload)
    {
        if (messageId == Guid.Empty || propertyId == Guid.Empty || correlationId == Guid.Empty || occurredAt == default ||
            occurredAt.Offset != TimeSpan.Zero) throw new ArgumentException("Invalid outbox identity.");
        if (string.IsNullOrWhiteSpace(payload) || payload.Length > MaxPayloadLength) throw new ArgumentException("Invalid outbox payload.");
        MessageId = messageId; PropertyId = propertyId; CorrelationId = correlationId; Type = Required(type, nameof(type), 50);
        OccurredAt = occurredAt; Payload = payload; Status = OutboxStatus.PENDING; NextAttemptAt = occurredAt;
    }
    public Guid MessageId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string Type { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }
    public string Payload { get; private set; } = null!;
    public OutboxStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public string? LastErrorCode { get; private set; }

    public void MarkPublished(DateTimeOffset now)
    {
        if (Status != OutboxStatus.PENDING) throw new InvalidOperationException("Outbox message is already published.");
        Status = OutboxStatus.PUBLISHED; PublishedAt = now; Attempts++; LastErrorCode = null;
    }

    public void RecordFailure(string errorCode, DateTimeOffset nextAttemptAt)
    {
        if (Status != OutboxStatus.PENDING) throw new InvalidOperationException("Outbox message is already published.");
        Attempts++; LastErrorCode = Required(errorCode, nameof(errorCode), 50); NextAttemptAt = nextAttemptAt;
    }
}

public sealed class ProcessedMessage : BaseEntity
{
    private ProcessedMessage() { }
    public ProcessedMessage(string consumer, Guid messageId, Guid propertyId, string type, string payloadSha256, InboxStatus status, DateTimeOffset processedAt)
    {
        if (messageId == Guid.Empty || propertyId == Guid.Empty || !Enum.IsDefined(status)) throw new ArgumentException("Invalid inbox identity.");
        FinanceIdentity.Validate(payloadSha256);
        Consumer = Required(consumer, nameof(consumer), 100); MessageId = messageId; PropertyId = propertyId;
        Type = Required(type, nameof(type), 50); PayloadSha256 = payloadSha256; Status = status; ProcessedAt = processedAt;
    }
    public string Consumer { get; private set; } = null!;
    public Guid MessageId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Type { get; private set; } = null!;
    public string PayloadSha256 { get; private set; } = null!;
    public InboxStatus Status { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }
    public void MarkSkipped() => Status = InboxStatus.SKIPPED;
}
