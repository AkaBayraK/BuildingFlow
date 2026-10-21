namespace BuildingFlow.Application;
// İŞ20 Dashboard: one scoped, read-only summary of a management area for the centre home page. No person data.
public sealed record DashboardMonth(string Period,string Receipts,string Expenses);
public sealed record DashboardUnitType(string Type,int Count);
public sealed record DashboardDue(DateOnly DueDate,int ChargeCount,string Amount);
public sealed record DashboardCollection(string Period,string TotalAmount,string CollectedAmount,string PendingAmount,string OverdueAmount);
public sealed record DashboardRequests(int Recorded,int InProgress);
// Latest requests for the "Son Talepler" card; Title is the first line of the description, cut to RecentTitleLength.
public sealed record DashboardRecentRequest(Guid Id,string Category,string Title,string? BuildingName,string? UnitNumber,string Status,DateTimeOffset CreatedAt);
public sealed record DashboardSummary(Guid PropertyId,DateOnly Today,string Currency,int UnitCount,int OccupiedUnitCount,int VacantUnitCount,
    string ReceiptsThisMonth,string OverdueAmount,int OverdueChargeCount,string UpcomingAmount,int UpcomingChargeCount,
    DashboardRequests? Requests,IReadOnlyList<DashboardMonth> Trend,IReadOnlyList<DashboardUnitType> UnitTypes,IReadOnlyList<DashboardDue> UpcomingDues,
    bool MaintenanceAvailable,IReadOnlyList<DashboardRecentRequest>? RecentRequests=null,IReadOnlyList<DashboardCollection>? Collections=null);
public interface IDashboardStore { Task<DashboardSummary?> Get(Guid property,DateOnly today,AccessScope access,bool includeRequests,CancellationToken token); }
public sealed class DashboardService(IDashboardStore store,ICurrentUserService user,TimeProvider clock)
{
    public const int TrendMonths=6, UpcomingDays=30, UpcomingGroups=4, RecentRequests=4, RecentTitleLength=120;
    private static readonly TimeZoneInfo Zone=TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    public Task<DashboardSummary?> Get(Guid property,CancellationToken token)
    {
        if(!user.IsAuthenticated||string.IsNullOrWhiteSpace(user.UserId)||!new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"}.Any(user.IsInRole))throw new UnauthorizedAccessException();
        // Request counts follow the request-management policy: manager roles only; ACCOUNTANT gets null, not zero.
        var requests=new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER"}.Any(user.IsInRole);
        var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(),Zone).DateTime);
        return store.Get(property,today,new(user.UserId,user.IsInRole("SUPER_ADMIN")),requests,token);
    }
}
