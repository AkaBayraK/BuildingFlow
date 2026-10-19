using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class DashboardStore(BuildingFlowDbContext db):IDashboardStore
{
    public async Task<DashboardSummary?> Get(Guid property,DateOnly today,AccessScope access,bool includeRequests,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        if(!await db.Properties.AnyAsync(p=>p.Id==property&&(access.ReadAll||db.PropertyMemberships.Any(m=>m.PropertyId==property&&m.UserId==access.UserId&&m.IsEnabled)),token))return null;
        // Same occupancy meaning as the monthly report, evaluated for today: an enabled unit with a TENANT/RESIDENT relation covering today.
        var units=db.Units.Where(u=>u.PropertyId==property&&u.IsEnabled);
        var total=await units.CountAsync(token);
        var occupied=await units.CountAsync(u=>db.UnitOccupancies.Any(o=>o.PropertyId==property&&o.UnitId==u.Id&&o.IsEnabled&&(o.Type==OccupancyType.TENANT||o.Type==OccupancyType.RESIDENT)&&o.StartDate<=today&&(o.EndDate==null||o.EndDate>=today)),token);
        var types=await units.GroupBy(u=>u.Type).Select(g=>new{g.Key,Count=g.Count()}).ToListAsync(token);
        var open=db.DuesCharges.Where(d=>d.PropertyId==property&&!d.IsCancelled&&d.Amount>d.PaidAmount);
        var overdue=await open.Where(d=>d.DueDate<today).GroupBy(_=>1).Select(g=>new{Count=g.Count(),Sum=g.Sum(d=>d.Amount-d.PaidAmount)}).SingleOrDefaultAsync(token);
        var horizon=today.AddDays(DashboardService.UpcomingDays);
        var upcoming=await open.Where(d=>d.DueDate>=today&&d.DueDate<=horizon).GroupBy(_=>1).Select(g=>new{Count=g.Count(),Sum=g.Sum(d=>d.Amount-d.PaidAmount)}).SingleOrDefaultAsync(token);
        var dues=await open.Where(d=>d.DueDate>=today).GroupBy(d=>d.DueDate).Select(g=>new{DueDate=g.Key,Count=g.Count(),Sum=g.Sum(d=>d.Amount-d.PaidAmount)}).OrderBy(g=>g.DueDate).Take(DashboardService.UpcomingGroups).ToListAsync(token);
        var month=new DateOnly(today.Year,today.Month,1);var first=month.AddMonths(1-DashboardService.TrendMonths);var end=month.AddMonths(1);
        var receipts=await db.Payments.Where(p=>p.PropertyId==property&&!p.IsCancelled&&p.PaidDate>=first&&p.PaidDate<end).GroupBy(p=>new{p.PaidDate.Year,p.PaidDate.Month}).Select(g=>new{g.Key.Year,g.Key.Month,Sum=g.Sum(p=>p.Amount)}).ToListAsync(token);
        var expenses=await db.Expenses.Where(e=>e.PropertyId==property&&!e.IsCancelled&&e.OccurredDate>=first&&e.OccurredDate<end).GroupBy(e=>new{e.OccurredDate.Year,e.OccurredDate.Month}).Select(g=>new{g.Key.Year,g.Key.Month,Sum=g.Sum(e=>e.Amount)}).ToListAsync(token);
        DashboardRequests? requests=null;
        if(includeRequests){var counts=await db.Set<MobileRequest>().Where(r=>r.PropertyId==property&&(r.ManagementStatus=="RECORDED"||r.ManagementStatus=="IN_PROGRESS")).GroupBy(r=>r.ManagementStatus).Select(g=>new{g.Key,Count=g.Count()}).ToListAsync(token);requests=new(counts.SingleOrDefault(c=>c.Key=="RECORDED")?.Count??0,counts.SingleOrDefault(c=>c.Key=="IN_PROGRESS")?.Count??0);}
        await tx.CommitAsync(token);
        var trend=Enumerable.Range(0,DashboardService.TrendMonths).Select(i=>first.AddMonths(i)).Select(m=>new DashboardMonth(m.ToString("yyyy-MM"),
            DuesService.Money(receipts.SingleOrDefault(r=>r.Year==m.Year&&r.Month==m.Month)?.Sum??0),DuesService.Money(expenses.SingleOrDefault(r=>r.Year==m.Year&&r.Month==m.Month)?.Sum??0))).ToArray();
        return new(property,today,"TRY",total,occupied,total-occupied,trend[^1].Receipts,DuesService.Money(overdue?.Sum??0),overdue?.Count??0,DuesService.Money(upcoming?.Sum??0),upcoming?.Count??0,
            requests,trend,Enum.GetValues<UnitType>().Select(t=>new DashboardUnitType(t.ToString(),types.SingleOrDefault(x=>x.Key==t)?.Count??0)).ToArray(),
            dues.Select(d=>new DashboardDue(d.DueDate,d.Count,DuesService.Money(d.Sum))).ToArray(),false);
    }
}
