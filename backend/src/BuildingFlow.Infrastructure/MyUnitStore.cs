using System.Globalization;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class MyUnitStore(BuildingFlowDbContext db,IMyPortfolioStore portfolio,IFileStorage storage,TimeProvider clock):IMyUnitStore
{
    private sealed record Scope(ActiveContext Context,Guid TenantId,Guid PersonId);
    private static string Money(decimal value)=>value.ToString("0.00",CultureInfo.InvariantCulture);
    private DateOnly Today=>BusinessDate.Today(clock);
    private async Task<Scope?> Resolve(MyUnitQuery q,string capability,CancellationToken token)
    {
        if(q.Page is <1 or >100000 || q.PageSize is <1 or >50 || q.Capacity is not("OWNER" or "TENANT" or "RESIDENT"))throw new ArgumentException("Invalid unit query.");
        var context=await portfolio.ResolveAsync(new(q.PropertyId,q.UnitId,q.Capacity,q.RelationId),token);if(context==null)return null;
        if(!context.Capabilities.Contains(capability))throw new UnauthorizedAccessException();
        var person=q.Capacity switch
        {
            "OWNER"=>await db.Set<UnitOwnership>().Where(r=>r.Id==q.RelationId).Select(r=>r.PersonId).SingleAsync(token),
            "TENANT"=>await db.Set<UnitTenancy>().Where(r=>r.Id==q.RelationId).Select(r=>r.PersonId).SingleAsync(token),
            _=>await db.Set<UnitResidency>().Where(r=>r.Id==q.RelationId).Select(r=>r.PersonId).SingleAsync(token)
        };
        return new(context,context.Context.TenantId,person);
    }
    private static PagedRows<T> Paged<T>(T[] items,MyUnitQuery q,int total)=>new(items,q.Page,q.PageSize,total);
    // Debt follows the recorded charge parties (06.1): UNIT debts without a debtor, or a debtor snapshot of this capacity mapped to this person.
    private IQueryable<DuesCharge> Charges(Scope s,MyUnitQuery q)
    {
        var type=q.Capacity=="OWNER"?OccupancyType.OWNER:OccupancyType.TENANT;var legacy=db.Set<LegacyPersonMap>().Where(m=>m.TenantId==s.TenantId && m.PersonId==s.PersonId && m.IsEnabled).Select(m=>m.LegacyPersonId);
        return db.DuesCharges.AsNoTracking().Where(c=>c.PropertyId==q.PropertyId && c.UnitId==q.UnitId && c.IsEnabled &&
            (db.DuesAssessments.Any(a=>a.Id==c.AssessmentId && a.PropertyId==q.PropertyId && a.DebtorType=="UNIT") && !db.Set<DuesChargeParty>().Any(p=>p.ChargeId==c.Id && p.IsDebtor) ||
             db.Set<DuesChargeParty>().Any(p=>p.ChargeId==c.Id && p.PropertyId==q.PropertyId && p.UnitId==q.UnitId && p.Type==type && p.IsDebtor && legacy.Contains(p.PersonId))));
    }
    public async Task<MyDues?> DuesAsync(MyUnitQuery q,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        var s=await Resolve(q,PortfolioCapabilities.ViewDues,token);if(s==null)return null;var today=Today;var charges=Charges(s,q);var open=charges.Where(c=>!c.IsCancelled);
        var summary=new MobileFinanceSummary(Money(await open.SumAsync(c=>(decimal?)c.Amount,token)??0m),Money(await open.SumAsync(c=>(decimal?)c.PaidAmount,token)??0m),Money(await open.SumAsync(c=>(decimal?)(c.Amount-c.PaidAmount),token)??0m),Money(await open.Where(c=>c.DueDate<today).SumAsync(c=>(decimal?)(c.Amount-c.PaidAmount),token)??0m));
        var total=await charges.CountAsync(token);var rows=await charges.OrderByDescending(c=>c.Period).ThenBy(c=>c.Id).Skip((q.Page-1)*q.PageSize).Take(q.PageSize).ToArrayAsync(token);await tx.CommitAsync(token);
        return new(s.Context.ContextKey,summary,Paged(rows.Select(c=>new OwnerCharge(c.Id,c.Period.ToString("yyyy-MM",CultureInfo.InvariantCulture),Money(c.Amount),Money(c.PaidAmount),Money(c.Balance),c.DueDate,c.Status(today),"0.00",c.Kind.ToString(),c.Category?.ToString())).ToArray(),q,total));
    }
    public async Task<MyUnitPage<MyPaymentRow>?> PaymentsAsync(MyUnitQuery q,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        var s=await Resolve(q,PortfolioCapabilities.ViewPayments,token);if(s==null)return null;
        var rows=from allocation in db.PaymentAllocations.AsNoTracking() join payment in db.Payments on allocation.PaymentId equals payment.Id join c in Charges(s,q) on allocation.ChargeId equals c.Id
            where allocation.PropertyId==q.PropertyId && allocation.UnitId==q.UnitId && allocation.IsEnabled && payment.PropertyId==q.PropertyId && payment.UnitId==q.UnitId && payment.IsEnabled
            select new{allocation.Id,ChargeId=c.Id,c.Period,allocation.Amount,payment.PaidDate,payment.IsCancelled};
        var total=await rows.CountAsync(token);var page=await rows.OrderByDescending(x=>x.PaidDate).ThenBy(x=>x.Id).Skip((q.Page-1)*q.PageSize).Take(q.PageSize).ToArrayAsync(token);await tx.CommitAsync(token);
        return new(s.Context.ContextKey,Paged(page.Select(x=>new MyPaymentRow(x.Id,x.ChargeId,x.Period.ToString("yyyy-MM",CultureInfo.InvariantCulture),Money(x.Amount),x.PaidDate,x.IsCancelled)).ToArray(),q,total));
    }
    private IQueryable<Announcement> Published(Guid property)=>db.Announcements.AsNoTracking().Where(a=>a.PropertyId==property && a.Status==AnnouncementStatus.PUBLISHED && a.Audience==ContentAudience.ALL_MEMBERS && a.PublishedAt!=null);
    public async Task<MyUnitPage<MyAnnouncementRow>?> AnnouncementsAsync(MyUnitQuery q,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        var s=await Resolve(q,PortfolioCapabilities.ViewAnnouncements,token);if(s==null)return null;var rows=Published(q.PropertyId);
        var total=await rows.CountAsync(token);var page=await rows.OrderByDescending(a=>a.PublishedAt).ThenBy(a=>a.Id).Skip((q.Page-1)*q.PageSize).Take(q.PageSize).ToArrayAsync(token);await tx.CommitAsync(token);
        return new(s.Context.ContextKey,Paged(page.Select(a=>new MyAnnouncementRow(a.Id,a.Title,a.Content,a.Category.ToString(),a.PublishedAt!.Value)).ToArray(),q,total));
    }
    public async Task<MyUnitPage<MyDocumentRow>?> DocumentsAsync(MyUnitQuery q,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        var s=await Resolve(q,PortfolioCapabilities.ViewDocuments,token);if(s==null)return null;var published=Published(q.PropertyId);
        var rows=db.StoredDocuments.AsNoTracking().Where(d=>d.PropertyId==q.PropertyId && d.Audience==ContentAudience.ALL_MEMBERS && d.AnnouncementId!=null && published.Any(a=>a.Id==d.AnnouncementId));
        var total=await rows.CountAsync(token);var page=await rows.OrderByDescending(d=>d.CreatedAt).ThenBy(d=>d.Id).Skip((q.Page-1)*q.PageSize).Take(q.PageSize).ToArrayAsync(token);await tx.CommitAsync(token);
        return new(s.Context.ContextKey,Paged(page.Select(d=>new MyDocumentRow(d.Id,d.AnnouncementId!.Value,d.FileName,d.ContentType,d.Size,d.CreatedAt)).ToArray(),q,total));
    }
    // A tenant sees the contract of its own tenancy; an owner whose unit is rented sees the unit's contracts without party identities.
    public async Task<MyUnitPage<MyLeaseRow>?> LeasesAsync(MyUnitQuery q,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        var s=await Resolve(q,PortfolioCapabilities.ViewLease,token);if(s==null)return null;var today=Today;
        var own=q.Capacity=="TENANT"?await db.Set<UnitTenancy>().Where(r=>r.Id==q.RelationId).Select(r=>r.LeaseContractId).SingleAsync(token):null;
        var rows=db.Set<LeaseContract>().AsNoTracking().Where(c=>c.TenantId==s.TenantId && c.PropertyId==q.PropertyId && c.UnitId==q.UnitId && (q.Capacity=="OWNER" || c.Id==own));
        var total=await rows.CountAsync(token);
        var page=await rows.OrderByDescending(c=>c.StartDate).ThenBy(c=>c.Id).Skip((q.Page-1)*q.PageSize).Take(q.PageSize).Select(c=>new{Lease=c,Parties=db.Set<UnitTenancy>().Count(t=>t.LeaseContractId==c.Id),IsParty=db.Set<UnitTenancy>().Any(t=>t.LeaseContractId==c.Id && t.PersonId==s.PersonId)}).ToArrayAsync(token);await tx.CommitAsync(token);
        return new(s.Context.ContextKey,Paged(page.Select(x=>new MyLeaseRow(x.Lease.Id,x.Lease.StartDate,x.Lease.EndDate,x.Lease.ContractReference,!x.Lease.IsEnabled?"DISABLED":x.Lease.EndDate<today?"ENDED":x.Lease.StartDate>today?"UPCOMING":"ACTIVE",x.Parties,x.IsParty)).ToArray(),q,total));
    }
    public async Task<MyDocumentContent?> DownloadAsync(MyUnitQuery q,Guid document,CancellationToken token)
    {
        var s=await Resolve(q,PortfolioCapabilities.ViewDocuments,token);if(s==null)return null;var published=Published(q.PropertyId);
        var value=await db.StoredDocuments.AsNoTracking().SingleOrDefaultAsync(d=>d.Id==document && d.PropertyId==q.PropertyId && d.Audience==ContentAudience.ALL_MEMBERS && d.AnnouncementId!=null && published.Any(a=>a.Id==d.AnnouncementId),token);if(value==null)return null;
        var stream=await storage.DownloadAsync(value.ObjectKey,token);
        if(stream.Length!=value.Size || Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream,token))!=value.Sha256){await stream.DisposeAsync();throw new StorageUnavailableException();}
        stream.Position=0;return new(value.FileName,value.ContentType,stream);
    }
    // One total across every owner/tenant context that may view dues; a charge visible from two roles is counted once.
    public async Task<MyFinanceSummary> FinanceSummaryAsync(CancellationToken token)
    {
        var view=await portfolio.PortfolioAsync(token);var today=Today;var scope=new List<MyFinanceScope>();var ids=new HashSet<Guid>();var seen=0;
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,token);
        foreach(var person in view.People)foreach(var property in person.Properties)foreach(var unit in property.Buildings.SelectMany(b=>b.Units))
        {
            var access=view.Units.SingleOrDefault(u=>u.TenantId==person.TenantId && u.UnitId==unit.UnitId);if(access==null || !access.Capabilities.Contains(PortfolioCapabilities.ViewDues))continue;
            foreach(var context in access.Contexts.Where(c=>c.Capacity is "OWNER" or "TENANT"))
            {
                var q=new MyUnitQuery(property.PropertyId,unit.UnitId,context.Capacity,context.RelationId);var found=await Charges(new(null!,person.TenantId,person.PersonId),q).Select(c=>c.Id).ToArrayAsync(token);
                seen+=found.Length;ids.UnionWith(found);scope.Add(new(person.TenantId,property.PropertyId,property.PropertyName,unit.UnitId,unit.UnitNumber,context.Capacity));
            }
        }
        var list=ids.ToArray();var open=db.DuesCharges.AsNoTracking().Where(c=>list.Contains(c.Id) && !c.IsCancelled);
        var summary=new MobileFinanceSummary(Money(await open.SumAsync(c=>(decimal?)c.Amount,token)??0m),Money(await open.SumAsync(c=>(decimal?)c.PaidAmount,token)??0m),Money(await open.SumAsync(c=>(decimal?)(c.Amount-c.PaidAmount),token)??0m),Money(await open.Where(c=>c.DueDate<today).SumAsync(c=>(decimal?)(c.Amount-c.PaidAmount),token)??0m));
        await tx.CommitAsync(token);return new(today,scope,list.Length,seen-list.Length,summary);
    }
}
