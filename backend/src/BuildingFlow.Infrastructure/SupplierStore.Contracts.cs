using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed partial class SupplierStore
{
    private DateOnly ContractToday => BusinessDate.Today(clock);
    private SupplierContractResponse BuildContractResponse(SupplierContract x,string propertyName,string serviceName)
    {
        var today=ContractToday;var status=x.Status==SupplierContractStatus.ACTIVE?(x.EndDate<today?"EXPIRED":x.StartDate>today?"UPCOMING":"ACTIVE"):x.Status.ToString();
        return new(x.Id,x.TenantId,x.SupplierId,x.PropertyId,x.AreaServiceId,x.Title,x.StartDate,x.EndDate,x.Amount.ToString("F2",CultureInfo.InvariantCulture),x.Currency,x.ReminderDays,x.DocumentId,x.PreviousContractId,status,x.Status==SupplierContractStatus.ACTIVE && x.EndDate.AddDays(-x.ReminderDays)<=today,x.CancellationReason,x.Version,x.CreatedAt,propertyName,serviceName);
    }
    private async Task<SupplierContractResponse> ContractResponse(SupplierContract record,CancellationToken token)
    {
        var labels=await (from a in db.Set<SupplierAreaService>().AsNoTracking()
            join s in db.Set<BuildingFlow.Domain.SupplierService>() on new{a.TenantId,a.SupplierId,Id=a.ServiceId} equals new{s.TenantId,s.SupplierId,s.Id}
            join p in db.Properties on a.PropertyId equals p.Id
            where a.TenantId==record.TenantId && a.SupplierId==record.SupplierId && a.PropertyId==record.PropertyId && a.Id==record.AreaServiceId
            select new{PropertyName=p.Name,ServiceName=s.Name}).SingleOrDefaultAsync(token);
        if(labels is null)throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        return BuildContractResponse(record,labels.PropertyName,labels.ServiceName);
    }
    private IQueryable<SupplierContract> ContractRows(Guid tenant,Guid supplier)=>db.Set<SupplierContract>().Where(x=>x.TenantId==tenant && x.SupplierId==supplier);
    public async Task<SupplierContractResponse?> Contract(Guid tenant,Guid supplier,Guid id,CancellationToken token)
    { var row=await ContractRows(tenant,supplier).AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,token);return row is null || !await AreaAllowed(tenant,supplier,row.PropertyId,token)?null:await ContractResponse(row,token); }
    public async Task<PagedRows<SupplierContractResponse>?> Contracts(Guid tenant,Guid supplier,int page,int pageSize,bool renewalDue,CancellationToken token)
    {
        Page(page,pageSize);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        if(!await Allowed(tenant,token) || !await Rows(tenant).AnyAsync(x=>x.Id==supplier,token))return null;
        var scopes=PropertyScopes(tenant);
        var rows=from c in ContractRows(tenant,supplier).AsNoTracking()
                 join a in db.Set<SupplierAreaService>() on new{c.TenantId,c.SupplierId,c.PropertyId,Id=c.AreaServiceId} equals new{a.TenantId,a.SupplierId,a.PropertyId,a.Id}
                 join s in db.Set<BuildingFlow.Domain.SupplierService>() on new{a.TenantId,a.SupplierId,Id=a.ServiceId} equals new{s.TenantId,s.SupplierId,s.Id}
                 join p in db.Properties on c.PropertyId equals p.Id
                 where scopes.Any(scope=>scope.PropertyId==c.PropertyId)
                 select new{Record=c,PropertyName=p.Name,ServiceName=s.Name};
        if(renewalDue){var today=ContractToday;rows=rows.Where(x=>x.Record.Status==SupplierContractStatus.ACTIVE && x.Record.EndDate.AddDays(-x.Record.ReminderDays)<=today);}
        var count=await rows.CountAsync(token);var values=await rows.OrderBy(x=>x.Record.EndDate).ThenBy(x=>x.Record.Id).Skip((page-1)*pageSize).Take(pageSize).ToArrayAsync(token);
        await tx.CommitAsync(token);return new(values.Select(x=>BuildContractResponse(x.Record,x.PropertyName,x.ServiceName)).ToArray(),page,pageSize,count);
    }

    private static object ContractSnapshot(SupplierContract x)=>new{x.Title,x.StartDate,x.EndDate,Amount=x.Amount.ToString("F2",CultureInfo.InvariantCulture),x.Currency,x.ReminderDays,x.AreaServiceId,x.DocumentId,x.PreviousContractId,Status=x.Status.ToString(),x.CancellationReason};
    private async Task<bool> ValidContractLinks(Guid tenant,Guid supplier,Guid property,Guid area,Guid? document,CancellationToken token)
    {
        if(!await db.Set<SupplierAreaService>().AnyAsync(x=>x.TenantId==tenant && x.SupplierId==supplier && x.PropertyId==property && x.Id==area && x.IsEnabled && db.Set<BuildingFlow.Domain.SupplierService>().Any(s=>s.TenantId==tenant && s.SupplierId==supplier && s.Id==x.ServiceId && s.IsEnabled && db.Set<SupplierCategory>().Any(c=>c.TenantId==tenant && c.Id==s.CategoryId && c.IsEnabled)),token))return false;
        return !document.HasValue || await Files(tenant,supplier).AnyAsync(d=>d.Id==document && d.PropertyId==property && d.IsEnabled && db.StoredDocuments.Any(f=>f.PropertyId==property && f.Id==d.DocumentId && f.IsEnabled && f.IsSupplierOnly),token);
    }
    private async Task<SupplierContractMutation?> ContractReplay(Guid tenant,Guid supplier,Guid operation,string fingerprint,Supplier parent,CancellationToken token)
    {
        var receipt=await db.Set<SupplierContractRevision>().SingleOrDefaultAsync(x=>x.TenantId==tenant && x.ActorIssuer==actor.Issuer && x.ActorSubject==actor.UserId && x.OperationId==operation,token);if(receipt is null)return null;
        if(receipt.SupplierId!=supplier || receipt.Fingerprint!=fingerprint)throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        var current=await ContractRows(tenant,supplier).SingleOrDefaultAsync(x=>x.Id==receipt.ContractId,token);if(current is null)throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        return new(await ContractResponse(current,token),Response(parent),true,receipt.AppliedVersion);
    }
    public async Task<SupplierContractMutation?> SaveContract(Guid tenant,Guid supplier,Guid? id,SupplierContractRequest c,CancellationToken token)
    {
        if(c.OperationId==Guid.Empty || c.SupplierVersion<1 || (id.HasValue?c.Version<1:c.Version!=0) || c.PropertyId==Guid.Empty || c.AreaServiceId==Guid.Empty || c.DocumentId==Guid.Empty || c.PreviousContractId==Guid.Empty)InvalidSupplier("version");
        if(string.IsNullOrWhiteSpace(c.Title) || c.Title.Trim().Length>200 || c.Title.Any(char.IsControl) || c.StartDate.Year<2000 || c.EndDate.Year>2100 || c.EndDate<c.StartDate || c.ReminderDays is <0 or >365)InvalidSupplier("contract");
        decimal amount=0;if(c.Amount is not {Length:<=20} || !Regex.IsMatch(c.Amount,@"^[0-9]+(?:\.[0-9]{1,2})?$") || !decimal.TryParse(c.Amount,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out amount) || amount is <.01m or >99999999.99m)InvalidSupplier("amount");
        await using var tx=await db.Database.BeginTransactionAsync(token);var scope=await LockArea(tenant,supplier,c.PropertyId,true,token);if(scope is null)return null;
        var fingerprint=Hash(new{action=id.HasValue?"EDIT":"CREATE",tenant,supplier,id,c.SupplierVersion,c.Version,c.PropertyId,c.AreaServiceId,title=c.Title!.Trim(),c.StartDate,c.EndDate,amount,c.ReminderDays,c.DocumentId,c.PreviousContractId});
        var replay=await ContractReplay(tenant,supplier,c.OperationId,fingerprint,scope.Value.Supplier,token);if(replay is not null){await tx.CommitAsync(token);return replay;}
        Expected(c.SupplierVersion,scope.Value.Supplier.Version);if(!await ValidContractLinks(tenant,supplier,c.PropertyId,c.AreaServiceId,c.DocumentId,token))return null;
        if(c.PreviousContractId.HasValue)
        {
            var previous=await ContractRows(tenant,supplier).SingleOrDefaultAsync(x=>x.Id==c.PreviousContractId && x.PropertyId==c.PropertyId && x.AreaServiceId==c.AreaServiceId,token);
            if(previous is null)return null;
            if(c.StartDate<=previous.EndDate)throw new HierarchyConflictException(ErrorCatalog.SupplierContractState.Create());
        }
        SupplierContract row;
        if(id.HasValue)
        {
            row=(await ContractRows(tenant,supplier).SingleOrDefaultAsync(x=>x.Id==id && x.PropertyId==c.PropertyId,token))!;if(row is null)return null;Expected(c.Version,row.Version);
            if(row.Status!=SupplierContractStatus.DRAFT || row.AreaServiceId!=c.AreaServiceId || row.PreviousContractId!=c.PreviousContractId)throw new HierarchyConflictException(ErrorCatalog.SupplierContractState.Create());
            row.Edit(c.Title!,c.StartDate,c.EndDate,amount,c.ReminderDays,c.DocumentId);db.Entry(row).Property(x=>x.IsEnabled).IsModified=true;
        }
        else
        {
            if(c.PreviousContractId.HasValue)
            {
                var previous=await ContractRows(tenant,supplier).SingleOrDefaultAsync(x=>x.Id==c.PreviousContractId && x.PropertyId==c.PropertyId && x.AreaServiceId==c.AreaServiceId && x.Status==SupplierContractStatus.ACTIVE,token);
                if(previous is null)return null;
                if(c.StartDate<=previous.EndDate || await ContractRows(tenant,supplier).AnyAsync(x=>x.PreviousContractId==previous.Id,token))throw new HierarchyConflictException(ErrorCatalog.SupplierContractState.Create());
            }
            row=new(tenant,supplier,c.PropertyId,c.AreaServiceId,c.Title!,c.StartDate,c.EndDate,amount,c.ReminderDays,c.DocumentId,c.PreviousContractId);db.Add(row);
        }
        var applied=id.HasValue?c.Version+1:1;db.Add(new SupplierContractRevision(row,c.OperationId,actor.Issuer!,actor.UserId!,fingerprint,id.HasValue?"EDIT":"CREATE",JsonSerializer.Serialize(ContractSnapshot(row)),applied));TouchArea(scope.Value);
        await db.SaveChangesAsync(token);await tx.CommitAsync(token);return new(await ContractResponse(row,token),Response(scope.Value.Supplier),false,applied);
    }
    public async Task<SupplierContractMutation?> TransitionContract(Guid tenant,Guid supplier,Guid id,string action,SupplierContractTransition c,CancellationToken token)
    {
        if(c.OperationId==Guid.Empty || c.SupplierVersion<1 || c.Version<1 || action is not("ACTIVATE" or "CANCEL"))InvalidSupplier("version");
        if(action=="CANCEL" && (string.IsNullOrWhiteSpace(c.Reason) || c.Reason.Trim().Length>1000 || c.Reason.Any(char.IsControl)))InvalidSupplier("reason");
        if(!await Allowed(tenant,token))return null;
        var property=await ContractRows(tenant,supplier).Where(x=>x.Id==id).Select(x=>(Guid?)x.PropertyId).SingleOrDefaultAsync(token);if(property is null)return null;
        await using var tx=await db.Database.BeginTransactionAsync(token);var scope=await LockArea(tenant,supplier,property.Value,action=="ACTIVATE",token);if(scope is null)return null;
        var fingerprint=Hash(new{action,tenant,supplier,id,c.SupplierVersion,c.Version,reason=action=="CANCEL"?c.Reason!.Trim():null});
        var replay=await ContractReplay(tenant,supplier,c.OperationId,fingerprint,scope.Value.Supplier,token);if(replay is not null){await tx.CommitAsync(token);return replay;}
        Expected(c.SupplierVersion,scope.Value.Supplier.Version);var row=await ContractRows(tenant,supplier).SingleAsync(x=>x.Id==id,token);Expected(c.Version,row.Version);
        if(action=="ACTIVATE")
        {
            if(row.Status!=SupplierContractStatus.DRAFT)throw new HierarchyConflictException(ErrorCatalog.SupplierContractState.Create());
            if(!await ValidContractLinks(tenant,supplier,row.PropertyId,row.AreaServiceId,row.DocumentId,token))return null;row.Activate();
        }
        else {if(row.Status==SupplierContractStatus.CANCELLED)throw new HierarchyConflictException(ErrorCatalog.SupplierContractState.Create());row.Cancel(c.Reason!);}
        db.Add(new SupplierContractRevision(row,c.OperationId,actor.Issuer!,actor.UserId!,fingerprint,action,JsonSerializer.Serialize(ContractSnapshot(row)),c.Version+1));TouchArea(scope.Value);
        await db.SaveChangesAsync(token);await tx.CommitAsync(token);return new(await ContractResponse(row,token),Response(scope.Value.Supplier),false,row.Version);
    }
    public async Task<PagedRows<SupplierContractHistory>?> ContractHistory(Guid tenant,Guid supplier,Guid id,int page,int pageSize,CancellationToken token)
    {
        Page(page,pageSize);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        var contract=await ContractRows(tenant,supplier).AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,token);if(contract is null || !await AreaAllowed(tenant,supplier,contract.PropertyId,token))return null;
        var rows=db.Set<SupplierContractRevision>().AsNoTracking().Where(x=>x.TenantId==tenant && x.SupplierId==supplier && x.PropertyId==contract.PropertyId && x.ContractId==id);
        var total=await rows.CountAsync(token);var values=await rows.OrderByDescending(x=>x.AppliedVersion).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new SupplierContractHistory(x.Id,x.Action,x.AppliedVersion,x.Snapshot,x.CreatedAt)).ToArrayAsync(token);
        await tx.CommitAsync(token);return new(values,page,pageSize,total);
    }
}
