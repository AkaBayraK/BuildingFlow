using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;
public sealed record CommonAreaClosureRequest(Guid RequestId,Guid MaintenancePlanId,long MaintenanceVersion,string? Date,string? StartsAt,string? EndsAt,string? Notice);
public sealed record CommonAreaClosureReleaseRequest(long Version,string? Reason);
public sealed record CommonAreaClosureCommand(Guid RequestId,string Fingerprint,Guid Plan,long PlanVersion,DateOnly Date,TimeOnly Start,TimeOnly End,string Notice);
public sealed record CommonAreaClosureResponse(Guid Id,Guid PropertyId,Guid CommonAreaId,Guid MaintenancePlanId,string Date,string StartsAt,string EndsAt,string Notice,
    Guid AnnouncementId,DateTimeOffset? ReleasedAt,string? ReleaseReason,long Version,int NotificationCount,bool Replayed=false);
public interface ICommonAreaClosureStore
{
    Task<CommonAreaClosureResponse?> Close(Guid property,Guid area,CommonAreaClosureCommand command,CancellationToken token);
    Task<CommonAreaClosureResponse?> Release(Guid property,Guid area,Guid id,long version,string reason,CancellationToken token);
    Task<IReadOnlyList<CommonAreaClosureResponse>?> Closures(Guid property,Guid area,DateOnly date,CancellationToken token);
}
public sealed class CommonAreaClosureService(ICommonAreaClosureStore store,ICurrentUserService actor)
{
    private void Manager(){if(!actor.IsAuthenticated||string.IsNullOrWhiteSpace(actor.UserId)||string.IsNullOrWhiteSpace(actor.Issuer)||!new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER"}.Any(actor.IsInRole))throw new UnauthorizedAccessException();}
    public static void Invalid(string field)=>throw new RequestValidationException(new(){[field]=[ErrorCatalog.CommonAreaMaintenanceInvalid.Create()]});
    public Task<CommonAreaClosureResponse?> Close(Guid property,Guid area,CommonAreaClosureRequest r,CancellationToken token)
    {
        Manager();if(r.RequestId==Guid.Empty||r.MaintenancePlanId==Guid.Empty||r.MaintenanceVersion<1)Invalid("maintenancePlanId");
        var date=CommonAreaReservationService.ParseDate(r.Date,"date");
        if(!TimeOnly.TryParseExact(r.StartsAt,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var start)||!CommonAreaReservation.OnSlot(start))Invalid("startsAt");
        if(!TimeOnly.TryParseExact(r.EndsAt,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var end)||!CommonAreaReservation.OnSlot(end)||start>=end)Invalid("endsAt");
        if(string.IsNullOrWhiteSpace(r.Notice)||r.Notice.Trim().Length>1000)Invalid("notice");
        var notice=r.Notice!.Trim();var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{property,area,actor=actor.UserId,issuer=actor.Issuer,r.MaintenancePlanId,r.MaintenanceVersion,r.Date,r.StartsAt,r.EndsAt,notice}))));
        return store.Close(property,area,new(r.RequestId,hash,r.MaintenancePlanId,r.MaintenanceVersion,date,start,end,notice),token);
    }
    public Task<CommonAreaClosureResponse?> Release(Guid property,Guid area,Guid id,CommonAreaClosureReleaseRequest r,CancellationToken token)
    {Manager();if(r.Version<1||string.IsNullOrWhiteSpace(r.Reason)||r.Reason.Trim().Length>500)Invalid("reason");return store.Release(property,area,id,r.Version,r.Reason!.Trim(),token);}
    public Task<IReadOnlyList<CommonAreaClosureResponse>?> List(Guid property,Guid area,string date,CancellationToken token)
    {Manager();return store.Closures(property,area,CommonAreaReservationService.ParseDate(date,"date"),token);}
}
