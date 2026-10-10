namespace BuildingFlow.Domain;

public sealed class PropertyBudget : BaseEntity
{
    private readonly List<BudgetRevision> _revisions = [];
    private PropertyBudget() { }
    public PropertyBudget(Guid propertyId, DateOnly period, ExpenseCategory category)
    {
        if (propertyId == Guid.Empty || period.Day != 1 || period.Year < 2000 || period.Year > 9998 || !Enum.IsDefined(category))
            throw new ArgumentException("Invalid budget scope.");
        PropertyId = propertyId; Period = period; Category = category;
    }
    public Guid PropertyId { get; private set; }
    public DateOnly Period { get; private set; }
    public ExpenseCategory Category { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public int RevisionCount { get; private set; }
    public IReadOnlyCollection<BudgetRevision> Revisions => _revisions.AsReadOnly();
    public BudgetRevision Revise(Guid requestId, string fingerprint, decimal amount, string explanation)
    {
        if (requestId == Guid.Empty || _revisions.Any(value => value.RequestId == requestId)) throw new ArgumentException("Invalid revision request.");
        FinanceIdentity.Validate(fingerprint);
        if (amount < 0 || amount > 99999999.99m || decimal.Round(amount, 2) != amount) throw new ArgumentOutOfRangeException(nameof(amount));
        explanation = Required(explanation, nameof(explanation), 500);
        var revision = new BudgetRevision(this, requestId, fingerprint, amount, checked(RevisionCount + 1), explanation);
        _revisions.Add(revision); Amount = amount; RevisionCount++; return revision;
    }
}

public sealed class BudgetRevision : BaseEntity
{
    private BudgetRevision() { }
    internal BudgetRevision(PropertyBudget budget, Guid requestId, string fingerprint, decimal amount, int sequence, string explanation)
    {
        PropertyId = budget.PropertyId; BudgetId = budget.Id; RequestId = requestId; RequestFingerprint = fingerprint;
        Amount = amount; Sequence = sequence; Explanation = explanation;
    }
    public Guid PropertyId { get; private set; }
    public Guid BudgetId { get; private set; }
    public Guid RequestId { get; private set; }
    public string RequestFingerprint { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public int Sequence { get; private set; }
    public string Explanation { get; private set; } = null!;
}
