using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class MobileProfileTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory> {
 private async Task<(HttpClient Client,string User,Guid Membership)> Setup(string role="MANAGER") {
  var user=Guid.NewGuid().ToString();var property=new Property("Profile fixture","City","District","Address");var membership=new PropertyMembership(property.Id,user);
  await factory.WithDb(async db=>{db.Add(property);db.Add(membership);await db.SaveChangesAsync();});return(TestToken.Client(factory,user,role),user,membership.Id);
 }
 private static MobileProfileCommand Command()=>new(Guid.NewGuid(),0,"Fixture","Profile",null,"KEEP",null);
 private static Task<HttpResponseMessage> Push(HttpClient c,MobileProfileCommand cmd)=>c.PostAsJsonAsync("/api/v1/mobile/profile/commands",cmd);
 [PostgresFact] public async Task ReplayConflictAndConcurrentUpdatesPreserveOneVersion(){
  var s=await Setup();using var c=s.Client;Assert.Equal(0,(await c.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.Version);
  var cmd=Command();var responses=await Task.WhenAll(Push(c,cmd),Push(c,cmd));Assert.All(responses,r=>Assert.Equal(HttpStatusCode.OK,r.StatusCode));
  var results=await Task.WhenAll(responses.Select(r=>r.Content.ReadFromJsonAsync<MobileProfileResult>()));Assert.Contains(results,r=>r!.Replayed);Assert.All(results,r=>Assert.Equal(1,r!.Profile.Version));
  Assert.All(results,r=>{Assert.Equal("Fixture",r!.Profile.FirstName);Assert.Equal("PROFİLE",r.Profile.LastName);});
  Assert.Equal(HttpStatusCode.Conflict,(await Push(c,cmd with{FirstName="Other"})).StatusCode);
  Assert.Equal(HttpStatusCode.Conflict,(await Push(c,cmd with{OperationId=Guid.NewGuid()})).StatusCode);
  Assert.Equal(1,(await c.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.Version);
 }
 [PostgresFact] public async Task AccountIsolationUnlinkedAccountsAndAnonymousFailClosed(){
  var s=await Setup();using var c=s.Client;Assert.Equal(HttpStatusCode.OK,(await Push(c,Command())).StatusCode);
  var other=await Setup();using var foreign=other.Client;Assert.Equal(0,(await foreign.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.Version);
  using var unlinked=TestToken.Client(factory,Guid.NewGuid().ToString(),"MANAGER");Assert.Equal(0,(await unlinked.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.Version);Assert.Equal(HttpStatusCode.OK,(await Push(unlinked,Command())).StatusCode);
  using var anon=TestToken.Client(factory,null,"");Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync("/api/v1/mobile/profile")).StatusCode);
  await factory.WithDb(async db=>{var m=await db.PropertyMemberships.FindAsync(s.Membership);m!.SetEnabled(false);await db.SaveChangesAsync();});Assert.Equal(1,(await c.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.Version);
 }
 [PostgresFact] public async Task ValidationAndAccountingOwnProfileAreSupported(){
  var s=await Setup("ACCOUNTANT");using var c=s.Client;var cmd=Command();Assert.Equal(HttpStatusCode.OK,(await Push(c,cmd)).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Push(c,cmd with{OperationId=Guid.NewGuid(),ExpectedVersion=1,FirstName=""})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Push(c,cmd with{OperationId=Guid.NewGuid(),ExpectedVersion=1,PhotoAction="REPLACE",PhotoBase64="PHN2Zz4="})).StatusCode);
  Assert.Equal(HttpStatusCode.RequestEntityTooLarge,(await c.PostAsync("/api/v1/mobile/profile/commands",new StringContent(new string('x',400000),System.Text.Encoding.UTF8,"application/json"))).StatusCode);
 }
 [StorageFact] public async Task PhotoReplaceRemoveRejectsOldAndForeignAccessThenPurgesObjects(){
  var s=await Setup();using var c=s.Client;await ConsentTestHelper.GrantAsync(factory,c,"PROFILE_PHOTO");var bytes=await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures","avatar.jpg"));var cmd=Command() with{PhotoAction="REPLACE",PhotoBase64=Convert.ToBase64String(bytes)};
  var response=await Push(c,cmd);Assert.Equal(HttpStatusCode.OK,response.StatusCode);var first=(await response.Content.ReadFromJsonAsync<MobileProfileResult>())!;Assert.NotNull(first.Profile.PhotoId);
  var image=await c.GetByteArrayAsync("/api/v1/mobile/profile/photo/"+first.Profile.PhotoId);Assert.Equal(ProfilePhoto.Normalize(bytes),image);
  Assert.True((await (await Push(c,cmd)).Content.ReadFromJsonAsync<MobileProfileResult>())!.Replayed);
  var foreign=await Setup();using var other=foreign.Client;Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync("/api/v1/mobile/profile/photo/"+first.Profile.PhotoId)).StatusCode);
  var replacement=cmd with{OperationId=Guid.NewGuid(),ExpectedVersion=first.Profile.Version};var second=(await (await Push(c,replacement)).Content.ReadFromJsonAsync<MobileProfileResult>())!;Assert.NotEqual(first.Profile.PhotoId,second.Profile.PhotoId);
  Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/api/v1/mobile/profile/photo/"+first.Profile.PhotoId)).StatusCode);
  var removed=(await (await Push(c,replacement with{OperationId=Guid.NewGuid(),ExpectedVersion=second.Profile.Version,PhotoAction="REMOVE",PhotoBase64=null})).Content.ReadFromJsonAsync<MobileProfileResult>())!;Assert.Null(removed.Profile.PhotoId);
  Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/api/v1/mobile/profile/photo/"+second.Profile.PhotoId)).StatusCode);
  using var scope=factory.Services.CreateScope();await scope.ServiceProvider.GetRequiredService<IMobileProfileStore>().MaintainAsync(default);
  await factory.WithDb(async db=>Assert.All(await db.Set<MobileProfilePhoto>().Where(x=>x.OwnerUserId==s.User).ToListAsync(),x=>Assert.Equal("PURGED",x.State)));
  using var storage=StorageTestClient.Create();var objects=await storage.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request{BucketName=factory.StorageBucket,Prefix="profiles/"});Assert.Empty(objects.S3Objects??[]);
 }
}
public sealed class ProfileImageTests {
 [Fact] public void InvalidImagesAndOversizeAreRejected(){foreach(var bytes in new[]{Array.Empty<byte>(),"<svg>bad</svg>"u8.ToArray(),new byte[ProfilePhoto.MaxBytes+1],new byte[]{255,216,255,217}})Assert.Throws<ArgumentException>(()=>ProfilePhoto.Normalize(bytes));}
 [Fact] public void MetadataIsRemovedAndTrailingBytesRejected(){var image=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","avatar.jpg"));var segment=new byte[]{255,225,0,8,69,120,105,102,0,0};var withExif=image[..2].Concat(segment).Concat(image[2..]).ToArray();Assert.Equal(ProfilePhoto.Normalize(image),ProfilePhoto.Normalize(withExif));Assert.Throws<ArgumentException>(()=>ProfilePhoto.Normalize(image.Concat(new byte[]{0}).ToArray()));}
}
