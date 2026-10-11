using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;
public sealed class MeterStore(BuildingFlowDbContext db, TimeProvider clock) : IMeterStore
{
    internal IQueryable<Property> Properties(AccessScope access) => access.ReadAll ? db.Properties : db.Properties.Where(p => db.PropertyMemberships.Any(m => m.PropertyId == p.Id && m.IsEnabled && m.UserId == access.UserId));
    private static string Hash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private static MeterResponse Response(Meter m, bool replay = false) => new(m.Id,m.PropertyId,m.UnitId,m.Name,m.Kind.ToString(),m.Kind==MeterKind.ELECTRICITY?"kWh":"m³",m.IsEnabled,m.Version,replay);
    private async Task<Property?> Lock(Guid property, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(p => p.Id==property,token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id={property} FOR UPDATE",token);
        return await Properties(access).SingleOrDefaultAsync(p => p.Id==property,token);
    }
    private static void Active(Property p) { if(!p.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.PaymentsInactiveProperty.Create()); }
    private async Task Save(Property p, CancellationToken token)
    {
        db.Entry(p).Property(x => x.IsEnabled).IsModified=true;
        try { await db.SaveChangesAsync(token); }
        catch(DbUpdateException e) when(e.InnerException is PostgresException {SqlState:PostgresErrorCodes.UniqueViolation}) { throw new HierarchyConflictException(ErrorCatalog.MeterConflict.Create()); }
    }
    public async Task<MeterResponse?> Create(Guid property, CreateMeterRequest request, AccessScope access, CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(token); var p=await Lock(property,access,token);if(p is null)return null;
        var hash=Hash(new{request.Name,request.Kind,request.UnitId});var old=await db.Meters.SingleOrDefaultAsync(m=>m.PropertyId==property&&m.RequestId==request.RequestId,token);
        if(old is not null){if(old.RequestFingerprint!=hash)throw new HierarchyConflictException(ErrorCatalog.MeterConflict.Create());await tx.CommitAsync(token);return Response(old,true);}
        Active(p);
        if(request.UnitId.HasValue&&!await db.Units.AnyAsync(u=>u.Id==request.UnitId&&u.PropertyId==property&&u.IsEnabled,token))throw new HierarchyConflictException(ErrorCatalog.MeterInvalidUnit.Create());
        var m=new Meter(property,request.UnitId,request.RequestId,hash,request.Name!,Enum.Parse<MeterKind>(request.Kind!));db.Add(m);await Save(p,token);await tx.CommitAsync(token);return Response(m);
    }
    public async Task<MeterPage?> List(Guid property,int page,int size,AccessScope access,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        if(!await Properties(access).AnyAsync(p=>p.Id==property,token))return null;
        var q=db.Meters.AsNoTracking().Where(m=>m.PropertyId==property);var count=await q.CountAsync(token);var rows=await q.OrderBy(m=>m.Name).ThenBy(m=>m.Id).Skip((page-1)*size).Take(size).ToListAsync(token);
        await tx.CommitAsync(token);return new(rows.Select(m=>Response(m)).ToArray(),page,size,count);
    }
    private async Task<ReadingResponse> Response(MeterReading r,bool replay,CancellationToken token)
    {
        var previous=await db.MeterReadings.AsNoTracking().Where(x=>x.PropertyId==r.PropertyId&&x.MeterId==r.MeterId&&x.ReadingDate<r.ReadingDate).OrderByDescending(x=>x.ReadingDate).Select(x=>(decimal?)x.Value).FirstOrDefaultAsync(token);
        var revisions=await db.MeterReadingRevisions.AsNoTracking().Where(x=>x.PropertyId==r.PropertyId&&x.ReadingId==r.Id).OrderByDescending(x=>x.Sequence).Select(x=>new ReadingRevisionResponse(x.Id,MeterService.Number(x.Value),x.Sequence,x.Reason,x.CreatedAt)).ToListAsync(token);
        return new(r.Id,r.PropertyId,r.MeterId,r.ReadingDate,MeterService.Number(r.Value),previous.HasValue?MeterService.Number(r.Value-previous.Value):null,r.Version,revisions,replay);
    }
    public async Task<ReadingResponse?> Save(Guid property,Guid meter,Guid? reading,ReadingCommand request,AccessScope access,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(token);var p=await Lock(property,access,token);if(p is null)return null;
        var m=await db.Meters.SingleOrDefaultAsync(x=>x.PropertyId==property&&x.Id==meter,token);if(m is null)return null;
        var hash=Hash(new{meter,reading,date=request.Date.ToString("yyyy-MM-dd"),value=MeterService.Number(request.Value),request.Reason,request.Version});
        var old=await db.MeterReadingRevisions.AsNoTracking().SingleOrDefaultAsync(x=>x.PropertyId==property&&x.RequestId==request.RequestId,token);
        if(old is not null){if(old.RequestFingerprint!=hash)throw new HierarchyConflictException(ErrorCatalog.MeterConflict.Create());var current=await db.MeterReadings.AsNoTracking().SingleAsync(x=>x.PropertyId==property&&x.Id==old.ReadingId,token);var replay=await Response(current,true,token);await tx.CommitAsync(token);return replay;}
        Active(p);if(!m.IsEnabled)throw new HierarchyConflictException(ErrorCatalog.MeterInactive.Create());
        if(request.Date>BusinessDate.Today(clock))throw new RequestValidationException(new(){["readingDate"]=[ErrorCatalog.ReadingInvalid.Create()]});
        MeterReading r;
        if(reading.HasValue){var existing=await db.MeterReadings.SingleOrDefaultAsync(x=>x.PropertyId==property&&x.MeterId==meter&&x.Id==reading,token);if(existing is null)return null;r=existing;if(r.Version!=request.Version)throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());if(r.ReadingDate!=request.Date)throw new RequestValidationException(new(){["readingDate"]=[ErrorCatalog.ReadingInvalid.Create()]});}
        else {if(await db.MeterReadings.AnyAsync(x=>x.PropertyId==property&&x.MeterId==meter&&x.ReadingDate==request.Date,token))throw new HierarchyConflictException(ErrorCatalog.MeterConflict.Create());r=new(property,meter,request.Date);db.Add(r);}
        var neighbours=db.MeterReadings.Where(x=>x.PropertyId==property&&x.MeterId==meter&&x.Id!=r.Id);
        var before=await neighbours.Where(x=>x.ReadingDate<request.Date).OrderByDescending(x=>x.ReadingDate).Select(x=>(decimal?)x.Value).FirstOrDefaultAsync(token);
        var after=await neighbours.Where(x=>x.ReadingDate>request.Date).OrderBy(x=>x.ReadingDate).Select(x=>(decimal?)x.Value).FirstOrDefaultAsync(token);
        if((before.HasValue&&request.Value<before)||(after.HasValue&&request.Value>after))throw new HierarchyConflictException(ErrorCatalog.ReadingOrder.Create());
        db.Add(r.Revise(request.RequestId,hash,request.Value,request.Reason));await Save(p,token);var result=await Response(r,false,token);await tx.CommitAsync(token);return result;
    }
    public async Task<ReadingPage?> Readings(Guid property,Guid meter,int page,int size,AccessScope access,CancellationToken token)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
        if(!await Properties(access).AnyAsync(p=>p.Id==property,token)||!await db.Meters.AnyAsync(m=>m.PropertyId==property&&m.Id==meter,token))return null;
        var q=db.MeterReadings.AsNoTracking().Where(r=>r.PropertyId==property&&r.MeterId==meter);var count=await q.CountAsync(token);var rows=await q.OrderByDescending(r=>r.ReadingDate).Skip((page-1)*size).Take(size).ToListAsync(token);List<ReadingResponse> results=[];
        foreach(var r in rows)results.Add(await Response(r,false,token));await tx.CommitAsync(token);return new(results,page,size,count);
    }
}
