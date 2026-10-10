using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}/payments").WithTags("Payments").RequireAuthorization("CanManageDues");
        root.MapGet("", async (Guid propertyId, PaymentService service, CancellationToken token,
            int page = 1, int pageSize = 10, string? search = null, Guid? unitId = null, Guid? chargeId = null,
            DateOnly? from = null, DateOnly? to = null, string? status = null, string? method = null, string sortBy = "paidDate", string sortDirection = "desc") =>
            Found(await service.ListAsync(propertyId, new(page, pageSize, search, unitId, chargeId, from, to, status, method, sortBy, sortDirection), token)));
        root.MapGet("/{paymentId:guid}", async (Guid propertyId, Guid paymentId, PaymentService service, CancellationToken token) => Found(await service.GetAsync(propertyId, paymentId, token)));
        root.MapPost("", async (Guid propertyId, CreatePaymentRequest request, PaymentService service, CancellationToken token) => {
            var result = await service.CreateAsync(propertyId, request, token);
            return result is null ? Results.NotFound() : result.Replayed ? Results.Ok(result) : Results.Created($"/api/v1/properties/{propertyId}/payments/{result.Id}", result);
        });
        root.MapPut("/{paymentId:guid}/cancel", async (Guid propertyId, Guid paymentId, CancelChargeRequest request, PaymentService service, CancellationToken token) => Found(await service.CancelAsync(propertyId, paymentId, request, token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
