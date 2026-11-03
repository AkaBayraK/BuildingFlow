using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class ReportStore(BuildingFlowDbContext db,TimeProvider clock):IReportStore
{
    public async Task<ManagementReport?> Get(Guid property,DateOnly period,bool year,AccessScope access,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        var generatedAt=clock.GetUtcNow();
        var propertyName=await db.Properties.Where(p=>p.Id==property&&(access.ReadAll||db.PropertyMemberships.Any(m=>m.PropertyId==property&&m.UserId==access.UserId&&m.IsEnabled))).Select(p=>p.Name).SingleOrDefaultAsync(token);
        if(propertyName is null)return null;
        var end=year?period.AddYears(1):period.AddMonths(1);var day=end.AddDays(-1);var units=db.Units.Where(u=>u.PropertyId==property);var total=await units.CountAsync(token);var active=await units.CountAsync(u=>u.IsEnabled,token);
        var occupied=await units.CountAsync(u=>u.IsEnabled&&db.UnitOccupancies.Any(o=>o.PropertyId==property&&o.UnitId==u.Id&&o.IsEnabled&&(o.Type==OccupancyType.TENANT||o.Type==OccupancyType.RESIDENT)&&o.StartDate<=day&&(o.EndDate==null||o.EndDate>=day)),token);
        var dues=db.DuesCharges.Where(d=>d.PropertyId==property&&d.Period>=period&&d.Period<end&&!d.IsCancelled);
        var assessed=await dues.SumAsync(d=>(decimal?)d.Amount,token)??0;var outstanding=await dues.SumAsync(d=>(decimal?)(d.Amount-d.PaidAmount),token)??0;
        var payments=db.Payments.Where(p=>p.PropertyId==property&&!p.IsCancelled&&p.PaidDate>=period&&p.PaidDate<end);
        var other=FinancialIncomeStore.ActiveReceipts(db,property).Where(r=>r.PaidOn>=period&&r.PaidOn<end);
        var spent=db.Expenses.Where(e=>e.PropertyId==property&&!e.IsCancelled&&e.OccurredDate>=period&&e.OccurredDate<end);
        var receipts=await payments.SumAsync(p=>(decimal?)p.Amount,token)??0;var expenses=await spent.SumAsync(e=>(decimal?)e.Amount,token)??0;
        receipts+=await other.SumAsync(r=>(decimal?)r.Amount,token)??0;
        var categories=await spent.GroupBy(e=>e.Category).Select(g=>new{g.Key,Sum=g.Sum(e=>e.Amount)}).ToListAsync(token);
        IReadOnlyList<ReportMonth>? months=null;
        if(year)
        {
            var charged=await dues.GroupBy(d=>d.Period).Select(g=>new{g.Key,Assessed=g.Sum(d=>d.Amount),Outstanding=g.Sum(d=>d.Amount-d.PaidAmount)}).ToListAsync(token);
            var paid=await payments.GroupBy(p=>p.PaidDate.Month).Select(g=>new{g.Key,Sum=g.Sum(p=>p.Amount)}).ToListAsync(token);
            var otherPaid=await other.GroupBy(r=>r.PaidOn.Month).Select(g=>new{g.Key,Sum=g.Sum(r=>r.Amount)}).ToListAsync(token);
            var cost=await spent.GroupBy(e=>e.OccurredDate.Month).Select(g=>new{g.Key,Sum=g.Sum(e=>e.Amount)}).ToListAsync(token);
            months=Enumerable.Range(1,12).Select(m=>{var c=charged.SingleOrDefault(r=>r.Key.Month==m);
                return new ReportMonth(new DateOnly(period.Year,m,1).ToString("yyyy-MM"),DuesService.Money(c?.Assessed??0),DuesService.Money(c?.Outstanding??0),
                    DuesService.Money((paid.SingleOrDefault(r=>r.Key==m)?.Sum??0)+(otherPaid.SingleOrDefault(r=>r.Key==m)?.Sum??0)),DuesService.Money(cost.SingleOrDefault(r=>r.Key==m)?.Sum??0));}).ToArray();
        }
        // LAG includes the baseline before the requested month. No person join, client query or identifiers are exported as metrics.
        var consumption=await db.Database.SqlQuery<ConsumptionRow>($"""
            WITH readings AS (
                SELECT r.meter_id,r.reading_date,r.value,
                  LAG(r.value) OVER(PARTITION BY r.meter_id ORDER BY r.reading_date) AS previous
                FROM meter_readings r WHERE r.property_id={property} AND NOT r.is_deleted AND r.reading_date<{end}
            )
            SELECT m.kind AS "Kind",count(*)::int AS "ReadingCount",
              count(*) FILTER(WHERE r.previous IS NULL)::int AS "BaselineCount",
              COALESCE(sum(r.value-r.previous),0)::numeric AS "Consumption"
            FROM readings r JOIN meters m ON m.id=r.meter_id AND m.property_id={property} AND NOT m.is_deleted
            WHERE r.reading_date>={period} GROUP BY m.kind
            """).ToListAsync(token);
        var kinds=Enum.GetValues<MeterKind>().Select(k=>{var row=consumption.SingleOrDefault(r=>r.Kind==k.ToString());return new ConsumptionReport(k.ToString(),k==MeterKind.ELECTRICITY?"kWh":"m³",row?.ReadingCount??0,row?.BaselineCount??0,MeterService.Number(row?.Consumption??0));}).ToArray();
        await tx.CommitAsync(token);
        return new(property,period.ToString(year?"yyyy":"yyyy-MM"),"TRY",total,active,occupied,active-occupied,DuesService.Money(assessed),DuesService.Money(outstanding),DuesService.Money(receipts),DuesService.Money(expenses),kinds,
            Enum.GetValues<ExpenseCategory>().Select(c=>new ReportExpenseCategory(c.ToString(),DuesService.Money(categories.SingleOrDefault(r=>r.Key==c)?.Sum??0))).ToArray(),months,propertyName,generatedAt);
    }
    private sealed record ConsumptionRow(string Kind,int ReadingCount,int BaselineCount,decimal Consumption);
}
