using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class LatePenaltyEndpoints
{
    public static void MapLatePenaltyEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}/late-penalties").WithTags("Late penalties").RequireAuthorization("CanManageDues");
        root.MapGet("/settings", async (Guid propertyId, LatePenaltyService service, CancellationToken token) => Found(await service.SettingsAsync(propertyId, token)));
        root.MapPost("/switches", async (Guid propertyId, LatePenaltySwitchRequest request, LatePenaltyService service, CancellationToken token) =>
            Created(await service.SwitchAsync(propertyId, request, token), value => value.Replayed, value => $"/api/v1/properties/{propertyId}/late-penalties/settings"));
        root.MapPost("/policies", async (Guid propertyId, LatePenaltyPolicyRequest request, LatePenaltyService service, CancellationToken token) =>
            Created(await service.CreatePolicyAsync(propertyId, request, token), value => value.Replayed, value => $"/api/v1/properties/{propertyId}/late-penalties/policies/{value.Id}"));
        root.MapPut("/policies/{policyId:guid}", async (Guid propertyId, Guid policyId, LatePenaltyPolicyRequest request, LatePenaltyService service, CancellationToken token) =>
            Found(await service.UpdatePolicyAsync(propertyId, policyId, request, token)));
        root.MapPut("/policies/{policyId:guid}/approve", async (Guid propertyId, Guid policyId, LatePenaltyStatusRequest request, LatePenaltyService service, CancellationToken token) =>
            Found(await service.ApprovePolicyAsync(propertyId, policyId, request, token)));
        root.MapPut("/policies/{policyId:guid}/cancel", async (Guid propertyId, Guid policyId, LatePenaltyStatusRequest request, LatePenaltyService service, CancellationToken token) =>
            Found(await service.CancelPolicyAsync(propertyId, policyId, request, token)));
        root.MapPut("/policies/{policyId:guid}/end", async (Guid propertyId, Guid policyId, LatePenaltyStatusRequest request, LatePenaltyService service, CancellationToken token) =>
            Found(await service.EndPolicyAsync(propertyId, policyId, request, token)));
        root.MapGet("/overdue", async (Guid propertyId, LatePenaltyService service, CancellationToken token, int page = 1, int pageSize = 10, Guid? unitId = null, int? year = null, string? search = null) =>
            Found(await service.OverdueAsync(propertyId, new(page, pageSize, unitId, year, search), token)));
        root.MapGet("/charges/{chargeId:guid}", async (Guid propertyId, Guid chargeId, LatePenaltyService service, CancellationToken token) => Found(await service.ChargeAsync(propertyId, chargeId, token)));
        root.MapGet("/preview", async (Guid propertyId, LatePenaltyService service, CancellationToken token, Guid unitId, DateOnly? paidDate = null, string? amount = null) =>
            Found(await service.PreviewAsync(propertyId, unitId, paidDate, amount, token)));
        root.MapPost("/accruals/run", async (Guid propertyId, LatePenaltyRunRequest request, LatePenaltyService service, CancellationToken token) => Found(await service.RunAsync(propertyId, request, token)));
        root.MapPut("/accruals/{accrualId:guid}/waive", async (Guid propertyId, Guid accrualId, LatePenaltyWaiveRequest request, LatePenaltyService service, CancellationToken token) =>
            Found(await service.WaiveAsync(propertyId, accrualId, request, token)));
        root.MapGet("/report", async (Guid propertyId, LatePenaltyService service, CancellationToken token) => Found(await service.ReportAsync(propertyId, token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
    private static IResult Created<T>(T? value, Func<T, bool> replayed, Func<T, string> location) where T : class =>
        value is null ? Results.NotFound() : replayed(value) ? Results.Ok(value) : Results.Created(location(value), value);
}
