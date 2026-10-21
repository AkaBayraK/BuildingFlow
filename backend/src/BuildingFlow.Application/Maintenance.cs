using BuildingFlow.Domain;
namespace BuildingFlow.Application;

// Planlama & Bakımlar: scoped CRUD for planned work and the upcoming list that feeds the dashboard card.
public sealed record MaintenancePlanRequest(string? Title, string? Description, string? Type, DateOnly? PlannedDate, string? Location, string? Responsible, string? Status, long Version = 0);
public sealed record MaintenancePlanCommand(string Title, string? Description, MaintenanceType Type, DateOnly PlannedDate, string? Location, string? Responsible, MaintenanceStatus Status, long Version);
public sealed record MaintenancePlanResponse(Guid Id, Guid PropertyId, string Title, string? Description, string Type, DateOnly PlannedDate, string? Location, string? Responsible, string Status, long Version, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);
public sealed record MaintenanceTypeCount(string Type, int Count);
// TypeCounts ignore only the type filter, so the chips show how many rows each type would give.
public sealed record MaintenancePlanPage(IReadOnlyList<MaintenancePlanResponse> Items, int Page, int PageSize, int TotalCount, IReadOnlyList<MaintenanceTypeCount> TypeCounts);
public sealed record MaintenanceQuery(int Page = 1, int PageSize = 10, string? Search = null, string? Type = null, string? Status = null, DateOnly? From = null, DateOnly? To = null);
// Open (planned or in progress) plans from today on, nearest first; NextDays counts them within UpcomingDays.
public sealed record MaintenanceUpcoming(DateOnly Today, IReadOnlyList<MaintenancePlanResponse> Items, int NextDays);
public interface IMaintenanceStore
{
    Task<MaintenancePlanPage?> List(Guid property, MaintenanceQuery query, AccessScope access, CancellationToken token);
    Task<MaintenancePlanResponse?> Get(Guid property, Guid id, AccessScope access, CancellationToken token);
    Task<MaintenanceUpcoming?> Upcoming(Guid property, DateOnly today, int limit, AccessScope access, CancellationToken token);
    Task<MaintenancePlanResponse?> Save(Guid property, Guid? id, MaintenancePlanCommand command, AccessScope access, CancellationToken token);
    Task<bool> Delete(Guid property, Guid id, long version, AccessScope access, CancellationToken token);
}
public sealed class MaintenanceService(IMaintenanceStore store, ICurrentUserService user, TimeProvider clock)
{
    public const int UpcomingDays = 30, UpcomingLimit = 4, MaxUpcoming = 10;
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    private AccessScope Access(bool write = false)
    {
        var roles = write ? new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" } : new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT" };
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !roles.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    private static void Invalid(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.PlanInvalid.Create()] });
    private static bool Name<T>(string? value) where T : struct, Enum => value is not null && Enum.GetNames<T>().Contains(value);
    public DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Zone).DateTime);
    public Task<MaintenancePlanPage?> List(Guid property, MaintenanceQuery query, CancellationToken token)
    {
        var access = Access();
        if (query.Page is < 1 or > 1000000 || query.PageSize is < 1 or > 100) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] });
        if (query.Search?.Length > 200) throw new RequestValidationException(new() { ["search"] = [ErrorCatalog.ErrorsSearchBounds.Create()] });
        if (!string.IsNullOrEmpty(query.Type) && !Name<MaintenanceType>(query.Type)) Invalid("type");
        if (!string.IsNullOrEmpty(query.Status) && !Name<MaintenanceStatus>(query.Status)) Invalid("status");
        if (query.From > query.To) Invalid("from");
        return store.List(property, query, access, token);
    }
    public Task<MaintenancePlanResponse?> Get(Guid property, Guid id, CancellationToken token) => store.Get(property, id, Access(), token);
    public Task<MaintenanceUpcoming?> Upcoming(Guid property, int limit, CancellationToken token)
    {
        var access = Access(); if (limit is < 1 or > MaxUpcoming) Invalid("limit");
        return store.Upcoming(property, Today(), limit, access, token);
    }
    public Task<MaintenancePlanResponse?> Save(Guid property, Guid? id, MaintenancePlanRequest request, CancellationToken token)
    {
        var access = Access(true);
        if (id.HasValue ? request.Version < 1 : request.Version != 0) Invalid("version");
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > MaintenancePlan.TitleLength) Invalid("title");
        if (request.Description?.Trim().Length > MaintenancePlan.DescriptionLength) Invalid("description");
        if (request.Location?.Trim().Length > MaintenancePlan.TextLength) Invalid("location");
        if (request.Responsible?.Trim().Length > MaintenancePlan.TextLength) Invalid("responsible");
        if (!Name<MaintenanceType>(request.Type)) Invalid("type");
        if (!Name<MaintenanceStatus>(request.Status)) Invalid("status");
        if (request.PlannedDate is not { Year: >= 2000 and <= 2100 }) Invalid("plannedDate");
        var command = new MaintenancePlanCommand(request.Title!.Trim(), Clean(request.Description), Enum.Parse<MaintenanceType>(request.Type!), request.PlannedDate!.Value, Clean(request.Location), Clean(request.Responsible), Enum.Parse<MaintenanceStatus>(request.Status!), request.Version);
        return store.Save(property, id, command, access, token);
    }
    public Task<bool> Delete(Guid property, Guid id, long version, CancellationToken token)
    { var access = Access(true); if (version < 1) Invalid("version"); return store.Delete(property, id, version, access, token); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
