using BuildingFlow.Application;
namespace BuildingFlow.Api;

internal static class ParkingEndpoints
{
    public static void MapParkingEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/properties/{propertyId:guid}/parking").RequireAuthorization("CanReadProperties").WithTags("Parking");
        root.MapGet("/report", async (Guid propertyId, DateOnly from, DateOnly through, ParkingService s, CancellationToken t) => Found(await s.Report(propertyId, from, through, t)));
        MapSetup(root); MapVehicles(root); MapOperations(root);
        var mobile = app.MapGroup("/api/v1/mobile/parking").RequireAuthorization("CanReadProperties").WithTags("Parking");
        mobile.MapGet("", async ([AsParameters] MobileParkingContext q, ParkingService s, CancellationToken t) => Results.Ok(await s.MobileRead(q,t)));
        mobile.MapPost("/vehicles", async ([AsParameters] MobileParkingContext q, ResidentVehicleRequest r, ParkingService s, CancellationToken t) => Results.Ok(await s.MobileRegister(q,r,t)));
        mobile.MapPost("/guests", async ([AsParameters] MobileParkingContext q, GuestVehicleRequest r, ParkingService s, CancellationToken t) => Found(await s.MobileGuest(q,r,t)));
        mobile.MapPost("/guests/{id:guid}/{action}", async ([AsParameters] MobileParkingContext q, Guid id, string action, ParkingVersionRequest r, ParkingService s, CancellationToken t) => action switch {
            "arrive" => Found(await s.MobileGuestAction(q,id,GuestAction.ARRIVE,r.Version,t)),
            "depart" => Found(await s.MobileGuestAction(q,id,GuestAction.DEPART,r.Version,t)),
            "cancel" => Found(await s.MobileGuestAction(q,id,GuestAction.CANCEL,r.Version,t)),
            _ => Results.NotFound()
        });
        var mine = app.MapGroup("/api/v1/parking/my-vehicles").RequireAuthorization("CanReadProperties").WithTags("Parking");
        mine.MapGet("", async (Guid propertyId, Guid unitId, ParkingService s, CancellationToken t) => Results.Ok(await s.MyVehicles(propertyId, unitId, t)));
        mine.MapPost("", async (ResidentVehicleRequest r, ParkingService s, CancellationToken t) => Results.Ok(await s.RegisterMyVehicle(r, t)));
        mine.MapPost("/{vehicleId:guid}/deactivate", async (Guid vehicleId, Guid propertyId, ParkingVersionRequest r, ParkingService s, CancellationToken t) => Found(await s.DeactivateMyVehicle(propertyId, vehicleId, r.Version, t)));
    }

    private static void MapSetup(RouteGroupBuilder root)
    {
        root.MapGet("/policy", async (Guid propertyId, ParkingService s, CancellationToken t) => Found(await s.Policy(propertyId, t)));
        root.MapPut("/policy", async (Guid propertyId, ParkingPolicyRequest r, ParkingService s, CancellationToken t) => Found(await s.SavePolicy(propertyId, r, t)));
        root.MapGet("/facilities", async (Guid propertyId, ParkingService s, CancellationToken t) => Found(await s.Facilities(propertyId, t)));
        root.MapPost("/facilities", async (Guid propertyId, ParkingFacilityRequest r, ParkingService s, CancellationToken t) => Created(await s.SaveFacility(propertyId, null, r, t), x => $"facilities/{x.Id}"));
        root.MapPut("/facilities/{id:guid}", async (Guid propertyId, Guid id, ParkingFacilityRequest r, ParkingService s, CancellationToken t) => Found(await s.SaveFacility(propertyId, id, r, t)));
        root.MapDelete("/facilities/{id:guid}", async (Guid propertyId, Guid id, long version, ParkingService s, CancellationToken t) => Deleted(await s.DeleteFacility(propertyId, id, version, t)));
        root.MapPost("/levels", async (Guid propertyId, ParkingLevelRequest r, ParkingService s, CancellationToken t) => Created(await s.SaveLevel(propertyId, null, r, t), x => $"levels/{x.Id}"));
        root.MapPut("/levels/{id:guid}", async (Guid propertyId, Guid id, ParkingLevelRequest r, ParkingService s, CancellationToken t) => Found(await s.SaveLevel(propertyId, id, r, t)));
        root.MapDelete("/levels/{id:guid}", async (Guid propertyId, Guid id, long version, ParkingService s, CancellationToken t) => Deleted(await s.DeleteLevel(propertyId, id, version, t)));
        root.MapGet("/spaces", async (Guid propertyId, ParkingService s, CancellationToken t, Guid? facilityId = null, Guid? levelId = null, string? type = null, int page = 1, int pageSize = 20) =>
            Found(await s.Spaces(propertyId, new(facilityId, levelId, type, page, pageSize), t)));
        root.MapPost("/spaces", async (Guid propertyId, ParkingSpaceRequest r, ParkingService s, CancellationToken t) => Created(await s.SaveSpace(propertyId, null, r, t), x => $"spaces/{x.Id}"));
        root.MapPut("/spaces/{id:guid}", async (Guid propertyId, Guid id, ParkingSpaceRequest r, ParkingService s, CancellationToken t) => Found(await s.SaveSpace(propertyId, id, r, t)));
        root.MapDelete("/spaces/{id:guid}", async (Guid propertyId, Guid id, long version, ParkingService s, CancellationToken t) => Deleted(await s.DeleteSpace(propertyId, id, version, t)));
    }

    private static void MapVehicles(RouteGroupBuilder root)
    {
        root.MapGet("/vehicles", async (Guid propertyId, ParkingService s, CancellationToken t, string? status = null, string? search = null, Guid? unitId = null, int page = 1, int pageSize = 20) =>
            Found(await s.Vehicles(propertyId, new(status, search, unitId, page, pageSize), t)));
        root.MapPost("/vehicles", async (Guid propertyId, VehicleRequest r, ParkingService s, CancellationToken t) => Created(await s.CreateVehicle(propertyId, r, t), x => $"vehicles/{x.Id}"));
        root.MapPut("/vehicles/{id:guid}", async (Guid propertyId, Guid id, VehicleEditRequest r, ParkingService s, CancellationToken t) => Found(await s.EditVehicle(propertyId, id, r, t)));
        root.MapPost("/vehicles/{id:guid}/approve", async (Guid propertyId, Guid id, VehicleReviewRequest r, ParkingService s, CancellationToken t) => Found(await s.ReviewVehicle(propertyId, id, VehicleAction.APPROVE, r, t)));
        root.MapPost("/vehicles/{id:guid}/reject", async (Guid propertyId, Guid id, VehicleReviewRequest r, ParkingService s, CancellationToken t) => Found(await s.ReviewVehicle(propertyId, id, VehicleAction.REJECT, r, t)));
        root.MapPost("/vehicles/{id:guid}/deactivate", async (Guid propertyId, Guid id, VehicleReviewRequest r, ParkingService s, CancellationToken t) => Found(await s.ReviewVehicle(propertyId, id, VehicleAction.DEACTIVATE, r, t)));
        root.MapGet("/units", async (Guid propertyId, ParkingService s, CancellationToken t, string? search = null) => Found(await s.Units(propertyId, search, t)));
        root.MapGet("/units/{unitId:guid}/residents", async (Guid propertyId, Guid unitId, ParkingService s, CancellationToken t) => Found(await s.Residents(propertyId, unitId, t)));
        root.MapGet("/plates", async (Guid propertyId, string? search, ParkingService s, CancellationToken t) => Found(await s.Plates(propertyId, search, t)));
    }

    private static void MapOperations(RouteGroupBuilder root)
    {
        root.MapGet("/closures", async (Guid propertyId, ParkingService s, CancellationToken t, int page = 1, int pageSize = 20) => Found(await s.Closures(propertyId, page, pageSize, t)));
        root.MapPost("/closures", async (Guid propertyId, ParkingClosureRequest r, ParkingService s, CancellationToken t) => Created(await s.CreateClosure(propertyId, r, t), x => $"closures/{x.Id}"));
        root.MapPost("/closures/{id:guid}/cancel", async (Guid propertyId, Guid id, ParkingClosureCancelRequest r, ParkingService s, CancellationToken t) => Found(await s.CancelClosure(propertyId, id, r, t)));
        root.MapGet("/allocations", async (Guid propertyId, ParkingService s, CancellationToken t, Guid? unitId = null, Guid? spaceId = null, bool activeOnly = true, int page = 1, int pageSize = 20) =>
            Found(await s.Allocations(propertyId, new(unitId, spaceId, activeOnly, page, pageSize), t)));
        root.MapPost("/allocations", async (Guid propertyId, ParkingAllocationRequest r, ParkingService s, CancellationToken t) => Created(await s.Allocate(propertyId, r, t), x => $"allocations/{x.Id}"));
        root.MapPost("/allocations/{id:guid}/end", async (Guid propertyId, Guid id, ParkingAllocationEndRequest r, ParkingService s, CancellationToken t) => Found(await s.EndAllocation(propertyId, id, r, t)));
        root.MapGet("/guest-policy", async (Guid propertyId, ParkingService s, CancellationToken t) => Found(await s.GuestPolicy(propertyId, t)));
        root.MapGet("/guest-spaces", async (Guid propertyId, ParkingService s, CancellationToken t, int page = 1) => Found(await s.GuestSpaces(propertyId, page, t)));
        root.MapGet("/guests", async (Guid propertyId, ParkingService s, CancellationToken t, DateTimeOffset? from = null, DateTimeOffset? to = null, string? status = null, int page = 1, int pageSize = 20) =>
            Found(await s.Guests(propertyId, new(from, to, status, page, pageSize), t)));
        root.MapPost("/guests", async (Guid propertyId, GuestVehicleRequest r, ParkingService s, CancellationToken t) => Created(await s.CreateGuest(propertyId, r, t), x => $"guests/{x.Id}"));
        foreach (var action in Enum.GetValues<GuestAction>())
            root.MapPost($"/guests/{{id:guid}}/{action.ToString().ToLowerInvariant()}", async (Guid propertyId, Guid id, ParkingVersionRequest r, ParkingService s, CancellationToken t) =>
                Found(await s.ChangeGuest(propertyId, id, action, r.Version, t)));
    }

    private static IResult Found<T>(T? value) where T : class => value is null ? Results.NotFound() : Results.Ok(value);
    private static IResult Deleted(bool deleted) => deleted ? Results.NoContent() : Results.NotFound();
    private static IResult Created<T>(T? value, Func<T, string> path) where T : class => value is null ? Results.NotFound() : Results.Created(path(value), value);
}
