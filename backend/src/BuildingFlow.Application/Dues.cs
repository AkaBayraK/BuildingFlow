using System.Globalization;
using System.Text.RegularExpressions;
namespace BuildingFlow.Application;

public sealed record CreateAssessmentRequest(Guid RequestId, IReadOnlyList<Guid>? UnitIds, string? Period,
    string? Amount, string? Currency, DateOnly? DueDate, string? Description, string DebtorType = "UNIT");
public sealed record AssessmentCommand(Guid RequestId, IReadOnlyList<Guid> UnitIds, DateOnly Period,
    decimal Amount, string Currency, DateOnly DueDate, string Description, string DebtorType = "UNIT");
public sealed record AssessmentResponse(Guid Id, Guid PropertyId, Guid RequestId, string Period, string Amount,
    string Currency, DateOnly DueDate, string Description, int UnitCount, string TotalAmount, IReadOnlyList<Guid> UnitIds, bool Replayed);
public sealed record DuesQuery(int Page = 1, int PageSize = 10, string? Search = null, Guid? UnitId = null,
    string? Period = null, DateOnly? DueFrom = null, DateOnly? DueTo = null, string? Status = null,
    string SortBy = "dueDate", string SortDirection = "desc", Guid? PersonId = null, string? PartyRole = null);
public sealed record DuesSummary(string OutstandingAmount, string OverdueAmount, string CancelledAmount,
    int OpenCount, int OverdueCount, int CancelledCount, string Currency);
public sealed record DuesChargeResponse(Guid Id, Guid PropertyId, Guid AssessmentId, Guid UnitId,
    Guid? BuildingId, string UnitNumber, string? BuildingName, string Period, string Amount, string Balance,
    string Currency, DateOnly DueDate, string Description, string Status, long Version, DateTimeOffset CreatedAt,
    string? CancellationReason, DateTimeOffset? CancelledAt, bool PartiesRecorded = false, bool PartiesVisible = false,
    string DebtorType = "UNIT", IReadOnlyList<DuesPartyResponse>? Parties = null, string? CancellationReasonCode = null, string? CancellationReasonName = null);
public sealed record DuesPartyResponse(Guid ChargeId, Guid PersonId, Guid OccupancyId, string PersonName, string Type, DateOnly StartDate, DateOnly? EndDate, bool IsDebtor);
public sealed record DuesPersonChoice(Guid Id, string Name, Guid PropertyId);
public sealed record CancellationReasonResponse(Guid Id, string Code, string Name, bool RequiresExplanation);
public sealed record DuesPage(IReadOnlyList<DuesChargeResponse> Items, int Page, int PageSize, int TotalCount, DuesSummary Summary);
public sealed record CancelChargeRequest(string? Reason, long Version, Guid? ReasonId = null);
public interface IDuesStore
{
    Task<AssessmentResponse?> CreateAsync(Guid property, AssessmentCommand command, AccessScope access, CancellationToken token);
    Task<AssessmentResponse?> GetAssessmentAsync(Guid property, Guid assessment, AccessScope access, CancellationToken token);
    Task<DuesPage?> ListAsync(Guid property, DuesQuery query, AccessScope access, CancellationToken token);
    Task<DuesChargeResponse?> GetAsync(Guid property, Guid charge, AccessScope access, CancellationToken token);
    Task<DuesChargeResponse?> CancelAsync(Guid property, Guid charge, CancelChargeRequest request, AccessScope access, CancellationToken token);
    Task<PagedRows<UnitChoice>?> UnitChoicesAsync(Guid property, PeopleQuery query, AccessScope access, CancellationToken token);
    Task<PagedRows<DuesPersonChoice>?> PersonChoicesAsync(Guid property, PeopleQuery query, AccessScope access, CancellationToken token);
    Task<IReadOnlyList<CancellationReasonResponse>?> CancellationReasonsAsync(Guid property, AccessScope access, CancellationToken token);
}
public sealed class DuesService(IDuesStore store, ICurrentUserService user)
{
    private AccessScope Access()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) ||
            !new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT" }.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"), new[] {"SUPER_ADMIN","SITE_ADMIN","MANAGER"}.Any(user.IsInRole));
    }
    public static bool TryPeriod(string? value, out DateOnly period)
    {
        period = default;
        return value is { Length: 7 } && DateOnly.TryParseExact(value + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out period) && period.Year >= 2000;
    }
    public static string Money(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
    private static void Check(Dictionary<string, ApiErrorMessage[]> errors) { if (errors.Count > 0) throw new RequestValidationException(errors); }
    public Task<AssessmentResponse?> CreateAsync(Guid property, CreateAssessmentRequest request, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (request.DebtorType is not ("UNIT" or "TENANT" or "OWNER")) errors["debtorType"] = [ErrorCatalog.DuesErrorDebtor.Create()];
        if (request.DebtorType != "UNIT" && !access.ReadFinancialParties) throw new UnauthorizedAccessException();
        if (request.RequestId == Guid.Empty) errors["requestId"] = [ErrorCatalog.DuesErrorRequestId.Create()];
        if (request.UnitIds is null || request.UnitIds.Count is < 1 or > 500 || request.UnitIds.Contains(Guid.Empty) || request.UnitIds.Distinct().Count() != request.UnitIds.Count)
            errors["unitIds"] = [ErrorCatalog.DuesErrorUnitSelection.Create()];
        if (!TryPeriod(request.Period, out var period)) errors["period"] = [ErrorCatalog.DuesErrorPeriod.Create()];
        decimal amount = 0;
        if (request.Amount is null || request.Amount.Length > 20 || !Regex.IsMatch(request.Amount, @"^[0-9]+(?:\.[0-9]{1,2})?$") ||
            !decimal.TryParse(request.Amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) || amount <= 0 || amount > 99999999.99m)
            errors["amount"] = [ErrorCatalog.DuesErrorAmount.Create()];
        if (request.Currency != "TRY") errors["currency"] = [ErrorCatalog.DuesErrorCurrency.Create()];
        if (request.DueDate is null || request.DueDate < period) errors["dueDate"] = [ErrorCatalog.DuesErrorDue.Create()];
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 500) errors["description"] = [ErrorCatalog.DuesErrorDescription.Create()];
        Check(errors);
        return store.CreateAsync(property, new(request.RequestId, request.UnitIds!.Order().ToArray(), period, amount,
            request.Currency!, request.DueDate!.Value, request.Description!.Trim(), request.DebtorType), access, token);
    }
    public Task<DuesPage?> ListAsync(Guid property, DuesQuery query, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if ((query.PersonId is not null || query.PartyRole is not null) && !access.ReadFinancialParties) throw new UnauthorizedAccessException();
        if (query.PersonId == Guid.Empty) errors["personId"] = [ErrorCatalog.PersonInvalid.Create()];
        if (query.PartyRole is not null && query.PartyRole is not ("OWNER" or "TENANT" or "RESIDENT")) errors["partyRole"] = [ErrorCatalog.OccupancyTypeInvalid.Create()];
        if (query.Page is < 1 or > 1000000) errors["page"] = [ErrorCatalog.InvalidPage.Create()];
        if (query.PageSize is < 1 or > 100) errors["pageSize"] = [ErrorCatalog.PageSizeBounds.Create()];
        if (query.Search?.Trim().Length > 200) errors["search"] = [ErrorCatalog.ErrorsSearchBounds.Create()];
        if (query.UnitId == Guid.Empty) errors["unitId"] = [ErrorCatalog.DuesErrorUnit.Create()];
        if (!string.IsNullOrEmpty(query.Period) && !TryPeriod(query.Period, out _)) errors["period"] = [ErrorCatalog.DuesErrorPeriod.Create()];
        if (query.DueFrom > query.DueTo) errors["dueTo"] = [ErrorCatalog.DuesErrorDueRange.Create()];
        if (!string.IsNullOrEmpty(query.Status) && query.Status is not ("OPEN" or "OVERDUE" or "CANCELLED" or "PARTIALLY_PAID" or "PAID" or "OUTSTANDING")) errors["status"] = [ErrorCatalog.DuesErrorStatus.Create()];
        if (query.SortBy is not ("dueDate" or "period" or "amount" or "createdAt")) errors["sortBy"] = [ErrorCatalog.InvalidSortField.Create()];
        if (query.SortDirection is not ("asc" or "desc")) errors["sortDirection"] = [ErrorCatalog.InvalidSortDirection.Create()];
        Check(errors); return store.ListAsync(property, query, access, token);
    }
    public Task<DuesChargeResponse?> GetAsync(Guid property, Guid charge, CancellationToken token) => store.GetAsync(property, charge, Access(), token);
    public Task<AssessmentResponse?> GetAssessmentAsync(Guid property, Guid assessment, CancellationToken token) => store.GetAssessmentAsync(property, assessment, Access(), token);
    public Task<DuesChargeResponse?> CancelAsync(Guid property, Guid charge, CancelChargeRequest request, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (request.Version < 1) errors["version"] = [ErrorCatalog.VersionRequired.Create()];
        if (request.ReasonId == Guid.Empty) errors["reasonId"] = [ErrorCatalog.DuesChooseReason.Create()];
        if (request.Reason?.Trim().Length > 500 || (request.ReasonId is null && string.IsNullOrWhiteSpace(request.Reason))) errors["reason"] = [ErrorCatalog.DuesErrorReason.Create()];
        Check(errors); return store.CancelAsync(property, charge, request, access, token);
    }
    public Task<PagedRows<UnitChoice>?> UnitChoicesAsync(Guid property, PeopleQuery query, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (query.Page is < 1 or > 1000000 || query.PageSize is < 1 or > 100 || query.Search?.Trim().Length > 200)
            errors["unitIds"] = [ErrorCatalog.DuesErrorChoices.Create()];
        Check(errors); return store.UnitChoicesAsync(property, query, access, token);
    }
    public Task<PagedRows<DuesPersonChoice>?> PersonChoicesAsync(Guid property, PeopleQuery query, CancellationToken token)
    {
        var access = Access(); if (!access.ReadFinancialParties) throw new UnauthorizedAccessException();
        if (query.Page is < 1 or > 1000000 || query.PageSize is < 1 or > 100 || query.Search?.Trim().Length > 200)
            throw new RequestValidationException(new() { ["personId"] = [ErrorCatalog.DuesErrorPersonChoices.Create()] });
        return store.PersonChoicesAsync(property,query,access,token);
    }
    public Task<IReadOnlyList<CancellationReasonResponse>?> CancellationReasonsAsync(Guid property,CancellationToken token) => store.CancellationReasonsAsync(property,Access(),token);
}
