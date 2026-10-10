using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;

public sealed class ExpenseStore(BuildingFlowDbContext db, TimeProvider clock) : IExpenseStore
{
    private IQueryable<Property> Properties(AccessScope access)
    {
        var parents = db.Properties.Where(value => !value.IsDeleted);
        return access.ReadAll ? parents : parents.Where(value => db.PropertyMemberships.Any(member => !member.IsDeleted && member.IsEnabled && member.PropertyId == value.Id && member.UserId == access.UserId));
    }
    private IQueryable<Expense> Expenses(Guid property, AccessScope access) => db.Expenses.AsNoTracking().Where(value => value.PropertyId == property && Properties(access).Any(parent => parent.Id == property));
    private async Task<Property?> Lock(Guid property, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        return await Properties(access).SingleOrDefaultAsync(value => value.Id == property, token);
    }
    private static void Active(Property parent) { if (!parent.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.PaymentsInactiveProperty.Create()); }
    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private void Touch(Property parent) => db.Entry(parent).Property(value => value.IsEnabled).IsModified = true;
    private async Task Save(CancellationToken token)
    {
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create()); }
    }
    private static ExpenseResponse Response(Expense value, bool replayed = false) => new(value.Id, value.PropertyId, value.RequestId, value.Category.ToString(), DuesService.Money(value.Amount), value.Currency, value.OccurredDate, value.Description, value.DocumentReference, value.IsCancelled ? "CANCELLED" : "RECORDED", value.Version, value.CreatedAt, value.CancellationReason, value.CancelledAt, replayed);
    public async Task<ExpenseResponse?> CreateAsync(Guid property, ExpenseCommand command, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var fingerprint = Hash(new { category = command.Category.ToString(), amount = DuesService.Money(command.Amount), date = command.OccurredDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), command.Description, command.DocumentReference });
        var previous = await db.Expenses.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property && value.RequestId == command.RequestId, token);
        if (previous is not null)
        {
            if (previous.RequestFingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.ExpenseRequestConflict.Create());
            await transaction.CommitAsync(token); return Response(previous, true);
        }
        Active(parent);
        if (command.OccurredDate > BusinessDate.Today(clock)) throw new RequestValidationException(new() { ["occurredDate"] = [ErrorCatalog.ExpenseDateInvalid.Create()] });
        var expense = new Expense(property, command.RequestId, fingerprint, command.Category, command.Amount, command.OccurredDate, command.Description, command.DocumentReference);
        db.Expenses.Add(expense); Touch(parent); await Save(token); await transaction.CommitAsync(token); return Response(expense);
    }
    public Task<ExpenseResponse?> GetAsync(Guid property, Guid expense, AccessScope access, CancellationToken token) => GetExpense(property, expense, access, token);
    private async Task<ExpenseResponse?> GetExpense(Guid property, Guid expense, AccessScope access, CancellationToken token)
    {
        var value = await Expenses(property, access).SingleOrDefaultAsync(value => value.Id == expense, token); return value is null ? null : Response(value);
    }
    public async Task<ExpensePage?> ListAsync(Guid property, ExpenseQuery query, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        var rows = Expenses(property, access);
        if (!string.IsNullOrEmpty(query.Period)) { ExpenseService.Period(query.Period, out var start); var end = start.AddMonths(1); rows = rows.Where(value => value.OccurredDate >= start && value.OccurredDate < end); }
        if (!string.IsNullOrEmpty(query.Category)) { var category = Enum.Parse<ExpenseCategory>(query.Category); rows = rows.Where(value => value.Category == category); }
        if (query.Status == "RECORDED") rows = rows.Where(value => !value.IsCancelled);
        if (query.Status == "CANCELLED") rows = rows.Where(value => value.IsCancelled);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = "%" + query.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            rows = rows.Where(value => EF.Functions.ILike(value.Description, pattern, "\\") || (value.DocumentReference != null && EF.Functions.ILike(value.DocumentReference, pattern, "\\")));
        }
        var total = await rows.CountAsync(token);
        var groups = await rows.GroupBy(value => value.IsCancelled).Select(group => new { Cancelled = group.Key, Count = group.Count(), Amount = group.Sum(value => value.Amount) }).ToListAsync(token);
        var recorded = groups.SingleOrDefault(value => !value.Cancelled); var cancelled = groups.SingleOrDefault(value => value.Cancelled);
        var descending = query.SortDirection == "desc";
        var ordered = query.SortBy switch {
            "amount" => descending ? rows.OrderByDescending(value => value.Amount) : rows.OrderBy(value => value.Amount),
            "createdAt" => descending ? rows.OrderByDescending(value => value.CreatedAt) : rows.OrderBy(value => value.CreatedAt),
            _ => descending ? rows.OrderByDescending(value => value.OccurredDate) : rows.OrderBy(value => value.OccurredDate)
        };
        var values = await ordered.ThenBy(value => value.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        await transaction.CommitAsync(token);
        return new(values.Select(value => Response(value)).ToArray(), query.Page, query.PageSize, total, new(DuesService.Money(recorded?.Amount ?? 0), DuesService.Money(cancelled?.Amount ?? 0), recorded?.Count ?? 0, cancelled?.Count ?? 0, "TRY"));
    }
    public async Task<ExpenseResponse?> CancelAsync(Guid property, Guid expense, CancelChargeRequest request, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null; Active(parent);
        var value = await db.Expenses.SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == expense, token); if (value is null) return null;
        if (value.Version != request.Version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        if (value.IsCancelled) throw new HierarchyConflictException(ErrorCatalog.ExpenseAlreadyCancelled.Create());
        value.Cancel(request.Reason!, clock.GetUtcNow()); Touch(parent); await Save(token); await transaction.CommitAsync(token); return Response(value);
    }
    private static BudgetResponse BudgetResponse(PropertyBudget value, bool replayed = false, int? applied = null) => new(value.Id, value.PropertyId, value.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture), value.Category.ToString(), DuesService.Money(value.Amount), value.Currency, value.Version, value.Revisions.OrderByDescending(revision => revision.Sequence).Select(revision => new BudgetRevisionResponse(revision.Id, DuesService.Money(revision.Amount), revision.Sequence, revision.Explanation, revision.CreatedAt)).ToArray(), replayed, applied ?? value.RevisionCount);
    public async Task<BudgetResponse?> SaveBudgetAsync(Guid property, BudgetCommand command, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, access, token); if (parent is null) return null;
        var fingerprint = Hash(new { date = command.Period.ToString("yyyy-MM", CultureInfo.InvariantCulture), category = command.Category.ToString(), amount = DuesService.Money(command.Amount), command.Explanation, command.Version });
        var previous = await db.BudgetRevisions.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property && value.RequestId == command.RequestId, token);
        if (previous is not null)
        {
            if (previous.RequestFingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.BudgetRequestConflict.Create());
            var replay = await db.PropertyBudgets.AsNoTracking().Include(value => value.Revisions).SingleAsync(value => value.PropertyId == property && value.Id == previous.BudgetId, token);
            await transaction.CommitAsync(token); return BudgetResponse(replay, true, previous.Sequence);
        }
        Active(parent);
        var budget = await db.PropertyBudgets.Include(value => value.Revisions).SingleOrDefaultAsync(value => value.PropertyId == property && value.Period == command.Period && value.Category == command.Category, token);
        if ((budget?.Version ?? 0) != command.Version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        if (budget is null) { budget = new PropertyBudget(property, command.Period, command.Category); db.PropertyBudgets.Add(budget); }
        budget.Revise(command.RequestId, fingerprint, command.Amount, command.Explanation); Touch(parent); await Save(token); await transaction.CommitAsync(token); return BudgetResponse(budget);
    }
    public async Task<BudgetResponse?> GetBudgetAsync(Guid property, Guid budget, AccessScope access, CancellationToken token)
    {
        var value = await db.PropertyBudgets.AsNoTracking().Include(value => value.Revisions).SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == budget && Properties(access).Any(parent => parent.Id == property), token);
        return value is null ? null : BudgetResponse(value);
    }
    public async Task<FinanceSummary?> SummaryAsync(Guid property, DateOnly period, AccessScope access, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        var end = period.AddMonths(1);
        var income = await db.Payments.AsNoTracking().Where(value => value.PropertyId == property && !value.IsCancelled && value.PaidDate >= period && value.PaidDate < end).SumAsync(value => (decimal?)value.Amount, token) ?? 0;
        var expenses = await Expenses(property, access).Where(value => !value.IsCancelled && value.OccurredDate >= period && value.OccurredDate < end).GroupBy(value => value.Category).Select(group => new { Category = group.Key, Amount = group.Sum(value => value.Amount) }).ToDictionaryAsync(value => value.Category, value => value.Amount, token);
        var budgets = await db.PropertyBudgets.AsNoTracking().Where(value => value.PropertyId == property && value.Period == period).ToDictionaryAsync(value => value.Category, token);
        var categories = Enum.GetValues<ExpenseCategory>().Select(category => {
            var actual = expenses.GetValueOrDefault(category); var budget = budgets.GetValueOrDefault(category);
            return new CategoryFinanceResponse(category.ToString(), DuesService.Money(actual), DuesService.Money(budget?.Amount ?? 0), DuesService.Money((budget?.Amount ?? 0) - actual), budget is not null, budget?.Id, budget?.Version);
        }).ToArray();
        var expenseTotal = expenses.Values.Sum(); var budgetTotal = budgets.Values.Sum(value => value.Amount);
        await transaction.CommitAsync(token);
        return new(property, period.ToString("yyyy-MM", CultureInfo.InvariantCulture), "TRY", DuesService.Money(income), DuesService.Money(expenseTotal), DuesService.Money(budgetTotal), DuesService.Money(income - expenseTotal), DuesService.Money(budgetTotal - expenseTotal), categories);
    }
}
