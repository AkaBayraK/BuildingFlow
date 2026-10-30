using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class SupplierDocumentsEndpoints
{
    public static void MapSupplierDocumentsEndpoints(this WebApplication app)
    {
        var root=app.MapGroup("/api/v2/tenants/{tenantId:guid}/suppliers/{supplierId:guid}").RequireAuthorization("CanManagePeople").WithTags("Supplier documents and contracts");
        root.MapGet("/documents",async(Guid tenantId,Guid supplierId,ISupplierDocumentsStore store,CancellationToken token,int page=1,int pageSize=10)=>Found(await store.Documents(tenantId,supplierId,page,pageSize,token)));
        root.MapPost("/documents",Upload);
        root.MapGet("/documents/{documentId:guid}/download",async(Guid tenantId,Guid supplierId,Guid documentId,ISupplierDocumentsStore store,HttpResponse response,CancellationToken token)=>{
            var result=await store.Download(tenantId,supplierId,documentId,token);if(result is null)return Results.NotFound();
            response.Headers.CacheControl="no-store";response.Headers["X-Content-Type-Options"]="nosniff";
            return Results.File(result.Content,result.Metadata.ContentType,result.Metadata.FileName,enableRangeProcessing:false);
        });
        root.MapPut("/documents/{documentId:guid}/remove",async(Guid tenantId,Guid supplierId,Guid documentId,SupplierFileRemove request,ISupplierDocumentsStore store,CancellationToken token)=>Found(await store.RemoveDocument(tenantId,supplierId,documentId,request,token)));
        root.MapGet("/contracts",async(Guid tenantId,Guid supplierId,ISupplierDocumentsStore store,CancellationToken token,int page=1,int pageSize=10,bool renewalDue=false)=>Found(await store.Contracts(tenantId,supplierId,page,pageSize,renewalDue,token)));
        root.MapGet("/contracts/{contractId:guid}",async(Guid tenantId,Guid supplierId,Guid contractId,ISupplierDocumentsStore store,CancellationToken token)=>Found(await store.Contract(tenantId,supplierId,contractId,token)));
        root.MapPost("/contracts",async(Guid tenantId,Guid supplierId,SupplierContractRequest request,ISupplierDocumentsStore store,CancellationToken token)=>Found(await store.SaveContract(tenantId,supplierId,null,request,token)));
        root.MapPut("/contracts/{contractId:guid}",async(Guid tenantId,Guid supplierId,Guid contractId,SupplierContractRequest request,ISupplierDocumentsStore store,CancellationToken token)=>Found(await store.SaveContract(tenantId,supplierId,contractId,request,token)));
        root.MapPut("/contracts/{contractId:guid}/activate",async(Guid tenantId,Guid supplierId,Guid contractId,SupplierContractTransition request,ISupplierDocumentsStore store,CancellationToken token)=>Found(await store.TransitionContract(tenantId,supplierId,contractId,"ACTIVATE",request,token)));
        root.MapPut("/contracts/{contractId:guid}/cancel",async(Guid tenantId,Guid supplierId,Guid contractId,SupplierContractTransition request,ISupplierDocumentsStore store,CancellationToken token)=>Found(await store.TransitionContract(tenantId,supplierId,contractId,"CANCEL",request,token)));
        root.MapGet("/contracts/{contractId:guid}/history",async(Guid tenantId,Guid supplierId,Guid contractId,ISupplierDocumentsStore store,CancellationToken token,int page=1,int pageSize=10)=>Found(await store.ContractHistory(tenantId,supplierId,contractId,page,pageSize,token)));
    }
    private static async Task<IResult> Upload(Guid tenantId,Guid supplierId,HttpRequest request,ISupplierDocumentsStore store,CancellationToken token)
    {
        var upload=await CommunicationEndpoints.ReadUpload(request,token);var form=upload.Form;
        Guid property=Guid.Empty;long version=0;
        if(!Guid.TryParse(form["operationId"],out var operation) || !Guid.TryParse(form["propertyId"],out property) || !long.TryParse(form["supplierVersion"],out version))CommunicationService.Invalid("file",ErrorCatalog.SupplierInvalid);
        DateOnly? expires=null;Guid? previous=null;
        if(!string.IsNullOrEmpty(form["expiresOn"])) {if(!DateOnly.TryParseExact(form["expiresOn"],"yyyy-MM-dd",out var day))CommunicationService.Invalid("expiresOn",ErrorCatalog.SupplierInvalid);expires=day;}
        if(!string.IsNullOrEmpty(form["previousDocumentId"])) {if(!Guid.TryParse(form["previousDocumentId"],out var id))CommunicationService.Invalid("previousDocumentId",ErrorCatalog.SupplierInvalid);previous=id;}
        var result=await store.Upload(tenantId,supplierId,new(operation,version,property,upload.File.FileName,upload.File.ContentType,upload.Bytes,expires,previous),token);
        return Found(result);
    }
    private static IResult Found<T>(T? value) where T:class=>value is null?Results.NotFound():Results.Ok(value);
}
