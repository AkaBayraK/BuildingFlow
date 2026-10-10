using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record PeopleQuery(int Page = 1, int PageSize = 10, string? Search = null, string SortBy = "name", string SortDirection = "asc");
public sealed record CreatePersonRequest(string? FirstName, string? LastName, string? PhoneNumber = null, string? Email = null);
public sealed record UpdatePersonRequest(string? FirstName, string? LastName, string? PhoneNumber, string? Email, bool? IsEnabled, long Version);
public sealed record PersonResponse(Guid Id, Guid PropertyId, string FirstName, string LastName, string? PhoneNumber, string? Email, bool IsEnabled, long Version, DateTimeOffset CreatedAt, int ActiveOccupancyCount);
public sealed record CreateOccupancyRequest(Guid PersonId, Guid UnitId, string Type, DateOnly? StartDate, DateOnly? EndDate = null, bool IsPrimaryContact = false);
public sealed record ChargeCancellationApproval(Guid ChargeId, long Version);
public sealed record EndOccupancyRequest(DateOnly? EndDate, long Version, IReadOnlyList<ChargeCancellationApproval>? CancellationApprovals = null);
public sealed record TenantChargeReview(Guid Id, long Version, string Period, string Amount, DateOnly DueDate);
public sealed record TenantMoveOutPreview(Guid OccupancyId, long Version, DateOnly EndDate, int EligibleCount, string TotalAmount,
    int SkippedPaymentCount, IReadOnlyList<TenantChargeReview> Items, bool ApprovalAvailable);
public sealed record TenantDebtResult(int CancelledCount, int PendingCount, int SkippedPaymentCount);
public sealed record OccupancyResponse(Guid Id, Guid PropertyId, Guid PersonId, Guid UnitId, Guid? BuildingId,
    string PersonName, string UnitNumber, string? BuildingName, string Type, DateOnly StartDate, DateOnly? EndDate,
    bool IsPrimaryContact, string Status, long Version, TenantDebtResult? TenantDebtResult = null);
public sealed record UnitChoice(Guid Id, Guid? BuildingId, string Number, string? BuildingName, bool IsEnabled);
public interface IPeopleStore
{
    Task<PagedRows<PersonResponse>?> ListAsync(Guid property, PeopleQuery query, AccessScope access, CancellationToken token);
    Task<PersonResponse?> GetAsync(Guid property, Guid person, AccessScope access, CancellationToken token);
    Task<PersonResponse?> CreateAsync(Guid property, CreatePersonRequest request, AccessScope access, CancellationToken token);
    Task<PersonResponse?> UpdateAsync(Guid property, Guid person, UpdatePersonRequest request, AccessScope access, CancellationToken token);
    Task<bool> DeleteAsync(Guid property, Guid person, long version, AccessScope access, CancellationToken token);
    Task<PagedRows<UnitChoice>?> UnitChoicesAsync(Guid property, PeopleQuery query, AccessScope access, CancellationToken token);
    Task<PagedRows<OccupancyResponse>?> OccupanciesAsync(Guid property, Guid? person, Guid? unit, PeopleQuery query, AccessScope access, CancellationToken token);
    Task<OccupancyResponse?> GetOccupancyAsync(Guid property, Guid occupancy, AccessScope access, CancellationToken token);
    Task<OccupancyResponse?> CreateOccupancyAsync(Guid property, CreateOccupancyRequest request, AccessScope access, CancellationToken token);
    Task<OccupancyResponse?> EndOccupancyAsync(Guid property, Guid occupancy, EndOccupancyRequest request, AccessScope access, CancellationToken token);
    Task<TenantMoveOutPreview?> EndPreviewAsync(Guid property, Guid occupancy, DateOnly endDate, long version, AccessScope access, CancellationToken token);
}

public sealed class PeopleService(IPeopleStore store, ICurrentUserService user)
{
    private AccessScope Access()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) ||
            !(user.IsInRole("SUPER_ADMIN") || user.IsInRole("SITE_ADMIN") || user.IsInRole("MANAGER")))
            throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    private static void Check(Dictionary<string, ApiErrorMessage[]> errors)
    { if (errors.Count > 0) throw new RequestValidationException(errors); }
    private static void ValidatePerson(string? first, string? last, string? phone, string? email)
    {
        Dictionary<string, ApiErrorMessage[]> errors = [];
        foreach (var (field, value, label) in new[] { ("firstName", first, "Ad"), ("lastName", last, "Soyad") })
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 100) errors[field] = [ErrorCatalog.FieldRequiredMax.Create(("field", ErrorCatalog.FieldKey(label)), ("max", 100))];
        if (phone?.Trim().Length > 30) errors["phoneNumber"] = [ErrorCatalog.PhoneTooLong.Create()];
        if (!string.IsNullOrWhiteSpace(email) && (email.Trim().Length > 254 || !System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var parsed) || parsed.Address != email.Trim()))
            errors["email"] = [ErrorCatalog.UiEnterAValidEmailAddress.Create()];
        Check(errors);
    }
    private static void Query(PeopleQuery query)
    {
        Dictionary<string, ApiErrorMessage[]> errors = [];
        if (query.Page is < 1 or > 1000000) errors["page"] = [ErrorCatalog.InvalidPage.Create()];
        if (query.PageSize is < 1 or > 100) errors["pageSize"] = [ErrorCatalog.PageSizeBounds.Create()];
        if (query.Search?.Trim().Length > 200) errors["search"] = [ErrorCatalog.ErrorsSearchBounds.Create()];
        if (query.SortBy is not ("name" or "createdAt")) errors["sortBy"] = [ErrorCatalog.InvalidSortField.Create()];
        if (query.SortDirection is not ("asc" or "desc")) errors["sortDirection"] = [ErrorCatalog.InvalidSortDirection.Create()];
        Check(errors);
    }
    private static void Version(long version)
    { if (version < 1) throw new RequestValidationException(new() { ["version"] = [ErrorCatalog.VersionRequired.Create()] }); }
    public Task<PagedRows<PersonResponse>?> ListAsync(Guid property, PeopleQuery query, CancellationToken token)
    { var access = Access(); Query(query); return store.ListAsync(property, query, access, token); }
    public Task<PersonResponse?> GetAsync(Guid property, Guid person, CancellationToken token) => store.GetAsync(property, person, Access(), token);
    public Task<PersonResponse?> CreateAsync(Guid property, CreatePersonRequest request, CancellationToken token)
    { var access = Access(); ValidatePerson(request.FirstName, request.LastName, request.PhoneNumber, request.Email); return store.CreateAsync(property, request, access, token); }
    public Task<PersonResponse?> UpdateAsync(Guid property, Guid person, UpdatePersonRequest request, CancellationToken token)
    {
        var access = Access(); ValidatePerson(request.FirstName, request.LastName, request.PhoneNumber, request.Email); Version(request.Version);
        if (request.IsEnabled is null) throw new RequestValidationException(new() { ["isEnabled"] = [ErrorCatalog.EnabledRequired.Create()] });
        return store.UpdateAsync(property, person, request, access, token);
    }
    public Task<bool> DeleteAsync(Guid property, Guid person, long version, CancellationToken token)
    { var access = Access(); Version(version); return store.DeleteAsync(property, person, version, access, token); }
    public Task<PagedRows<UnitChoice>?> UnitChoicesAsync(Guid property, PeopleQuery query, CancellationToken token)
    { var access = Access(); Query(query); return store.UnitChoicesAsync(property, query, access, token); }
    public Task<PagedRows<OccupancyResponse>?> OccupanciesAsync(Guid property, Guid? person, Guid? unit, PeopleQuery query, CancellationToken token)
    { var access = Access(); Query(query); return store.OccupanciesAsync(property, person, unit, query, access, token); }
    public Task<OccupancyResponse?> GetOccupancyAsync(Guid property, Guid occupancy, CancellationToken token) => store.GetOccupancyAsync(property, occupancy, Access(), token);
    public Task<OccupancyResponse?> CreateOccupancyAsync(Guid property, CreateOccupancyRequest request, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (request.PersonId == Guid.Empty) errors["personId"] = [ErrorCatalog.PersonRequired.Create()];
        if (request.UnitId == Guid.Empty) errors["unitId"] = [ErrorCatalog.UiSelectAUnit.Create()];
        if (!Enum.TryParse<OccupancyType>(request.Type, out var type) || !Enum.IsDefined(type) || request.Type != type.ToString()) errors["type"] = [ErrorCatalog.OccupancyTypeInvalid.Create()];
        if (request.StartDate is null || request.StartDate == DateOnly.MinValue) errors["startDate"] = [ErrorCatalog.UiAStartDateIsRequired.Create()];
        if (request.EndDate < request.StartDate) errors["endDate"] = [ErrorCatalog.UiTheEndDateCannotPrecedeTheStartDate.Create()];
        Check(errors); return store.CreateOccupancyAsync(property, request, access, token);
    }
    public Task<OccupancyResponse?> EndOccupancyAsync(Guid property, Guid occupancy, EndOccupancyRequest request, CancellationToken token)
    {
        var access = Access(); Version(request.Version);
        if (request.EndDate is null) throw new RequestValidationException(new() { ["endDate"] = [ErrorCatalog.EndDateRequired.Create()] });
        if (request.CancellationApprovals is { } approvals && (approvals.Count > 500 || approvals.Any(value => value is null || value.ChargeId == Guid.Empty || value.Version < 1) || approvals.Select(value => value.ChargeId).Distinct().Count() != approvals.Count))
            throw new RequestValidationException(new() { ["cancellationApprovals"] = [ErrorCatalog.DuesErrorApproval.Create()] });
        return store.EndOccupancyAsync(property, occupancy, request, access, token);
    }
    public Task<TenantMoveOutPreview?> EndPreviewAsync(Guid property,Guid occupancy,DateOnly endDate,long version,CancellationToken token)
    {
        var access=Access();Version(version);
        if (endDate == DateOnly.MinValue) throw new RequestValidationException(new() { ["endDate"] = [ErrorCatalog.EndDateRequired.Create()] });
        return store.EndPreviewAsync(property,occupancy,endDate,version,access,token);
    }
}
