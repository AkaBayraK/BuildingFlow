namespace BuildingFlow.Application;
public sealed record ConsumptionReport(string Kind, string MeasurementUnit, int ReadingCount, int BaselineCount, string Consumption);
public sealed record ReportMonth(string Period,string AssessedAmount,string OutstandingAmount,string ReceiptsAmount,string ExpensesAmount);
public sealed record ReportExpenseCategory(string Category,string Amount);
// Period is YYYY-MM for a month or YYYY for a calendar year; Months is the yearly breakdown and null for a single month.
public sealed record ManagementReport(Guid PropertyId,string Period,string Currency,int UnitCount,int ActiveUnitCount,int OccupiedUnitCount,int VacantUnitCount,string AssessedAmount,string OutstandingAmount,string ReceiptsAmount,string ExpensesAmount,IReadOnlyList<ConsumptionReport> Consumption,
    IReadOnlyList<ReportExpenseCategory>? ExpenseCategories=null,IReadOnlyList<ReportMonth>? Months=null,string? PropertyName=null,DateTimeOffset? GeneratedAt=null);
public interface IReportStore { Task<ManagementReport?> Get(Guid property,DateOnly from,bool year,AccessScope access,CancellationToken token); }
public sealed class ReportService(IReportStore store,ICurrentUserService user)
{
    public Task<ManagementReport?> Get(Guid property,string period,CancellationToken token)
    {
        if(!user.IsAuthenticated||string.IsNullOrWhiteSpace(user.UserId)||!new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"}.Any(user.IsInRole))throw new UnauthorizedAccessException();
        var year=Year(period,out var start);
        if(!year&&!ExpenseService.Period(period,out start))throw new RequestValidationException(new(){["period"]=[ErrorCatalog.DuesErrorPeriod.Create()]});
        return store.Get(property,start,year,new(user.UserId,user.IsInRole("SUPER_ADMIN")),token);
    }
    private static bool Year(string? text,out DateOnly start)
    {
        start=default;
        if(text is not {Length:4}||!text.All(char.IsAsciiDigit)||int.Parse(text) is < 2000 or > 9998)return false;
        start=new DateOnly(int.Parse(text),1,1);return true;
    }
}
