using BuildingFlow.Application;
namespace BuildingFlow.Api;
internal static class SupplierMaintenanceEndpoints
{
    public static void MapSupplierMaintenanceEndpoints(this WebApplication app)
    {
        var tenant = app.MapGroup("/api/v2/tenants/{tenantId:guid}/supplier-holidays").RequireAuthorization("CanManagePeople").WithTags("Supplier maintenance");
        tenant.MapGet("", async (Guid tenantId, int year, ISupplierMaintenanceStore store, CancellationToken token, int page = 1, int pageSize = 50) => Found(await store.Holidays(tenantId, year, page, pageSize, token)));
        tenant.MapPost("", async (Guid tenantId, SupplierHolidayRequest request, ISupplierMaintenanceStore store, CancellationToken token) => Found(await store.AddHoliday(tenantId, request, token)));
        tenant.MapDelete("/{holidayId:guid}", async (Guid tenantId, Guid holidayId, long version, ISupplierMaintenanceStore store, CancellationToken token) => await store.RemoveHoliday(tenantId, holidayId, version, token) ? Results.NoContent() : Results.NotFound());
        var root = app.MapGroup("/api/v2/tenants/{tenantId:guid}/suppliers/{supplierId:guid}").RequireAuthorization("CanManagePeople").WithTags("Supplier maintenance");
        root.MapPost("/maintenance-schedules/preview", async (Guid tenantId, Guid supplierId, SupplierMaintenancePlanRequest request, ISupplierMaintenanceStore store, CancellationToken token) => Found(await store.PreviewMaintenance(tenantId, supplierId, request, token)));
        root.MapGet("/maintenance-schedules", async (Guid tenantId, Guid supplierId, ISupplierMaintenanceStore store, CancellationToken token, int page = 1, int pageSize = 10) => Found(await store.MaintenanceSchedules(tenantId, supplierId, page, pageSize, token)));
        root.MapPost("/maintenance-schedules", async (Guid tenantId, Guid supplierId, SupplierMaintenanceScheduleCreate request, ISupplierMaintenanceStore store, CancellationToken token) => Found(await store.CreateMaintenanceSchedule(tenantId, supplierId, request, token)));
        root.MapGet("/maintenance-schedules/{scheduleId:guid}", async (Guid tenantId, Guid supplierId, Guid scheduleId, ISupplierMaintenanceStore store, CancellationToken token) => Found(await store.MaintenanceSchedule(tenantId, supplierId, scheduleId, token)));
        root.MapPut("/maintenance-schedules/{scheduleId:guid}/end", async (Guid tenantId, Guid supplierId, Guid scheduleId, SupplierMaintenanceScheduleEnd request, ISupplierMaintenanceStore store, CancellationToken token) => Found(await store.EndMaintenanceSchedule(tenantId, supplierId, scheduleId, request, token)));
        root.MapGet("/maintenance-schedules/{scheduleId:guid}/history", async (Guid tenantId, Guid supplierId, Guid scheduleId, ISupplierMaintenanceStore store, CancellationToken token, int page = 1, int pageSize = 10) => Found(await store.MaintenanceHistory(tenantId, supplierId, scheduleId, page, pageSize, token)));
        root.MapGet("/maintenance-occurrences", async (Guid tenantId, Guid supplierId, ISupplierMaintenanceStore store, CancellationToken token, Guid? scheduleId = null, DateOnly? from = null, DateOnly? to = null, string? status = null, bool reminderDue = false, int page = 1, int pageSize = 10) =>
            Found(await store.MaintenanceOccurrences(tenantId, supplierId, new(scheduleId, from, to, status, reminderDue, page, pageSize), token)));
        root.MapPut("/maintenance-occurrences/{occurrenceId:guid}/complete", async (Guid tenantId, Guid supplierId, Guid occurrenceId, SupplierMaintenanceComplete request, ISupplierMaintenanceStore store, CancellationToken token) => Found(await store.CompleteMaintenance(tenantId, supplierId, occurrenceId, request, token)));
        root.MapPut("/maintenance-occurrences/{occurrenceId:guid}/skip", async (Guid tenantId, Guid supplierId, Guid occurrenceId, SupplierMaintenanceSkip request, ISupplierMaintenanceStore store, CancellationToken token) => Found(await store.SkipMaintenance(tenantId, supplierId, occurrenceId, request, token)));
    }
    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
}
