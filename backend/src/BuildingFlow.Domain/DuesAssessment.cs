namespace BuildingFlow.Domain;

public enum ChargeKind { DUES, EXTRA }
public enum ChargeCategory { FIXTURE, REPAIR, HEATING, UTILITY, INSURANCE, OTHER }

public sealed class DuesAssessment : BaseEntity
{
    private readonly List<DuesCharge> _charges = [];
    private DuesAssessment() { }
    public DuesAssessment(Guid propertyId, Guid requestId, string fingerprint, DateOnly period,
        decimal amount, string currency, DateOnly dueDate, string description, string debtorType = "UNIT",
        ChargeKind kind = ChargeKind.DUES, ChargeCategory? category = null)
    {
        if (propertyId == Guid.Empty || requestId == Guid.Empty) throw new ArgumentException("Valid identities are required.");
        if (string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Length != 64 || fingerprint.Any(value => !Uri.IsHexDigit(value))) throw new ArgumentException("Invalid request fingerprint.");
        Validate(period, amount, currency, dueDate);
        if (debtorType is not ("UNIT" or "TENANT" or "OWNER")) throw new ArgumentException("Invalid debtor type.");
        if (!Enum.IsDefined(kind) || (kind == ChargeKind.DUES) != (category is null) || (category is { } value && !Enum.IsDefined(value)))
            throw new ArgumentException("Extra charges need a category; dues have none.");
        DebtorType = debtorType; Kind = kind; Category = category;
        PropertyId = propertyId; RequestId = requestId; RequestFingerprint = fingerprint;
        Period = period; Amount = amount; Currency = currency; DueDate = dueDate;
        Description = Required(description, nameof(description), 500);
    }
    public Guid PropertyId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestFingerprint { get; private set; } = null!;
    public DateOnly Period { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public DateOnly DueDate { get; private set; }
    public string Description { get; private set; } = null!;
    public string DebtorType { get; private set; } = "UNIT";
    public ChargeKind Kind { get; private set; }
    public ChargeCategory? Category { get; private set; }
    public IReadOnlyCollection<DuesCharge> Charges => _charges.AsReadOnly();
    public DuesCharge AddCharge(Guid unitId)
    {
        if (unitId == Guid.Empty || _charges.Any(value => value.UnitId == unitId)) throw new ArgumentException("Select distinct units.");
        if (_charges.Count >= 500) throw new InvalidOperationException("An assessment contains at most 500 units.");
        var charge = new DuesCharge(this, unitId); _charges.Add(charge); return charge;
    }
    public static void Validate(DateOnly period, decimal amount, string currency, DateOnly dueDate)
    {
        if (period.Year < 2000 || period.Day != 1) throw new ArgumentException("A monthly period from year 2000 is required.");
        if (amount <= 0 || amount > 99999999.99m || decimal.Round(amount, 2) != amount) throw new ArgumentOutOfRangeException(nameof(amount));
        if (currency != "TRY") throw new ArgumentException("Only TRY is supported.");
        if (dueDate < period) throw new ArgumentException("Due date cannot precede the assessed month.");
    }
}

public sealed class DuesCharge : BaseEntity
{
    private readonly List<DuesChargeParty> _parties = [];
    private DuesCharge() { }
    internal DuesCharge(DuesAssessment assessment, Guid unitId)
    {
        PropertyId = assessment.PropertyId; AssessmentId = assessment.Id; UnitId = unitId;
        Period = assessment.Period; Amount = assessment.Amount; Currency = assessment.Currency; DueDate = assessment.DueDate;
        Kind = assessment.Kind; Category = assessment.Category;
    }
    public Guid PropertyId { get; private set; }
    public Guid AssessmentId { get; private set; }
    public Guid UnitId { get; private set; }
    public DateOnly Period { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public DateOnly DueDate { get; private set; }
    public ChargeKind Kind { get; private set; }
    public ChargeCategory? Category { get; private set; }
    public bool IsCancelled { get; private set; }
    public decimal PaidAmount { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public bool PartiesRecorded { get; private set; }
    public IReadOnlyCollection<DuesChargeParty> Parties => _parties.AsReadOnly();
    public Guid? CancellationReasonId { get; private set; }
    public string? CancellationReasonCode { get; private set; }
    public string? CancellationReasonName { get; private set; }
    public Guid? CancelledByOccupancyId { get; private set; }
    public void RecordParties(IEnumerable<DuesChargeParty> parties)
    {
        if (PartiesRecorded) throw new InvalidOperationException("Recorded parties cannot be rewritten.");
        var values = parties.ToArray();
        if (values.Any(value => value.PropertyId != PropertyId || value.UnitId != UnitId || value.ChargeId != Id) ||
            values.Select(value => value.OccupancyId).Distinct().Count() != values.Length || values.Count(value => value.IsDebtor) > 1)
            throw new ArgumentException("Invalid party snapshot.");
        _parties.AddRange(values); PartiesRecorded = true;
    }
    public string Status(DateOnly today) => IsCancelled ? "CANCELLED" : Balance == 0 ? "PAID" : DueDate < today ? "OVERDUE" : PaidAmount > 0 ? "PARTIALLY_PAID" : "OPEN";
    public decimal Balance => IsCancelled ? 0m : Amount - PaidAmount;
    public void ApplyPayment(decimal amount)
    {
        Payment.ValidateAmount(amount);
        if (IsCancelled || amount > Balance) throw new InvalidOperationException("Payment exceeds the available balance.");
        PaidAmount += amount;
    }
    public void ReversePayment(decimal amount)
    {
        Payment.ValidateAmount(amount);
        if (IsCancelled || amount > PaidAmount) throw new InvalidOperationException("Invalid payment reversal.");
        PaidAmount -= amount;
    }
    public void Cancel(string reason, DateTimeOffset at, Guid? reasonId = null, string? reasonCode = null, string? reasonName = null, Guid? occupancyId = null)
    {
        if (IsCancelled) throw new InvalidOperationException("A cancelled charge cannot be rewritten.");
        if (PaidAmount > 0) throw new InvalidOperationException("Reverse allocated payments before cancelling the charge.");
        reason = Required(reason, nameof(reason), 500);
        if (at == default) throw new ArgumentException("Cancellation timestamp is required.");
        if (reasonId == Guid.Empty || occupancyId == Guid.Empty ||
            (reasonId is null ? reasonCode is not null || reasonName is not null : string.IsNullOrWhiteSpace(reasonCode) || string.IsNullOrWhiteSpace(reasonName)))
            throw new ArgumentException("Invalid cancellation reason metadata.");
        CancellationReasonId = reasonId; CancellationReasonCode = reasonCode; CancellationReasonName = reasonName; CancelledByOccupancyId = occupancyId;
        IsCancelled = true; CancellationReason = reason; CancelledAt = at;
    }
}
