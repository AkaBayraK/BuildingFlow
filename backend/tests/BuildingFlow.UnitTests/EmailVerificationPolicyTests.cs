using BuildingFlow.Application;
using Xunit;
namespace BuildingFlow.UnitTests;

public sealed class EmailVerificationPolicyTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task NormalAndTemporaryInvitationsFollowSuccessfulLocalCreation(bool temporary)
    {
        var kc=new Identity();var store=new Directory();
        var service=new UserService(kc,store,new Actor(),emailPolicy:new(true));
        var result=await service.CreateAsync(new("new.owner","new@example.test","Test","Owner",TemporaryPassword:temporary?"StrongPassword123":null),default);
        Assert.Equal("ACCEPTED",result.InvitationStatus);Assert.False(result.EmailVerified);
        Assert.Equal(new[]{"VERIFY_EMAIL","UPDATE_PASSWORD"},Assert.Single(kc.Mails));
        Assert.True(store.Initialized);Assert.False(kc.Deleted);
    }

    [Fact] public async Task FailedInvitationKeepsCreatedAccountAndReportsUnavailable()
    {
        var kc=new Identity{FailMail=true};var service=new UserService(kc,new Directory(),new Actor(),emailPolicy:new(true));
        var result=await service.CreateAsync(new("new.owner","new@example.test","Test","Owner"),default);
        Assert.Equal("UNAVAILABLE",result.InvitationStatus);Assert.False(result.EmailVerified);Assert.False(kc.Deleted);
        Assert.True(await service.SendPasswordResetAsync("target",default));
    }

    [Fact] public async Task FailedLocalInitializationSendsNoInvitationAndCompensatesIdentity()
    {
        var kc=new Identity();var service=new UserService(kc,new Directory{FailInitialize=true},new Actor(),emailPolicy:new(true));
        await Assert.ThrowsAsync<IdentityUnavailableException>(()=>service.CreateAsync(new("new.owner","new@example.test","Test","Owner"),default));
        Assert.Empty(kc.Mails);Assert.True(kc.Deleted);
    }

    [Fact] public async Task EmailChangeByAdministratorCannotBypassNativeOwnerFlow()
    {
        var kc=new Identity();var service=new UserService(kc,new Directory(),new Actor(),emailPolicy:new(true));
        await Assert.ThrowsAsync<RequestValidationException>(()=>service.UpdateAsync("target",new("changed@example.test","Test","Owner",true,[]),default));
        Assert.Equal(0,kc.Updates);Assert.True(kc.Person.EmailVerified);
        await service.UpdateAsync("target",new(kc.Person.Email,"Changed","Name",true,[]),default);
        Assert.Equal(1,kc.Updates);Assert.True(kc.Person.EmailVerified);
    }

    [Fact] public async Task ScopedAdministratorCannotInviteForeignAccountAndResidentCannotInvite()
    {
        var kc=new Identity();var service=new UserService(kc,new Directory(),new Actor("SITE_ADMIN"),emailPolicy:new(true));
        Assert.False(await service.SendPasswordResetAsync("target",default));Assert.Empty(kc.Mails);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>new UserService(kc,new Directory(),new Actor("RESIDENT"),emailPolicy:new(true)).SendPasswordResetAsync("target",default));
    }

    [Fact] public async Task ExistingVerifiedAccountResetDoesNotRequestRepeatedVerification()
    {
        var kc=new Identity();var service=new UserService(kc,new Directory(),new Actor(),emailPolicy:new(true));
        Assert.True(await service.SendPasswordResetAsync("target",default));
        Assert.Equal(new[]{"UPDATE_PASSWORD"},Assert.Single(kc.Mails));Assert.True(kc.Person.EmailVerified);
    }

    [Fact] public async Task MobileCreationUsesSharedIdentityAndDoesNotSendDuplicateInvitation()
    {
        var kc=new Identity();var store=new Onboarding();
        var service=new MobileOnboardingService(kc,store,new Hasher(),new Actor("RESIDENT"));
        var request=new MobileRegistrationRequest(Guid.NewGuid(),"mobile.owner","StrongPassword123","StrongPassword123","Test","Owner","+905551234567","mobile@example.test");
        var created=await service.RegisterAsync(request,"test-client",default);
        var replay=await service.RegisterAsync(request,"test-client",default);
        Assert.False(created.Replayed);Assert.True(replay.Replayed);Assert.Equal(1,kc.Creates);Assert.Empty(kc.Mails);
    }

    private sealed class Actor(string role="SUPER_ADMIN") : ICurrentUserService
    {public string? UserId=>"actor";public string? Username=>"actor";public bool IsAuthenticated=>true;public bool IsInRole(string value)=>value==role;}
    private sealed class Identity : IKeycloakAdmin
    {
        public KeycloakUser Person=new("target","target","original@example.test","Test","Owner",true,true,null);
        public List<string[]> Mails=[];public bool FailMail;public bool Deleted;public int Updates,Creates;
        public Task<(IReadOnlyList<KeycloakUser>,int)> SearchAsync(string? s,int f,int m,CancellationToken t)=>Task.FromResult<(IReadOnlyList<KeycloakUser>,int)>(([Person],1));
        public Task<KeycloakUser?> GetAsync(string id,CancellationToken t)=>Task.FromResult<KeycloakUser?>(Person);
        public Task<IReadOnlyList<string>> RealmRolesAsync(string id,CancellationToken t)=>Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string> CreateAsync(KeycloakUserDraft d,CancellationToken t){Creates++;Person=Person with{Email=d.Email,EmailVerified=false};return Task.FromResult(Person.Id);}
        public Task UpdateAsync(string id,KeycloakUserDraft d,CancellationToken t){Updates++;return Task.CompletedTask;}
        public Task ChangeRealmRolesAsync(string id,IReadOnlyCollection<string> a,IReadOnlyCollection<string> r,CancellationToken t)=>Task.CompletedTask;
        public Task SetPasswordAsync(string id,string p,bool temp,CancellationToken t)=>Task.CompletedTask;
        public Task SendActionsEmailAsync(string id,IReadOnlyCollection<string> a,CancellationToken t){Mails.Add(a.ToArray());if(FailMail&&Mails.Count==1)throw new IdentityUnavailableException();return Task.CompletedTask;}
        public Task DeleteAsync(string id,CancellationToken t){Deleted=true;return Task.CompletedTask;}
    }
    private sealed class Directory : IUserDirectoryStore
    {
        public bool Initialized,FailInitialize;
        public Task<IReadOnlyList<Guid>> ManagedPropertiesAsync(string a,CancellationToken t)=>Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<PagedRows<string>> MemberIdsAsync(IReadOnlyList<Guid> p,int page,int size,CancellationToken t)=>Task.FromResult(new PagedRows<string>([],page,size,0));
        public Task<IReadOnlySet<string>> MembersAmongAsync(IReadOnlyList<Guid>? p,IReadOnlyCollection<string> u,CancellationToken t)=>Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
        public Task<IReadOnlyDictionary<string,(int PropertyCount,IReadOnlyList<LinkedPerson> People)>> LinksAsync(IReadOnlyCollection<string> u,IReadOnlyList<Guid>? p,CancellationToken t)=>Task.FromResult<IReadOnlyDictionary<string,(int,IReadOnlyList<LinkedPerson>)>>(new Dictionary<string,(int,IReadOnlyList<LinkedPerson>)>());
        public Task<bool> PropertyExistsAsync(Guid p,CancellationToken t)=>Task.FromResult(true);
        public Task EnsureMembershipAsync(Guid p,string u,CancellationToken t)=>Task.CompletedTask;
        public Task InitializeCreatedAccountAsync(Guid? p,string u,string f,string l,string? phone,CancellationToken t){Initialized=true;if(FailInitialize)throw new IdentityUnavailableException();return Task.CompletedTask;}
        public Task<LinkedPerson?> LinkAsync(Guid p,Guid person,long v,string u,CancellationToken t)=>Task.FromResult<LinkedPerson?>(null);
        public Task<bool> UnlinkAsync(Guid p,Guid person,long v,string u,CancellationToken t)=>Task.FromResult(false);
    }
    private sealed class Hasher : IPersonCodeHasher {public string Hash(string code)=>"test";}
    private sealed class Onboarding : IMobileOnboardingStore
    {
        private StoredRegistration? saved;
        public Task<StoredRegistration?> RegistrationAsync(Guid id,CancellationToken t)=>Task.FromResult(saved);
        public Task<int> RecentRegistrationsAsync(string k,TimeSpan w,CancellationToken t)=>Task.FromResult(0);
        public Task SaveRegistrationAsync(Guid id,string fp,string user,string name,string k,PersonIdentity i,CancellationToken t){saved=new(fp,user,name);return Task.CompletedTask;}
        public Task<PersonLinkResult> LinkAsync(string u,string h,PersonIdentity i,CancellationToken t)=>Task.FromResult(new PersonLinkResult(PersonMatching.NotRequested));
        public Task<bool> IsLockedAsync(string u,CancellationToken t)=>Task.FromResult(false);
        public Task RecordFailureAsync(string u,CancellationToken t)=>Task.CompletedTask;
    }
}
