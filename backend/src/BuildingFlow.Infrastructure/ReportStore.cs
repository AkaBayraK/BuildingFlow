using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class ReportStore(BuildingFlowDbContext db):IReportStore
{
    public async Task<ManagementReport?> Get(Guid property,DateOnly period,AccessScope access,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        if(!await db.Properties.AnyAsync(p=>p.Id==property&&(access.ReadAll||db.PropertyMemberships.Any(m=>m.PropertyId==property&&m.UserId==access.UserId&&m.IsEnabled)),token))return null;
        var end=period.AddMonths(1);var day=end.AddDays(-1);var units=db.Units.Where(u=>u.PropertyId==property);var total=await units.CountAsync(token);var active=await units.CountAsync(u=>u.IsEnabled,token);
        var occupied=await units.CountAsync(u=>u.IsEnabled&&db.UnitOccupancies.Any(o=>o.PropertyId==property&&o.UnitId==u.Id&&o.IsEnabled&&(o.Type==OccupancyType.TENANT||o.Type==OccupancyType.RESIDENT)&&o.StartDate<=day&&(o.EndDate==null||o.EndDate>=day)),token);
        var dues=db.DuesCharges.Where(d=>d.PropertyId==property&&d.Period==period&&!d.IsCancelled);
        var assessed=await dues.SumAsync(d=>(decimal?)d.Amount,token)??0;var outstanding=await dues.SumAsync(d=>(decimal?)(d.Amount-d.PaidAmount),token)??0;
        var receipts=await db.Payments.Where(p=>p.PropertyId==property&&!p.IsCancelled&&p.PaidDate>=period&&p.PaidDate<end).SumAsync(p=>(decimal?)p.Amount,token)??0;
        var expenses=await db.Expenses.Where(e=>e.PropertyId==property&&!e.IsCancelled&&e.OccurredDate>=period&&e.OccurredDate<end).SumAsync(e=>(decimal?)e.Amount,token)??0;
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
        return new(property,period.ToString("yyyy-MM"),"TRY",total,active,occupied,active-occupied,DuesService.Money(assessed),DuesService.Money(outstanding),DuesService.Money(receipts),DuesService.Money(expenses),kinds);
    }
    private sealed record ConsumptionRow(string Kind,int ReadingCount,int BaselineCount,decimal Consumption);
}
