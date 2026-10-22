using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed partial class UserManagementTests
{
    [PostgresFact] public async Task NewUserPhoneIsSharedByWebAndMobileWithoutChangingKeycloakOrPhotoConsent()
    {
        var x=await Seed();var kc=new FakeKeycloak();
        await PhotoClient(kc,x.Actor,"SUPER_ADMIN",async admin=>{
            var command=new CreateUserRequest("phone.owner","phone.owner@example.test","Ada","Kaya",Roles:["MANAGER"],Phone:"+90 555 123 45 67");
            using var response=await admin.PostAsJsonAsync("/api/v1/users",command);Assert.Equal(HttpStatusCode.Created,response.StatusCode);
            var created=(await response.Content.ReadFromJsonAsync<UserResponse>())!;
            using var owner=TestToken.Client(factory,created.Id,"MANAGER");
            var mobile=(await owner.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!;
            Assert.Equal(command.Phone,mobile.Phone);Assert.Equal("Ada",mobile.FirstName);Assert.Equal("KAYA",mobile.LastName);Assert.Equal(1,mobile.Version);Assert.Null(mobile.PhotoId);
            using var account=await owner.GetAsync("/api/v1/account");var json=await account.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal(command.Phone,json.GetProperty("profile").GetProperty("phone").GetString());
            Assert.DoesNotContain("\"phone\":",await response.Content.ReadAsStringAsync(),StringComparison.OrdinalIgnoreCase);
            var changed=new MobileProfileCommand(Guid.NewGuid(),mobile.Version,mobile.FirstName,mobile.LastName,"+90 555 987 65 43","KEEP",null);
            Assert.Equal(HttpStatusCode.OK,(await owner.PostAsJsonAsync("/api/v1/account/profile/commands",changed)).StatusCode);
            Assert.Equal(changed.Phone,(await owner.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.Phone);
            var consent=await ConsentTestHelper.ItemAsync(owner,"PROFILE_PHOTO");Assert.NotEqual(ConsentStates.Active,consent.Status.State);
            await factory.WithDb(async db=>{var p=await db.Set<MobileProfile>().SingleAsync(p=>p.OwnerUserId==created.Id);Assert.Equal(x.Actor,p.CreatedBy);});
            // Existing clients can omit the field and still start with profile version0.
            using var oldResponse=await admin.PostAsJsonAsync("/api/v1/users",new CreateUserRequest("old.owner","old.owner@example.test","Old","Owner",Roles:["RESIDENT"]));
            Assert.Equal(HttpStatusCode.Created,oldResponse.StatusCode);var old=(await oldResponse.Content.ReadFromJsonAsync<UserResponse>())!;
            using var oldOwner=TestToken.Client(factory,old.Id,"RESIDENT");Assert.Equal(0,(await oldOwner.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.Version);
        });
    }
    [PostgresFact] public async Task NewUserPhoneValidationAndRolesRunBeforeIdentityCreation()
    {
        var x=await Seed();var other=await Seed();var kc=new FakeKeycloak();
        await PhotoClient(kc,x.Actor,"SITE_ADMIN",async admin=>{
            foreach(var phone in new[]{"abc",new string('5',33),"123456","+90\n5551234567",new string('5',16)}){
                using var r=await admin.PostAsJsonAsync("/api/v1/users",new CreateUserRequest("invalid.phone","invalid@example.test","Ada","Kaya",Roles:["MANAGER"],PropertyId:x.Property,Phone:phone));
                Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);Assert.Contains("BF_USER_INVALID",await r.Content.ReadAsStringAsync());Assert.Empty(kc.Users);
            }
            using var longName=await admin.PostAsJsonAsync("/api/v1/users",new CreateUserRequest("long.name","long@example.test",new string('a',81),"Kaya",Roles:["MANAGER"],PropertyId:x.Property,Phone:"+905551234567"));Assert.Equal(HttpStatusCode.BadRequest,longName.StatusCode);Assert.Empty(kc.Users);
            using var foreign=await admin.PostAsJsonAsync("/api/v1/users",new CreateUserRequest("foreign.user","foreign@example.test","Ada","Kaya",Roles:["MANAGER"],PropertyId:other.Property,Phone:"+905551234567"));Assert.Equal(HttpStatusCode.BadRequest,foreign.StatusCode);Assert.Empty(kc.Users);
            using var ok=await admin.PostAsJsonAsync("/api/v1/users",new CreateUserRequest("site.owner","site.owner@example.test","Ada","Kaya",Roles:["RESIDENT"],PropertyId:x.Property,Phone:"+905551234567"));Assert.Equal(HttpStatusCode.Created,ok.StatusCode);
            var user=(await ok.Content.ReadFromJsonAsync<UserResponse>())!;
            await factory.WithDb(async db=>{Assert.True(await db.PropertyMemberships.AnyAsync(m=>m.PropertyId==x.Property&&m.UserId==user.Id));Assert.Equal("+905551234567",(await db.Set<MobileProfile>().SingleAsync(p=>p.OwnerUserId==user.Id)).Phone);});
        });
        foreach(var role in new[]{"MANAGER","ACCOUNTANT","RESIDENT","spoofed","client-role"})await PhotoClient(kc,x.Actor,role,async client=>{
            Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/v1/users",new CreateUserRequest("denied.user","denied@example.test","Ada","Kaya",Phone:"+905551234567"))).StatusCode);
        });
    }
    [PostgresFact] public async Task NewUserProfileInitializationCannotOverwriteAnExistingProfileOrLeaveMembershipOnFailure()
    {
        var x=await Seed();var id=Guid.NewGuid().ToString();
        await factory.WithDb(async db=>{var p=new MobileProfile(id);p.Edit("Existing","OWNER","+905550000000",null);db.Add(p);await db.SaveChangesAsync();});
        using(var scope=factory.Services.CreateScope()){
            var store=scope.ServiceProvider.GetRequiredService<IUserDirectoryStore>();
            await Assert.ThrowsAsync<HierarchyConflictException>(()=>store.InitializeCreatedAccountAsync(x.Property,id,"New","OWNER","+905551234567",default));
        }
        await factory.WithDb(async db=>{Assert.False(await db.PropertyMemberships.AnyAsync(m=>m.PropertyId==x.Property&&m.UserId==id));Assert.Equal("+905550000000",(await db.Set<MobileProfile>().SingleAsync(p=>p.OwnerUserId==id)).Phone);});
    }
}
