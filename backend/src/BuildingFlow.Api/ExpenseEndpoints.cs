using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class ExpenseEndpoints
{
    public static void MapExpenseEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}").WithTags("Expenses and budgets").RequireAuthorization("CanManageDues");
        root.MapGet("/expenses", async (Guid propertyId, ExpenseService service, CancellationToken token, int page = 1, int pageSize = 10, string? search = null, string? period = null, string? category = null, string? status = null, string sortBy = "occurredDate", string sortDirection = "desc") => Found(await service.ListAsync(propertyId, new(page, pageSize, search, period, category, status, sortBy, sortDirection), token)));
        root.MapGet("/expenses/{expenseId:guid}", async (Guid propertyId, Guid expenseId, ExpenseService service, CancellationToken token) => Found(await service.GetAsync(propertyId, expenseId, token)));
        root.MapPost("/expenses", async (Guid propertyId, CreateExpenseRequest request, ExpenseService service, CancellationToken token) => {
            var result = await service.CreateAsync(propertyId, request, token);
            return result is null ? Results.NotFound() : result.Replayed ? Results.Ok(result) : Results.Created($"/api/v1/properties/{propertyId}/expenses/{result.Id}", result);
        });
        root.MapPut("/expenses/{expenseId:guid}/cancel", async (Guid propertyId, Guid expenseId, CancelChargeRequest request, ExpenseService service, CancellationToken token) => Found(await service.CancelAsync(propertyId, expenseId, request, token)));
        root.MapPost("/budgets", async (Guid propertyId, SaveBudgetRequest request, ExpenseService service, CancellationToken token) => Found(await service.SaveBudgetAsync(propertyId, request, token)));
        root.MapGet("/budgets/{budgetId:guid}", async (Guid propertyId, Guid budgetId, ExpenseService service, CancellationToken token) => Found(await service.GetBudgetAsync(propertyId, budgetId, token)));
        root.MapGet("/finance-summary", async (Guid propertyId, string period, ExpenseService service, CancellationToken token) => Found(await service.SummaryAsync(propertyId, period, token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
