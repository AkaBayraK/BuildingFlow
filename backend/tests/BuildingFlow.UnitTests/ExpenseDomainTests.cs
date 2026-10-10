using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class ExpenseDomainTests
{
    private static Expense Expense(decimal amount = 12.35m) => new(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), ExpenseCategory.ENERGY, amount, new(2026, 10, 1), "  Electricity  ", "  INV-1  ");
    [Theory] [InlineData(0)] [InlineData(-1)] [InlineData(1.001)] [InlineData(100000000)]
    public void ExpensesRejectInvalidAmounts(decimal amount) => Assert.Throws<ArgumentOutOfRangeException>(() => Expense(amount));
    [Fact] public void CancellationPreservesOriginalFinancialValuesAndCannotRepeat()
    {
        var value = Expense(); value.Cancel("  Wrong entry  ", DateTimeOffset.UtcNow);
        Assert.Equal(12.35m, value.Amount); Assert.Equal("Electricity", value.Description); Assert.Equal("INV-1", value.DocumentReference); Assert.Equal("TRY", value.Currency);
        Assert.True(value.IsCancelled); Assert.Equal("Wrong entry", value.CancellationReason);
        Assert.Throws<InvalidOperationException>(() => value.Cancel("rewrite", DateTimeOffset.UtcNow));
    }
    [Fact] public void InvalidCancellationIsAtomic()
    {
        var value = Expense(); Assert.Throws<ArgumentException>(() => value.Cancel(" ", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => value.Cancel("Reason", default)); Assert.False(value.IsCancelled); Assert.Null(value.CancellationReason);
    }
    [Fact] public void ExpenseRejectsInvalidScopeAndFields()
    {
        Assert.Throws<ArgumentException>(() => new Expense(Guid.Empty, Guid.NewGuid(), new string('a', 64), ExpenseCategory.OTHER, 1, new(2026, 10, 1), "x", null));
        Assert.Throws<ArgumentException>(() => new Expense(Guid.NewGuid(), Guid.NewGuid(), "invalid", ExpenseCategory.OTHER, 1, new(2026, 10, 1), "x", null));
        Assert.Throws<ArgumentException>(() => new Expense(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), (ExpenseCategory)99, 1, new(2026, 10, 1), "x", null));
        Assert.Throws<ArgumentException>(() => new Expense(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), ExpenseCategory.OTHER, 1, new(1999, 10, 1), "x", null));
        Assert.Throws<ArgumentException>(() => new Expense(Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), ExpenseCategory.OTHER, 1, new(2026, 10, 1), "x", new string('r', 101)));
    }
    [Fact] public void BudgetKeepsImmutableRevisionsIncludingAnExplicitZeroPlan()
    {
        var budget = new PropertyBudget(Guid.NewGuid(), new(2026, 10, 1), ExpenseCategory.ENERGY);
        var first = budget.Revise(Guid.NewGuid(), new string('a', 64), 12.35m, " First plan ");
        budget.Revise(Guid.NewGuid(), new string('b', 64), 0, "No expenditure planned");
        Assert.Equal(12.35m, first.Amount); Assert.Equal("First plan", first.Explanation); Assert.Equal(1, first.Sequence);
        Assert.Equal(0, budget.Amount); Assert.Equal(2, budget.RevisionCount); Assert.Equal(2, budget.Revisions.Count);
        Assert.All(budget.Revisions, value => { Assert.Equal(budget.Id, value.BudgetId); Assert.Equal(budget.PropertyId, value.PropertyId); });
    }
    [Theory] [InlineData(-1)] [InlineData(.001)] [InlineData(100000000)]
    public void InvalidBudgetRevisionDoesNotChangeCurrentPlan(decimal amount)
    {
        var budget = new PropertyBudget(Guid.NewGuid(), new(2026, 10, 1), ExpenseCategory.OTHER);
        budget.Revise(Guid.NewGuid(), new string('a', 64), 1, "Initial");
        Assert.Throws<ArgumentOutOfRangeException>(() => budget.Revise(Guid.NewGuid(), new string('a', 64), amount, "Invalid"));
        Assert.Equal(1, budget.Amount); Assert.Single(budget.Revisions);
    }
    [Fact] public void BudgetRequiresMonthlyPeriodAndUniqueRevisionIdentity()
    {
        Assert.Throws<ArgumentException>(() => new PropertyBudget(Guid.NewGuid(), new(2026, 10, 2), ExpenseCategory.OTHER));
        Assert.Throws<ArgumentException>(() => new PropertyBudget(Guid.NewGuid(), new(1999, 10, 1), ExpenseCategory.OTHER));
        var budget = new PropertyBudget(Guid.NewGuid(), new(2026, 10, 1), ExpenseCategory.OTHER); var id = Guid.NewGuid();
        budget.Revise(id, new string('a', 64), 1, "Initial");
        Assert.Throws<ArgumentException>(() => budget.Revise(id, new string('a', 64), 2, "Duplicate"));
    }
}
