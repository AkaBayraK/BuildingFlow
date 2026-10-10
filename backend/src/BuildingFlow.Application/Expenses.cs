using System.Globalization;
using System.Text.RegularExpressions;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record CreateExpenseRequest(Guid RequestId, string? Category, string? Amount, string? Currency, DateOnly? OccurredDate, string? Description, string? DocumentReference);
public sealed record ExpenseCommand(Guid RequestId, ExpenseCategory Category, decimal Amount, DateOnly OccurredDate, string Description, string? DocumentReference);
public sealed record ExpenseResponse(Guid Id, Guid PropertyId, Guid RequestId, string Category, string Amount, string Currency, DateOnly OccurredDate, string Description, string? DocumentReference, string Status, long Version, DateTimeOffset CreatedAt, string? CancellationReason, DateTimeOffset? CancelledAt, bool Replayed);
public sealed record ExpenseQuery(int Page = 1, int PageSize = 10, string? Search = null, string? Period = null, string? Category = null, string? Status = null, string SortBy = "occurredDate", string SortDirection = "desc");
public sealed record ExpensePage(IReadOnlyList<ExpenseResponse> Items, int Page, int PageSize, int TotalCount, PaymentSummary Summary);
public sealed record SaveBudgetRequest(Guid RequestId, string? Period, string? Category, string? Amount, string? Currency, string? Explanation, long Version);
public sealed record BudgetCommand(Guid RequestId, DateOnly Period, ExpenseCategory Category, decimal Amount, string Explanation, long Version);
public sealed record BudgetRevisionResponse(Guid Id, string Amount, int Sequence, string Explanation, DateTimeOffset CreatedAt);
public sealed record BudgetResponse(Guid Id, Guid PropertyId, string Period, string Category, string Amount, string Currency, long Version, IReadOnlyList<BudgetRevisionResponse> Revisions, bool Replayed, int AppliedRevision);
public sealed record CategoryFinanceResponse(string Category, string ExpenseAmount, string BudgetAmount, string Variance, bool HasBudget, Guid? BudgetId, long? BudgetVersion);
public sealed record FinanceSummary(Guid PropertyId, string Period, string Currency, string IncomeAmount, string ExpenseAmount, string BudgetAmount, string NetAmount, string BudgetVariance, IReadOnlyList<CategoryFinanceResponse> Categories);
public interface IExpenseStore
{
    Task<ExpenseResponse?> CreateAsync(Guid property, ExpenseCommand command, AccessScope access, CancellationToken token);
    Task<ExpenseResponse?> GetAsync(Guid property, Guid expense, AccessScope access, CancellationToken token);
    Task<ExpensePage?> ListAsync(Guid property, ExpenseQuery query, AccessScope access, CancellationToken token);
    Task<ExpenseResponse?> CancelAsync(Guid property, Guid expense, CancelChargeRequest request, AccessScope access, CancellationToken token);
    Task<BudgetResponse?> SaveBudgetAsync(Guid property, BudgetCommand command, AccessScope access, CancellationToken token);
    Task<BudgetResponse?> GetBudgetAsync(Guid property, Guid budget, AccessScope access, CancellationToken token);
    Task<FinanceSummary?> SummaryAsync(Guid property, DateOnly period, AccessScope access, CancellationToken token);
}
public sealed class ExpenseService(IExpenseStore store, ICurrentUserService user)
{
    private AccessScope Access()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT" }.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    public static bool Period(string? text, out DateOnly period) => DateOnly.TryParseExact(text + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out period) && period.Year is >= 2000 and <= 9998 && text?.Length == 7;
    private static bool Category(string? text, out ExpenseCategory category) => Enum.TryParse(text, out category) && Enum.GetNames<ExpenseCategory>().Contains(text);
    private static bool Money(string? text, bool zero, out decimal amount)
    {
        amount = 0;
        return text is { Length: <= 20 } && Regex.IsMatch(text, @"^[0-9]+(?:\.[0-9]{1,2})?$") && decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) && amount >= (zero ? 0 : .01m) && amount <= 99999999.99m;
    }
    private static void Check(Dictionary<string, ApiErrorMessage[]> errors) { if (errors.Count > 0) throw new RequestValidationException(errors); }
    public Task<ExpenseResponse?> CreateAsync(Guid property, CreateExpenseRequest request, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (request.RequestId == Guid.Empty) errors["requestId"] = [ErrorCatalog.DuesErrorRequestId.Create()];
        if (!Category(request.Category, out var category)) errors["category"] = [ErrorCatalog.ExpenseCategoryInvalid.Create()];
        if (!Money(request.Amount, false, out var amount)) errors["amount"] = [ErrorCatalog.DuesErrorAmount.Create()];
        if (request.Currency != "TRY") errors["currency"] = [ErrorCatalog.DuesErrorCurrency.Create()];
        if (request.OccurredDate is null || request.OccurredDate.Value.Year < 2000) errors["occurredDate"] = [ErrorCatalog.ExpenseDateInvalid.Create()];
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 500) errors["description"] = [ErrorCatalog.DuesErrorDescription.Create()];
        if (request.DocumentReference?.Trim().Length > 100) errors["documentReference"] = [ErrorCatalog.ExpenseReferenceInvalid.Create()];
        Check(errors);
        return store.CreateAsync(property, new(request.RequestId, category, amount, request.OccurredDate!.Value, request.Description!.Trim(), string.IsNullOrWhiteSpace(request.DocumentReference) ? null : request.DocumentReference.Trim()), access, token);
    }
    public Task<ExpensePage?> ListAsync(Guid property, ExpenseQuery query, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (query.Page is < 1 or > 1000000 || query.PageSize is < 1 or > 100) errors["page"] = [ErrorCatalog.InvalidPage.Create()];
        if (query.Search?.Trim().Length > 200) errors["search"] = [ErrorCatalog.ErrorsSearchBounds.Create()];
        if (!string.IsNullOrEmpty(query.Period) && !Period(query.Period, out _)) errors["period"] = [ErrorCatalog.DuesErrorPeriod.Create()];
        if (!string.IsNullOrEmpty(query.Category) && !Category(query.Category, out _)) errors["category"] = [ErrorCatalog.ExpenseCategoryInvalid.Create()];
        if (!string.IsNullOrEmpty(query.Status) && query.Status is not ("RECORDED" or "CANCELLED")) errors["status"] = [ErrorCatalog.PaymentsErrorStatus.Create()];
        if (query.SortBy is not ("occurredDate" or "amount" or "createdAt") || query.SortDirection is not ("asc" or "desc")) errors["sortBy"] = [ErrorCatalog.InvalidSortField.Create()];
        Check(errors); return store.ListAsync(property, query, access, token);
    }
    public Task<ExpenseResponse?> GetAsync(Guid property, Guid expense, CancellationToken token) => store.GetAsync(property, expense, Access(), token);
    public Task<ExpenseResponse?> CancelAsync(Guid property, Guid expense, CancelChargeRequest request, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (request.Version < 1) errors["version"] = [ErrorCatalog.VersionRequired.Create()];
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500) errors["reason"] = [ErrorCatalog.DuesErrorReason.Create()];
        Check(errors); return store.CancelAsync(property, expense, request, access, token);
    }
    public Task<BudgetResponse?> SaveBudgetAsync(Guid property, SaveBudgetRequest request, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (request.RequestId == Guid.Empty) errors["requestId"] = [ErrorCatalog.DuesErrorRequestId.Create()];
        if (!Period(request.Period, out var period)) errors["period"] = [ErrorCatalog.DuesErrorPeriod.Create()];
        if (!Category(request.Category, out var category)) errors["category"] = [ErrorCatalog.ExpenseCategoryInvalid.Create()];
        if (!Money(request.Amount, true, out var amount)) errors["amount"] = [ErrorCatalog.BudgetAmountInvalid.Create()];
        if (request.Currency != "TRY") errors["currency"] = [ErrorCatalog.DuesErrorCurrency.Create()];
        if (request.Version < 0) errors["version"] = [ErrorCatalog.VersionRequired.Create()];
        if (string.IsNullOrWhiteSpace(request.Explanation) || request.Explanation.Trim().Length > 500) errors["explanation"] = [ErrorCatalog.DuesErrorDescription.Create()];
        Check(errors); return store.SaveBudgetAsync(property, new(request.RequestId, period, category, amount, request.Explanation!.Trim(), request.Version), access, token);
    }
    public Task<BudgetResponse?> GetBudgetAsync(Guid property, Guid budget, CancellationToken token) => store.GetBudgetAsync(property, budget, Access(), token);
    public Task<FinanceSummary?> SummaryAsync(Guid property, string period, CancellationToken token)
    {
        var access = Access(); if (!Period(period, out var parsed)) throw new RequestValidationException(new() { ["period"] = [ErrorCatalog.DuesErrorPeriod.Create()] });
        return store.SummaryAsync(property, parsed, access, token);
    }
}
