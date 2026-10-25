namespace BuildingFlow.Domain;

public enum PaymentMethod { CASH, BANK_TRANSFER, ONLINE_CARD }

public sealed class Payment : BaseEntity
{
    private readonly List<PaymentAllocation> _allocations = [];
    private Payment() { }
    public Payment(Guid propertyId, Guid unitId, Guid requestId, string fingerprint, decimal amount,
        string currency, DateOnly paidDate, PaymentMethod method, string? reference, string description)
    {
        if (propertyId == Guid.Empty || unitId == Guid.Empty || requestId == Guid.Empty) throw new ArgumentException("Valid identities are required.");
        if (string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Length != 64 || fingerprint.Any(value => !Uri.IsHexDigit(value))) throw new ArgumentException("Invalid fingerprint.");
        ValidateAmount(amount);
        if (currency != "TRY" || !Enum.IsDefined(method) || paidDate.Year < 2000) throw new ArgumentException("Invalid payment values.");
        reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        if (reference?.Length > 100 || (method != PaymentMethod.CASH && reference is null)) throw new ArgumentException("A reference is required.");
        PropertyId = propertyId; UnitId = unitId; RequestId = requestId; RequestFingerprint = fingerprint;
        Amount = amount; Currency = currency; PaidDate = paidDate; Method = method;
        Reference = method == PaymentMethod.BANK_TRANSFER ? reference!.ToUpperInvariant() : reference;
        Description = Required(description, nameof(description), 500);
    }
    public Guid PropertyId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestFingerprint { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public DateOnly PaidDate { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? Reference { get; private set; }
    public string Description { get; private set; } = null!;
    public Guid? BankAccountId { get; private set; }
    public bool IsCancelled { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public IReadOnlyCollection<PaymentAllocation> Allocations => _allocations.AsReadOnly();
    public static void ValidateAmount(decimal amount)
    {
        if (amount <= 0 || amount > 99999999.99m || decimal.Round(amount, 2) != amount) throw new ArgumentOutOfRangeException(nameof(amount));
    }
    public void ReceivedInto(PaymentBankAccount account)
    {
        if (Method != PaymentMethod.BANK_TRANSFER || account.PropertyId != PropertyId || !account.UsableOn(PaidDate))
            throw new InvalidOperationException("The bank account cannot receive this payment.");
        BankAccountId = account.Id;
    }
    public PaymentAllocation Allocate(DuesCharge charge, decimal amount)
    {
        ValidateAmount(amount);
        if (IsCancelled || charge.PropertyId != PropertyId || charge.UnitId != UnitId ||
            _allocations.Any(value => value.ChargeId == charge.Id && value.Kind == AllocationKind.PRINCIPAL)) throw new InvalidOperationException("Invalid allocation scope.");
        EnsureCapacity(amount);
        charge.ApplyPayment(amount);
        var allocation = new PaymentAllocation(this, charge.Id, amount); _allocations.Add(allocation); return allocation;
    }
    public IReadOnlyList<PaymentAllocation> AllocatePenalty(DuesCharge charge, IEnumerable<LatePenaltyAccrual> accruals, decimal amount)
    {
        ValidateAmount(amount);
        if (IsCancelled || charge.PropertyId != PropertyId || charge.UnitId != UnitId ||
            _allocations.Any(value => value.ChargeId == charge.Id && value.Kind == AllocationKind.PENALTY)) throw new InvalidOperationException("Invalid allocation scope.");
        EnsureCapacity(amount);
        var open = accruals.Where(value => value.ChargeId == charge.Id && value.Balance > 0).OrderBy(value => value.FromDate).ToArray();
        if (open.Sum(value => value.Balance) < amount) throw new InvalidOperationException("Payment exceeds the penalty balance.");
        List<PaymentAllocation> created = [];
        var remaining = amount;
        foreach (var accrual in open)
        {
            var part = Math.Min(remaining, accrual.Balance);
            accrual.ApplyPayment(part);
            created.Add(new PaymentAllocation(this, charge.Id, part, accrual.Id)); remaining -= part;
            if (remaining == 0) break;
        }
        if (_allocations.Count + created.Count > 100) throw new InvalidOperationException("Allocation exceeds payment.");
        _allocations.AddRange(created); return created;
    }
    private void EnsureCapacity(decimal amount)
    {
        if (_allocations.Count >= 100 || _allocations.Sum(value => value.Amount) + amount > Amount) throw new InvalidOperationException("Allocation exceeds payment.");
    }
    public void ValidateComplete()
    {
        if (_allocations.Count == 0 || _allocations.Sum(value => value.Amount) != Amount) throw new InvalidOperationException("The full payment must be allocated.");
    }
    public void Cancel(string reason, DateTimeOffset at)
    {
        if (IsCancelled) throw new InvalidOperationException("A cancelled payment cannot be rewritten.");
        reason = Required(reason, nameof(reason), 500);
        if (at == default) throw new ArgumentException("Cancellation timestamp is required.");
        IsCancelled = true; CancellationReason = reason; CancelledAt = at;
    }
}

public sealed class PaymentAllocation : BaseEntity
{
    private PaymentAllocation() { }
    internal PaymentAllocation(Payment payment, Guid charge, decimal amount, Guid? accrualId = null)
    {
        PropertyId = payment.PropertyId; UnitId = payment.UnitId; PaymentId = payment.Id; ChargeId = charge; Amount = amount;
        Kind = accrualId is null ? AllocationKind.PRINCIPAL : AllocationKind.PENALTY; AccrualId = accrualId;
    }
    public Guid PropertyId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid PaymentId { get; private set; }
    public Guid ChargeId { get; private set; }
    public decimal Amount { get; private set; }
    public AllocationKind Kind { get; private set; }
    public Guid? AccrualId { get; private set; }
}
