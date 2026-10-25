namespace BuildingFlow.Domain;

public enum LatePenaltyMethod { DAILY_30, DAILY_ACTUAL_MONTH }
public enum LatePenaltyLegalBasis { KMK_20, MANAGEMENT_PLAN, GENERAL_ASSEMBLY, COURT_DECISION }
public enum LatePenaltyPolicyStatus { DRAFT, APPROVED, CANCELLED }
public enum LatePenaltyAccrualStatus { ACTIVE, WAIVED, SUPERSEDED }
public enum AllocationKind { PRINCIPAL, PENALTY }

public static class LatePenaltyRules
{
    public const decimal StatutoryMonthlyRate = 5m;
    public const string StatutoryReference = "634 sayılı Kat Mülkiyeti Kanunu m.20";
    // User decision K1 (23.10.2026): monthly rate × days / 30.
    public const LatePenaltyMethod DefaultMethod = LatePenaltyMethod.DAILY_30;
    // User decision 23.10.2026: only the statutory method applies; any other rate, method or basis is evaluated separately.
    public static void ValidateRate(decimal rate)
    {
        if (rate != StatutoryMonthlyRate) throw new ArgumentOutOfRangeException(nameof(rate));
    }
    public static bool IsStatutory(decimal rate, LatePenaltyLegalBasis basis) => rate == StatutoryMonthlyRate && basis == LatePenaltyLegalBasis.KMK_20;
}

public sealed class LatePenaltySwitch : BaseEntity
{
    private LatePenaltySwitch() { }
    public LatePenaltySwitch(Guid propertyId, Guid requestId, DateOnly effectiveFrom, bool apply, string reason)
    {
        if (propertyId == Guid.Empty || requestId == Guid.Empty || effectiveFrom.Year < 2000) throw new ArgumentException("Invalid switch scope.");
        PropertyId = propertyId; RequestId = requestId; EffectiveFrom = effectiveFrom; Apply = apply;
        Reason = Required(reason, nameof(reason), 500);
    }
    public Guid PropertyId { get; private set; }
    public Guid RequestId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public bool Apply { get; private set; }
    public string Reason { get; private set; } = null!;
}

public sealed class LatePenaltyPolicy : BaseEntity
{
    private LatePenaltyPolicy() { }
    public LatePenaltyPolicy(Guid propertyId, Guid requestId, DateOnly validFrom, DateOnly? validTo, decimal monthlyRate,
        LatePenaltyMethod method, LatePenaltyLegalBasis legalBasis, string legalReference, Guid? approvalDocumentId)
    {
        if (propertyId == Guid.Empty || requestId == Guid.Empty) throw new ArgumentException("Valid identities are required.");
        PropertyId = propertyId; RequestId = requestId;
        Set(validFrom, validTo, monthlyRate, method, legalBasis, legalReference, approvalDocumentId);
    }
    public Guid PropertyId { get; private set; }
    public Guid RequestId { get; private set; }
    public int Year { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public decimal MonthlyRate { get; private set; }
    public LatePenaltyMethod Method { get; private set; }
    public LatePenaltyLegalBasis LegalBasis { get; private set; }
    public string LegalReference { get; private set; } = null!;
    public Guid? ApprovalDocumentId { get; private set; }
    private const LatePenaltyMethod DefaultMethodOnly = LatePenaltyRules.DefaultMethod;
    public LatePenaltyPolicyStatus Status { get; private set; } = LatePenaltyPolicyStatus.DRAFT;
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? StatusReason { get; private set; }
    public bool Covers(DateOnly day) => Status == LatePenaltyPolicyStatus.APPROVED && ValidFrom <= day && (ValidTo is null || ValidTo >= day);
    public bool Overlaps(LatePenaltyPolicy other) =>
        ValidFrom <= (other.ValidTo ?? DateOnly.MaxValue) && other.ValidFrom <= (ValidTo ?? DateOnly.MaxValue);
    public void UpdateDraft(DateOnly validFrom, DateOnly? validTo, decimal monthlyRate, LatePenaltyMethod method,
        LatePenaltyLegalBasis legalBasis, string legalReference, Guid? approvalDocumentId)
    {
        if (Status != LatePenaltyPolicyStatus.DRAFT) throw new InvalidOperationException("Only drafts can be edited.");
        Set(validFrom, validTo, monthlyRate, method, legalBasis, legalReference, approvalDocumentId);
    }
    public void Approve(string approver, DateTimeOffset at)
    {
        if (Status != LatePenaltyPolicyStatus.DRAFT) throw new InvalidOperationException("Only drafts can be approved.");
        approver = Required(approver, nameof(approver), 200);
        if (!LatePenaltyRules.IsStatutory(MonthlyRate, LegalBasis) &&
            (LegalBasis == LatePenaltyLegalBasis.KMK_20 || ApprovalDocumentId is null || approver == CreatedBy))
            throw new InvalidOperationException("A non-statutory rate needs its own legal basis, a document and a second approver.");
        Status = LatePenaltyPolicyStatus.APPROVED; ApprovedBy = approver; ApprovedAt = at;
    }
    public void Cancel(string reason)
    {
        if (Status != LatePenaltyPolicyStatus.DRAFT) throw new InvalidOperationException("Only drafts can be cancelled.");
        Status = LatePenaltyPolicyStatus.CANCELLED; StatusReason = Required(reason, nameof(reason), 500);
    }
    public void EndOn(DateOnly validTo, string reason)
    {
        if (Status != LatePenaltyPolicyStatus.APPROVED || validTo < ValidFrom || (ValidTo is not null && validTo >= ValidTo))
            throw new InvalidOperationException("An approved period can only be shortened.");
        ValidTo = validTo; StatusReason = Required(reason, nameof(reason), 500);
    }
    private void Set(DateOnly validFrom, DateOnly? validTo, decimal monthlyRate, LatePenaltyMethod method,
        LatePenaltyLegalBasis legalBasis, string legalReference, Guid? approvalDocumentId)
    {
        if (validFrom.Year < 2000 || validTo < validFrom || method != DefaultMethodOnly || legalBasis != LatePenaltyLegalBasis.KMK_20 || approvalDocumentId == Guid.Empty)
            throw new ArgumentException("Invalid policy values.");
        LatePenaltyRules.ValidateRate(monthlyRate);
        ValidFrom = validFrom; ValidTo = validTo; Year = validFrom.Year; MonthlyRate = monthlyRate; Method = method;
        LegalBasis = legalBasis; LegalReference = Required(legalReference, nameof(legalReference), 500); ApprovalDocumentId = approvalDocumentId;
    }
}

public sealed class LatePenaltyAccrual : BaseEntity
{
    private readonly List<LatePenaltySegment> _segments = [];
    private LatePenaltyAccrual() { }
    public LatePenaltyAccrual(DuesCharge charge, IReadOnlyList<PenaltySegmentResult> segments)
    {
        if (segments.Count == 0 || charge.IsCancelled) throw new ArgumentException("Segments are required.");
        PropertyId = charge.PropertyId; UnitId = charge.UnitId; ChargeId = charge.Id;
        FromDate = segments[0].From; ToDate = segments[^1].To;
        Amount = LatePenaltyCalculator.Round(segments.Sum(value => value.Amount));
        if (Amount <= 0 || FromDate <= charge.DueDate) throw new ArgumentException("Invalid accrual.");
        _segments.AddRange(segments.Select(value => new LatePenaltySegment(this, value)));
    }
    public Guid PropertyId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid ChargeId { get; private set; }
    public DateOnly FromDate { get; private set; }
    public DateOnly ToDate { get; private set; }
    public decimal Amount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public LatePenaltyAccrualStatus Status { get; private set; } = LatePenaltyAccrualStatus.ACTIVE;
    public string? StatusReason { get; private set; }
    public DateTimeOffset? StatusChangedAt { get; private set; }
    public IReadOnlyCollection<LatePenaltySegment> Segments => _segments.AsReadOnly();
    public decimal Balance => Status == LatePenaltyAccrualStatus.ACTIVE ? Amount - PaidAmount : 0m;
    public void ApplyPayment(decimal amount)
    {
        Payment.ValidateAmount(amount);
        if (amount > Balance) throw new InvalidOperationException("Payment exceeds the penalty balance.");
        PaidAmount += amount;
    }
    public void ReversePayment(decimal amount)
    {
        Payment.ValidateAmount(amount);
        if (amount > PaidAmount) throw new InvalidOperationException("Invalid penalty reversal.");
        PaidAmount -= amount;
    }
    public void Waive(string reason, DateTimeOffset at) => Close(LatePenaltyAccrualStatus.WAIVED, reason, at);
    public void Supersede(string reason, DateTimeOffset at) => Close(LatePenaltyAccrualStatus.SUPERSEDED, reason, at);
    private void Close(LatePenaltyAccrualStatus status, string reason, DateTimeOffset at)
    {
        if (Status != LatePenaltyAccrualStatus.ACTIVE || PaidAmount > 0) throw new InvalidOperationException("Only unpaid active accruals can be closed.");
        if (at == default) throw new ArgumentException("Timestamp is required.");
        Status = status; StatusReason = Required(reason, nameof(reason), 500); StatusChangedAt = at;
    }
}

public sealed class LatePenaltySegment : BaseEntity
{
    private LatePenaltySegment() { }
    internal LatePenaltySegment(LatePenaltyAccrual accrual, PenaltySegmentResult value)
    {
        PropertyId = accrual.PropertyId; AccrualId = accrual.Id; FromDate = value.From; ToDate = value.To; Days = value.Days;
        Principal = value.Principal; MonthlyRate = value.MonthlyRate; Method = value.Method; PolicyId = value.PolicyId; Amount = value.Amount;
    }
    public Guid PropertyId { get; private set; }
    public Guid AccrualId { get; private set; }
    public DateOnly FromDate { get; private set; }
    public DateOnly ToDate { get; private set; }
    public int Days { get; private set; }
    public decimal Principal { get; private set; }
    public decimal MonthlyRate { get; private set; }
    public LatePenaltyMethod Method { get; private set; }
    public Guid? PolicyId { get; private set; }
    public decimal Amount { get; private set; }
}

public sealed record PenaltyPrincipalEvent(DateOnly PaidDate, decimal Amount);
public sealed record PenaltyRatePeriod(DateOnly From, DateOnly? To, decimal MonthlyRate, LatePenaltyMethod Method, Guid? PolicyId);
public sealed record PenaltySwitchPeriod(DateOnly EffectiveFrom, bool Apply);
public sealed record PenaltySegmentResult(DateOnly From, DateOnly To, int Days, decimal Principal, decimal MonthlyRate,
    LatePenaltyMethod Method, Guid? PolicyId, decimal Amount);

// Simple (non-compounding) penalty on the remaining principal only. A payment dated D
// covers day D; the reduced principal applies from D+1. Days with the switch off yield nothing.
public static class LatePenaltyCalculator
{
    public static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    public static IReadOnlyList<PenaltySegmentResult> Calculate(decimal amount, DateOnly dueDate, DateOnly from, DateOnly through,
        IReadOnlyCollection<PenaltyPrincipalEvent> payments, IReadOnlyCollection<PenaltySwitchPeriod> switches, IReadOnlyCollection<PenaltyRatePeriod> policies)
    {
        var start = from > dueDate ? from : dueDate.AddDays(1);
        var orderedSwitches = switches.OrderBy(value => value.EffectiveFrom).ToArray();
        var segments = new List<PenaltySegmentResult>();
        for (var day = start; day <= through; day = day.AddDays(1))
        {
            var principal = amount - payments.Where(value => value.PaidDate < day).Sum(value => value.Amount);
            if (principal <= 0) break;
            if (!(orderedSwitches.LastOrDefault(value => value.EffectiveFrom <= day)?.Apply ?? false)) continue;
            var policy = policies.FirstOrDefault(value => value.From <= day && (value.To is null || value.To >= day));
            var rate = policy?.MonthlyRate ?? LatePenaltyRules.StatutoryMonthlyRate;
            var method = policy?.Method ?? LatePenaltyRules.DefaultMethod;
            var last = segments.Count == 0 ? null : segments[^1];
            if (last is not null && last.To.AddDays(1) == day && last.Principal == principal && last.MonthlyRate == rate &&
                last.Method == method && last.PolicyId == policy?.PolicyId && (method == LatePenaltyMethod.DAILY_30 || last.To.Month == day.Month))
                segments[^1] = last with { To = day, Days = last.Days + 1 };
            else segments.Add(new(day, day, 1, principal, rate, method, policy?.PolicyId, 0m));
        }
        return segments.Select(value => value with { Amount = SegmentAmount(value) }).ToArray();
    }
    private static decimal SegmentAmount(PenaltySegmentResult value)
    {
        var divisor = value.Method == LatePenaltyMethod.DAILY_30 ? 30m : DateTime.DaysInMonth(value.From.Year, value.From.Month);
        return decimal.Round(value.Principal * value.MonthlyRate * value.Days / (100m * divisor), 6, MidpointRounding.AwayFromZero);
    }
}
