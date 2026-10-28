namespace BuildingFlow.Domain;

public enum QaDataKind { DEMO, TEST }
public enum QaDataBatchStatus { ACTIVE, CLEANED }

// A batch of synthetic records; every created row is listed in QaDataRecord so cleanup never guesses.
public sealed class QaDataBatch : BaseEntity
{
    private QaDataBatch() { }
    public QaDataBatch(string code, QaDataKind kind, string environment, Guid requestId, string? runCode, DateTimeOffset createdAt)
    {
        if (requestId == Guid.Empty || !Enum.IsDefined(kind)) throw new ArgumentException("Invalid QA data batch.");
        Code = Required(code, nameof(code), 40);
        Kind = kind;
        Environment = Required(environment, nameof(environment), 40);
        RequestId = requestId;
        RunCode = string.IsNullOrWhiteSpace(runCode) ? null : Required(runCode, nameof(runCode), 40);
        GeneratedAt = createdAt;
    }
    public string Code { get; private set; } = null!;
    public QaDataKind Kind { get; private set; }
    public string Environment { get; private set; } = null!;
    public Guid RequestId { get; private set; }
    public string? RunCode { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public QaDataBatchStatus Status { get; private set; } = QaDataBatchStatus.ACTIVE;
    public DateTimeOffset? CleanedAt { get; private set; }
    public int RecordCount { get; private set; }

    public void Registered(int count) => RecordCount = count;

    public void MarkCleaned(DateTimeOffset now)
    {
        if (Kind == QaDataKind.DEMO) throw new InvalidOperationException("Demo data is protected.");
        if (Status != QaDataBatchStatus.ACTIVE) throw new InvalidOperationException("QA data batch is already cleaned.");
        Status = QaDataBatchStatus.CLEANED;
        CleanedAt = now;
    }
}

public sealed class QaDataRecord : BaseEntity
{
    private QaDataRecord() { }
    public QaDataRecord(Guid batchId, string entityType, Guid entityId, int cleanupOrder)
    {
        if (batchId == Guid.Empty || entityId == Guid.Empty || cleanupOrder is < 1 or > 100) throw new ArgumentException("Invalid QA data record.");
        BatchId = batchId;
        EntityType = Required(entityType, nameof(entityType), 40);
        EntityId = entityId;
        CleanupOrder = cleanupOrder;
    }
    public Guid BatchId { get; private set; }
    public string EntityType { get; private set; } = null!;
    public Guid EntityId { get; private set; }
    public int CleanupOrder { get; private set; }
}
