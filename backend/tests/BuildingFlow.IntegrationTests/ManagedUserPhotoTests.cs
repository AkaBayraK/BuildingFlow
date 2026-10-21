using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed partial class UserManagementTests
{
    private async Task PhotoClient(FakeKeycloak keycloak,string actor,string role,Func<HttpClient,Task> action)
    {
        using var app=factory.WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.RemoveAll<IKeycloakAdmin>();s.AddSingleton<IKeycloakAdmin>(keycloak);}));
        using var client=app.CreateClient();using var signed=TestToken.Client(factory,actor,role);
        client.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;
        await action(client);
    }
    private static async Task<UserPhotoResult> SavePhoto(HttpClient client,string path,UserPhotoCommand command)
    {
        using var response=await client.PostAsJsonAsync(path,command);
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserPhotoResult>())!;
    }
    [StorageFact] public async Task ManagedPhotoSharesMobilePointerPreservesDetailsAndChecksConsentReplayVersionAndAudit()
    {
        var x=await Seed();var kc=new FakeKeycloak();var target=kc.Add("photo.manager",["MANAGER"]);
        using var owner=TestToken.Client(factory,target,"MANAGER");
        var original=new MobileProfileCommand(Guid.NewGuid(),0,"Foto","Sahibi","+905550001234","KEEP",null);
        Assert.Equal(HttpStatusCode.OK,(await owner.PostAsJsonAsync("/api/v1/account/profile/commands",original)).StatusCode);
        var jpeg=Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures/avatar.jpg")));
        var path=$"/api/v1/users/{target}/photo";
        await PhotoClient(kc,x.Actor,"SUPER_ADMIN",async admin=>{
            Assert.False((await admin.GetFromJsonAsync<UserPhotoState>(path+"/state"))!.CanReplace);
            var command=new UserPhotoCommand(Guid.NewGuid(),1,"REPLACE",jpeg);
            using var denied=await admin.PostAsJsonAsync(path+"/commands",command);
            Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);Assert.Contains("BF_CONSENT_REQUIRED",await denied.Content.ReadAsStringAsync());
            await ConsentTestHelper.GrantAsync(factory,owner,"PROFILE_PHOTO");
            var pair=await Task.WhenAll(SavePhoto(admin,path+"/commands",command),SavePhoto(admin,path+"/commands",command));
            Assert.Contains(pair,r=>r.Replayed);Assert.Equal(pair[0].Photo.PhotoId,pair[1].Photo.PhotoId);
            var first=pair[0];Assert.Equal(2,first.Photo.Version);
            var mobile=(await owner.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!;
            Assert.Equal(first.Photo.PhotoId,mobile.PhotoId);Assert.Equal("Foto",mobile.FirstName);Assert.Equal("SAHİBİ",mobile.LastName);Assert.Equal(original.Phone,mobile.Phone);
            Assert.Equal(HttpStatusCode.OK,(await owner.GetAsync($"/api/v1/mobile/profile/photo/{mobile.PhotoId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(path+"/commands",command with{PhotoAction="REMOVE",PhotoBase64=null})).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict,(await admin.PostAsJsonAsync(path+"/commands",new UserPhotoCommand(Guid.NewGuid(),1,"REMOVE"))).StatusCode);
            var replacement=await SavePhoto(admin,path+"/commands",command with{OperationId=Guid.NewGuid(),ExpectedVersion=2});
            Assert.NotEqual(first.Photo.PhotoId,replacement.Photo.PhotoId);
            Assert.Equal(HttpStatusCode.NotFound,(await owner.GetAsync($"/api/v1/account/profile/photo/{first.Photo.PhotoId}")).StatusCode);
            await factory.WithDb(async db=>{
                var p=await db.Set<MobileProfile>().SingleAsync(p=>p.OwnerUserId==target);
                Assert.Equal(target,p.CreatedBy);Assert.Equal(x.Actor,p.ModifiedBy);
                var photo=await db.Set<MobileProfilePhoto>().SingleAsync(p=>p.Id==replacement.Photo.PhotoId);
                Assert.Equal(target,photo.OwnerUserId);Assert.Equal(x.Actor,photo.CreatedBy);
            });
            var json=await admin.GetStringAsync(path+"/state");Assert.DoesNotContain("phone",json,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain(original.Phone!,json);
            await ConsentTestHelper.PublishAsync(factory,"PROFILE_PHOTO","Yeni fotoğraf rızası.");
            Assert.False((await admin.GetFromJsonAsync<UserPhotoState>(path+"/state"))!.CanReplace);
            var removed=await SavePhoto(admin,path+"/commands",new(Guid.NewGuid(),replacement.Photo.Version,"REMOVE"));
            Assert.Null(removed.Photo.PhotoId);Assert.Null((await owner.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.PhotoId);
            Assert.Equal(HttpStatusCode.Forbidden,(await admin.PostAsJsonAsync(path+"/commands",command with{OperationId=Guid.NewGuid(),ExpectedVersion=removed.Photo.Version})).StatusCode);
            // An old acknowledged command may replay, but must never resurrect its retired photo after removal/withdrawal.
            Assert.True((await SavePhoto(admin,path+"/commands",command)).Replayed);
            Assert.Null((await owner.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.PhotoId);
        });
    }
    [StorageFact] public async Task SuperAdminPhotosCoverBothAdminRolesAndSiteAdminEditsOnlyOwnOrManagedAccounts()
    {
        var x=await Seed();var kc=new FakeKeycloak();var system=kc.Add("photo.system",["SUPER_ADMIN"]);var site=kc.Add("photo.site",["SITE_ADMIN"]);var manager=kc.Add("photo.manager",["MANAGER"]);
        kc.Add("photo.self",["SITE_ADMIN"],x.Actor);
        await factory.WithDb(async db=>{foreach(var id in new[]{system,site,manager})db.Add(new PropertyMembership(x.Property,id));await db.SaveChangesAsync();});
        var jpeg=Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures/avatar.jpg")));
        foreach(var (target,role) in new[]{(system,"SUPER_ADMIN"),(site,"SITE_ADMIN"),(manager,"MANAGER"),(x.Actor,"SITE_ADMIN")}){
            using var owner=TestToken.Client(factory,target,role);await ConsentTestHelper.GrantAsync(factory,owner,"PROFILE_PHOTO");
        }
        var commands=new Dictionary<string,UserPhotoCommand>();
        await PhotoClient(kc,x.Actor,"SUPER_ADMIN",async admin=>{
            foreach(var target in new[]{system,site,manager}){
                var command=new UserPhotoCommand(Guid.NewGuid(),0,"REPLACE",jpeg);commands[target]=command;
                Assert.NotNull((await SavePhoto(admin,$"/api/v1/users/{target}/photo/commands",command)).Photo.PhotoId);
                using var owner=TestToken.Client(factory,target,kc.Roles[target].First());
                var mobile=(await owner.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!;
                Assert.Equal("F",mobile.FirstName);Assert.Equal("L",mobile.LastName);Assert.Null(mobile.Phone);
            }
            var list=(await admin.GetFromJsonAsync<PagedRows<UserResponse>>("/api/v1/users"))!;Assert.All(list.Items,u=>Assert.True(u.CanManagePhoto));
        });
        await PhotoClient(kc,Guid.NewGuid().ToString(),"SUPER_ADMIN",async other=>{
            Assert.Equal(HttpStatusCode.Conflict,(await other.PostAsJsonAsync($"/api/v1/users/{manager}/photo/commands",commands[manager])).StatusCode);
        });
        await PhotoClient(kc,x.Actor,"SITE_ADMIN",async admin=>{
            var list=(await admin.GetFromJsonAsync<PagedRows<UserResponse>>("/api/v1/users"))!;
            Assert.False(list.Items.Single(u=>u.Id==system).CanManagePhoto);Assert.False(list.Items.Single(u=>u.Id==site).CanManagePhoto);
            Assert.True(list.Items.Single(u=>u.Id==x.Actor).CanManagePhoto);Assert.True(list.Items.Single(u=>u.Id==manager).CanManagePhoto);
            Assert.NotNull((await SavePhoto(admin,$"/api/v1/users/{manager}/photo/commands",new(Guid.NewGuid(),1,"REPLACE",jpeg))).Photo.PhotoId);
            Assert.NotNull((await SavePhoto(admin,$"/api/v1/users/{x.Actor}/photo/commands",new(Guid.NewGuid(),0,"REPLACE",jpeg))).Photo.PhotoId);
            foreach(var target in new[]{system,site})Assert.Equal(HttpStatusCode.Forbidden,(await admin.PostAsJsonAsync($"/api/v1/users/{target}/photo/commands",new UserPhotoCommand(Guid.NewGuid(),1,"REMOVE"))).StatusCode);
        });
        // Delegated first upload can precede the owner's name form. Withdrawal still clears it unconditionally.
        using var systemOwner=TestToken.Client(factory,system,"SUPER_ADMIN");
        Assert.Equal(HttpStatusCode.OK,(await ConsentTestHelper.CommandAsync(systemOwner,ConsentTestHelper.Withdraw("PROFILE_PHOTO"))).StatusCode);
        Assert.Null((await systemOwner.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.PhotoId);
        await PhotoClient(kc,x.Actor,"SUPER_ADMIN",async admin=>{
            Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync($"/api/v1/users/{system}/photo")).StatusCode);
            Assert.True((await SavePhoto(admin,$"/api/v1/users/{system}/photo/commands",commands[system])).Replayed);
            Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync($"/api/v1/users/{system}/photo")).StatusCode);
        });
    }
    [PostgresFact] public async Task ManagedPhotoScopePrivilegedRolesOwnerExceptionAndInvalidBodiesFailClosed()
    {
        var x=await Seed();var kc=new FakeKeycloak();var target=kc.Add("photo.target",["MANAGER"]);var elevated=kc.Add("photo.elevated",["SITE_ADMIN"]);
        kc.Add("photo.self",["SITE_ADMIN"],x.Actor);
        await factory.WithDb(async db=>{db.Add(new PropertyMembership(x.Property,target));db.Add(new PropertyMembership(x.Property,elevated));await db.SaveChangesAsync();});
        await PhotoClient(kc,x.Actor,"SITE_ADMIN",async admin=>{
            var path=$"/api/v1/users/{target}/photo";
            Assert.Equal(HttpStatusCode.OK,(await admin.GetAsync(path+"/state")).StatusCode);
            Assert.Equal(HttpStatusCode.OK,(await admin.GetAsync($"/api/v1/users/{x.Actor}/photo/state")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await admin.GetAsync($"/api/v1/users/{elevated}/photo/state")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await admin.PostAsJsonAsync($"/api/v1/users/{elevated}/photo/commands",new UserPhotoCommand(Guid.NewGuid(),0,"REMOVE"))).StatusCode);
            var foreign=kc.Add("photo.foreign",["MANAGER"]);
            Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync($"/api/v1/users/{foreign}/photo/state")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,(await admin.PostAsJsonAsync($"/api/v1/users/{foreign}/photo/commands",new UserPhotoCommand(Guid.NewGuid(),0,"REMOVE"))).StatusCode);
            foreach(var command in new[]{new UserPhotoCommand(Guid.Empty,0,"REMOVE"),new(Guid.NewGuid(),-1,"REMOVE"),new(Guid.NewGuid(),0,"KEEP"),new(Guid.NewGuid(),0,"REMOVE","extra"),new(Guid.NewGuid(),0,"REPLACE","bad!"),new(Guid.NewGuid(),0,"REPLACE",Convert.ToBase64String(Encoding.UTF8.GetBytes("not jpeg"))),new(Guid.NewGuid(),0,"REPLACE",Convert.ToBase64String(new byte[256*1024+1]))})
                Assert.Equal(HttpStatusCode.BadRequest,(await admin.PostAsJsonAsync(path+"/commands",command)).StatusCode);
            using var oversized=new StringContent(new string('x',385*1024),Encoding.UTF8,"application/json");
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge,(await admin.PostAsync(path+"/commands",oversized)).StatusCode);
            using var wrongType=new StringContent("{}",Encoding.UTF8,"text/plain");Assert.Equal(HttpStatusCode.UnsupportedMediaType,(await admin.PostAsync(path+"/commands",wrongType)).StatusCode);
            // Revoking active property membership removes the delegated capability immediately.
            await factory.WithDb(async db=>{var m=await db.PropertyMemberships.SingleAsync(m=>m.PropertyId==x.Property&&m.UserId==x.Actor);m.SetEnabled(false);await db.SaveChangesAsync();});
            Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync(path+"/state")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,(await admin.PostAsJsonAsync(path+"/commands",new UserPhotoCommand(Guid.NewGuid(),0,"REMOVE"))).StatusCode);
        });
        foreach(var role in new[]{"MANAGER","ACCOUNTANT","RESIDENT","spoofed","client-role"})await PhotoClient(kc,x.Actor,role,async c=>{
            Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/v1/users/{target}/photo/state")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync($"/api/v1/users/{target}/photo/commands",new UserPhotoCommand(Guid.NewGuid(),0,"REMOVE"))).StatusCode);
        });
        using var anonymous=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync($"/api/v1/users/{target}/photo/state")).StatusCode);
    }
}
