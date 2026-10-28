using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class CommonAreaEndpoints
{
    public static void MapCommonAreaEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}/common-areas").RequireAuthorization("CanManageHierarchy").WithTags("Common areas");
        root.MapGet("", async (Guid propertyId, CommonAreaService service, CancellationToken token, int page = 1, int pageSize = 20, string? search = null, string? type = null, Guid? buildingId = null, bool? isEnabled = null) => Found(await service.List(propertyId, new(page, pageSize, search, type, buildingId, isEnabled), token)));
        root.MapGet("/{areaId:guid}", async (Guid propertyId, Guid areaId, CommonAreaService service, CancellationToken token) => Found(await service.Get(propertyId, areaId, token)));
        root.MapPost("", async (Guid propertyId, CommonAreaRequest request, CommonAreaService service, CancellationToken token) => {
            var result = await service.Save(propertyId, null, request, token);
            return result is null ? Results.NotFound() : result.Replayed ? Results.Ok(result) : Results.Created($"/api/v1/properties/{propertyId}/common-areas/{result.Id}", result);
        });
        root.MapPut("/{areaId:guid}", async (Guid propertyId, Guid areaId, CommonAreaRequest request, CommonAreaService service, CancellationToken token) => Found(await service.Save(propertyId, areaId, request, token)));
        root.MapDelete("/{areaId:guid}", async (Guid propertyId, Guid areaId, long version, CommonAreaService service, CancellationToken token) => await service.Delete(propertyId, areaId, version, token) ? Results.NoContent() : Results.NotFound());
        root.MapGet("/{areaId:guid}/photos", async (Guid propertyId, Guid areaId, ICommonAreaPhotoStore store, CancellationToken token) => Found(await store.Photos(propertyId, areaId, null, token)));
        root.MapPost("/{areaId:guid}/photos", async (Guid propertyId, Guid areaId, CommonAreaPhotoRequest request, ICommonAreaPhotoStore store, CancellationToken token) => Found(await store.Link(propertyId, areaId, request, token)));
        root.MapDelete("/{areaId:guid}/photos/{photoId:guid}", async (Guid propertyId, Guid areaId, Guid photoId, long version, ICommonAreaPhotoStore store, CancellationToken token) => await store.Unlink(propertyId, areaId, photoId, version, token) ? Results.NoContent() : Results.NotFound());
        root.MapGet("/{areaId:guid}/photos/{photoId:guid}/download", async (Guid propertyId, Guid areaId, Guid photoId, ICommonAreaPhotoStore store, CancellationToken token) => await Photo(await store.DownloadPhoto(propertyId, areaId, photoId, null, token)));
        root.MapGet("/{areaId:guid}/reservations", async (Guid propertyId, Guid areaId, CommonAreaReservationService service, CancellationToken token, string? from = null, string? to = null, string? status = null, int page = 1, int pageSize = 50) => Found(await service.Reservations(propertyId, areaId, new(from, to, status, page, pageSize), token)));
        root.MapPost("/{areaId:guid}/reservations", async (Guid propertyId, Guid areaId, CommonAreaReservationRequest request, CommonAreaReservationService service, CancellationToken token) => {
            var result = await service.Save(propertyId, areaId, null, request, token);
            return result is null ? Results.NotFound() : result.Replayed ? Results.Ok(result) : Results.Created($"/api/v1/properties/{propertyId}/common-areas/{areaId}/reservations/{result.Id}", result);
        });
        root.MapPut("/{areaId:guid}/reservations/{reservationId:guid}", async (Guid propertyId, Guid areaId, Guid reservationId, CommonAreaReservationRequest request, CommonAreaReservationService service, CancellationToken token) => Found(await service.Save(propertyId, areaId, reservationId, request, token)));
        root.MapPost("/{areaId:guid}/reservations/{reservationId:guid}/cancel", async (Guid propertyId, Guid areaId, Guid reservationId, CommonAreaReservationCancelRequest request, CommonAreaReservationService service, CancellationToken token) => Found(await service.Cancel(propertyId, areaId, reservationId, request, token)));
        root.MapGet("/{areaId:guid}/report", async (Guid propertyId, Guid areaId, string? from, string? to, CommonAreaReservationService service, CancellationToken token) => Found(await service.Report(propertyId, areaId, from, to, token)));
        root.MapGet("/{areaId:guid}/schedule", async (Guid propertyId, Guid areaId, string? date, CommonAreaReservationService service, CancellationToken token) => Found(await service.Schedule(propertyId, areaId, date, null, token)));
        root.MapGet("/{areaId:guid}/closures",async(Guid propertyId,Guid areaId,string date,CommonAreaClosureService service,CancellationToken token)=>Found(await service.List(propertyId,areaId,date,token)));
        root.MapPost("/{areaId:guid}/closures",async(Guid propertyId,Guid areaId,CommonAreaClosureRequest request,CommonAreaClosureService service,CancellationToken token)=>Found(await service.Close(propertyId,areaId,request,token)));
        root.MapPost("/{areaId:guid}/closures/{closureId:guid}/release",async(Guid propertyId,Guid areaId,Guid closureId,CommonAreaClosureReleaseRequest request,CommonAreaClosureService service,CancellationToken token)=>Found(await service.Release(propertyId,areaId,closureId,request,token)));
        var me = app.MapGroup("/api/v2/me/units/{unitId:guid}/common-areas").RequireAuthorization();
        me.AddEndpointFilter(async (context, next) => app.Configuration.GetValue<bool>("PersonRefactor:Enabled") && app.Configuration.GetValue<bool>("PersonRefactor:SessionContextsEnabled") ? await next(context) : Results.NotFound());
        me.MapGet("/{areaId:guid}/photos", async (Guid propertyId, Guid unitId, Guid areaId, string capacity, Guid relationId, ICommonAreaPhotoStore store, CancellationToken token) => Found(await store.Photos(propertyId, areaId, new(propertyId, unitId, capacity, relationId), token)));
        me.MapGet("/{areaId:guid}/photos/{photoId:guid}/download", async (Guid propertyId, Guid unitId, Guid areaId, Guid photoId, string capacity, Guid relationId, ICommonAreaPhotoStore store, CancellationToken token) => await Photo(await store.DownloadPhoto(propertyId, areaId, photoId, new(propertyId, unitId, capacity, relationId), token)));
        me.MapGet("/{areaId:guid}/schedule", async (Guid propertyId, Guid unitId, Guid areaId, string capacity, Guid relationId, string? date, CommonAreaReservationService service, CancellationToken token) => Found(await service.Schedule(propertyId, areaId, date, new(propertyId, unitId, capacity, relationId), token)));
        me.MapGet("", async (Guid unitId, Guid propertyId, string capacity, Guid relationId, CommonAreaService service, CancellationToken token, int page = 1, int pageSize = 20, string? search = null, string? type = null) => Found(await service.ForUnit(new(propertyId, unitId, capacity, relationId), new(page, pageSize, search, type), null, token)));
        me.MapGet("/{areaId:guid}", async (Guid unitId, Guid areaId, Guid propertyId, string capacity, Guid relationId, CommonAreaService service, CancellationToken token) => {
            var result = await service.ForUnit(new(propertyId, unitId, capacity, relationId), new(), areaId, token);
            return result?.Items.SingleOrDefault() is {} value ? Results.Ok(value) : Results.NotFound();
        });
    }
    private static Task<IResult> Photo(DocumentDownload? file) => Task.FromResult<IResult>(file is null ? Results.NotFound() : Results.File(file.Content, file.Metadata.ContentType, file.Metadata.FileName, enableRangeProcessing: false));
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
