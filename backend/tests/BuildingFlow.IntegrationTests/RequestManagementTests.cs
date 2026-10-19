using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class RequestManagementTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory> {
 private async Task<(HttpClient Client,Guid Property,string User,Guid Member,MobileSyncCommand Command,MobileSyncResult Created)> Setup(){
  var user=Guid.NewGuid().ToString();var p=new Property("Request fixture","City","District","Address");var m=new PropertyMembership(p.Id,user);
  await factory.WithDb(async db=>{db.AddRange(p,m);await db.SaveChangesAsync();});var client=TestToken.Client(factory,user,"MANAGER");var cmd=new MobileSyncCommand(Guid.NewGuid(),Guid.NewGuid(),p.Id,null,"MANAGER","CREATE",0,"Fixture water leak","FAULT","HIGH");
  var response=await client.PostAsJsonAsync("/api/v1/mobile/sync/commands",cmd);response.EnsureSuccessStatusCode();return(client,p.Id,user,m.Id,cmd,(await response.Content.ReadFromJsonAsync<MobileSyncResult>())!);
 }
 private static string Root(Guid p)=>$"/api/v1/properties/{p}/requests";
 private static Task<HttpResponseMessage> Change(HttpClient c,Guid p,Guid id,ManageRequest cmd)=>c.PostAsJsonAsync(Root(p)+$"/{id}/management",cmd);
 private static async Task<RequestDetail> Value(HttpResponseMessage r){r.EnsureSuccessStatusCode();return(await r.Content.ReadFromJsonAsync<RequestDetail>())!;}
 [PostgresFact]public async Task WorkflowReplayCurrentResultAndMobileConflictsPreserveHistory(){
  var s=await Setup();using var c=s.Client;var id=s.Command.RecordId;var cmd=new ManageRequest(Guid.NewGuid(),s.Created.Record.Version,"IN_PROGRESS","Investigating");
  var d=await Value(await Change(c,s.Property,id,cmd));Assert.Equal("IN_PROGRESS",d.Record.Status);Assert.Single(d.History);Assert.Equal(s.Created.Record.Version+1,d.Record.Version);
  Assert.Equal(HttpStatusCode.Conflict,(await Change(c,s.Property,id,cmd with{OperationId=Guid.NewGuid()})).StatusCode);
  Assert.Equal(HttpStatusCode.Conflict,(await Change(c,s.Property,id,cmd with{Response="different"})).StatusCode);
  var resolved=await Value(await Change(c,s.Property,id,new(Guid.NewGuid(),d.Record.Version,"RESOLVED","Repaired")));Assert.Equal(2,resolved.History.Length);
  var replay=await Value(await Change(c,s.Property,id,cmd));Assert.True(replay.Replayed);Assert.Equal("RESOLVED",replay.Record.Status);Assert.Equal(resolved.Record.Version,replay.Record.Version);
  var reopened=await Value(await Change(c,s.Property,id,new(Guid.NewGuid(),resolved.Record.Version,"RECORDED","Issue returned")));Assert.Equal(3,reopened.History.Length);
  var pull=(await c.GetFromJsonAsync<MobileRequestSnapshot>($"/api/v1/mobile/sync/requests?propertyId={s.Property}&role=MANAGER"))!;var own=Assert.Single(pull.Records);Assert.Equal("RECORDED",own.ManagementStatus);Assert.Equal("Issue returned",own.ManagementResponse);Assert.True(own.HasManagementHistory);
  // A new offline command may never remove or replace managed content, even after reopening.
  foreach(var action in new[]{"DELETE","UPDATE"})Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/v1/mobile/sync/commands",s.Command with{OperationId=Guid.NewGuid(),Action=action,ExpectedVersion=own.Version,Description=action=="DELETE"?null:"Overwrite",Category=null,Priority=null})).StatusCode);
  // A historical mobile create receipt still replays its original version and content.
  var old=(await(await c.PostAsJsonAsync("/api/v1/mobile/sync/commands",s.Command)).Content.ReadFromJsonAsync<MobileSyncResult>())!;Assert.True(old.Replayed);Assert.Equal(s.Created.Record.Version,old.Record.Version);
  await factory.WithDb(async db=>{Assert.Equal(3,await db.Set<RequestManagementEvent>().CountAsync(e=>e.RequestId==id));var r=await db.Set<MobileRequest>().SingleAsync(r=>r.Id==id);Assert.Equal(s.Command.Description,r.Description);Assert.Equal(s.User,r.ModifiedBy);Assert.True(s.Created.Record.ModifiedAt<=r.ModifiedAt);await Assert.ThrowsAsync<Npgsql.PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE request_management_events SET response='changed' WHERE request_id={id}"));});
 }
 [PostgresFact]public async Task InvalidTransitionsMissingReasonAndConcurrentSameVersionAreRejected(){var s=await Setup();using var c=s.Client;var id=s.Command.RecordId;
  Assert.Equal(HttpStatusCode.Conflict,(await Change(c,s.Property,id,new(Guid.NewGuid(),1,"RESOLVED","Invalid shortcut"))).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Change(c,s.Property,id,new(Guid.NewGuid(),1,"IN_PROGRESS"," "))).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await Change(c,s.Property,id,new(Guid.Empty,1,"IN_PROGRESS","Reason"))).StatusCode);
  var cmds=new[]{new ManageRequest(Guid.NewGuid(),1,"IN_PROGRESS","First"),new ManageRequest(Guid.NewGuid(),1,"REJECTED","Second")};var result=await Task.WhenAll(cmds.Select(cmd=>Change(c,s.Property,id,cmd)));Assert.Single(result,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(result,r=>r.StatusCode==HttpStatusCode.Conflict);
 }
 [PostgresFact]public async Task ConcurrentIdenticalOperationCreatesOneEvent(){var s=await Setup();using var c=s.Client;var cmd=new ManageRequest(Guid.NewGuid(),1,"IN_PROGRESS","Investigating");var responses=await Task.WhenAll(Change(c,s.Property,s.Command.RecordId,cmd),Change(c,s.Property,s.Command.RecordId,cmd));var ds=await Task.WhenAll(responses.Select(Value));Assert.Single(ds,d=>d.Replayed);Assert.All(ds,d=>Assert.Single(d.History));}
 [PostgresFact]public async Task RolesMembershipForeignScopeAndRevocationApplyToReadsWritesAndReplay(){var s=await Setup();using var c=s.Client;var cmd=new ManageRequest(Guid.NewGuid(),1,"IN_PROGRESS","Investigating");Assert.Equal(HttpStatusCode.OK,(await Change(c,s.Property,s.Command.RecordId,cmd)).StatusCode);
  foreach(var role in new[]{"ACCOUNTANT","RESIDENT","spoofed"}){using var denied=TestToken.Client(factory,s.User,role);Assert.Equal(HttpStatusCode.Forbidden,(await denied.GetAsync(Root(s.Property))).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await denied.GetAsync(Root(s.Property)+"/"+s.Command.RecordId)).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await Change(denied,s.Property,s.Command.RecordId,cmd)).StatusCode);}
  using var foreign=TestToken.Client(factory,Guid.NewGuid().ToString(),"MANAGER");using var anon=TestToken.Client(factory,null,"");Assert.Equal(HttpStatusCode.Forbidden,(await foreign.GetAsync(Root(s.Property))).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await anon.GetAsync(Root(s.Property))).StatusCode);
  using var admin=TestToken.Client(factory,Guid.NewGuid().ToString(),"SUPER_ADMIN");Assert.Equal(HttpStatusCode.OK,(await admin.GetAsync(Root(s.Property))).StatusCode);
  var other=new Property("Other fixture","City","District","Address");await factory.WithDb(async db=>{db.AddRange(other,new PropertyMembership(other.Id,s.User));await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(other.Id)+"/"+s.Command.RecordId)).StatusCode);Assert.Equal(HttpStatusCode.Conflict,(await Change(c,other.Id,s.Command.RecordId,cmd)).StatusCode);
  await factory.WithDb(async db=>{(await db.PropertyMemberships.FindAsync(s.Member))!.SetEnabled(false);await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Root(s.Property))).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await Change(c,s.Property,s.Command.RecordId,cmd)).StatusCode);
 }
 [PostgresFact]public async Task ListFiltersStablePaginationAndResponsePrivacy(){var s=await Setup();using var c=s.Client;await factory.WithDb(async db=>{for(var i=0;i<14;i++)db.Add(new MobileRequest(Guid.NewGuid(),s.Property,null,"MANAGER",s.User,"Fixture ventilation "+i));await db.SaveChangesAsync();});
  var first=(await c.GetFromJsonAsync<RequestPage>(Root(s.Property)+"?page=1&pageSize=10&sort=oldest"))!;var second=(await c.GetFromJsonAsync<RequestPage>(Root(s.Property)+"?page=2&pageSize=10&sort=oldest"))!;Assert.Equal(15,first.TotalCount);Assert.Equal(10,first.Items.Length);Assert.Equal(5,second.Items.Length);Assert.Empty(first.Items.Select(r=>r.Id).Intersect(second.Items.Select(r=>r.Id)));
  var filtered=(await c.GetFromJsonAsync<RequestPage>(Root(s.Property)+"?search=water&category=FAULT&priority=HIGH&status=RECORDED"))!;Assert.Equal(s.Command.RecordId,Assert.Single(filtered.Items).Id);
  var json=await c.GetStringAsync(Root(s.Property)+"/"+s.Command.RecordId);Assert.DoesNotContain("ownerUserId",json);Assert.DoesNotContain("actorUserId",json);Assert.DoesNotContain("createdBy",json);Assert.DoesNotContain(s.User,json);
  foreach(var query in new[]{"page=0","pageSize=101","sort=description","status=INVALID","category=INVALID","priority=INVALID"})Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Root(s.Property)+"?"+query)).StatusCode);
 }
 [PostgresFact]public async Task MobileDeleteAndManagerTransitionShareTheSameRecordLock(){var s=await Setup();using var c=s.Client;var del=s.Command with{OperationId=Guid.NewGuid(),Action="DELETE",ExpectedVersion=1,Description=null,Category=null,Priority=null};var managed=Change(c,s.Property,s.Command.RecordId,new(Guid.NewGuid(),1,"IN_PROGRESS","Investigating"));var deleted=c.PostAsJsonAsync("/api/v1/mobile/sync/commands",del);var result=await Task.WhenAll(managed,deleted);Assert.Single(result,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(result,r=>r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound);await factory.WithDb(async db=>{var r=await db.Set<MobileRequest>().IgnoreQueryFilters().SingleAsync(r=>r.Id==s.Command.RecordId);var count=await db.Set<RequestManagementEvent>().CountAsync(e=>e.RequestId==r.Id);Assert.Equal(r.IsDeleted?0:1,count);Assert.Equal(r.IsDeleted?"RECORDED":"IN_PROGRESS",r.ManagementStatus);});}
 [PostgresFact]public async Task InactiveAndDeletedRequestsOrPropertiesCannotBeManaged(){var s=await Setup();using var c=s.Client;using(var admin=TestToken.Client(factory,s.User,"SUPER_ADMIN"))Assert.Equal(HttpStatusCode.Conflict,(await admin.DeleteAsync($"/api/v1/properties/{s.Property}?version=1")).StatusCode);await factory.WithDb(async db=>{var r=await db.Set<MobileRequest>().SingleAsync(r=>r.Id==s.Command.RecordId);r.SetEnabled(false);await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(s.Property)+"/"+s.Command.RecordId)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await Change(c,s.Property,s.Command.RecordId,new(Guid.NewGuid(),1,"IN_PROGRESS","Reason"))).StatusCode);await factory.WithDb(async db=>{(await db.Properties.FindAsync(s.Property))!.SetEnabled(false);await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync(Root(s.Property))).StatusCode);}

}
