using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class MobileRequestPhotoTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory> {
 private async Task<(HttpClient Client,Guid Property,Guid Unit,string User,Guid Membership)> Setup(){
  var user=Guid.NewGuid().ToString();var property=new Property("Request photo fixture","City","District","Address");var unit=property.AddBuilding("A").AddUnit("1",1,90m);
  var person=new Person(property.Id,"Photo","Tenant","5550000000","photo@example.test");person.SetProfile("NATURAL","FOREIGN","US","PHOTO1234",null,null,null,user);
  var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
  var membership=new PropertyMembership(property.Id,user);
  await factory.WithDb(async db=>{db.AddRange(property,person,new UnitOccupancy(property.Id,unit.Id,person.Id,OccupancyType.TENANT,today.AddDays(-1)),membership);await db.SaveChangesAsync();});
  return(TestToken.Client(factory,user,"RESIDENT"),property.Id,unit.Id,user,membership.Id);
 }
 private static MobileSyncCommand Create(Guid p,Guid u)=>new(Guid.NewGuid(),Guid.NewGuid(),p,u,"TENANT","CREATE",0,"Kitchen leak","PLUMBING","HIGH");
 private static Task<HttpResponseMessage> Push(HttpClient c,MobileSyncCommand cmd)=>c.PostAsJsonAsync("/api/v1/mobile/sync/commands",cmd);
 private static Task<HttpResponseMessage> Attach(HttpClient c,Guid request,MobileRequestPhotoCommand cmd)=>c.PostAsJsonAsync($"/api/v1/mobile/sync/requests/{request}/photos",cmd);
 private static string Pull(Guid p,Guid u)=>$"/api/v1/mobile/sync/requests?propertyId={p}&unitId={u}&role=TENANT";
 private static string Image(Guid p,Guid u,Guid r,Guid photo)=>$"/api/v1/mobile/sync/requests/{r}/photos/{photo}?propertyId={p}&unitId={u}&role=TENANT";
 private static async Task<string> Jpeg()=>Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures","avatar.jpg")));
 [PostgresFact] public async Task CategoryAndPriorityAreStoredValidatedAndLegacyCommandsDefault(){
  var s=await Setup();using var c=s.Client;var cmd=Create(s.Property,s.Unit);
  var created=(await (await Push(c,cmd)).Content.ReadFromJsonAsync<MobileSyncResult>())!;Assert.Equal("PLUMBING",created.Record.Category);Assert.Equal("HIGH",created.Record.Priority);Assert.Empty(created.Record.Photos!);
  Assert.True((await (await Push(c,cmd)).Content.ReadFromJsonAsync<MobileSyncResult>())!.Replayed);
  Assert.Equal(HttpStatusCode.Conflict,(await Push(c,cmd with{Priority="URGENT"})).StatusCode);
  var legacy=(await (await Push(c,cmd with{OperationId=Guid.NewGuid(),RecordId=Guid.NewGuid(),Category=null,Priority=null})).Content.ReadFromJsonAsync<MobileSyncResult>())!;Assert.Equal("OTHER",legacy.Record.Category);Assert.Equal("NORMAL",legacy.Record.Priority);
  var edited=(await (await Push(c,cmd with{OperationId=Guid.NewGuid(),Action="UPDATE",ExpectedVersion=created.Record.Version,Category=null,Priority="URGENT"})).Content.ReadFromJsonAsync<MobileSyncResult>())!;Assert.Equal("PLUMBING",edited.Record.Category);Assert.Equal("URGENT",edited.Record.Priority);
  Assert.Equal(HttpStatusCode.BadRequest,(await Push(c,cmd with{OperationId=Guid.NewGuid(),RecordId=Guid.NewGuid(),Category="FIRE"})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Push(c,cmd with{OperationId=Guid.NewGuid(),RecordId=Guid.NewGuid(),Priority="low"})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Push(c,cmd with{OperationId=Guid.NewGuid(),Action="DELETE",ExpectedVersion=edited.Record.Version,Description=null})).StatusCode);
  var deleted=(await (await Push(c,cmd with{OperationId=Guid.NewGuid(),Action="DELETE",ExpectedVersion=edited.Record.Version,Description=null,Category=null,Priority=null})).Content.ReadFromJsonAsync<MobileSyncResult>())!;Assert.Null(deleted.Record.Category);Assert.Null(deleted.Record.Priority);
  await factory.WithDb(async db=>Assert.Equal("URGENT",(await db.Set<MobileRequest>().IgnoreQueryFilters().SingleAsync(x=>x.Id==cmd.RecordId)).Priority));
 }
 [PostgresFact] public async Task PhotoValidationScopeAndAccountingRejections(){
  var s=await Setup();using var c=s.Client;var cmd=Create(s.Property,s.Unit);Assert.Equal(HttpStatusCode.OK,(await Push(c,cmd)).StatusCode);
  var photo=new MobileRequestPhotoCommand(Guid.NewGuid(),s.Property,s.Unit,"TENANT","PHN2Zz4=");
  Assert.Equal(HttpStatusCode.BadRequest,(await Attach(c,cmd.RecordId,photo)).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Attach(c,cmd.RecordId,photo with{PhotoBase64="%%%"})).StatusCode);
  Assert.Equal(HttpStatusCode.RequestEntityTooLarge,(await c.PostAsync($"/api/v1/mobile/sync/requests/{cmd.RecordId}/photos",new StringContent(new string('x',400000),System.Text.Encoding.UTF8,"application/json"))).StatusCode);
  var valid=photo with{PhotoBase64=await Jpeg()};
  Assert.Equal(HttpStatusCode.Forbidden,(await Attach(c,Guid.NewGuid(),valid)).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await Attach(c,cmd.RecordId,valid with{UnitId=Guid.NewGuid()})).StatusCode);
  var other=await Setup();using var foreign=other.Client;Assert.Equal(HttpStatusCode.Forbidden,(await Attach(foreign,cmd.RecordId,valid with{PropertyId=other.Property,UnitId=other.Unit})).StatusCode);
  using var anon=TestToken.Client(factory,null,"");Assert.Equal(HttpStatusCode.Unauthorized,(await Attach(anon,cmd.RecordId,valid)).StatusCode);
  using var accountant=TestToken.Client(factory,s.User,"ACCOUNTANT");Assert.Equal(HttpStatusCode.Forbidden,(await Attach(accountant,cmd.RecordId,valid with{UnitId=null,Role="ACCOUNTANT"})).StatusCode);
  await factory.WithDb(async db=>Assert.Empty(await db.Set<MobileRequestPhoto>().Where(x=>x.RequestId==cmd.RecordId).ToListAsync()));
 }
 [StorageFact] public async Task PhotoUploadReplayLimitIsolationDeleteAndPurge(){
  var s=await Setup();using var c=s.Client;var cmd=Create(s.Property,s.Unit);var created=(await (await Push(c,cmd)).Content.ReadFromJsonAsync<MobileSyncResult>())!;
  var jpeg=await Jpeg();var photo=new MobileRequestPhotoCommand(Guid.NewGuid(),s.Property,s.Unit,"TENANT",jpeg);
  var responses=await Task.WhenAll(Attach(c,cmd.RecordId,photo),Attach(c,cmd.RecordId,photo));Assert.All(responses,r=>Assert.Equal(HttpStatusCode.OK,r.StatusCode));
  var results=await Task.WhenAll(responses.Select(r=>r.Content.ReadFromJsonAsync<MobileRequestPhotoResult>()));Assert.Contains(results,r=>r!.Replayed);Assert.Single(results.Select(r=>r!.Photo.Id).Distinct());
  var first=results[0]!.Photo;Assert.Equal(ProfilePhoto.Normalize(Convert.FromBase64String(jpeg)),await c.GetByteArrayAsync(Image(s.Property,s.Unit,cmd.RecordId,first.Id)));
  Assert.Equal(HttpStatusCode.Conflict,(await Attach(c,Guid.NewGuid(),photo)).StatusCode);
  Assert.Equal(HttpStatusCode.OK,(await Attach(c,cmd.RecordId,photo with{OperationId=Guid.NewGuid()})).StatusCode);
  Assert.Equal(HttpStatusCode.OK,(await Attach(c,cmd.RecordId,photo with{OperationId=Guid.NewGuid()})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Attach(c,cmd.RecordId,photo with{OperationId=Guid.NewGuid()})).StatusCode);
  var pulled=Assert.Single((await c.GetFromJsonAsync<MobileRequestSnapshot>(Pull(s.Property,s.Unit)))!.Records);Assert.Equal(3,pulled.Photos!.Length);Assert.Equal(first.Id,pulled.Photos[0].Id);Assert.Equal(created.Record.Version,pulled.Version);
  var other=await Setup();using var foreign=other.Client;Assert.Equal(HttpStatusCode.NotFound,(await foreign.GetAsync(Image(other.Property,other.Unit,cmd.RecordId,first.Id))).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await foreign.GetAsync(Image(s.Property,s.Unit,cmd.RecordId,first.Id))).StatusCode);
  var deleted=(await (await Push(c,cmd with{OperationId=Guid.NewGuid(),Action="DELETE",ExpectedVersion=created.Record.Version,Description=null,Category=null,Priority=null})).Content.ReadFromJsonAsync<MobileSyncResult>())!;Assert.Empty(deleted.Record.Photos!);
  Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Image(s.Property,s.Unit,cmd.RecordId,first.Id))).StatusCode);
  Assert.Equal(HttpStatusCode.Conflict,(await Attach(c,cmd.RecordId,photo with{OperationId=Guid.NewGuid()})).StatusCode);
  Assert.True((await (await Attach(c,cmd.RecordId,photo)).Content.ReadFromJsonAsync<MobileRequestPhotoResult>())!.Replayed);
  using var scope=factory.Services.CreateScope();await scope.ServiceProvider.GetRequiredService<IMobileSyncStore>().MaintainPhotosAsync(default);
  await factory.WithDb(async db=>Assert.All(await db.Set<MobileRequestPhoto>().Where(x=>x.RequestId==cmd.RecordId).ToListAsync(),x=>Assert.Equal("PURGED",x.State)));
  using var storage=StorageTestClient.Create();var objects=await storage.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request{BucketName=factory.StorageBucket,Prefix="requests/"+cmd.RecordId+"/"});Assert.Empty(objects.S3Objects??[]);
 }
 [StorageFact] public async Task RevokedMembershipBlocksUploadAndDownload(){
  var s=await Setup();using var c=s.Client;var cmd=Create(s.Property,s.Unit);Assert.Equal(HttpStatusCode.OK,(await Push(c,cmd)).StatusCode);
  var photo=new MobileRequestPhotoCommand(Guid.NewGuid(),s.Property,s.Unit,"TENANT",await Jpeg());var stored=(await (await Attach(c,cmd.RecordId,photo)).Content.ReadFromJsonAsync<MobileRequestPhotoResult>())!;
  await factory.WithDb(async db=>{var m=await db.PropertyMemberships.FindAsync(s.Membership);m!.SetEnabled(false);await db.SaveChangesAsync();});
  Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Image(s.Property,s.Unit,cmd.RecordId,stored.Photo.Id))).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await Attach(c,cmd.RecordId,photo with{OperationId=Guid.NewGuid()})).StatusCode);
 }
}
