using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class AutomationTypeApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private static string Types(Guid property) => $"/api/v1/properties/{property}/automation-types";
    private static string Rules(Guid property) => $"/api/v1/properties/{property}/automations";
    private sealed record Fixture(string Actor, string Resident, string Manager, Property Property, Unit Unit);

    private async Task<Fixture> Seed()
    {
        var actor = Guid.NewGuid().ToString(); var resident = Guid.NewGuid().ToString(); var manager = Guid.NewGuid().ToString();
        var property = new Property("Tür " + actor[..8], "City", "District", "Address", PropertyType.APARTMENT);
        var unit = property.AddUnit("22", 2, 100);
        var owner = new Person(property.Id, "Ada", "Kaya", "0555 123 45 67", "ada@example.test");
        owner.SetProfile("NATURAL", "DOMESTIC", "TR", "12345678901", null, null, null, resident);
        var tenant = new PersonTenant("Automation " + actor[..8]); var account = new PersonUserAccount("http://localhost:8080/realms/buildingflow", manager);
        var central = new CentralPerson(tenant.Id, "NATURAL", "DOMESTIC", "TR", "12345678902", "Yönetici", "Bey", null, null, "+905551112233", "yonetici@example.test");
        await factory.WithDb(async db => {
            db.AddRange(property, new PropertyMembership(property.Id, actor), new PropertyMembership(property.Id, resident), new PropertyMembership(property.Id, manager), owner,
                new UnitOccupancy(property.Id, unit.Id, owner.Id, OccupancyType.OWNER, Today.AddYears(-1), null, true), tenant, account, central,
                new PersonTenantMembership(tenant.Id, account.Id), new PersonPropertyScope(tenant.Id, property.Id));
            await db.SaveChangesAsync();
            db.AddRange(new CentralPersonAccountLink(tenant.Id, account.Id, central.Id, DateTimeOffset.UtcNow.AddMinutes(-5)),
                new ManagementAssignment(tenant.Id, property.Id, central.Id, "MANAGER", Today.AddDays(-10)));
            await db.SaveChangesAsync();
        });
        return new(actor, resident, manager, property, unit);
    }

    private static AutomationTypeRequest Type(string source, string trigger, string[] scopes, AutomationCondition[]? conditions = null, string? dateField = null,
        int[]? offsets = null, int? dayOfWeek = null, string body = "{{propertyName}} bildirimi", bool platform = false, long version = 0) =>
        new("Özel tür", "BellRing", source, conditions ?? [], trigger, dateField, offsets ?? [], null, dayOfWeek, scopes, "Bilgilendirme", body, platform, "Yönetim kararı", version);

    private static async Task<AutomationTypeResponse> Published(HttpClient client, Guid property, AutomationTypeRequest request)
    {
        var created = await client.PostAsJsonAsync(Types(property), request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var type = (await created.Content.ReadFromJsonAsync<AutomationTypeResponse>())!;
        Assert.Equal("DRAFT", type.Status);
        var published = await client.PostAsJsonAsync($"{Types(property)}/{type.Id}/publish", new AutomationTypeStatusRequest("Yayına alındı", type.Version));
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        return (await published.Content.ReadFromJsonAsync<AutomationTypeResponse>())!;
    }

    private static async Task<AutomationExecutionResponse> RuleRun(HttpClient client, Guid property, AutomationTypeResponse type, int[] offsets)
    {
        var request = new AutomationRequest("CUSTOM", type.Name, null, null, 0, 0, 0, offsets, true, false, false, type.DefaultTitle, type.DefaultBody, "Kural", 0, type.Id);
        var created = await client.PostAsJsonAsync(Rules(property), request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var rule = (await created.Content.ReadFromJsonAsync<AutomationResponse>())!;
        Assert.Equal(type.Id, rule.CustomTypeId);
        var active = (await (await client.PostAsJsonAsync($"{Rules(property)}/{rule.Id}/activate", new AutomationStatusRequest("Devreye", rule.Version))).Content.ReadFromJsonAsync<AutomationResponse>())!;
        var run = await client.PostAsync($"{Rules(property)}/{active.Id}/run", null);
        Assert.Equal(HttpStatusCode.OK, run.StatusCode);
        return (await run.Content.ReadFromJsonAsync<AutomationExecutionResponse>())!;
    }

    private async Task<List<PersonMessage>> Messages(Guid property)
    {
        List<PersonMessage> rows = [];
        await factory.WithDb(async db => rows = await db.PersonMessages.AsNoTracking().Where(m => m.PropertyId == property).ToListAsync());
        return rows;
    }

    [PostgresFact]
    public async Task OnlySiteAndPlatformAdminsDefineTypesAndPlatformTypesNeedSuperAdmin()
    {
        var x = await Seed();
        using var manager = TestToken.Client(factory, x.Actor, "MANAGER");
        var sources = (await manager.GetFromJsonAsync<List<AutomationSourceInfo>>(Types(x.Property.Id) + "/sources"))!;
        Assert.Equal(6, sources.Count);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync(Types(x.Property.Id), Type("UNIT", "CALENDAR", ["OWNERS"], dayOfWeek: 1))).StatusCode);
        using var site = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.Forbidden, (await site.PostAsJsonAsync(Types(x.Property.Id), Type("UNIT", "CALENDAR", ["OWNERS"], dayOfWeek: 1, platform: true))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await site.PostAsJsonAsync(Types(x.Property.Id), Type("UNIT", "CALENDAR", ["OWNERS"], dayOfWeek: 1))).StatusCode);
        using var platform = TestToken.Client(factory, x.Actor, "SUPER_ADMIN");
        var global = (await (await platform.PostAsJsonAsync(Types(x.Property.Id), Type("UNIT", "CALENDAR", ["OWNERS"], dayOfWeek: 2, platform: true))).Content.ReadFromJsonAsync<AutomationTypeResponse>())!;
        Assert.True(global.IsPlatform);
        var other = await Seed();
        using var otherAdmin = TestToken.Client(factory, other.Actor, "SITE_ADMIN");
        var visible = (await otherAdmin.GetFromJsonAsync<List<AutomationTypeResponse>>(Types(other.Property.Id)))!;
        Assert.Contains(visible, t => t.Id == global.Id && !t.CanEdit);
        Assert.DoesNotContain(visible, t => !t.IsPlatform);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherAdmin.PostAsJsonAsync($"{Types(other.Property.Id)}/{global.Id}/archive", new AutomationTypeStatusRequest("x", global.Version))).StatusCode);
    }

    [PostgresFact]
    public async Task TypesOnlyUseCodeOwnedFieldsValuesScopesAndVariables()
    {
        var x = await Seed(); using var site = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        var bad = new[] {
            Type("PAYROLL", "EVENT", ["MANAGERS"]),
            Type("REQUEST", "EVENT", ["MANAGERS"], [new("description", "EQ", ["x"])]),
            Type("REQUEST", "EVENT", ["MANAGERS"], [new("category", "LIKE", ["ELEVATOR"])]),
            Type("REQUEST", "EVENT", ["MANAGERS"], [new("category", "IN", ["DROP TABLE"])]),
            Type("REQUEST", "DATE_OFFSET", ["MANAGERS"], offsets: [2]),
            Type("SUPPLIER_CONTRACT", "EVENT", ["OWNERS"]),
            Type("UNIT", "EVENT", ["OWNERS"]),
            Type("REQUEST", "EVENT", ["MANAGERS"], body: "{{personPhone}}"),
            Type("UNIT", "CALENDAR", ["OWNERS"])
        };
        foreach (var request in bad)
        {
            var response = await site.PostAsJsonAsync(Types(x.Property.Id), request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("BF_AUTOMATION_INVALID", await response.Content.ReadAsStringAsync());
        }
    }

    [PostgresFact]
    public async Task DraftTypeCannotBeUsedAndArchivedTypeStopsRunning()
    {
        var x = await Seed(); using var site = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        var draft = (await (await site.PostAsJsonAsync(Types(x.Property.Id), Type("UNIT", "CALENDAR", ["OWNERS"], dayOfWeek: 1))).Content.ReadFromJsonAsync<AutomationTypeResponse>())!;
        var request = new AutomationRequest("CUSTOM", "x", null, null, 0, 0, 0, [], true, false, false, "t", "b", "Kural", 0, draft.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await site.PostAsJsonAsync(Rules(x.Property.Id), request)).StatusCode);
        var weekday = (int)Today.DayOfWeek == 0 ? 7 : (int)Today.DayOfWeek;
        var type = await Published(site, x.Property.Id, Type("UNIT", "CALENDAR", ["OWNERS"], dayOfWeek: weekday, body: "{{unitNumber}} için sayaç okuma günü"));
        Assert.Equal(HttpStatusCode.BadRequest, (await site.PostAsJsonAsync(Rules(x.Property.Id), new AutomationRequest("CUSTOM", "x", null, null, 0, 0, 0, [], true, false, false, "t", "{{balance}}", "Kural", 0, type.Id))).StatusCode);
        var run = await RuleRun(site, x.Property.Id, type, []);
        Assert.Equal(1, run.Dispatched);
        var message = Assert.Single(await Messages(x.Property.Id));
        Assert.Equal(x.Resident, message.RecipientUserId); Assert.Equal("22 için sayaç okuma günü", message.Body);
        var current = (await site.GetFromJsonAsync<List<AutomationTypeResponse>>(Types(x.Property.Id)))!.Single(t => t.Id == type.Id);
        Assert.Equal(1, current.RuleCount);
        Assert.Equal(HttpStatusCode.Conflict, (await site.PutAsJsonAsync($"{Types(x.Property.Id)}/{type.Id}", Type("REQUEST", "EVENT", ["MANAGERS"], version: current.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await site.PostAsJsonAsync($"{Types(x.Property.Id)}/{type.Id}/archive", new AutomationTypeStatusRequest("Kullanılmıyor", current.Version))).StatusCode);
        var preview = (await site.GetFromJsonAsync<AutomationOverview>(Rules(x.Property.Id)))!;
        Assert.DoesNotContain(preview.Types, t => t.CustomTypeId == type.Id);
        var revisions = (await site.GetFromJsonAsync<List<AutomationRevisionResponse>>($"{Types(x.Property.Id)}/{type.Id}/revisions"))!;
        Assert.Equal(["ARCHIVED", "PUBLISHED", "CREATED"], revisions.Select(r => r.Action));
    }

    [PostgresFact]
    public async Task NewElevatorRequestNotifiesManagerAccountsAndRequesterOnlyOnce()
    {
        var x = await Seed(); using var site = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        var type = await Published(site, x.Property.Id, Type("REQUEST", "EVENT", ["MANAGERS", "REQUESTER"], [new("category", "IN", ["ELEVATOR", "WATER"])],
            body: "{{unitNumber}} bölümünden yeni {{requestCategory}} talebi ({{requestPriority}})."));
        var rule = new AutomationRequest("CUSTOM", type.Name, null, null, 0, 0, 0, [], true, false, false, type.DefaultTitle, type.DefaultBody, "Kural", 0, type.Id);
        var created = (await (await site.PostAsJsonAsync(Rules(x.Property.Id), rule)).Content.ReadFromJsonAsync<AutomationResponse>())!;
        await site.PostAsJsonAsync($"{Rules(x.Property.Id)}/{created.Id}/activate", new AutomationStatusRequest("Devreye", created.Version));
        var elevator = new MobileRequest(Guid.NewGuid(), x.Property.Id, x.Unit.Id, "OWNER", x.Resident, "Asansör çalışmıyor, 3. katta kaldı");
        elevator.Details("ELEVATOR", "HIGH", null);
        var cleaning = new MobileRequest(Guid.NewGuid(), x.Property.Id, x.Unit.Id, "OWNER", x.Resident, "Merdiven kirli");
        cleaning.Details("CLEANING", "LOW", null);
        await factory.WithDb(async db => { db.AddRange(elevator, cleaning); await db.SaveChangesAsync(); });
        var first = (await (await site.PostAsync($"{Rules(x.Property.Id)}/{created.Id}/run", null)).Content.ReadFromJsonAsync<AutomationExecutionResponse>())!;
        Assert.Equal(1, first.Evaluated); Assert.Equal(2, first.Dispatched);
        var messages = await Messages(x.Property.Id);
        Assert.Equal(new[] { x.Manager, x.Resident }.Order(), messages.Select(m => m.RecipientUserId).Order());
        Assert.All(messages, m => { Assert.Contains("Asansör", m.Body); Assert.Contains("Yüksek", m.Body); Assert.DoesNotContain("3. katta", m.Body); });
        Assert.Null(messages.Single(m => m.RecipientUserId == x.Manager).PersonId);
        Assert.Equal(0, (await (await site.PostAsync($"{Rules(x.Property.Id)}/{created.Id}/run", null)).Content.ReadFromJsonAsync<AutomationExecutionResponse>())!.Dispatched);
        using var managerClient = TestToken.Client(factory, x.Manager, "MANAGER");
        var inbox = (await managerClient.GetFromJsonAsync<PagedRows<PersonMessageResponse>>("/api/v1/notifications"))!;
        Assert.Single(inbox.Items);
    }

    [PostgresFact]
    public async Task MaintenanceDateOffsetReachesCurrentOccupantsWithPreview()
    {
        var x = await Seed(); using var site = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        await factory.WithDb(async db => {
            db.Add(new MaintenancePlan(x.Property.Id, "Asansör periyodik bakımı", null, MaintenanceType.MAINTENANCE, Today.AddDays(2), "A blok", null, MaintenanceStatus.PLANNED));
            db.Add(new MaintenancePlan(x.Property.Id, "İptal edilen bakım", null, MaintenanceType.MAINTENANCE, Today.AddDays(2), null, null, MaintenanceStatus.CANCELLED));
            await db.SaveChangesAsync();
        });
        var type = await Published(site, x.Property.Id, Type("MAINTENANCE_PLAN", "DATE_OFFSET", ["OWNERS"], [new("status", "EQ", ["PLANNED"])], "plannedDate", [-2],
            body: "{{plannedDate}} tarihinde {{maintenanceTitle}} yapılacak ({{daysLeft}} gün)."));
        var preview = (await site.GetFromJsonAsync<AutomationTypePreview>($"{Types(x.Property.Id)}/{type.Id}/preview"))!;
        Assert.Equal(1, preview.Records); Assert.Equal(1, preview.Recipients);
        var run = await RuleRun(site, x.Property.Id, type, [-2]);
        Assert.Equal(1, run.Dispatched);
        Assert.Contains("Asansör periyodik bakımı yapılacak (2 gün)", Assert.Single(await Messages(x.Property.Id)).Body);
    }
}
