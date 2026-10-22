using System.Security.Cryptography;
using System.Text.Json.Serialization;
namespace BuildingFlow.Application;
public sealed record IssuedPersonLinkCode(string Code,string Hint,DateTimeOffset ExpiresAt);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LinkMyPersonRequest(string? Code,string? FirstName,string? LastName,string? PhoneNumber,string? Email);
public sealed record LinkedCentralPerson(Guid TenantId,string TenantName,SelfPerson Person,DateTimeOffset LinkedAt);
public sealed class DuplicateLinkCodeException():Exception("Link code collision.");
public interface IPersonAccountLinkStore
{
    Task<IssuedPersonLinkCode?> IssueAsync(Guid tenant,Guid person,string code,string hash,CancellationToken token);
    Task<PersonAccountStatus?> EndAsync(Guid tenant,Guid person,CancellationToken token);
    Task<LinkedCentralPerson?> LinkAsync(string hash,PersonIdentity identity,CancellationToken token);
}
// The code proves a manager-approved link; name/phone only narrow it further and never create a link alone.
public sealed class PersonAccountLinkService(IPersonAccountLinkStore store,IMobileOnboardingStore attempts,IPersonCodeHasher hasher,ICurrentUserService user)
{
    private const int CollisionRetries=5;
    public async Task<IssuedPersonLinkCode?> IssueAsync(Guid tenant,Guid person,CancellationToken token)
    {
        for(var attempt=0;;attempt++)
        {
            var code=PersonCodePolicy.Generate(RandomNumberGenerator.GetInt32);
            try{return await store.IssueAsync(tenant,person,code,hasher.Hash(code),token);}
            catch(DuplicateLinkCodeException) when(attempt<CollisionRetries){}
        }
    }
    public Task<PersonAccountStatus?> EndAsync(Guid tenant,Guid person,CancellationToken token)=>store.EndAsync(tenant,person,token);
    public async Task<LinkedCentralPerson> LinkAsync(LinkMyPersonRequest request,CancellationToken token)
    {
        if(!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || string.IsNullOrWhiteSpace(user.Issuer))throw new UnauthorizedAccessException();
        var identity=MobileOnboardingService.Identity(request.FirstName,request.LastName,request.PhoneNumber,request.Email,false);
        if(await attempts.IsLockedAsync(user.UserId,token))throw new HierarchyConflictException(ErrorCatalog.MobileLinkLocked.Create());
        var code=request.Code?.Trim()??"";
        var linked=PersonCodePolicy.IsWellFormed(code)?await store.LinkAsync(hasher.Hash(code),identity,token):null;
        if(linked!=null)return linked;
        await attempts.RecordFailureAsync(user.UserId,token);
        throw new HierarchyConflictException(ErrorCatalog.MobileLinkRejected.Create());
    }
}
