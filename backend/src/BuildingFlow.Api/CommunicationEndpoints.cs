using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.AspNetCore.Http.Features;
namespace BuildingFlow.Api;
internal static class CommunicationEndpoints
{
    public static void MapCommunicationEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}").WithTags("Announcements and documents").RequireAuthorization("CanReadProperties");
        root.MapGet("/announcements", async (Guid propertyId, CommunicationService service, CancellationToken token, int page = 1, int pageSize = 10, string? search = null, string? status = null, string? category = null) => Found(await service.Announcements(propertyId, new(page, pageSize, search, status, Category: category), token)));
        root.MapGet("/announcements/{id:guid}", async (Guid propertyId, Guid id, CommunicationService service, CancellationToken token) => Found(await service.Announcement(propertyId, id, token)));
        root.MapPost("/announcements", async (Guid propertyId, AnnouncementRequest request, CommunicationService service, CancellationToken token) => {
            var result = await service.Save(propertyId, null, request, token);
            return result is null ? Results.NotFound() : Results.Created($"/api/v1/properties/{propertyId}/announcements/{result.Id}", result);
        }).RequireAuthorization("CanManageHierarchy");
        root.MapPut("/announcements/{id:guid}", async (Guid propertyId, Guid id, AnnouncementRequest request, CommunicationService service, CancellationToken token) => Found(await service.Save(propertyId, id, request, token))).RequireAuthorization("CanManageHierarchy");
        root.MapPut("/announcements/{id:guid}/publish", async (Guid propertyId, Guid id, ContentVersionRequest request, CommunicationService service, CancellationToken token) => Found(await service.ChangeStatus(propertyId, id, request.Version, true, token))).RequireAuthorization("CanManageHierarchy");
        root.MapPut("/announcements/{id:guid}/archive", async (Guid propertyId, Guid id, ContentVersionRequest request, CommunicationService service, CancellationToken token) => Found(await service.ChangeStatus(propertyId, id, request.Version, false, token))).RequireAuthorization("CanManageHierarchy");
        root.MapGet("/documents", async (Guid propertyId, CommunicationService service, CancellationToken token, int page = 1, int pageSize = 10, string? search = null, Guid? announcementId = null) => Found(await service.Documents(propertyId, new(page, pageSize, search, AnnouncementId: announcementId), token)));
        root.MapPost("/documents", Upload).RequireAuthorization("CanManageHierarchy");
        root.MapGet("/documents/{id:guid}/download", async (Guid propertyId, Guid id, CommunicationService service, HttpResponse response, CancellationToken token) => {
            var result = await service.Download(propertyId, id, token); if (result is null) return Results.NotFound();
            response.Headers.CacheControl = "no-store"; response.Headers["X-Content-Type-Options"] = "nosniff";
            return Results.File(result.Content, result.Metadata.ContentType, result.Metadata.FileName, enableRangeProcessing: false);
        });
        root.MapDelete("/documents/{id:guid}", async (Guid propertyId, Guid id, long version, CommunicationService service, CancellationToken token) => await service.Delete(propertyId, id, version, token) ? Results.NoContent() : Results.NotFound()).RequireAuthorization("CanManageHierarchy");
    }
    private static async Task<IResult> Upload(Guid propertyId, HttpRequest request, CommunicationService service, CancellationToken token)
    {
        var upload = await ReadUpload(request, token); var form = upload.Form; var file = upload.File;
        if (!Guid.TryParse(form["requestId"], out var requestId)) CommunicationService.Invalid("requestId", ErrorCatalog.DuesErrorRequestId);
        Guid? announcement = null; long version = 0;
        if (!string.IsNullOrEmpty(form["announcementId"])) {
            if (!Guid.TryParse(form["announcementId"], out var id) || id == Guid.Empty || !long.TryParse(form["announcementVersion"], out version)) CommunicationService.Invalid("announcementId", ErrorCatalog.ContentInvalid);
            announcement = id;
        }
        var result = await service.Upload(propertyId, new(requestId, announcement, version, CommunicationService.Audience(form["audience"]), file.FileName, file.ContentType, upload.Bytes), token);
        return result is null ? Results.NotFound() : result.Replayed ? Results.Ok(result) : Results.Created($"/api/v1/properties/{propertyId}/documents/{result.Id}/download", result);
    }
    internal static async Task<(IFormCollection Form, IFormFile File, byte[] Bytes)> ReadUpload(HttpRequest request, CancellationToken token)
    {
        const long requestLimit = StoredDocument.MaxBytes + 65536;
        if (!request.HasFormContentType) throw new BadHttpRequestException("Multipart form required.",415);
        if (request.ContentLength > requestLimit) throw new DocumentSizeException();
        var feature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false }) feature.MaxRequestBodySize = requestLimit;
        IFormCollection form;
        try { form = await request.ReadFormAsync(new FormOptions { MultipartBodyLengthLimit = requestLimit, ValueLengthLimit = 1000, ValueCountLimit = 10, MultipartHeadersCountLimit = 16 }, token); }
        catch (InvalidDataException) { CommunicationService.Invalid("file", ErrorCatalog.DocumentInvalid); throw; }
        if (form.Files.Count != 1 || form.Files[0].Name != "file") CommunicationService.Invalid("file", ErrorCatalog.DocumentInvalid);
        var file = form.Files[0]; if (file.Length > StoredDocument.MaxBytes) throw new DocumentSizeException();
        // Limited copy even if a forged multipart length reports a smaller file.
        using var content = new MemoryStream(); using var source = file.OpenReadStream(); var buffer = new byte[81920]; int read;
        while ((read = await source.ReadAsync(buffer, token)) > 0) {
            if (content.Length + read > StoredDocument.MaxBytes) throw new DocumentSizeException();
            await content.WriteAsync(buffer.AsMemory(0, read), token);
        }
        return (form, file, content.ToArray());
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
public sealed record ContentVersionRequest(long Version);
