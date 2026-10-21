using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class SurveyEndpoints
{
    public static void MapSurveyEndpoints(this WebApplication app)
    {
        // Management: survey content and results are visible only to the three management roles with an active membership.
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}/surveys").RequireAuthorization("CanManageHierarchy").WithTags("Surveys");
        root.MapGet("", async (Guid propertyId, SurveyService service, CancellationToken token, int page = 1, int pageSize = 10, string? search = null, string? state = null, string? category = null, DateOnly? from = null, DateOnly? to = null) => Found(await service.List(propertyId, new(page, pageSize, search, state, category, from, to), token)));
        root.MapGet("/overview", async (Guid propertyId, SurveyService service, CancellationToken token) => Found(await service.Overview(propertyId, token)));
        root.MapGet("/{surveyId:guid}", async (Guid propertyId, Guid surveyId, SurveyService service, CancellationToken token) => Found(await service.Get(propertyId, surveyId, token)));
        root.MapGet("/{surveyId:guid}/results", async (Guid propertyId, Guid surveyId, SurveyService service, HttpResponse response, CancellationToken token) => {
            response.Headers.CacheControl = "no-store"; return Found(await service.Results(propertyId, surveyId, token));
        });
        root.MapPost("", async (Guid propertyId, SurveyRequest request, SurveyService service, CancellationToken token) => Created(propertyId, await service.Save(propertyId, null, request, token)));
        root.MapPut("/{surveyId:guid}", async (Guid propertyId, Guid surveyId, SurveyRequest request, SurveyService service, CancellationToken token) => Found(await service.Save(propertyId, surveyId, request, token)));
        root.MapPost("/{surveyId:guid}/copy", async (Guid propertyId, Guid surveyId, SurveyService service, CancellationToken token) => Created(propertyId, await service.Copy(propertyId, surveyId, token)));
        root.MapPut("/{surveyId:guid}/publish", async (Guid propertyId, Guid surveyId, SurveyVersionRequest request, SurveyService service, CancellationToken token) => Found(await service.Publish(propertyId, surveyId, request.Version, token)));
        root.MapPut("/{surveyId:guid}/schedule", async (Guid propertyId, Guid surveyId, SurveyScheduleRequest request, SurveyService service, CancellationToken token) => Found(await service.Extend(propertyId, surveyId, request, token)));
        root.MapPut("/{surveyId:guid}/close", async (Guid propertyId, Guid surveyId, SurveyVersionRequest request, SurveyService service, CancellationToken token) => Found(await service.Close(propertyId, surveyId, request.Version, token)));
        root.MapDelete("/{surveyId:guid}", async (Guid propertyId, Guid surveyId, long version, SurveyService service, CancellationToken token) => await service.Delete(propertyId, surveyId, version, token) ? Results.NoContent() : Results.NotFound());

        // Residents (mobile): answer once through one of their own active unit contexts.
        var mobile = app.MapGroup("/api/v1/mobile/surveys").RequireAuthorization().WithTags("Mobile surveys");
        mobile.MapGet("", async (Guid propertyId, Guid unitId, string role, SurveyService service, CancellationToken token) => Results.Ok(await service.Open(propertyId, unitId, role, token)));
        mobile.MapGet("/{surveyId:guid}", async (Guid surveyId, Guid propertyId, Guid unitId, string role, SurveyService service, CancellationToken token) => Results.Ok(await service.Detail(propertyId, unitId, role, surveyId, token)));
        mobile.MapPost("/{surveyId:guid}/responses", async (Guid surveyId, ResidentSurveySubmit request, SurveyService service, CancellationToken token) => {
            var receipt = await service.Submit(surveyId, request, token); return Results.Created($"/api/v1/mobile/surveys/{surveyId}", receipt);
        });
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
    private static IResult Created(Guid propertyId, SurveyDetail? value) => value is null ? Results.NotFound() : Results.Created($"/api/v1/properties/{propertyId}/surveys/{value.Survey.Id}", value);
}
