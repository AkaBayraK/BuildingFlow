using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierDocumentsContractsTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static readonly byte[] Pdf="%PDF-1.7\n supplier private fixture\n%%EOF"u8.ToArray();
    private static DateOnly Today=>DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private async Task<(Guid Tenant,Guid Supplier,Guid Property,Guid Area,string User)> Seed()
    {
        var t=new PersonTenant("Supplier documents fixture");var user=Guid.NewGuid().ToString();var a=new PersonUserAccount("http://localhost:8080/realms/buildingflow",user);var p=new Property("Fixture property","City","District","Address");var s=new Supplier(t.Id,"Document supplier",SupplierKind.COMPANY);s.ChangeStatus(SupplierStatus.ACTIVE,null);var category=new SupplierCategory(t.Id,"Maintenance");var service=new BuildingFlow.Domain.SupplierService(t.Id,s.Id,category.Id,"Elevator");var area=new SupplierAreaService(t.Id,s.Id,p.Id,service.Id);
        await factory.WithDb(async db=>{db.AddRange(t,a,p,s,category,service,area,new PersonTenantMembership(t.Id,a.Id),new PersonPropertyScope(t.Id,p.Id),new PropertyMembership(p.Id,user),new SupplierManagementArea(t.Id,s.Id,p.Id));await db.SaveChangesAsync();});return(t.Id,s.Id,p.Id,area.Id,user);
    }
    private static MultipartFormDataContent Form(Guid property,long version,Guid operation,Guid? previous=null,byte[]? bytes=null,string name="contract.pdf",string type="application/pdf")
    {
        var form=new MultipartFormDataContent();var file=new ByteArrayContent(bytes??Pdf);file.Headers.ContentType=new(type);form.Add(file,"file",name);form.Add(new StringContent(operation.ToString()),"operationId");form.Add(new StringContent(property.ToString()),"propertyId");form.Add(new StringContent(version.ToString()),"supplierVersion");form.Add(new StringContent("2027-12-31"),"expiresOn");if(previous.HasValue)form.Add(new StringContent(previous.ToString()!),"previousDocumentId");return form;
    }
    private static async Task<T> Value<T>(HttpResponseMessage r){Assert.Equal(HttpStatusCode.OK,r.StatusCode);return(await r.Content.ReadFromJsonAsync<T>())!;}
    [PostgresFact]public async Task PrivateUploadRevisionDownloadAndGenericRoutesCannotBypassSupplierScope()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.User,"MANAGER");var op=Guid.NewGuid();
        using var form=Form(x.Property,1,op);var one=await Value<SupplierFileMutation>(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents",form));Assert.Equal(1,one.Record.RevisionNumber);Assert.Equal(2,one.Supplier.Version);
        var download=await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents/{one.Record.Id}/download");Assert.Equal(Pdf,await download.Content.ReadAsByteArrayAsync());Assert.Equal("no-store",download.Headers.CacheControl!.ToString());Assert.Equal("attachment",download.Content.Headers.ContentDisposition!.DispositionType);
        var generic=$"/api/v1/properties/{x.Property}/documents";Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync(generic+$"/{one.Record.DocumentId}/download")).StatusCode);Assert.Equal(HttpStatusCode.NotFound,(await c.DeleteAsync(generic+$"/{one.Record.DocumentId}?version=1")).StatusCode);Assert.Empty((await c.GetFromJsonAsync<ContentPage<DocumentResponse>>(generic))!.Items);
        using var second=Form(x.Property,2,Guid.NewGuid(),one.Record.Id);var two=await Value<SupplierFileMutation>(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents",second));Assert.Equal(2,two.Record.RevisionNumber);Assert.Equal(one.Record.Id,two.Record.PreviousDocumentId);
        using var retry=Form(x.Property,1,op);var replay=await Value<SupplierFileMutation>(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents",retry));Assert.True(replay.Replayed);Assert.False(replay.Record.IsCurrent);Assert.Equal(two.Supplier.Version,replay.Supplier.Version);
        using var fork=Form(x.Property,two.Supplier.Version,Guid.NewGuid(),one.Record.Id);Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents",fork)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents/{one.Record.Id}/remove",new SupplierFileRemove(Guid.NewGuid(),two.Supplier.Version,1))).StatusCode);
        var files=(await c.GetFromJsonAsync<PagedRows<SupplierFileResponse>>($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents"))!;Assert.Equal(2,files.TotalCount);Assert.Single(files.Items,f=>f.IsCurrent);
        await factory.WithDb(async db=>await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE stored_documents SET is_supplier_only=false WHERE id={one.Record.DocumentId}")));
        await factory.WithDb(async db=>{var m=await db.PropertyMemberships.SingleAsync(m=>m.PropertyId==x.Property && m.UserId==x.User);m.SetEnabled(false);await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents/{two.Record.Id}/download")).StatusCode);Assert.Empty((await c.GetFromJsonAsync<PagedRows<SupplierFileResponse>>($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents"))!.Items);
    }
    [PostgresFact]public async Task UnreferencedFileRemovalIsVersionedSoftDeleteWithReplay()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.User,"MANAGER");using var form=Form(x.Property,1,Guid.NewGuid());var file=await Value<SupplierFileMutation>(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents",form));
        var remove=new SupplierFileRemove(Guid.NewGuid(),file.Supplier.Version,file.Record.Version);var result=await Value<SupplierMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents/{file.Record.Id}/remove",remove));Assert.Equal(3,result.Record.Version);
        Assert.True((await Value<SupplierMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents/{file.Record.Id}/remove",remove))).Replayed);Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents/{file.Record.Id}/download")).StatusCode);
        await factory.WithDb(async db=>{Assert.True((await db.Set<SupplierDocument>().IgnoreQueryFilters().SingleAsync(d=>d.Id==file.Record.Id)).IsDeleted);Assert.True((await db.StoredDocuments.IgnoreQueryFilters().SingleAsync(d=>d.Id==file.Record.DocumentId)).IsDeleted);});
    }
    [PostgresFact]public async Task ContractDraftActivationRenewalCancellationAndImmutableHistory()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.User,"MANAGER");
        var create=new SupplierContractRequest(Guid.NewGuid(),1,0,x.Property,x.Area,"Annual",Today.AddDays(-10),Today.AddDays(5),"1000.00");
        var one=await Value<SupplierContractMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts",create));Assert.Equal("DRAFT",one.Record.Status);Assert.Equal(one.Record,await c.GetFromJsonAsync<SupplierContractResponse>($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts/{one.Record.Id}"));
        var edit=create with{OperationId=Guid.NewGuid(),SupplierVersion=one.Supplier.Version,Version=one.Record.Version,Title="Revised"};var two=await Value<SupplierContractMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts/{one.Record.Id}",edit));
        var activate=new SupplierContractTransition(Guid.NewGuid(),two.Supplier.Version,two.Record.Version);var active=await Value<SupplierContractMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts/{one.Record.Id}/activate",activate));Assert.Equal("ACTIVE",active.Record.Status);Assert.True(active.Record.RenewalDue);
        Assert.Single((await c.GetFromJsonAsync<PagedRows<SupplierContractResponse>>($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts?renewalDue=true"))!.Items);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts/{one.Record.Id}",edit with{OperationId=Guid.NewGuid(),SupplierVersion=active.Supplier.Version,Version=active.Record.Version})).StatusCode);
        await factory.WithDb(async db=>await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_contracts SET amount=1 WHERE id={one.Record.Id}")));
        var renewal=create with{OperationId=Guid.NewGuid(),SupplierVersion=active.Supplier.Version,StartDate=create.EndDate.AddDays(1),EndDate=create.EndDate.AddYears(1),PreviousContractId=one.Record.Id};var next=await Value<SupplierContractMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts",renewal));Assert.Equal(one.Record.Id,next.Record.PreviousContractId);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts/{next.Record.Id}",renewal with{OperationId=Guid.NewGuid(),SupplierVersion=next.Supplier.Version,Version=next.Record.Version,StartDate=create.EndDate})).StatusCode);
        var replay=await Value<SupplierContractMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts",create));Assert.True(replay.Replayed);Assert.Equal(active.Record.Version,replay.Record.Version);Assert.Equal(next.Supplier.Version,replay.Supplier.Version);
        var cancelled=await Value<SupplierContractMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts/{one.Record.Id}/cancel",new SupplierContractTransition(Guid.NewGuid(),next.Supplier.Version,active.Record.Version,"Management decision")));Assert.Equal("CANCELLED",cancelled.Record.Status);
        var history=(await c.GetFromJsonAsync<PagedRows<SupplierContractHistory>>($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts/{one.Record.Id}/history"))!;Assert.Equal(4,history.TotalCount);Assert.Equal(new[]{"CANCEL","ACTIVATE","EDIT","CREATE"},history.Items.Select(x=>x.Action));
        await factory.WithDb(async db=>await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_contract_revisions SET snapshot='{{}}'::jsonb WHERE contract_id={one.Record.Id}")));
    }
    [PostgresFact]public async Task FileAndContractValidationRolesAndForeignScopeAreEnforced()
    {
        var x=await Seed();var other=await Seed();using var c=TestToken.Client(factory,x.User,"MANAGER");using var a=TestToken.Client(factory,x.User,"ACCOUNTANT");using var anonymous=factory.CreateClient();var root=$"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}";
        foreach(var path in new[]{"/documents","/contracts"}){Assert.Equal(HttpStatusCode.Forbidden,(await a.GetAsync(root+path)).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(root+path)).StatusCode);}
        using var foreign=Form(other.Property,1,Guid.NewGuid());Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents",foreign)).StatusCode);
        foreach(var bad in new[]{Form(x.Property,1,Guid.NewGuid(),bytes:"invalid"u8.ToArray()),Form(x.Property,1,Guid.NewGuid(),name:"contract.exe"),Form(x.Property,1,Guid.NewGuid(),type:"image/png")}){using(bad)Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents",bad)).StatusCode);}
        using var large=Form(x.Property,1,Guid.NewGuid(),bytes:new byte[StoredDocument.MaxBytes+1]);Assert.Equal(HttpStatusCode.RequestEntityTooLarge,(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents",large)).StatusCode);
        var request=new SupplierContractRequest(Guid.NewGuid(),1,0,x.Property,x.Area,"Annual",Today,Today.AddYears(1),"1.001");Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts",request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts",request with{Amount="100.00",PropertyId=other.Property})).StatusCode);
    }
    [PostgresFact]public async Task ActiveContractKeepsItsDocumentSnapshotAndPreventsAreaDeletion()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.User,"MANAGER");using var form=Form(x.Property,1,Guid.NewGuid());var file=await Value<SupplierFileMutation>(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents",form));
        var create=new SupplierContractRequest(Guid.NewGuid(),file.Supplier.Version,0,x.Property,x.Area,"Signed maintenance",Today,Today.AddYears(1),"1000.00",DocumentId:file.Record.Id);
        var draft=await Value<SupplierContractMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts",create));var active=await Value<SupplierContractMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts/{draft.Record.Id}/activate",new SupplierContractTransition(Guid.NewGuid(),draft.Supplier.Version,draft.Record.Version)));
        var rejected=await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents/{file.Record.Id}/remove",new SupplierFileRemove(Guid.NewGuid(),active.Supplier.Version,file.Record.Version));Assert.Equal(HttpStatusCode.Conflict,rejected.StatusCode);Assert.Contains("BF_SUPPLIER_DOCUMENT_PROTECTED",await rejected.Content.ReadAsStringAsync());
        using var replacement=Form(x.Property,active.Supplier.Version,Guid.NewGuid(),file.Record.Id);await Value<SupplierFileMutation>(await c.PostAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/documents",replacement));
        Assert.Equal(file.Record.Id,(await c.GetFromJsonAsync<SupplierContractResponse>($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/contracts/{draft.Record.Id}"))!.DocumentId);
        long version=0;await factory.WithDb(async db=>version=(await db.Properties.SingleAsync(p=>p.Id==x.Property)).Version);
        using var admin=TestToken.Client(factory,x.User,"SUPER_ADMIN");var deletion=await admin.DeleteAsync($"/api/v1/properties/{x.Property}?version={version}");Assert.Equal(HttpStatusCode.Conflict,deletion.StatusCode);Assert.Contains("BF_SUPPLIER_PROPERTY_HISTORY",await deletion.Content.ReadAsStringAsync());
    }

}
