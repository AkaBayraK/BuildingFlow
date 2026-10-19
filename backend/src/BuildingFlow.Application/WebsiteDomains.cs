namespace BuildingFlow.Application;
public sealed record WebsiteDomainCreate(Guid RequestId,string HostName,string Kind);
public sealed record WebsiteDomainAction(long Version);
public sealed record WebsiteDomainDto(Guid Id,string HostName,string Kind,string Status,bool IsPrimary,DateTimeOffset? VerifiedAt,DateTimeOffset? LastCheckedAt,DateTimeOffset? TlsCheckedAt,DateTimeOffset? ApprovedAt,long Version,string? VerificationToken=null,bool Replayed=false);
public sealed record PublicWebsiteDomain(string SiteKey,string HostName,bool IsPrimary);
public interface IWebsiteDomainVerifier{Task<(bool Dns,bool Tls)> Check(string host,string tokenHash,CancellationToken t);}
public interface IWebsiteDomainStore{Task<IReadOnlyList<WebsiteDomainDto>?> List(Guid p,AccessScope a,CancellationToken t);Task<WebsiteDomainDto?> Create(Guid p,WebsiteDomainCreate r,AccessScope a,CancellationToken t);Task<WebsiteDomainDto?> Act(Guid p,Guid id,string action,long version,AccessScope a,CancellationToken t);Task<int> Recheck(int limit,CancellationToken t);}
public interface IPublicWebsiteDomainStore{Task<PublicWebsiteDomain?> Resolve(string host,CancellationToken t);Task<string?> Primary(string key,CancellationToken t);}
public sealed class WebsiteDomainService(IWebsiteDomainStore store,ICurrentUserService user){
 private AccessScope Access(bool approve=false){if(!user.IsAuthenticated||string.IsNullOrWhiteSpace(user.UserId)||!(approve?user.IsInRole("SUPER_ADMIN"):new[]{"SUPER_ADMIN","SITE_ADMIN"}.Any(user.IsInRole)))throw new UnauthorizedAccessException();return new(user.UserId,user.IsInRole("SUPER_ADMIN"));}
 public Task<IReadOnlyList<WebsiteDomainDto>?> List(Guid p,CancellationToken t)=>store.List(p,Access(),t);
 public Task<WebsiteDomainDto?> Create(Guid p,WebsiteDomainCreate r,CancellationToken t){var a=Access();try{r=r with{HostName=DomainHost(r.HostName)};if(r.RequestId==Guid.Empty||r.Kind is not("CUSTOM" or "PLATFORM_SUBDOMAIN"))Invalid();}catch(ArgumentException){Invalid();}return store.Create(p,r,a,t);}
 public static string DomainHost(string host)=>BuildingFlow.Domain.WebsiteDomain.Normalize(host);
 public Task<WebsiteDomainDto?> Act(Guid p,Guid id,string action,long version,CancellationToken t){var a=Access(action=="APPROVE");if(version<1)Invalid();return store.Act(p,id,action,version,a,t);}
 public static void Invalid()=>throw new RequestValidationException(new(){["domain"]=[ErrorCatalog.WebsiteDomainInvalid.Create()]});
}
