using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// Araçlar/Otopark API: roles and Property scope, policy/relation/quota/plate rules, replay, version, races, guest flow and resident self-service.
public sealed class ParkingApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private static string Root(Guid p) => $"/api/v1/properties/{p}/parking";
    private sealed record Fixture(Guid Property, Guid Building, Guid Unit, Guid OtherUnit, Guid Person, string Manager, string Resident);

    private async Task<Fixture> Seed(bool policy = true)
    {
        var manager = Guid.NewGuid().ToString(); var resident = Guid.NewGuid().ToString();
        var p = new Property("Otopark " + manager, "City", "District", "Address", PropertyType.SITE);
        var block = p.AddBuilding("A"); var unit = block.AddUnit("1", 1, 100); var other = block.AddUnit("2", 1, 100);
        var person = new Person(p.Id, "Ayşe", "Yılmaz", "+905551234567", "ayse@example.test");
        person.SetProfile("NATURAL", "DOMESTIC", "TR", "12345678901", null, null, null, resident);
        await factory.WithDb(async db =>
        {
            db.AddRange(p, person, new PropertyMembership(p.Id, manager));
            await db.SaveChangesAsync();
            db.Add(new UnitOccupancy(p.Id, unit.Id, person.Id, OccupancyType.TENANT, Today.AddDays(-10)));
            if (policy) db.Add(new ParkingPolicy(p.Id, 2, 2, 1, 24));
            await db.SaveChangesAsync();
        });
        return new(p.Id, block.Id, unit.Id, other.Id, person.Id, manager, resident);
    }

    private static async Task<string?> Code(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    private static async Task<T> Read<T>(HttpResponseMessage r, HttpStatusCode status = HttpStatusCode.OK)
    {
        Assert.Equal(status, r.StatusCode); return (await r.Content.ReadFromJsonAsync<T>())!;
    }
    private static async Task Rejected(HttpResponseMessage r, string code)
    {
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode); Assert.Equal(code, await Code(r));
    }
    private static VehicleRequest Vehicle(Fixture x, string plate, Guid? request = null, Guid? unit = null) =>
        new(request ?? Guid.NewGuid(), x.Person, unit ?? x.Unit, plate, "CAR", "BMW", "5", "Siyah");
    private static async Task<ParkingSpaceResponse> Space(HttpClient c, Guid p, string code, string type = "STANDARD")
    {
        var facility = await Read<ParkingFacilityResponse>(await c.PostAsJsonAsync(Root(p) + "/facilities", new ParkingFacilityRequest("Otopark " + code, null)), HttpStatusCode.Created);
        return await Read<ParkingSpaceResponse>(await c.PostAsJsonAsync(Root(p) + "/spaces", new ParkingSpaceRequest(facility.Id, null, code, type)), HttpStatusCode.Created);
    }

    [PostgresFact]
    public async Task ManagerRegistersVehicleWithPolicyRelationQuotaPlateAndReplayRules()
    {
        var x = await Seed(policy: false); using var c = TestToken.Client(factory, x.Manager, "MANAGER");
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "34ABC123")), "BF_PARKING_POLICY_REQUIRED");
        var policy = await Read<ParkingPolicyResponse>(await c.PutAsJsonAsync(Root(x.Property) + "/policy", new ParkingPolicyRequest(1, 2, 1, 24, 30)));
        Assert.Equal((true, 1L), (policy.Configured, policy.Version));
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(Root(x.Property) + "/policy", new ParkingPolicyRequest(2, 2, 1, 24, 30))).StatusCode);
        policy = await Read<ParkingPolicyResponse>(await c.PutAsJsonAsync(Root(x.Property) + "/policy", new ParkingPolicyRequest(2, 2, 1, 24, 30, policy.Version)));

        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "34ABC123", unit: x.OtherUnit)), "BF_PARKING_RELATION_REQUIRED");
        var request = Guid.NewGuid();
        var vehicle = await Read<VehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, " 34 abc 123 ", request)), HttpStatusCode.Created);
        Assert.Equal(("34ABC123", "APPROVED", "Ayşe Yılmaz", "1"), (vehicle.Plate, vehicle.Status, vehicle.PersonName, vehicle.UnitNumber));
        var replay = await Read<VehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "34ABC123", request)), HttpStatusCode.Created);
        Assert.Equal(vehicle.Id, replay.Id);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property)+"/vehicles",Vehicle(x,"34ABC123",request) with {Brand="Changed"}),"BF_PARKING_REQUEST_REUSED");
        Assert.Equal(vehicle.Id,(await Read<VehicleResponse>(await c.PostAsJsonAsync(Root(x.Property)+"/vehicles",Vehicle(x,"34ABC123",request) with {Brand=" BMW "}),HttpStatusCode.Created)).Id);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "06DEF456", request)), "BF_PARKING_REQUEST_REUSED");
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "34-ABC-123")), "BF_PARKING_PLATE_TAKEN");
        await Read<VehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "06DEF456")), HttpStatusCode.Created);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "35GHI321")), "BF_PARKING_QUOTA_EXCEEDED");
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "34ÇAB12"))).StatusCode);

        var deactivated = await Read<VehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + $"/vehicles/{vehicle.Id}/deactivate", new VehicleReviewRequest(vehicle.Version)));
        Assert.Equal("PASSIVE", deactivated.Status);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + $"/vehicles/{vehicle.Id}/approve", new VehicleReviewRequest(deactivated.Version)), "BF_PARKING_STATE_INVALID");
        await Read<VehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "34ABC123")), HttpStatusCode.Created);
        var page = await Read<ParkingPage<VehicleResponse>>(await c.GetAsync(Root(x.Property) + "/vehicles?status=APPROVED&search=abc"));
        Assert.Equal(1, page.TotalCount);
        await factory.WithDb(async db => Assert.Equal(x.Manager, (await db.Vehicles.SingleAsync(v => v.Id == vehicle.Id)).CreatedBy));
    }

    [PostgresFact]
    public async Task AllocationsRespectSpaceTypeOverlapQuotaInUseAndVersion()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.Manager, "SITE_ADMIN");
        var space = await Space(c, x.Property, "A01"); var visitor = await Space(c, x.Property, "M01", "VISITOR"); var second = await Space(c, x.Property, "A02");
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(visitor.Id, x.Unit, null, Today)), "BF_PARKING_SPACE_OCCUPIED");
        var allocation = await Read<ParkingAllocationResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(space.Id, x.Unit, null, Today)), HttpStatusCode.Created);
        Assert.Equal(("A01", "1"), (allocation.SpaceCode, allocation.UnitNumber));
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(space.Id, x.OtherUnit, null, Today.AddDays(30))), "BF_PARKING_SPACE_OCCUPIED");
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(second.Id, x.Unit, null, Today)), "BF_PARKING_QUOTA_EXCEEDED");
        await Rejected(await c.DeleteAsync(Root(x.Property) + $"/spaces/{space.Id}?version={space.Version}"), "BF_PARKING_IN_USE");
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync(Root(x.Property) + $"/allocations/{allocation.Id}/end", new ParkingAllocationEndRequest(Today, allocation.Version + 1))).StatusCode);
        var ended = await Read<ParkingAllocationResponse>(await c.PostAsJsonAsync(Root(x.Property) + $"/allocations/{allocation.Id}/end", new ParkingAllocationEndRequest(Today, allocation.Version)));
        Assert.Equal(Today, ended.EndDate);
        await Read<ParkingAllocationResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(space.Id, x.OtherUnit, null, Today.AddDays(1))), HttpStatusCode.Created);
        var spaces = await Read<ParkingPage<ParkingSpaceResponse>>(await c.GetAsync(Root(x.Property) + "/spaces?type=STANDARD"));
        Assert.Equal(2, spaces.TotalCount);
    }

    [PostgresFact]
    public async Task ConcurrentAllocationOfOneSpaceCreatesExactlyOne()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.Manager, "MANAGER");
        var space = await Space(c, x.Property, "R01");
        var results = await Task.WhenAll(new[] { x.Unit, x.OtherUnit }.Select(unit => c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(space.Id, unit, null, Today))));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
        await factory.WithDb(async db => Assert.Equal(1, await db.ParkingAllocations.CountAsync(a => a.SpaceId == space.Id)));
    }

    [PostgresFact]
    public async Task SecurityGuardManagesGuestsAndSeesPlatesWithoutPersonData()
    {
        var x = await Seed(); using var manager = TestToken.Client(factory, x.Manager, "MANAGER");
        var visitor = await Space(manager, x.Property, "M01", "VISITOR");
        await Read<VehicleResponse>(await manager.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "34ABC123")), HttpStatusCode.Created);
        var guard = Guid.NewGuid().ToString();
        await factory.WithDb(async db => { db.Add(new PropertyMembership(x.Property, guard)); await db.SaveChangesAsync(); });
        using var g = TestToken.Client(factory, guard, "SECURITY");
        var guestPolicy = await Read<ParkingGuestPolicyResponse>(await g.GetAsync(Root(x.Property) + "/guest-policy"));
        Assert.Equal((true, 24), (guestPolicy.Configured, guestPolicy.GuestMaxHours));
        var guestSpaces = await Read<ParkingPage<ParkingSpaceResponse>>(await g.GetAsync(Root(x.Property) + "/guest-spaces"));
        Assert.All(guestSpaces.Items, s => Assert.Equal("VISITOR", s.Type));
        var start = DateTimeOffset.UtcNow.AddMinutes(-1);
        var request = new GuestVehicleRequest(Guid.NewGuid(), x.Unit, "34 tst 100", "Ahmet", start, start.AddHours(4), visitor.Id, null);
        var guest = await Read<GuestVehicleResponse>(await g.PostAsJsonAsync(Root(x.Property) + "/guests", request), HttpStatusCode.Created);
        Assert.Equal(("34TST100", "PLANNED", "M01"), (guest.Plate, guest.Status, guest.SpaceCode));
        Assert.Equal(guest.Id, (await Read<GuestVehicleResponse>(await g.PostAsJsonAsync(Root(x.Property) + "/guests", request), HttpStatusCode.Created)).Id);
        await Rejected(await g.PostAsJsonAsync(Root(x.Property) + "/guests", request with { RequestId = Guid.NewGuid(), Plate = "06XYZ1" }), "BF_PARKING_SPACE_OCCUPIED");
        await Rejected(await g.PostAsJsonAsync(Root(x.Property) + "/guests", request with { RequestId = Guid.NewGuid(), SpaceId = null, EndsAt = start.AddHours(25) }), "BF_PARKING_QUOTA_EXCEEDED");
        var arrived = await Read<GuestVehicleResponse>(await g.PostAsJsonAsync(Root(x.Property) + $"/guests/{guest.Id}/arrive", new ParkingVersionRequest(guest.Version)));
        await Rejected(await g.PostAsJsonAsync(Root(x.Property) + $"/guests/{guest.Id}/cancel", new ParkingVersionRequest(arrived.Version)), "BF_PARKING_STATE_INVALID");
        Assert.Equal("DEPARTED", (await Read<GuestVehicleResponse>(await g.PostAsJsonAsync(Root(x.Property) + $"/guests/{guest.Id}/depart", new ParkingVersionRequest(arrived.Version)))).Status);

        var plates = await g.GetAsync(Root(x.Property) + "/plates?search=34 ab");
        var body = await plates.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, plates.StatusCode); Assert.Contains("34ABC123", body);
        Assert.DoesNotContain("Ayşe", body); Assert.DoesNotContain(x.Person.ToString(), body);
        Assert.Equal(HttpStatusCode.BadRequest, (await g.GetAsync(Root(x.Property) + "/plates?search=3")).StatusCode);
        Assert.Equal(1, (await Read<ParkingPage<GuestVehicleResponse>>(await g.GetAsync(Root(x.Property) + "/guests?status=DEPARTED"))).TotalCount);
        foreach (var path in new[] { "/vehicles", "/policy", "/facilities", "/allocations", "/spaces" })
            Assert.Equal(HttpStatusCode.Forbidden, (await g.GetAsync(Root(x.Property) + path)).StatusCode);
    }

    [PostgresTheory]
    [InlineData("ACCOUNTANT")]
    [InlineData("RESIDENT")]
    [InlineData("spoofed")]
    public async Task OtherRolesCannotReachManagementOrGuestSurfaces(string role)
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.Manager, role);
        foreach (var path in new[] { "/vehicles", "/guests", "/plates?search=34", "/policy" })
            Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync(Root(x.Property) + path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "34ABC123"))).StatusCode);
        using var anonymous = TestToken.Client(factory, null, "MANAGER");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Root(x.Property) + "/vehicles")).StatusCode);
    }

    [PostgresFact]
    public async Task NonMembersAndForeignRecordsAreNotFound()
    {
        var x = await Seed(); var y = await Seed();
        using var outsider = TestToken.Client(factory, Guid.NewGuid().ToString(), "MANAGER");
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(Root(x.Property) + "/vehicles")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "34ABC123"))).StatusCode);
        using var other = TestToken.Client(factory, y.Manager, "MANAGER");
        var foreign = await Space(other, y.Property, "Y01");
        using var c = TestToken.Client(factory, x.Manager, "MANAGER");
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(foreign.Id, x.Unit, null, Today))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(foreign.Id, y.Unit, null, Today))).StatusCode);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/vehicles", Vehicle(x, "34ABC123", unit: y.Unit)), "BF_PARKING_RELATION_REQUIRED");
    }

    [PostgresFact]
    public async Task ResidentRegistersPendingVehicleThatManagerApprovesOrRejects()
    {
        var x = await Seed(); using var resident = TestToken.Client(factory, x.Resident, "RESIDENT");
        var request = new ResidentVehicleRequest(Guid.NewGuid(), x.Property, x.Unit, "34ABC123", "CAR", "Toyota", "Corolla", "Beyaz");
        var mine = await Read<ResidentVehicleResponse>(await resident.PostAsJsonAsync("/api/v1/parking/my-vehicles", request));
        Assert.Equal("PENDING", mine.Status);
        Assert.Equal(mine.Id, (await Read<ResidentVehicleResponse>(await resident.PostAsJsonAsync("/api/v1/parking/my-vehicles", request))).Id);
        await Rejected(await resident.PostAsJsonAsync("/api/v1/parking/my-vehicles",request with {Color="Changed"}),"BF_PARKING_REQUEST_REUSED");
        Assert.Equal(HttpStatusCode.Forbidden, (await resident.PostAsJsonAsync("/api/v1/parking/my-vehicles", request with { RequestId = Guid.NewGuid(), UnitId = x.OtherUnit })).StatusCode);
        using var stranger = TestToken.Client(factory, Guid.NewGuid().ToString(), "RESIDENT");
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/v1/parking/my-vehicles?propertyId={x.Property}&unitId={x.Unit}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync($"/api/v1/parking/my-vehicles/{mine.Id}/deactivate?propertyId={x.Property}", new ParkingVersionRequest(mine.Version))).StatusCode);

        using var manager = TestToken.Client(factory, x.Manager, "MANAGER");
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PostAsJsonAsync(Root(x.Property) + $"/vehicles/{mine.Id}/reject", new VehicleReviewRequest(mine.Version, " "))).StatusCode);
        var approved = await Read<VehicleResponse>(await manager.PostAsJsonAsync(Root(x.Property) + $"/vehicles/{mine.Id}/approve", new VehicleReviewRequest(mine.Version)));
        Assert.Equal("APPROVED", approved.Status);
        var second = await Read<ResidentVehicleResponse>(await resident.PostAsJsonAsync("/api/v1/parking/my-vehicles", request with { RequestId = Guid.NewGuid(), Plate = "06DEF456" }));
        var rejected = await Read<VehicleResponse>(await manager.PostAsJsonAsync(Root(x.Property) + $"/vehicles/{second.Id}/reject", new VehicleReviewRequest(second.Version, "Ruhsat eksik")));
        Assert.Equal(("REJECTED", "Ruhsat eksik"), (rejected.Status, rejected.ReviewReason));
        var list = await Read<ResidentVehicleResponse[]>(await resident.GetAsync($"/api/v1/parking/my-vehicles?propertyId={x.Property}&unitId={x.Unit}"));
        Assert.Equal(2, list.Length);
        var passive = await Read<ResidentVehicleResponse>(await resident.PostAsJsonAsync($"/api/v1/parking/my-vehicles/{mine.Id}/deactivate?propertyId={x.Property}", new ParkingVersionRequest(approved.Version)));
        Assert.Equal("PASSIVE", passive.Status);

        await factory.WithDb(async db =>
        {
            var occupancy = await db.UnitOccupancies.SingleAsync(o => o.PersonId == x.Person);
            occupancy.End(Today.AddDays(-1)); await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Forbidden, (await resident.GetAsync($"/api/v1/parking/my-vehicles?propertyId={x.Property}&unitId={x.Unit}")).StatusCode);
    }

    [PostgresFact]
    public async Task UnitAndResidentPickersRespectRoles()
    {
        var x = await Seed(); using var manager = TestToken.Client(factory, x.Manager, "MANAGER");
        var units = await Read<ParkingUnitOption[]>(await manager.GetAsync(Root(x.Property) + "/units?search=A"));
        Assert.Equal(2, units.Length); Assert.All(units, u => Assert.Equal("A", u.BuildingName));
        var residents = await Read<ParkingResidentOption[]>(await manager.GetAsync(Root(x.Property) + $"/units/{x.Unit}/residents"));
        Assert.Equal(("Ayşe Yılmaz", "TENANT"), (Assert.Single(residents).Name, residents[0].Relation));
        Assert.Empty(await Read<ParkingResidentOption[]>(await manager.GetAsync(Root(x.Property) + $"/units/{x.OtherUnit}/residents")));
        var guard = Guid.NewGuid().ToString();
        await factory.WithDb(async db => { db.Add(new PropertyMembership(x.Property, guard)); await db.SaveChangesAsync(); });
        using var g = TestToken.Client(factory, guard, "SECURITY");
        Assert.Equal(2, (await Read<ParkingUnitOption[]>(await g.GetAsync(Root(x.Property) + "/units"))).Length);
        Assert.Equal(HttpStatusCode.Forbidden, (await g.GetAsync(Root(x.Property) + $"/units/{x.Unit}/residents")).StatusCode);
        var y = await Seed();
        Assert.Equal(HttpStatusCode.NotFound, (await manager.GetAsync(Root(x.Property) + $"/units/{y.Unit}/residents")).StatusCode);
    }
    [PostgresFact]
    public async Task GuestWindowReplayBoundariesAndLateDepartureAreEnforced()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.Manager, "MANAGER");
        var visitor = await Space(c, x.Property, "WINDOW", "VISITOR");
        var now = DateTimeOffset.UtcNow;
        var request = new GuestVehicleRequest(Guid.NewGuid(), x.Unit, "34GUEST1", " Name ", now.AddHours(1), now.AddHours(2), visitor.Id, " Note ");
        var first = await Read<GuestVehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/guests", request), HttpStatusCode.Created);
        Assert.Equal(first.Id, (await Read<GuestVehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/guests", request with { GuestName = "Name", Note = "Note" }), HttpStatusCode.Created)).Id);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/guests", request with { GuestName = "Changed" }), "BF_PARKING_REQUEST_REUSED");
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/guests", request with { Note = "Changed" }), "BF_PARKING_REQUEST_REUSED");
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + $"/guests/{first.Id}/arrive", new ParkingVersionRequest(first.Version)), "BF_PARKING_STATE_INVALID");
        await Read<GuestVehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/guests", request with { RequestId = Guid.NewGuid(), Plate = "34GUEST2", StartsAt = request.EndsAt, EndsAt = request.EndsAt.AddHours(1) }), HttpStatusCode.Created);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/guests", request with { RequestId = Guid.NewGuid(), SpaceId = null }), "BF_PARKING_SPACE_OCCUPIED");
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/guests", request with { RequestId = Guid.NewGuid(), Plate = "34PAST1", StartsAt = now.AddHours(-2), EndsAt = now.AddHours(-1) }), "BF_PARKING_STATE_INVALID");
        var late = new GuestVehicle(new ParkingPolicy(x.Property, 2, 2, 1, 24), x.Unit, "34LATE1", null, now.AddHours(-3), now.AddHours(-1), visitor.Id, null);
        late.MarkArrived(now.AddHours(-2));
        await factory.WithDb(async db => { db.Add(late); await db.SaveChangesAsync(); });
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/guests", request with { RequestId = Guid.NewGuid(), Plate = "34NEW1", StartsAt = now, EndsAt = now.AddMinutes(30) }), "BF_PARKING_SPACE_OCCUPIED");
        await Rejected(await c.DeleteAsync(Root(x.Property) + $"/spaces/{visitor.Id}?version={visitor.Version}"), "BF_PARKING_IN_USE");
        await Read<GuestVehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + $"/guests/{late.Id}/depart", new ParkingVersionRequest(late.Version)));
        await Read<GuestVehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/guests", request with { RequestId = Guid.NewGuid(), Plate = "34NEW1", StartsAt = now, EndsAt = now.AddMinutes(30) }), HttpStatusCode.Created);
    }

    [PostgresFact]
    public async Task ClosuresEnforceReplayVersionScopeRolesAndBothDirectionsOfOverlap()
    {
        var x = await Seed(); var y = await Seed(); using var c = TestToken.Client(factory, x.Manager, "MANAGER");
        var space = await Space(c, x.Property, "MAINT", "VISITOR"); var start = DateTimeOffset.UtcNow.AddHours(1);
        var request = new ParkingClosureRequest(Guid.NewGuid(), space.Id, start, start.AddHours(2), " Paint ");
        var closure = await Read<ParkingClosureResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/closures", request), HttpStatusCode.Created);
        Assert.Equal("Paint", closure.Reason);
        Assert.Equal(closure.Id, (await Read<ParkingClosureResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/closures", request with { Reason = "Paint" }), HttpStatusCode.Created)).Id);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/closures", request with { Reason = "Other" }), "BF_PARKING_REQUEST_REUSED");
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/closures", request with { RequestId = Guid.NewGuid() }), "BF_PARKING_SPACE_OCCUPIED");
        var guest = new GuestVehicleRequest(Guid.NewGuid(), x.Unit, "34MAINT1", null, start, start.AddHours(1), space.Id, null);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/guests", guest), "BF_PARKING_SPACE_OCCUPIED");
        await Read<GuestVehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/guests", guest with { StartsAt = request.EndsAt, EndsAt = request.EndsAt.AddHours(1) }), HttpStatusCode.Created);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/closures", request with { RequestId = Guid.NewGuid(), StartsAt = request.EndsAt, EndsAt = request.EndsAt.AddHours(1) }), "BF_PARKING_SPACE_OCCUPIED");
        using var guard = TestToken.Client(factory, x.Manager, "SECURITY");
        Assert.Equal(1, (await Read<ParkingPage<ParkingClosureResponse>>(await guard.GetAsync(Root(x.Property) + "/closures"))).TotalCount);
        Assert.Equal(HttpStatusCode.Forbidden, (await guard.PostAsJsonAsync(Root(x.Property) + "/closures", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await guard.PostAsJsonAsync(Root(x.Property) + $"/closures/{closure.Id}/cancel", new ParkingClosureCancelRequest(closure.Version, "Reason"))).StatusCode);
        using var accountant = TestToken.Client(factory, x.Manager, "ACCOUNTANT");
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync(Root(x.Property) + "/closures")).StatusCode);
        using var outsider = TestToken.Client(factory, y.Manager, "MANAGER");
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(Root(x.Property) + "/closures")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.PostAsJsonAsync(Root(y.Property) + "/closures", request)).StatusCode);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + $"/closures/{closure.Id}/cancel", new ParkingClosureCancelRequest(closure.Version + 1, "Reason")), "BF_RECORD_CHANGED");
        var cancelled = await Read<ParkingClosureResponse>(await c.PostAsJsonAsync(Root(x.Property) + $"/closures/{closure.Id}/cancel", new ParkingClosureCancelRequest(closure.Version, " Finished early ")));
        Assert.Equal("Finished early", cancelled.CancellationReason);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + $"/closures/{closure.Id}/cancel", new ParkingClosureCancelRequest(cancelled.Version, "Again")), "BF_PARKING_STATE_INVALID");
        await Read<GuestVehicleResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/guests", guest with { RequestId = Guid.NewGuid(), Plate = "34MAINT2" }), HttpStatusCode.Created);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(Root(x.Property) + "/closures", request with { Reason = " " })).StatusCode);
    }

    [PostgresFact]
    public async Task ConcurrentClosureAndGuestHaveExactlyOneWinner()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.Manager, "MANAGER");
        var space = await Space(c, x.Property, "RACE", "VISITOR"); var start = DateTimeOffset.UtcNow.AddHours(1);
        var responses = await Task.WhenAll(
            c.PostAsJsonAsync(Root(x.Property) + "/closures", new ParkingClosureRequest(Guid.NewGuid(), space.Id, start, start.AddHours(1), "Maintenance")),
            c.PostAsJsonAsync(Root(x.Property) + "/guests", new GuestVehicleRequest(Guid.NewGuid(), x.Unit, "34RACE1", null, start, start.AddHours(1), space.Id, null)));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
    }

    [PostgresFact]
    public async Task AllocationQuotaUsesConcurrentPeakAndMaintenanceUsesIstanbulDays()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.Manager, "MANAGER");
        var policy = await Read<ParkingPolicyResponse>(await c.GetAsync(Root(x.Property) + "/policy"));
        await c.PutAsJsonAsync(Root(x.Property) + "/policy", new ParkingPolicyRequest(2, 2, 2, 24, 30, policy.Version));
        var a = await Space(c, x.Property, "Q1"); var b = await Space(c, x.Property, "Q2"); var target = await Space(c, x.Property, "Q3");
        var day = Today.AddDays(2);
        await Read<ParkingAllocationResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(a.Id, x.Unit, null, day, day)), HttpStatusCode.Created);
        await Read<ParkingAllocationResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(b.Id, x.Unit, null, day.AddDays(2), day.AddDays(2))), HttpStatusCode.Created);
        await Read<ParkingAllocationResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(target.Id, x.Unit, null, day, day.AddDays(2))), HttpStatusCode.Created);
        var start = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3));
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/closures", new ParkingClosureRequest(Guid.NewGuid(), target.Id, start.AddDays(2).AddHours(23), start.AddDays(3), "Repair")), "BF_PARKING_SPACE_OCCUPIED");
        var unused = await Space(c, x.Property, "Q4");
        await Read<ParkingClosureResponse>(await c.PostAsJsonAsync(Root(x.Property) + "/closures", new ParkingClosureRequest(Guid.NewGuid(), unused.Id, start, start.AddHours(1), "Repair")), HttpStatusCode.Created);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property) + "/allocations", new ParkingAllocationRequest(unused.Id, x.OtherUnit, null, day)), "BF_PARKING_SPACE_OCCUPIED");
    }


    [PostgresFact]
    public async Task ReportsSeparateIstanbulMovementsFromCurrentRecordsAndEnforceManagementScope()
    {
        var x=await Seed(); using var c=TestToken.Client(factory,x.Manager,"MANAGER");
        var day=Today.AddDays(-2); var start=new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue),TimeSpan.FromHours(3)).ToUniversalTime();
        await factory.WithDb(async db=>{
            var policy=await db.ParkingPolicies.SingleAsync(p=>p.PropertyId==x.Property);
            var a=new GuestVehicle(policy,x.Unit,"34DAY01",null,start.AddMinutes(-30),start.AddHours(2),null,null);
            a.MarkArrived(start.AddMinutes(-1)); a.MarkDeparted(start.AddMinutes(1));
            var b=new GuestVehicle(policy,x.Unit,"34DAY02",null,start,start.AddHours(1),null,null);
            b.MarkArrived(start); // manual overdue entry, no measured physical occupancy
            var cancelled=new GuestVehicle(policy,x.Unit,"34DAY03",null,start,start.AddHours(1),null,null);cancelled.Cancel(start);
            db.AddRange(a,b,cancelled);await db.SaveChangesAsync();
        });
        var report=await Read<ParkingReport>(await c.GetAsync(Root(x.Property)+$"/report?from={day:yyyy-MM-dd}&through={day:yyyy-MM-dd}"));
        var row=Assert.Single(report.Days);Assert.Equal((1,1,1),(row.Entries,row.Exits,row.Reservations));
        Assert.Equal((1,1,false,false),(report.ManualGuestsInside,report.OverdueGuests,report.ScheduledOperationsEnabled,report.HardwareEnabled));
        foreach(var role in new[]{"SECURITY","ACCOUNTANT","RESIDENT"}){
            using var denied=TestToken.Client(factory,x.Manager,role);
            Assert.Equal(HttpStatusCode.Forbidden,(await denied.GetAsync(Root(x.Property)+$"/report?from={day:yyyy-MM-dd}&through={day:yyyy-MM-dd}")).StatusCode);
        }
        using var stranger=TestToken.Client(factory,Guid.NewGuid().ToString(),"MANAGER");
        Assert.Equal(HttpStatusCode.NotFound,(await stranger.GetAsync(Root(x.Property)+$"/report?from={day:yyyy-MM-dd}&through={day:yyyy-MM-dd}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync(Root(x.Property)+"/report?from=2020-01-01&through=2026-01-01")).StatusCode);
    }

    [PostgresFact]
    public async Task ParkingLifecycleMessagesAreAtomicPrivateAndDeduplicatedOnReplay()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Manager,"MANAGER");
        await factory.WithDb(async db=>{db.Add(new PropertyMembership(x.Property,x.Resident));await db.SaveChangesAsync();});
        var request=Vehicle(x,"34MSG01");
        var v=await Read<VehicleResponse>(await c.PostAsJsonAsync(Root(x.Property)+"/vehicles",request),HttpStatusCode.Created);
        await Read<VehicleResponse>(await c.PostAsJsonAsync(Root(x.Property)+"/vehicles",request),HttpStatusCode.Created);
        await factory.WithDb(async db=>{
            var messages=await db.PersonMessages.Where(m=>m.PropertyId==x.Property).ToListAsync();
            var message=Assert.Single(messages);Assert.Equal(x.Resident,message.RecipientUserId);
            Assert.DoesNotContain(request.Plate!,message.Body);Assert.DoesNotContain("private",message.Body);
            Assert.Single(await db.ProcessedMessages.Where(m=>m.PropertyId==x.Property).ToListAsync());
        });
        // Stale request must not write any message.
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync(Root(x.Property)+$"/vehicles/{v.Id}/deactivate",new VehicleReviewRequest(v.Version+1))).StatusCode);
        await Read<VehicleResponse>(await c.PostAsJsonAsync(Root(x.Property)+$"/vehicles/{v.Id}/deactivate",new VehicleReviewRequest(v.Version)));
        await factory.WithDb(async db=>Assert.Equal(2,await db.PersonMessages.CountAsync(m=>m.PropertyId==x.Property)));
    }

    [PostgresFact]
    public async Task ScheduledOperationsAreOptInRepeatSafeAndRetainTerminalMetadata()
    {
        var x=await Seed();var now=DateTimeOffset.UtcNow;Guid terminal=Guid.Empty,overdue=Guid.Empty; GuestVehicleRequest? oldRequest=null;
        await factory.WithDb(async db=>{
            db.Add(new PropertyMembership(x.Property,x.Resident));var policy=await db.ParkingPolicies.SingleAsync(p=>p.PropertyId==x.Property);
            var a=new GuestVehicle(policy,x.Unit,"34OLD01","private guest",now.AddDays(-32),now.AddDays(-32).AddHours(1),null,"private note");a.Cancel(now.AddDays(-31));terminal=a.Id;oldRequest=new(a.RequestId,x.Unit,a.Plate,a.GuestName,a.StartsAt,a.EndsAt,null,a.Note);
            var b=new GuestVehicle(policy,x.Unit,"34LATE1",null,now.AddHours(-2),now.AddHours(-1),null,null);b.MarkArrived(now.AddHours(-2));overdue=b.Id;
            var d=new GuestVehicle(policy,x.Unit,"34SOON1",null,now.AddMinutes(-10),now.AddMinutes(30),null,null);
            db.AddRange(a,b,d);await db.SaveChangesAsync();
        });
        using(var scope=factory.Services.CreateScope())Assert.Null(await scope.ServiceProvider.GetRequiredService<IParkingOperations>().RunBatch(null,default));
        async Task Sweep(){using var scope=factory.Services.CreateScope();var provider=scope.ServiceProvider;var configuration=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Parking:Operations:Enabled"]="true"}).Build();
            var store=new ParkingStore(provider.GetRequiredService<BuildingFlowDbContext>(),TimeProvider.System,provider.GetRequiredService<ICurrentUserService>(),provider.GetRequiredService<IMobileSessionStore>(),provider.GetRequiredService<ISessionContextStore>(),configuration);
            Guid? cursor=null;do{cursor=await store.RunBatch(cursor,default);}while(cursor!=null);
        }
        await Sweep();await Sweep();
        await factory.WithDb(async db=>{
            var old=await db.GuestVehicles.SingleAsync(g=>g.Id==terminal);Assert.NotNull(old.PersonalDataRedactedAt);Assert.Equal("REDACTED",old.Plate);Assert.Null(old.GuestName);Assert.Null(old.Note);Assert.Equal(x.Unit,old.HostUnitId);
            Assert.Null((await db.GuestVehicles.SingleAsync(g=>g.Id==overdue)).PersonalDataRedactedAt);
            Assert.Equal(2,await db.PersonMessages.CountAsync(m=>m.PropertyId==x.Property));
            Assert.Equal(2,await db.ProcessedMessages.CountAsync(m=>m.PropertyId==x.Property));
        });
        using var c=TestToken.Client(factory,x.Manager,"MANAGER");
        var guests=await Read<ParkingPage<GuestVehicleResponse>>(await c.GetAsync(Root(x.Property)+"/guests"));Assert.DoesNotContain(guests.Items,g=>g.Id==terminal);
        await Rejected(await c.PostAsJsonAsync(Root(x.Property)+"/guests",oldRequest!),"BF_PARKING_REQUEST_REUSED");
    }

    [PostgresFact]
    public async Task ReportsRespectHistoricalIstanbulWinterOffset()
    {
        var x=await Seed();var start=new DateTimeOffset(2010,1,1,21,0,0,TimeSpan.Zero);
        await factory.WithDb(async db=>{
            var policy=await db.ParkingPolicies.SingleAsync(p=>p.PropertyId==x.Property);
            var g=new GuestVehicle(policy,x.Unit,"34DST01",null,start,start.AddHours(2),null,null);
            g.MarkArrived(start.AddMinutes(30));g.MarkDeparted(start.AddMinutes(90));db.Add(g);await db.SaveChangesAsync();
        });
        using var c=TestToken.Client(factory,x.Manager,"MANAGER");
        var report=await Read<ParkingReport>(await c.GetAsync(Root(x.Property)+"/report?from=2010-01-01&through=2010-01-02"));
        Assert.Equal((1,0,1),(report.Days[0].Entries,report.Days[0].Exits,report.Days[0].Reservations));
        Assert.Equal((0,1,0),(report.Days[1].Entries,report.Days[1].Exits,report.Days[1].Reservations));
    }

    [PostgresFact]
    public async Task UnlinkedOrNonmemberRecipientsAreSkippedWithoutExposingVehicleDetails()
    {
        var x=await Seed();using var c=TestToken.Client(factory,x.Manager,"MANAGER");
        await Read<VehicleResponse>(await c.PostAsJsonAsync(Root(x.Property)+"/vehicles",Vehicle(x,"34SKIP1")),HttpStatusCode.Created);
        await factory.WithDb(async db=>{
            Assert.Empty(await db.PersonMessages.Where(m=>m.PropertyId==x.Property).ToListAsync());
            var receipt=Assert.Single(await db.ProcessedMessages.Where(m=>m.PropertyId==x.Property).ToListAsync());Assert.Equal(InboxStatus.SKIPPED,receipt.Status);
        });
    }

    [PostgresFact]
    public async Task ReleaseReportAggregatesTwoThousandRecordsWithoutReturningPersonalFields()
    {
        var x=await Seed();var start=DateTimeOffset.UtcNow.AddDays(-5);const int count=2000;
        await factory.WithDb(async db=>{
            var policy=await db.ParkingPolicies.SingleAsync(p=>p.PropertyId==x.Property);
            for(var i=0;i<count;i++){
                var g=new GuestVehicle(policy,x.Unit,"T"+i.ToString("D7"),null,start,start.AddHours(1),null,null);
                g.MarkArrived(start);if(i%2==0)g.MarkDeparted(start.AddMinutes(30));db.Add(g);
            }
            await db.SaveChangesAsync();
        });
        using var c=TestToken.Client(factory,x.Manager,"MANAGER");var samples=new List<double>();
        for(var i=0;i<7;i++){
            var watch=System.Diagnostics.Stopwatch.StartNew();
            var report=await Read<ParkingReport>(await c.GetAsync(Root(x.Property)+$"/report?from={Today.AddDays(-6):yyyy-MM-dd}&through={Today:yyyy-MM-dd}"));
            samples.Add(watch.Elapsed.TotalMilliseconds);
            Assert.Equal(count,report.Days.Sum(d=>d.Entries));Assert.Equal(count/2,report.Days.Sum(d=>d.Exits));Assert.Equal(count,report.Days.Sum(d=>d.Reservations));
            Assert.Equal(count/2,report.ManualGuestsInside);Assert.Equal(count/2,report.OverdueGuests);
            var json=JsonSerializer.Serialize(report);Assert.DoesNotContain("Plate",json);Assert.DoesNotContain("PersonId",json);Assert.DoesNotContain("HostUnitId",json);
        }
        samples.Sort();Console.WriteLine($"[BUILDINGFLOW][PERFORMANCE] rows={count};samples={samples.Count};p50_ms={samples[3]:F2};p95_ms={samples[^1]:F2};local-isolated-not-production-SLO");
    }

    [PostgresFact]
    public async Task LegacySelfVehicleEndpointsRejectAccountantEvenWithLinkedOccupancy()
    {
        var x=await Seed();using var accountant=TestToken.Client(factory,x.Resident,"ACCOUNTANT");
        var root=$"/api/v1/parking/my-vehicles?propertyId={x.Property}&unitId={x.Unit}";
        Assert.Equal(HttpStatusCode.Forbidden,(await accountant.GetAsync(root)).StatusCode);
        var request=new ResidentVehicleRequest(Guid.NewGuid(),x.Property,x.Unit,"34ROLE08","CAR",null,null,null);
        Assert.Equal(HttpStatusCode.Forbidden,(await accountant.PostAsJsonAsync("/api/v1/parking/my-vehicles",request)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await accountant.PostAsJsonAsync($"/api/v1/parking/my-vehicles/{Guid.NewGuid()}/deactivate?propertyId={x.Property}",new ParkingVersionRequest(1))).StatusCode);
    }
}
