using System.Text.Json;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;
public sealed record ListingEdit(Guid RequestId,Guid? UnitId,string Kind,string Title,string Description,decimal Price,string RoomLayout,decimal GrossArea,decimal NetArea,string? FloorLabel,string? BuildingLabel,string[] Features,long Version=0);
public sealed record ListingAction(long Version,string? Reason=null,bool ComplianceConfirmed=false,DateTimeOffset? PublishUntil=null);
public sealed record ListingPhotos(long Version,Guid[] MediaIds);
public sealed record ListingConsent(long Version,string Kind,string? AgentLicenseNo,string FileName,string ContentType,byte[] Bytes);
public sealed record ListingDto(Guid Id,Guid? UnitId,string Kind,string Title,string Description,decimal Price,string RoomLayout,decimal GrossArea,decimal NetArea,string? FloorLabel,string? BuildingLabel,string[] Features,string Status,DateTimeOffset? PublishUntil,string? ConsentKind,string? AgentLicenseNo,string? ComplianceNote,long Version,Guid[] MediaIds,bool Replayed=false);
public sealed record ListingEventDto(Guid Id,string Action,string? Note,long ListingVersion,DateTimeOffset CreatedAt,string CreatedBy);
public sealed record PublicListing(Guid Id,string Kind,string Title,string Description,decimal Price,string Currency,string? PricePeriod,string RoomLayout,decimal GrossArea,decimal NetArea,string? FloorLabel,string? BuildingLabel,string[] Features,IReadOnlyList<PublicWebsiteMedia> Photos);
public sealed record ListingUnitOption(Guid Id,string Label);
public interface IListingStore{
 Task<IReadOnlyList<ListingUnitOption>?> Units(Guid property,AccessScope access,CancellationToken t);
 Task<ListingDto?> Get(Guid property,Guid id,AccessScope access,CancellationToken t);
 Task<IReadOnlyList<ListingDto>?> List(Guid property,AccessScope access,CancellationToken t);
 Task<ListingDto?> Save(Guid property,Guid? id,ListingEdit r,AccessScope access,CancellationToken t);
 Task<ListingDto?> Photos(Guid property,Guid id,ListingPhotos r,AccessScope access,CancellationToken t);
 Task<ListingDto?> Consent(Guid property,Guid id,ListingConsent r,AccessScope access,CancellationToken t);
 Task<WebsiteMediaContent?> ConsentContent(Guid property,Guid id,AccessScope access,CancellationToken t);
 Task<ListingDto?> Act(Guid property,Guid id,string action,ListingAction r,AccessScope access,CancellationToken t);
 Task<bool> Delete(Guid property,Guid id,long version,AccessScope access,CancellationToken t);
 Task<IReadOnlyList<ListingEventDto>?> Events(Guid property,Guid id,AccessScope access,CancellationToken t);
}
public interface IPublicListingStore{Task<IReadOnlyList<PublicListing>?> List(string key,string? kind,int page,CancellationToken t);Task<PublicListing?> Get(string key,Guid id,CancellationToken t);}
// A human declaration is not an EIDS result. No production verifier is shipped.
public sealed record ListingAuthorityVerification(string Reference,DateTimeOffset ValidUntil);
public interface IListingAuthorityVerifier{Task<ListingAuthorityVerification?> Verify(Guid property,Guid listing,CancellationToken token);}
public sealed class UnavailableListingAuthorityVerifier:IListingAuthorityVerifier{public Task<ListingAuthorityVerification?> Verify(Guid p,Guid l,CancellationToken t)=>Task.FromResult<ListingAuthorityVerification?>(null);}
public static class ListingValidation{
 public static readonly string[] FeatureCodes=["CLOSED_PARKING","PARKING","BALCONY","POOL_VIEW","ELEVATOR","SECURITY","GARDEN"];
 public static ListingEdit Normalize(ListingEdit r){var features=(r.Features??[]).Distinct().Order().ToArray();if(r.RequestId==Guid.Empty||features.Any(x=>!FeatureCodes.Contains(x))||r.UnitId==Guid.Empty)Invalid();r=r with {Title=r.Title?.Trim()??"",Description=r.Description?.Trim()??"",RoomLayout=r.RoomLayout??"",FloorLabel=string.IsNullOrWhiteSpace(r.FloorLabel)?null:r.FloorLabel.Trim(),BuildingLabel=string.IsNullOrWhiteSpace(r.BuildingLabel)?null:r.BuildingLabel.Trim(),Features=features};try{var l=new PropertyListing(Guid.NewGuid(),Guid.NewGuid(),r.RequestId,"x");l.Edit(r.UnitId,r.Kind,r.Title,r.Description,r.Price,r.RoomLayout,r.GrossArea,r.NetArea,r.FloorLabel,r.BuildingLabel,JsonSerializer.Serialize(features));}catch(ArgumentException){Invalid();}return r;}
 public static void Invalid()=>throw new RequestValidationException(new(){["listing"]=[ErrorCatalog.ListingInvalid.Create()]});
}
public sealed class ListingService(IListingStore store,ICurrentUserService user){
 private AccessScope Access(bool publish=false){if(!user.IsAuthenticated||string.IsNullOrWhiteSpace(user.UserId)||!(publish?new[]{"SUPER_ADMIN","SITE_ADMIN"}:new[]{"SUPER_ADMIN","SITE_ADMIN","MANAGER"}).Any(user.IsInRole))throw new UnauthorizedAccessException();return new(user.UserId,user.IsInRole("SUPER_ADMIN"));}
 public Task<IReadOnlyList<ListingUnitOption>?> Units(Guid p,CancellationToken t)=>store.Units(p,Access(),t);
 public Task<ListingDto?> Get(Guid p,Guid id,CancellationToken t)=>store.Get(p,id,Access(),t);
 public Task<IReadOnlyList<ListingDto>?> List(Guid p,CancellationToken t)=>store.List(p,Access(),t);
 public Task<ListingDto?> Save(Guid p,Guid? id,ListingEdit r,CancellationToken t){var a=Access();r=ListingValidation.Normalize(r);if(id is not null&&r.Version<1)ListingValidation.Invalid();return store.Save(p,id,r,a,t);}
 public Task<ListingDto?> Photos(Guid p,Guid id,ListingPhotos r,CancellationToken t){var a=Access();if(r.Version<1||r.MediaIds is null||r.MediaIds.Length>20||r.MediaIds.Distinct().Count()!=r.MediaIds.Length)ListingValidation.Invalid();return store.Photos(p,id,r,a,t);}
 public Task<ListingDto?> Consent(Guid p,Guid id,ListingConsent r,CancellationToken t){var a=Access();if(r.Version<1||r.Kind is not("WRITTEN_STATEMENT" or "AUTHORIZED_AGENT")||r.Kind=="AUTHORIZED_AGENT"&&string.IsNullOrWhiteSpace(r.AgentLicenseNo)||r.AgentLicenseNo?.Length>60)ListingValidation.Invalid();CommunicationService.ValidateFile(r.FileName,r.ContentType,r.Bytes);return store.Consent(p,id,r,a,t);}
 public Task<WebsiteMediaContent?> ConsentContent(Guid p,Guid id,CancellationToken t)=>store.ConsentContent(p,id,Access(),t);
 public Task<ListingDto?> Act(Guid p,Guid id,string action,ListingAction r,CancellationToken t){var a=Access(action!="SUBMIT");if(r.Version<1||r.Reason?.Length>500||action is "APPROVE" or "REJECT" or "WITHDRAW"&&string.IsNullOrWhiteSpace(r.Reason))ListingValidation.Invalid();if(action=="APPROVE"&&!r.ComplianceConfirmed)throw new HierarchyConflictException(ErrorCatalog.ListingCompliance.Create());return store.Act(p,id,action,r,a,t);}
 public Task<bool> Delete(Guid p,Guid id,long v,CancellationToken t){var a=Access();if(v<1)ListingValidation.Invalid();return store.Delete(p,id,v,a,t);}
 public Task<IReadOnlyList<ListingEventDto>?> Events(Guid p,Guid id,CancellationToken t)=>store.Events(p,id,Access(),t);
}
