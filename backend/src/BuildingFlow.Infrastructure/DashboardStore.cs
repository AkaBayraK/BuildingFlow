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
        // Assessment-period cohort, current balances: overdue and pending are disjoint.
        // Payment dates do not move allocations to a different assessment period.
        var previous=month.AddMonths(-1);
        var collectionRows=await db.DuesCharges.Where(d=>d.PropertyId==property&&!d.IsCancelled&&d.Period>=previous&&d.Period<=month)
            .GroupBy(d=>d.Period).Select(g=>new{Period=g.Key,Total=g.Sum(d=>d.Amount),Collected=g.Sum(d=>d.PaidAmount),Pending=g.Sum(d=>d.DueDate>=today?d.Amount-d.PaidAmount:0m),Overdue=g.Sum(d=>d.DueDate<today?d.Amount-d.PaidAmount:0m)}).ToListAsync(token);
        var collections=new[]{previous,month}.Select(period=>{var row=collectionRows.SingleOrDefault(r=>r.Period==period);return new DashboardCollection(period.ToString("yyyy-MM"),DuesService.Money(row?.Total??0),DuesService.Money(row?.Collected??0),DuesService.Money(row?.Pending??0),DuesService.Money(row?.Overdue??0));}).ToArray();
        DashboardRequests? requests=null;IReadOnlyList<DashboardRecentRequest>? recent=null;
        if(includeRequests){var counts=await db.Set<MobileRequest>().Where(r=>r.PropertyId==property&&(r.ManagementStatus=="RECORDED"||r.ManagementStatus=="IN_PROGRESS")).GroupBy(r=>r.ManagementStatus).Select(g=>new{g.Key,Count=g.Count()}).ToListAsync(token);requests=new(counts.SingleOrDefault(c=>c.Key=="RECORDED")?.Count??0,counts.SingleOrDefault(c=>c.Key=="IN_PROGRESS")?.Count??0);
            var rows=await db.Set<MobileRequest>().AsNoTracking().Where(r=>r.PropertyId==property&&r.IsEnabled).OrderByDescending(r=>r.CreatedAt).ThenBy(r=>r.Id).Take(DashboardService.RecentRequests)
                .Select(r=>new{r.Id,r.Category,r.Description,r.ManagementStatus,r.CreatedAt,Unit=db.Units.Where(u=>u.PropertyId==r.PropertyId&&u.Id==r.UnitId).Select(u=>new{u.Number,Building=db.Buildings.Where(b=>b.Id==u.BuildingId).Select(b=>b.Name).FirstOrDefault()}).FirstOrDefault()}).ToListAsync(token);
            recent=rows.Select(r=>new DashboardRecentRequest(r.Id,r.Category,Title(r.Description),r.Unit?.Building,r.Unit?.Number,r.ManagementStatus,r.CreatedAt)).ToArray();}
        await tx.CommitAsync(token);
        var trend=Enumerable.Range(0,DashboardService.TrendMonths).Select(i=>first.AddMonths(i)).Select(m=>new DashboardMonth(m.ToString("yyyy-MM"),
            DuesService.Money(receipts.SingleOrDefault(r=>r.Year==m.Year&&r.Month==m.Month)?.Sum??0),DuesService.Money(expenses.SingleOrDefault(r=>r.Year==m.Year&&r.Month==m.Month)?.Sum??0))).ToArray();
        return new(property,today,"TRY",total,occupied,total-occupied,trend[^1].Receipts,DuesService.Money(overdue?.Sum??0),overdue?.Count??0,DuesService.Money(upcoming?.Sum??0),upcoming?.Count??0,
            requests,trend,Enum.GetValues<UnitType>().Select(t=>new DashboardUnitType(t.ToString(),types.SingleOrDefault(x=>x.Key==t)?.Count??0)).ToArray(),
            dues.Select(d=>new DashboardDue(d.DueDate,d.Count,DuesService.Money(d.Sum))).ToArray(),false,recent,collections);
    }
    private static string Title(string description)
    {
        var line=description.Trim().Split('\n')[0].Trim();
        return line.Length<=DashboardService.RecentTitleLength?line:line[..(DashboardService.RecentTitleLength-1)].TrimEnd()+"…";
    }
}
