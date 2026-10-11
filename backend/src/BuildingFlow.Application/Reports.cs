namespace BuildingFlow.Application;
public sealed record ConsumptionReport(string Kind, string MeasurementUnit, int ReadingCount, int BaselineCount, string Consumption);
public sealed record ManagementReport(Guid PropertyId,string Period,string Currency,int UnitCount,int ActiveUnitCount,int OccupiedUnitCount,int VacantUnitCount,string AssessedAmount,string OutstandingAmount,string ReceiptsAmount,string ExpensesAmount,IReadOnlyList<ConsumptionReport> Consumption);
public interface IReportStore { Task<ManagementReport?> Get(Guid property,DateOnly period,AccessScope access,CancellationToken token); }
public sealed class ReportService(IReportStore store,ICurrentUserService user)
{
    public Task<ManagementReport?> Get(Guid property,string period,CancellationToken token)
    {
        if(!user.IsAuthenticated||string.IsNullOrWhiteSpace(user.UserId)||!new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"}.Any(user.IsInRole))throw new UnauthorizedAccessException();
        if(!ExpenseService.Period(period,out var month))throw new RequestValidationException(new(){["period"]=[ErrorCatalog.DuesErrorPeriod.Create()]});
        return store.Get(property,month,new(user.UserId,user.IsInRole("SUPER_ADMIN")),token);
    }
}
