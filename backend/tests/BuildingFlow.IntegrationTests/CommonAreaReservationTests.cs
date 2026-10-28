using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class CommonAreaReservationTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static readonly string Day = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)).AddDays(7).ToString("yyyy-MM-dd");
    private sealed record World(Property P, string User, CommonArea Area, Unit Unit);
    private static string Root(World w) => $"/api/v1/properties/{w.P.Id}/common-areas/{w.Area.Id}";
    private static CommonAreaReservationRequest Request(string start = "10:00", string end = "11:00", int size = 10, Guid? id = null, Guid? unit = null) =>
        new(id ?? Guid.NewGuid(), unit, Day, start, end, size, " Maç ");
    private async Task<World> Seed(CommonAreaUsage usage = CommonAreaUsage.RESERVATION, PropertyType type = PropertyType.APARTMENT)
    {
        var user = Guid.NewGuid().ToString(); var p = new Property("Rez " + user, "City", "District", "Address", type); var unit = p.AddUnit("1", 0, 80);
        var area = new CommonArea(p.Id, Guid.NewGuid(), new string('A', 64), "Halı saha", CommonAreaType.SPORTS_COURT, null, CommonAreaVisibility.PROPERTY, usage, 14, null, null, new(8, 0), new(22, 0), 0);
        await factory.WithDb(async db => { db.AddRange(p, unit, new PropertyMembership(p.Id, user)); await db.SaveChangesAsync(); db.Add(area); await db.SaveChangesAsync(); });
        return new(p, user, area, unit);
    }
    private static async Task<T> Read<T>(HttpResponseMessage r, HttpStatusCode status = HttpStatusCode.OK)
    { Assert.True(r.StatusCode == status, $"{r.StatusCode}: {await r.Content.ReadAsStringAsync()}"); return (await r.Content.ReadFromJsonAsync<T>())!; }
    private static async Task Code(HttpResponseMessage r, HttpStatusCode status, string code)
    { var body = await r.Content.ReadAsStringAsync(); Assert.True(r.StatusCode == status, $"{r.StatusCode}: {body}"); Assert.Contains(code, body); }

    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")]
    public async Task ManagerCreatesReplaysEditsAndCancelsWithHistory(string role)
    {
        var w = await Seed(); using var c = TestToken.Client(factory, w.User, role); var request = Request(unit: w.Unit.Id);
        var created = await Read<CommonAreaReservationResponse>(await c.PostAsJsonAsync(Root(w) + "/reservations", request), HttpStatusCode.Created);
        Assert.Equal(("10:00", "11:00", 10, "Maç", "CONFIRMED", "1", 1L), (created.StartsAt, created.EndsAt, created.PartySize, created.Note, created.Status, created.UnitNumber, created.Version));
        var replay = await Read<CommonAreaReservationResponse>(await c.PostAsJsonAsync(Root(w) + "/reservations", request with { Note = "Maç" }));
        Assert.True(replay.Replayed); Assert.Equal(created.Id, replay.Id);
        await Code(await c.PostAsJsonAsync(Root(w) + "/reservations", request with { PartySize = 9 }), HttpStatusCode.Conflict, "BF_COMMON_AREA_CONFLICT");
        var edited = await Read<CommonAreaReservationResponse>(await c.PutAsJsonAsync(Root(w) + $"/reservations/{created.Id}", request with { StartsAt = "12:00", EndsAt = "13:30", Version = 1 }));
        Assert.Equal(("12:00", "13:30", 2L), (edited.StartsAt, edited.EndsAt, edited.Version));
        await Code(await c.PutAsJsonAsync(Root(w) + $"/reservations/{created.Id}", request with { Version = 1 }), HttpStatusCode.Conflict, "BF_RECORD_CHANGED");
        await Code(await c.PostAsJsonAsync(Root(w) + $"/reservations/{created.Id}/cancel", new CommonAreaReservationCancelRequest(" ", 2)), HttpStatusCode.BadRequest, "BF_COMMON_AREA_RESERVATION_INVALID");
        var cancelled = await Read<CommonAreaReservationResponse>(await c.PostAsJsonAsync(Root(w) + $"/reservations/{created.Id}/cancel", new CommonAreaReservationCancelRequest(" Yağmur ", 2)));
        Assert.Equal(("CANCELLED", "Yağmur", 3L), (cancelled.Status, cancelled.CancelReason, cancelled.Version)); Assert.NotNull(cancelled.CancelledAt);
        await Code(await c.PostAsJsonAsync(Root(w) + $"/reservations/{created.Id}/cancel", new CommonAreaReservationCancelRequest("Tekrar", 3)), HttpStatusCode.Conflict, "BF_COMMON_AREA_RESERVATION_CLOSED");
        await Code(await c.PutAsJsonAsync(Root(w) + $"/reservations/{created.Id}", request with { Version = 3 }), HttpStatusCode.Conflict, "BF_COMMON_AREA_RESERVATION_CLOSED");
        var list = await Read<PagedRows<CommonAreaReservationResponse>>(await c.GetAsync(Root(w) + $"/reservations?from={Day}&to={Day}"));
        Assert.Single(list.Items); Assert.Equal("CANCELLED", list.Items[0].Status);
        Assert.Empty((await Read<PagedRows<CommonAreaReservationResponse>>(await c.GetAsync(Root(w) + $"/reservations?from={Day}&to={Day}&status=CONFIRMED"))).Items);
        Assert.Empty((await Read<CommonAreaScheduleResponse>(await c.GetAsync(Root(w) + $"/schedule?date={Day}"))).Busy);
        await factory.WithDb(async db => { var row = await db.Set<CommonAreaReservation>().SingleAsync(x => x.Id == created.Id); Assert.Equal(w.User, row.CreatedBy); Assert.Equal(created.CreatedAt, row.CreatedAt); });
    }

    [PostgresFact] public async Task CapacityIsSummedOverOverlapsAndScheduleShowsLoadOnly()
    {
        var w = await Seed(); using var c = TestToken.Client(factory, w.User, "MANAGER"); var url = Root(w) + "/reservations";
        await Read<CommonAreaReservationResponse>(await c.PostAsJsonAsync(url, Request("10:00", "11:00", 10, unit: w.Unit.Id)), HttpStatusCode.Created);
        await Code(await c.PostAsJsonAsync(url, Request("10:30", "11:30", 5)), HttpStatusCode.Conflict, "BF_COMMON_AREA_RESERVATION_FULL");
        await Read<CommonAreaReservationResponse>(await c.PostAsJsonAsync(url, Request("10:30", "11:30", 4)), HttpStatusCode.Created);
        await Read<CommonAreaReservationResponse>(await c.PostAsJsonAsync(url, Request("11:30", "12:00", 14)), HttpStatusCode.Created);
        await Code(await c.PostAsJsonAsync(url, Request("09:00", "13:00", 1)), HttpStatusCode.Conflict, "BF_COMMON_AREA_RESERVATION_FULL");
        var raw = await c.GetStringAsync(Root(w) + $"/schedule?date={Day}");
        Assert.DoesNotContain("Maç", raw); Assert.DoesNotContain(w.Unit.Id.ToString(), raw);
        var schedule = System.Text.Json.JsonSerializer.Deserialize<CommonAreaScheduleResponse>(raw, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
        Assert.Equal(("08:00", "22:00", 14, true), (schedule.OpensAt, schedule.ClosesAt, schedule.Capacity, schedule.Reservable));
        Assert.Equal([new("10:00", "10:30", 10, "PARTIAL"), new("10:30", "11:00", 14, "FULL"), new("11:00", "11:30", 4, "PARTIAL"), new("11:30", "12:00", 14, "FULL")], schedule.Busy);
    }

    [PostgresFact] public async Task RejectsInvalidClosedAndForeignInputWithoutPartialWrites()
    {
        var w = await Seed(); var foreign = await Seed(); using var c = TestToken.Client(factory, w.User, "MANAGER"); var url = Root(w) + "/reservations"; var r = Request();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        foreach (var bad in new[] { r with { StartsAt = "10:10" }, r with { EndsAt = "09:00" }, r with { PartySize = 0 }, r with { Date = "2026-13-01" }, r with { RequestId = Guid.Empty }, r with { Version = 1 }, r with { UnitId = foreign.Unit.Id }, r with { Note = new string('x', 501) }, r with { StartsAt = "10:00:00" } })
            await Code(await c.PostAsJsonAsync(url, bad), HttpStatusCode.BadRequest, "BF_COMMON_AREA_RESERVATION_INVALID");
        foreach (var closed in new[] { r with { StartsAt = "07:00" }, r with { EndsAt = "22:15" }, r with { Date = today.AddDays(-1).ToString("yyyy-MM-dd") }, r with { Date = today.AddDays(400).ToString("yyyy-MM-dd") } })
            await Code(await c.PostAsJsonAsync(url, closed), HttpStatusCode.Conflict, "BF_COMMON_AREA_RESERVATION_CLOSED");
        await Code(await c.PostAsJsonAsync(url, r with { PartySize = 15 }), HttpStatusCode.Conflict, "BF_COMMON_AREA_RESERVATION_FULL");
        foreach (var bad in new[] { "?from=2026-01-10&to=2026-01-01", "?status=DONE", "?pageSize=101", "?from=2026-01-01&to=2026-06-01" })
            Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync(url + bad)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync(Root(w) + "/schedule?date=bad")).StatusCode);
        await factory.WithDb(async db => Assert.False(await db.Set<CommonAreaReservation>().AnyAsync(x => x.PropertyId == w.P.Id)));
        var open = await Seed(CommonAreaUsage.OPEN_USE); using var o = TestToken.Client(factory, open.User, "MANAGER");
        await Code(await o.PostAsJsonAsync(Root(open) + "/reservations", r), HttpStatusCode.Conflict, "BF_COMMON_AREA_RESERVATION_CLOSED");
        Assert.False((await Read<CommonAreaScheduleResponse>(await o.GetAsync(Root(open) + $"/schedule?date={Day}"))).Reservable);
        await factory.WithDb(async db => { var a = await db.Set<CommonArea>().SingleAsync(x => x.Id == w.Area.Id); a.SetEnabled(false); await db.SaveChangesAsync(); });
        await Code(await c.PostAsJsonAsync(url, r), HttpStatusCode.Conflict, "BF_COMMON_AREA_RESERVATION_CLOSED");
    }

    [PostgresFact] public async Task NonManagersAndForeignScopesCannotReadOrWrite()
    {
        var w = await Seed(); var foreign = await Seed(); using var owner = TestToken.Client(factory, w.User, "MANAGER");
        var created = await Read<CommonAreaReservationResponse>(await owner.PostAsJsonAsync(Root(w) + "/reservations", Request()), HttpStatusCode.Created);
        foreach (var role in new[] { "RESIDENT", "OWNER", "TENANT", "ACCOUNTANT", "spoofed" }) {
            using var c = TestToken.Client(factory, w.User, role);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync(Root(w) + "/reservations", Request())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync(Root(w) + $"/reservations/{created.Id}/cancel", new CommonAreaReservationCancelRequest("x", 1))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync(Root(w) + $"/schedule?date={Day}")).StatusCode);
        }
        using var anonymous = factory.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Root(w) + $"/schedule?date={Day}")).StatusCode);
        using var other = TestToken.Client(factory, foreign.User, "MANAGER");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(Root(w) + "/reservations")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync(Root(w) + "/reservations", Request())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync(Root(foreign) + $"/reservations/{created.Id}/cancel", new CommonAreaReservationCancelRequest("x", 1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/v1/properties/{foreign.P.Id}/common-areas/{w.Area.Id}/reservations", Request())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PutAsJsonAsync($"/api/v1/properties/{w.P.Id}/common-areas/{Guid.NewGuid()}/reservations/{created.Id}", Request() with { Version = 1 })).StatusCode);
        await factory.WithDb(async db => Assert.Equal(1, await db.Set<CommonAreaReservation>().CountAsync(x => x.PropertyId == w.P.Id)));
        await factory.WithDb(async db => Assert.False(await db.Set<CommonAreaReservation>().AnyAsync(x => x.PropertyId == foreign.P.Id)));
    }

    [PostgresFact] public async Task ConcurrentBookingsNeverExceedCapacityAndSameUuidCreatesOnce()
    {
        var w = await Seed(); using var a = TestToken.Client(factory, w.User, "MANAGER"); using var b = TestToken.Client(factory, w.User, "MANAGER"); var url = Root(w) + "/reservations";
        var racing = await Task.WhenAll(a.PostAsJsonAsync(url, Request(size: 10)), b.PostAsJsonAsync(url, Request(size: 10)));
        Assert.Single(racing, r => r.StatusCode == HttpStatusCode.Created); Assert.Single(racing, r => r.StatusCode == HttpStatusCode.Conflict);
        var same = Request("12:00", "13:00", 2);
        var replay = await Task.WhenAll(a.PostAsJsonAsync(url, same), b.PostAsJsonAsync(url, same));
        Assert.Single(replay, r => r.StatusCode == HttpStatusCode.Created); Assert.Single(replay, r => r.StatusCode == HttpStatusCode.OK);
        await factory.WithDb(async db => Assert.Equal(12, await db.Set<CommonAreaReservation>().Where(x => x.PropertyId == w.P.Id).SumAsync(x => x.PartySize)));
    }

    [PostgresFact] public async Task FutureBookingsProtectAreaUnitAndDatabaseRules()
    {
        var w = await Seed(); using var c = TestToken.Client(factory, w.User, "MANAGER");
        var created = await Read<CommonAreaReservationResponse>(await c.PostAsJsonAsync(Root(w) + "/reservations", Request(unit: w.Unit.Id)), HttpStatusCode.Created);
        long areaVersion = 0; await factory.WithDb(async db => areaVersion = (await db.Set<CommonArea>().AsNoTracking().SingleAsync(a => a.Id == w.Area.Id)).Version);
        Assert.True(areaVersion > w.Area.Version); // A booking touches its area so concurrent hour/capacity edits cannot win silently.
        var area = new CommonAreaRequest(w.Area.RequestId, "Halı saha", "SPORTS_COURT", null, "PROPERTY", "RESERVATION", 14, null, null, "08:00", "22:00", "0.00", Version: areaVersion);
        foreach (var bad in new[] { area with { Capacity = 9 }, area with { OpensAt = "11:00" }, area with { Usage = "OPEN_USE" } })
            await Code(await c.PutAsJsonAsync($"/api/v1/properties/{w.P.Id}/common-areas/{w.Area.Id}", bad), HttpStatusCode.Conflict, "BF_COMMON_AREA_CONFLICT");
        var renamed = await Read<CommonAreaResponse>(await c.PutAsJsonAsync($"/api/v1/properties/{w.P.Id}/common-areas/{w.Area.Id}", area with { Name = "Kapalı saha", Capacity = 10 }));
        await Code(await c.DeleteAsync($"/api/v1/properties/{w.P.Id}/common-areas/{w.Area.Id}?version={renamed.Version}"), HttpStatusCode.Conflict, "BF_COMMON_AREA_CONFLICT");
        await c.PostAsJsonAsync(Root(w) + $"/reservations/{created.Id}/cancel", new CommonAreaReservationCancelRequest("Test", 1));
        await Code(await c.DeleteAsync($"/api/v1/properties/{w.P.Id}/common-areas/{w.Area.Id}?version={renamed.Version}"), HttpStatusCode.Conflict, "BF_COMMON_AREA_CONFLICT");
        long unitVersion = 0; await factory.WithDb(async db => unitVersion = (await db.Units.AsNoTracking().SingleAsync(u => u.Id == w.Unit.Id)).Version);
        Assert.Equal(HttpStatusCode.Conflict, (await c.DeleteAsync($"/api/v1/properties/{w.P.Id}/units/{w.Unit.Id}?version={unitVersion}")).StatusCode);
        await factory.WithDb(async db => {
            await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE common_area_reservations SET starts_at='10:05' WHERE id={created.Id}"));
            await Assert.ThrowsAnyAsync<Exception>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE common_area_reservations SET status='CONFIRMED' WHERE id={created.Id}"));
        });
    }
}
