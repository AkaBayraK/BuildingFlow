namespace BuildingFlow.Domain;

public enum ExpenseCategory { MAINTENANCE, CLEANING, STAFF, ENERGY, SECURITY, INSURANCE, ADMINISTRATION, OTHER }

public sealed class Expense : BaseEntity
{
    private Expense() { }
    public Expense(Guid propertyId, Guid requestId, string fingerprint, ExpenseCategory category,
        decimal amount, DateOnly occurredDate, string description, string? documentReference)
    {
        if (propertyId == Guid.Empty || requestId == Guid.Empty || !Enum.IsDefined(category)) throw new ArgumentException("Invalid expense scope.");
        FinanceIdentity.Validate(fingerprint);
        Payment.ValidateAmount(amount);
        if (occurredDate.Year < 2000) throw new ArgumentException("Invalid expense date.");
        description = Required(description, nameof(description), 500);
        documentReference = string.IsNullOrWhiteSpace(documentReference) ? null : Required(documentReference, nameof(documentReference), 100);
        PropertyId = propertyId; RequestId = requestId; RequestFingerprint = fingerprint; Category = category;
        Amount = amount; OccurredDate = occurredDate; Description = description; DocumentReference = documentReference;
    }
    public Guid PropertyId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestFingerprint { get; private set; } = null!;
    public ExpenseCategory Category { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public DateOnly OccurredDate { get; private set; }
    public string Description { get; private set; } = null!;
    public string? DocumentReference { get; private set; }
    public bool IsCancelled { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public void Cancel(string reason, DateTimeOffset at)
    {
        if (IsCancelled) throw new InvalidOperationException("Expense history cannot be rewritten.");
        reason = Required(reason, nameof(reason), 500);
        if (at == default) throw new ArgumentException("Cancellation timestamp is required.");
        IsCancelled = true; CancellationReason = reason; CancelledAt = at;
    }
}

internal static class FinanceIdentity
{
    public static void Validate(string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Length != 64 || fingerprint.Any(value => !Uri.IsHexDigit(value)))
            throw new ArgumentException("Invalid request fingerprint.");
    }
}
