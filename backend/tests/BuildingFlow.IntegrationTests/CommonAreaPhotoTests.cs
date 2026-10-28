using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class CommonAreaPhotoTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>, IDisposable
{
    private readonly List<WebApplicationFactory<Program>> children=[];
    private readonly MemoryStorage storage=new();
    private HttpClient Client(string user,string role,bool canonical=false)
    {
        var child=factory.WithWebHostBuilder(b=>{
            b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["PersonRefactor:Enabled"]=canonical?"true":"false",["PersonRefactor:SessionContextsEnabled"]=canonical?"true":"false"}));
            b.ConfigureServices(s=>{s.RemoveAll<IFileStorage>();s.AddSingleton<IFileStorage>(storage);});
        });children.Add(child);return TestToken.Client(child,user,role);
    }
    private sealed record World(Guid Property,Guid Area,string Manager,Guid Unit,Guid Relation,string Resident);
    private async Task<World> Seed()
    {
        var t=new PersonTenant("Photo fixture");var p=new Property("Area","City","District","Address",PropertyType.APARTMENT);var u=p.AddUnit("1",0,80);
        var manager=Guid.NewGuid().ToString();var managerAccount=new PersonUserAccount("http://localhost:8080/realms/buildingflow",manager);var resident=Guid.NewGuid().ToString();var account=new PersonUserAccount("http://localhost:8080/realms/buildingflow",resident);
        var person=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","12345678901","Photo","Fixture",null,null,null,null);
        var relation=new UnitResidency(t.Id,p.Id,u.Id,person.Id,DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5));relation.MarkLegacy();
        var area=new CommonArea(p.Id,Guid.NewGuid(),new string('A',64),"Halı saha",CommonAreaType.SPORTS_COURT,null,CommonAreaVisibility.PROPERTY,CommonAreaUsage.RESERVATION,14,null,null,null,null,0);
        await factory.WithDb(async db=>{db.AddRange(t,p,u,managerAccount,new PersonTenantMembership(t.Id,managerAccount.Id),account,person,new PersonPropertyScope(t.Id,p.Id),new PersonTenantMembership(t.Id,account.Id),new PropertyMembership(p.Id,manager));await db.SaveChangesAsync();db.AddRange(area,relation,new CentralPersonAccountLink(t.Id,account.Id,person.Id,DateTimeOffset.UtcNow.AddMinutes(-1)));await db.SaveChangesAsync();});
        return new(p.Id,area.Id,manager,u.Id,relation.Id,resident);
    }
    private static string Root(World w)=>$"/api/v1/properties/{w.Property}/common-areas/{w.Area}/photos";
    private static string Me(World w)=>$"/api/v2/me/units/{w.Unit}/common-areas/{w.Area}/photos";
    private static string Query(World w)=>$"?propertyId={w.Property}&capacity=RESIDENT&relationId={w.Relation}";
    private async Task<StoredDocument> Document(Guid property,string type="image/png")
    {
        var bytes=new byte[]{137,80,78,71,13,10,26,10,1,2,3};var sha=Convert.ToHexString(SHA256.HashData(bytes));
        var d=new StoredDocument(property,Guid.NewGuid(),new string('A',64),null,ContentAudience.MANAGEMENT,type=="application/pdf"?"test.pdf":"test.png",type,bytes.Length,sha);
        await factory.WithDb(async db=>{db.Add(d);await db.SaveChangesAsync();});storage.Data[d.ObjectKey]=bytes;return d;
    }
    [PostgresFact] public async Task ManagerLinksAndMemberReadsOnlyScopedPrivatePhotoWithHashAndVersion()
    {
        var w=await Seed();var d=await Document(w.Property);using var manager=Client(w.Manager,"MANAGER");using var resident=Client(w.Resident,"RESIDENT",true);
        var response=await manager.PostAsJsonAsync(Root(w),new CommonAreaPhotoRequest(d.Id,1,true));Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var photo=(await response.Content.ReadFromJsonAsync<CommonAreaPhotoResponse>())!;
        Assert.Equal(d.Sha256,photo.Sha256);Assert.Equal(HttpStatusCode.OK,(await manager.PostAsJsonAsync(Root(w),new CommonAreaPhotoRequest(d.Id,1,true))).StatusCode);
        var list=await resident.GetFromJsonAsync<CommonAreaPhotoResponse[]>(Me(w)+Query(w));Assert.Equal(photo.Id,Assert.Single(list!).Id);
        var file=await resident.GetAsync(Me(w)+$"/{photo.Id}/download"+Query(w));Assert.Equal(HttpStatusCode.OK,file.StatusCode);Assert.Equal("image/png",file.Content.Headers.ContentType!.MediaType);Assert.Contains("no-store",file.Headers.CacheControl!.ToString());Assert.Equal(storage.Data[d.ObjectKey],await file.Content.ReadAsByteArrayAsync());
        using var outsider=Client(Guid.NewGuid().ToString(),"RESIDENT",true);Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(Me(w)+Query(w))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await resident.PostAsJsonAsync(Root(w),new CommonAreaPhotoRequest(d.Id,1,true))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await manager.DeleteAsync(Root(w)+$"/{photo.Id}?version=99")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await manager.DeleteAsync(Root(w)+$"/{photo.Id}?version={photo.Version}")).StatusCode);
        Assert.Empty((await resident.GetFromJsonAsync<CommonAreaPhotoResponse[]>(Me(w)+Query(w)))!);
        Assert.Equal(HttpStatusCode.NotFound,(await resident.GetAsync(Me(w)+$"/{photo.Id}/download"+Query(w))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await manager.PostAsJsonAsync(Root(w),new CommonAreaPhotoRequest(d.Id,1,true))).StatusCode);
    }
    [PostgresFact] public async Task RejectsForeignDocumentsMissingRightsPdfStaleVersionAndCorruptStorage()
    {
        var w=await Seed();var other=await Seed();using var manager=Client(w.Manager,"MANAGER");
        var d=await Document(w.Property);var wrong=await Document(other.Property);var pdf=await Document(w.Property,"application/pdf");
        foreach(var request in new[]{new CommonAreaPhotoRequest(d.Id,1,false),new CommonAreaPhotoRequest(wrong.Id,1,true),new CommonAreaPhotoRequest(pdf.Id,1,true)})Assert.Equal(HttpStatusCode.BadRequest,(await manager.PostAsJsonAsync(Root(w),request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await manager.PostAsJsonAsync(Root(w),new CommonAreaPhotoRequest(d.Id,99,true))).StatusCode);
        var photo=(await (await manager.PostAsJsonAsync(Root(w),new CommonAreaPhotoRequest(d.Id,1,true))).Content.ReadFromJsonAsync<CommonAreaPhotoResponse>())!;
        storage.Data[d.ObjectKey]=new byte[]{1};Assert.Equal(HttpStatusCode.ServiceUnavailable,(await manager.GetAsync(Root(w)+$"/{photo.Id}/download")).StatusCode);
        await factory.WithDb(async db=>{var area=await db.Set<CommonArea>().SingleAsync(a=>a.Id==w.Area);area.SetEnabled(false);await db.SaveChangesAsync();});
        using var resident=Client(w.Resident,"RESIDENT",true);Assert.Equal(HttpStatusCode.NotFound,(await resident.GetAsync(Me(w)+Query(w))).StatusCode);
    }
    public void Dispose(){foreach(var child in children)child.Dispose();}
    private sealed class MemoryStorage:IFileStorage
    {
        public readonly Dictionary<string,byte[]> Data=[];
        public async Task UploadAsync(string key,Stream content,string contentType,CancellationToken token){using var m=new MemoryStream();await content.CopyToAsync(m,token);Data[key]=m.ToArray();}
        public Task<Stream> DownloadAsync(string key,CancellationToken token)=>Task.FromResult<Stream>(new MemoryStream(Data[key]));
        public Task DeleteAsync(string key,CancellationToken token){Data.Remove(key);return Task.CompletedTask;}
    }
}
