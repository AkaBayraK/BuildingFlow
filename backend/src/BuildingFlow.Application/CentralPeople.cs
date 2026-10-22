using System.Text.Json.Serialization;
namespace BuildingFlow.Application;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CentralPersonProfile(string? Kind,string? Citizenship,string? CountryCode,string? IdentityNumber,string? FirstName,string? LastName,DateOnly? BirthDate=null,string? Gender=null,string? PhoneNumber=null,string? Email=null) {
 public CentralPersonProfile Normalized()=>this with {Kind=Kind?.Trim(),Citizenship=Citizenship?.Trim(),CountryCode=CountryCode?.Trim().ToUpperInvariant(),IdentityNumber=IdentityNumber?.Trim(),FirstName=FirstName?.Trim(),LastName=LastName?.Trim(),PhoneNumber=string.IsNullOrWhiteSpace(PhoneNumber)?null:PhoneNumber.Trim(),Email=string.IsNullOrWhiteSpace(Email)?null:Email.Trim()};
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateCentralPersonRequest(Guid OperationId,CentralPersonProfile Profile);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateCentralPersonRequest(Guid OperationId,long ExpectedVersion,CentralPersonProfile Profile,bool IsEnabled);
public sealed record CentralPersonSummary(Guid Id,Guid TenantId,string DisplayName,string Kind,bool IsEnabled,long Version,DateTimeOffset CreatedAt);
public sealed record CentralPersonDetail(Guid Id,Guid TenantId,CentralPersonProfile Profile,bool IsEnabled,long Version,DateTimeOffset CreatedAt);
public sealed record SelfPerson(Guid Id,Guid TenantId,string DisplayName,string Kind,string MaskedIdentity,bool CanEdit=false);
public sealed record CentralPersonMutation(CentralPersonDetail Record,bool Replayed,long AppliedVersion);
public interface ICentralPeopleStore {
 Task<PagedRows<CentralPersonSummary>?> ListAsync(Guid tenant,PeopleQuery query,CancellationToken token);
 Task<CentralPersonDetail?> GetAsync(Guid tenant,Guid person,CancellationToken token);
 Task<SelfPerson?> SelfAsync(Guid tenant,CancellationToken token);
 Task<CentralPersonMutation?> WriteAsync(Guid tenant,Guid? person,Guid operation,long expectedVersion,CentralPersonProfile profile,bool enabled,CancellationToken token);
}
public sealed class CentralPeopleService(ICentralPeopleStore store,TimeProvider clock) {
 private static CentralPersonProfile Validate(CentralPersonProfile? profile){
  if(profile==null)throw new ArgumentException("Person profile required.");var p=profile.Normalized();
  if(new[]{p.FirstName,p.LastName,p.IdentityNumber,p.PhoneNumber,p.Email}.Any(s=>s?.Any(char.IsControl)==true))throw new ArgumentException("Invalid profile characters.");
  var (first,last)=PersonNames.Apply(p.FirstName,p.LastName,p.Kind,p.Citizenship);return p with{FirstName=first,LastName=last};
 }
 private void ValidateProfile(CentralPersonProfile profile)=>PersonProfileValidation.Validate(profile.FirstName,profile.LastName,profile.PhoneNumber,profile.Email,profile.Kind,profile.Citizenship,profile.CountryCode,profile.IdentityNumber,profile.BirthDate,profile.Gender,null,null,clock,false);
 public Task<PagedRows<CentralPersonSummary>?> ListAsync(Guid tenant,PeopleQuery q,CancellationToken token){
  if(q.Page is <1 or >1000000 || q.PageSize is <1 or >100 || q.Search?.Length>200 || q.SortBy is not("name" or "createdAt") || q.SortDirection is not("asc" or "desc"))throw new ArgumentException("Invalid people query.");return store.ListAsync(tenant,q,token);
 }
 public Task<CentralPersonDetail?> GetAsync(Guid tenant,Guid person,CancellationToken token)=>store.GetAsync(tenant,person,token);
 public Task<SelfPerson?> SelfAsync(Guid tenant,CancellationToken token)=>store.SelfAsync(tenant,token);
 public Task<CentralPersonMutation?> CreateAsync(Guid tenant,CreateCentralPersonRequest c,CancellationToken token){if(c.OperationId==Guid.Empty)throw new ArgumentException("Operation required.");var p=Validate(c.Profile);ValidateProfile(p);return store.WriteAsync(tenant,null,c.OperationId,0,p,true,token);}
 public Task<CentralPersonMutation?> UpdateAsync(Guid tenant,Guid person,UpdateCentralPersonRequest c,CancellationToken token){if(c.OperationId==Guid.Empty || c.ExpectedVersion<1)throw new ArgumentException("Operation and version required.");var p=Validate(c.Profile);ValidateProfile(p);return store.WriteAsync(tenant,person,c.OperationId,c.ExpectedVersion,p,c.IsEnabled,token);}
}

// Duplicate rule (user decision 22.10.2026): identity among non-deleted people, phone/email among active ones; the conflict never names the other person.
public static class PersonDuplicates
{
    public static string IdentityKey(string identity)=>identity.Trim().ToUpperInvariant();
    public static string? EmailKey(string? email)=>string.IsNullOrWhiteSpace(email)?null:email.Trim().ToLowerInvariant();
    public static ErrorDefinition? Contact(string? phone,string? email,IEnumerable<(string? Phone,string? Email)> others)
    {
        var p=SmsText.NormalizePhone(phone);var e=EmailKey(email);var rows=others.ToArray();
        if(p!=null && rows.Any(o=>SmsText.NormalizePhone(o.Phone)==p))return ErrorCatalog.PersonDuplicatePhone;
        if(e!=null && rows.Any(o=>EmailKey(o.Email)==e))return ErrorCatalog.PersonDuplicateEmail;
        return null;
    }
}
