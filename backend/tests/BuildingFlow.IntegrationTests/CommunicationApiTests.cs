using Amazon.S3.Model;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class CommunicationApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static string Root(Guid p)=>$"/api/v1/properties/{p}";
    private static readonly byte[] Pdf="%PDF-1.7\n test fixture\n%%EOF"u8.ToArray();
    private async Task<(Property Property,string Actor)> Seed()
    {var actor=Guid.NewGuid().ToString();var p=new Property("Content "+actor,"City","District","Address",PropertyType.APARTMENT);await factory.WithDb(async db=>{db.Add(p);db.Add(new PropertyMembership(p.Id,actor));await db.SaveChangesAsync();});return(p,actor);}
    private static async Task<AnnouncementResponse> Draft(HttpClient client,Guid p,string audience="ALL_MEMBERS")
    {var r=await client.PostAsJsonAsync(Root(p)+"/announcements",new AnnouncementRequest(" Water maintenance_% "," Plain <script> text ",audience));Assert.Equal(HttpStatusCode.Created,r.StatusCode);return(await r.Content.ReadFromJsonAsync<AnnouncementResponse>())!;}
    private static MultipartFormDataContent Form(Guid? request=null,string name="test.pdf",string type="application/pdf",byte[]? bytes=null,Guid? announcement=null,long version=0,string audience="ALL_MEMBERS")
    {var f=new MultipartFormDataContent();var file=new ByteArrayContent(bytes??Pdf);file.Headers.ContentType=new(type);f.Add(file,"file",name);f.Add(new StringContent((request??Guid.NewGuid()).ToString()),"requestId");f.Add(new StringContent(audience),"audience");if(announcement.HasValue){f.Add(new StringContent(announcement.ToString()!),"announcementId");f.Add(new StringContent(version.ToString()),"announcementVersion");}return f;}
    private static async Task<DocumentResponse> Upload(HttpClient client,Guid p,Guid? request=null,Guid? announcement=null,long version=0,string audience="ALL_MEMBERS")
    {using var form=Form(request,announcement:announcement,version:version,audience:audience);var r=await client.PostAsync(Root(p)+"/documents",form);Assert.Equal(HttpStatusCode.Created,r.StatusCode);return(await r.Content.ReadFromJsonAsync<DocumentResponse>())!;}
    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task ManagersCreateEditPublishArchiveWithImmutableAudit(string role)
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,role);var a=await Draft(c,x.Property.Id);Assert.Equal("Water maintenance_%",a.Title);
        var r=await c.PutAsJsonAsync(Root(x.Property.Id)+"/announcements/"+a.Id,new AnnouncementRequest("Edited","Body","MANAGEMENT",a.Version));Assert.Equal(HttpStatusCode.OK,r.StatusCode);a=(await r.Content.ReadFromJsonAsync<AnnouncementResponse>())!;
        var created=a.CreatedAt;r=await c.PutAsJsonAsync(Root(x.Property.Id)+$"/announcements/{a.Id}/publish",new{version=a.Version});Assert.Equal(HttpStatusCode.OK,r.StatusCode);a=(await r.Content.ReadFromJsonAsync<AnnouncementResponse>())!;Assert.Equal("PUBLISHED",a.Status);Assert.NotNull(a.PublishedAt);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(Root(x.Property.Id)+"/announcements/"+a.Id,new AnnouncementRequest("Rewrite","Body","ALL_MEMBERS",a.Version))).StatusCode);
        r=await c.PutAsJsonAsync(Root(x.Property.Id)+$"/announcements/{a.Id}/archive",new{version=a.Version});Assert.Equal(HttpStatusCode.OK,r.StatusCode);a=(await r.Content.ReadFromJsonAsync<AnnouncementResponse>())!;Assert.Equal(created,a.CreatedAt);Assert.Equal("ARCHIVED",a.Status);
        await factory.WithDb(async db=>{var row=await db.Announcements.SingleAsync(v=>v.Id==a.Id);Assert.Equal(x.Actor,row.CreatedBy);Assert.Equal(x.Actor,row.ModifiedBy);Assert.Equal(4,row.Version);});
    }
    [PostgresFact] public async Task MemberAudienceDraftAndArchivedVisibilityCannotBeBypassed()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");using var resident=TestToken.Client(factory,x.Actor,"RESIDENT");var a=await Draft(c,x.Property.Id);var management=await Draft(c,x.Property.Id,"MANAGEMENT");
        Assert.Equal(HttpStatusCode.NotFound,(await resident.GetAsync(Root(x.Property.Id)+"/announcements/"+a.Id)).StatusCode);
        foreach(var row in new[]{a,management})Assert.Equal(HttpStatusCode.OK,(await c.PutAsJsonAsync(Root(x.Property.Id)+$"/announcements/{row.Id}/publish",new{version=row.Version})).StatusCode);
        Assert.Equal(a.Id,Assert.Single((await resident.GetFromJsonAsync<ContentPage<AnnouncementResponse>>(Root(x.Property.Id)+"/announcements"))!.Items).Id);
        Assert.Equal(HttpStatusCode.NotFound,(await resident.GetAsync(Root(x.Property.Id)+"/announcements/"+management.Id)).StatusCode);
        a=(await c.GetFromJsonAsync<AnnouncementResponse>(Root(x.Property.Id)+"/announcements/"+a.Id))!;
        await c.PutAsJsonAsync(Root(x.Property.Id)+$"/announcements/{a.Id}/archive",new{version=a.Version});Assert.Empty((await resident.GetFromJsonAsync<ContentPage<AnnouncementResponse>>(Root(x.Property.Id)+"/announcements?status=ARCHIVED"))!.Items);
    }
    [PostgresFact] public async Task NegativeRolesJwtMembershipAndParentGuards()
    {
        var x=await Seed();using var admin=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var a=await Draft(admin,x.Property.Id);
        foreach(var role in new[]{"RESIDENT","ACCOUNTANT","client-role","spoofed"}){using var c=TestToken.Client(factory,x.Actor,role);Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync(Root(x.Property.Id)+"/announcements",new AnnouncementRequest("x","x","ALL_MEMBERS"))).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await c.PutAsJsonAsync(Root(x.Property.Id)+$"/announcements/{a.Id}/publish",new{version=a.Version})).StatusCode);using var f=Form();Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsync(Root(x.Property.Id)+"/documents",f)).StatusCode);}
        using var anonymous=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(Root(x.Property.Id)+"/announcements")).StatusCode);
        anonymous.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer","invalid");Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(Root(x.Property.Id)+"/documents")).StatusCode);
        using var outsider=TestToken.Client(factory,Guid.NewGuid().ToString(),"MANAGER");Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(Root(x.Property.Id)+"/announcements")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await outsider.GetAsync(Root(x.Property.Id)+"/documents")).StatusCode);
        await factory.WithDb(async db=>{var m=await db.PropertyMemberships.SingleAsync(v=>v.PropertyId==x.Property.Id);m.SetEnabled(false);await db.SaveChangesAsync();});using var member=TestToken.Client(factory,x.Actor,"MANAGER");Assert.Equal(HttpStatusCode.NotFound,(await member.GetAsync(Root(x.Property.Id)+"/announcements")).StatusCode);
        await factory.WithDb(async db=>{var p=await db.Properties.SingleAsync(v=>v.Id==x.Property.Id);p.SetEnabled(false);await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.Conflict,(await admin.PutAsJsonAsync(Root(x.Property.Id)+$"/announcements/{a.Id}/publish",new{version=a.Version})).StatusCode);
    }
    [PostgresFact] public async Task VersionRaceAndInvalidQueriesAreCodedAndAtomic()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var a=await Draft(c,x.Property.Id);var root=Root(x.Property.Id)+$"/announcements/{a.Id}/publish";
        var race=await Task.WhenAll(c.PutAsJsonAsync(root,new{version=a.Version}),c.PutAsJsonAsync(root,new{version=a.Version}));Assert.Single(race,v=>v.StatusCode==HttpStatusCode.OK);Assert.Single(race,v=>v.StatusCode==HttpStatusCode.Conflict);
        foreach(var query in new[]{"page=0","pageSize=101","status=unknown","search="+new string('x',201)})Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Root(x.Property.Id)+"/announcements?"+query)).StatusCode);
        foreach(var request in new[]{new AnnouncementRequest("","x","ALL_MEMBERS"),new AnnouncementRequest("x","","ALL_MEMBERS"),new AnnouncementRequest("x","x","0"),new AnnouncementRequest("x",new string('x',10001),"ALL_MEMBERS")})Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync(Root(x.Property.Id)+"/announcements",request)).StatusCode);
        var search=(await c.GetFromJsonAsync<ContentPage<AnnouncementResponse>>(Root(x.Property.Id)+"/announcements?search=%25"))!;Assert.Equal(a.Id,Assert.Single(search.Items).Id);
        var p=(await c.GetFromJsonAsync<PropertyResponse>(Root(x.Property.Id)))!;Assert.Equal(HttpStatusCode.Conflict,(await c.DeleteAsync(Root(x.Property.Id)+"?version="+p.Version)).StatusCode);
    }
    [PostgresTheory][InlineData("test.pdf","application/pdf","fake")][InlineData("test.svg","image/svg+xml","<svg>")][InlineData("test.png","application/pdf","%PDF-1.7")]
    public async Task InvalidFileReturnsCoded400AndNoMetadata(string name,string type,string bytes)
    {var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");using var form=Form(name:name,type:type,bytes:System.Text.Encoding.UTF8.GetBytes(bytes));var r=await c.PostAsync(Root(x.Property.Id)+"/documents",form);Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);Assert.Contains("BF_DOCUMENT_INVALID",await r.Content.ReadAsStringAsync());await factory.WithDb(async db=>Assert.False(await db.StoredDocuments.AnyAsync(v=>v.PropertyId==x.Property.Id)));}
    [PostgresFact] public async Task FileTooLargeAndWrongMediaAreCoded()
    {var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");using var form=Form(bytes:new byte[StoredDocument.MaxBytes+1]);var r=await c.PostAsync(Root(x.Property.Id)+"/documents",form);Assert.Equal(HttpStatusCode.RequestEntityTooLarge,r.StatusCode);Assert.Contains("BF_DOCUMENT_TOO_LARGE",await r.Content.ReadAsStringAsync());Assert.Equal(HttpStatusCode.UnsupportedMediaType,(await c.PostAsJsonAsync(Root(x.Property.Id)+"/documents",new{})).StatusCode);}
    [StorageFact] public async Task RealStorageRoundTripPrivateBucketReplayAndSoftDeletion()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");var request=Guid.NewGuid();var d=await Upload(c,x.Property.Id,request);var root=Root(x.Property.Id)+$"/documents/{d.Id}";
        var r=await c.GetAsync(root+"/download");Assert.Equal(HttpStatusCode.OK,r.StatusCode);Assert.Equal(Pdf,await r.Content.ReadAsByteArrayAsync());Assert.Equal("no-store",r.Headers.CacheControl!.ToString());Assert.Equal("attachment",r.Content.Headers.ContentDisposition!.DispositionType);
        using(var f=Form(request)){r=await c.PostAsync(Root(x.Property.Id)+"/documents",f);Assert.Equal(HttpStatusCode.OK,r.StatusCode);Assert.True((await r.Content.ReadFromJsonAsync<DocumentResponse>())!.Replayed);}
        using(var f=Form(request,name:"different.pdf"))Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsync(Root(x.Property.Id)+"/documents",f)).StatusCode);
        string? key=null;await factory.WithDb(async db=>{var value=await db.StoredDocuments.SingleAsync(v=>v.Id==d.Id);key=value.ObjectKey;Assert.Equal(x.Actor,value.CreatedBy);Assert.Equal(1,value.Version);});
        using var direct=new HttpClient();Assert.Equal(HttpStatusCode.Forbidden,(await direct.GetAsync(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_STORAGE_ENDPOINT")+"/"+factory.StorageBucket+"/"+key)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.DeleteAsync(root+"?version=999")).StatusCode);Assert.Equal(HttpStatusCode.NoContent,(await c.DeleteAsync(root+"?version="+d.Version)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(root+"/download")).StatusCode);
        using(var f=Form(request))Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsync(Root(x.Property.Id)+"/documents",f)).StatusCode);
        await factory.WithDb(async db=>{var value=await db.StoredDocuments.IgnoreQueryFilters().SingleAsync(v=>v.Id==d.Id);Assert.True(value.IsDeleted);Assert.Equal(2,value.Version);Assert.Equal(d.Sha256,value.Sha256);});
    }
    [StorageFact] public async Task AttachmentsInheritPublicationAndPropertyScope()
    {
        var x=await Seed();var other=await Seed();using var c=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");using var reader=TestToken.Client(factory,x.Actor,"RESIDENT");var a=await Draft(c,x.Property.Id);var d=await Upload(c,x.Property.Id,announcement:a.Id,version:a.Version);var url=Root(x.Property.Id)+$"/documents/{d.Id}/download";
        Assert.Equal(HttpStatusCode.NotFound,(await reader.GetAsync(url)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await reader.GetAsync(Root(x.Property.Id)+"/documents?announcementId="+a.Id)).StatusCode);
        using(var f=Form(announcement:a.Id,version:a.Version))Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsync(Root(other.Property.Id)+"/documents",f)).StatusCode);
        a=(await c.GetFromJsonAsync<AnnouncementResponse>(Root(x.Property.Id)+"/announcements/"+a.Id))!;Assert.Equal(2,a.Version);
        await c.PutAsJsonAsync(Root(x.Property.Id)+$"/announcements/{a.Id}/publish",new{version=a.Version});Assert.Equal(HttpStatusCode.OK,(await reader.GetAsync(url)).StatusCode);Assert.Equal(d.Id,Assert.Single((await reader.GetFromJsonAsync<ContentPage<DocumentResponse>>(Root(x.Property.Id)+"/documents"))!.Items).Id);
        using(var f=Form(announcement:a.Id,version:3))Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsync(Root(x.Property.Id)+"/documents",f)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.DeleteAsync(Root(x.Property.Id)+$"/documents/{d.Id}?version={d.Version}")).StatusCode);
        using var outside=TestToken.Client(factory,other.Actor,"MANAGER");Assert.Equal(HttpStatusCode.NotFound,(await outside.GetAsync(url)).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(other.Property.Id)+$"/documents/{d.Id}/download")).StatusCode);
        await c.PutAsJsonAsync(Root(x.Property.Id)+$"/announcements/{a.Id}/archive",new{version=3});Assert.Equal(HttpStatusCode.NotFound,(await reader.GetAsync(url)).StatusCode);
    }
    [StorageFact] public async Task StandaloneManagementFilesNeverLeakToResidentsOrAccountants()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"MANAGER");var d=await Upload(c,x.Property.Id,audience:"MANAGEMENT");
        foreach(var role in new[]{"RESIDENT","ACCOUNTANT","spoofed","client-role"}){using var member=TestToken.Client(factory,x.Actor,role);Assert.Empty((await member.GetFromJsonAsync<ContentPage<DocumentResponse>>(Root(x.Property.Id)+"/documents"))!.Items);Assert.Equal(HttpStatusCode.NotFound,(await member.GetAsync(Root(x.Property.Id)+$"/documents/{d.Id}/download")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await member.DeleteAsync(Root(x.Property.Id)+$"/documents/{d.Id}?version={d.Version}")).StatusCode);}
        await factory.WithDb(async db=>{var p=await db.Properties.SingleAsync(v=>v.Id==x.Property.Id);db.Remove(p);await db.SaveChangesAsync();});Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(Root(x.Property.Id)+$"/documents/{d.Id}/download")).StatusCode);
    }
    [StorageFact] public async Task ConcurrentUploadAndTamperedStorageNeverProduceDuplicateOrCorruptDownload()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var id=Guid.NewGuid();using var f=Form(id);using var g=Form(id);var race=await Task.WhenAll(c.PostAsync(Root(x.Property.Id)+"/documents",f),c.PostAsync(Root(x.Property.Id)+"/documents",g));Assert.Single(race,r=>r.StatusCode==HttpStatusCode.Created);Assert.Single(race,r=>r.StatusCode==HttpStatusCode.OK);var d=(await race[0].Content.ReadFromJsonAsync<DocumentResponse>())!;
        string? key=null;await factory.WithDb(async db=>{var v=Assert.Single(await db.StoredDocuments.Where(v=>v.PropertyId==x.Property.Id).ToListAsync());key=v.ObjectKey;});
        using var scope=factory.Services.CreateScope();var storage=scope.ServiceProvider.GetRequiredService<IFileStorage>();using var content=new MemoryStream("%PDF-changed"u8.ToArray());await storage.UploadAsync(key!,content,"application/pdf",CancellationToken.None);
        var r=await c.GetAsync(Root(x.Property.Id)+$"/documents/{d.Id}/download");Assert.Equal(HttpStatusCode.ServiceUnavailable,r.StatusCode);Assert.Contains("BF_STORAGE_UNAVAILABLE",await r.Content.ReadAsStringAsync());
    }
    [StorageFact] public async Task AttachmentLimitAndConcurrentPublicationPreserveVersions()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var a=await Draft(c,x.Property.Id);
        for(var i=0;i<10;i++){await Upload(c,x.Property.Id,announcement:a.Id,version:a.Version);a=(await c.GetFromJsonAsync<AnnouncementResponse>(Root(x.Property.Id)+"/announcements/"+a.Id))!;}
        using(var f=Form(announcement:a.Id,version:a.Version)){var r=await c.PostAsync(Root(x.Property.Id)+"/documents",f);Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);Assert.Contains("BF_DOCUMENT_LIMIT",await r.Content.ReadAsStringAsync());}
        var attachments=(await c.GetFromJsonAsync<ContentPage<DocumentResponse>>(Root(x.Property.Id)+"/documents?announcementId="+a.Id))!;Assert.Equal(10,attachments.TotalCount);
        var empty=await Draft(c,x.Property.Id);using var form=Form(announcement:empty.Id,version:empty.Version);
        var race=await Task.WhenAll(c.PostAsync(Root(x.Property.Id)+"/documents",form),c.PutAsJsonAsync(Root(x.Property.Id)+$"/announcements/{empty.Id}/publish",new{version=empty.Version}));
        Assert.Single(race,v=>v.StatusCode==HttpStatusCode.Conflict);Assert.Single(race,v=>v.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK);
    }
    [StorageFact] public async Task ProviderRefusesAnUnexpectedBucketPolicy()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Actor,"SUPER_ADMIN");var d=await Upload(c,x.Property.Id);
        using var storage=StorageTestClient.Create();
        var policy=System.Text.Json.JsonSerializer.Serialize(new{Version="2012-10-17",Statement=new[]{new{Effect="Deny",Principal="*",Action=new[]{"s3:GetObject"},Resource=new[]{"arn:aws:s3:::"+factory.StorageBucket+"/*"}}}});
        await storage.PutBucketPolicyAsync(new PutBucketPolicyRequest {BucketName=factory.StorageBucket,Policy=policy});
        try{var r=await c.GetAsync(Root(x.Property.Id)+$"/documents/{d.Id}/download");Assert.Equal(HttpStatusCode.ServiceUnavailable,r.StatusCode);Assert.Contains("BF_STORAGE_UNAVAILABLE",await r.Content.ReadAsStringAsync());}
        finally{await storage.DeleteBucketPolicyAsync(new DeleteBucketPolicyRequest {BucketName=factory.StorageBucket});}
    }

}
public sealed class StorageFactAttribute : FactAttribute
{ public StorageFactAttribute(){if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE"))||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_STORAGE_ENDPOINT")))Skip="Dedicated PostgreSQL and SeaweedFS S3 test configuration required.";} }
