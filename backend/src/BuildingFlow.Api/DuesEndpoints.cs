using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class DuesEndpoints
{
    public static void MapDuesEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}/dues").WithTags("Dues").RequireAuthorization("CanManageDues");
        root.MapGet("", async (Guid propertyId, DuesService service, CancellationToken token,
            int page = 1, int pageSize = 10, string? search = null, Guid? unitId = null, string? period = null,
            DateOnly? dueFrom = null, DateOnly? dueTo = null, string? status = null, string sortBy = "dueDate", string sortDirection = "desc", Guid? personId = null, string? partyRole = null, string? kind = null) =>
            Found(await service.ListAsync(propertyId, new(page, pageSize, search, unitId, period, dueFrom, dueTo, status, sortBy, sortDirection, personId, partyRole, kind), token)));
        root.MapGet("/person-choices", async (Guid propertyId,DuesService service,CancellationToken token,int page=1,int pageSize=20,string? search=null) => Found(await service.PersonChoicesAsync(propertyId,new(page,pageSize,search),token)));
        root.MapGet("/cancellation-reasons", async (Guid propertyId,DuesService service,CancellationToken token) => Found(await service.CancellationReasonsAsync(propertyId,token)));
        root.MapGet("/{chargeId:guid}", async (Guid propertyId, Guid chargeId, DuesService service, CancellationToken token) => Found(await service.GetAsync(propertyId, chargeId, token)));
        root.MapPut("/{chargeId:guid}/cancel", async (Guid propertyId, Guid chargeId, CancelChargeRequest request, DuesService service, CancellationToken token) => Found(await service.CancelAsync(propertyId, chargeId, request, token)));
        root.MapPost("/assessments", async (Guid propertyId, CreateAssessmentRequest request, DuesService service, CancellationToken token) => {
            var result = await service.CreateAsync(propertyId, request, token);
            return result is null ? Results.NotFound() : result.Replayed ? Results.Ok(result) : Results.Created($"/api/v1/properties/{propertyId}/dues/assessments/{result.Id}", result);
        });
        root.MapGet("/assessments/{assessmentId:guid}", async (Guid propertyId, Guid assessmentId, DuesService service, CancellationToken token) => Found(await service.GetAssessmentAsync(propertyId, assessmentId, token)));
        root.MapGet("/unit-choices", async (Guid propertyId, DuesService service, CancellationToken token, int page = 1, int pageSize = 10, string? search = null) => Found(await service.UnitChoicesAsync(propertyId, new(page, pageSize, search), token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
