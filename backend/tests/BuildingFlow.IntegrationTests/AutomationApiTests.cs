using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class AutomationApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
    private static string Root(Guid property) => $"/api/v1/properties/{property}/automations";
    private sealed record Fixture(string Actor, string Resident, Property Property, Unit Unit, Person Person, DuesCharge Charge);

    private async Task<Fixture> Seed(int dueInDays, decimal amount = 1000m, bool smsAccount = false, bool linked = true)
    {
        var actor = Guid.NewGuid().ToString(); var resident = Guid.NewGuid().ToString();
        var property = new Property("Rezidans " + actor[..8], "City", "District", "Address", PropertyType.APARTMENT);
        var unit = property.AddUnit("22", 2, 100);
        var due = Today.AddDays(dueInDays); var period = new DateOnly(due.Year, due.Month, 1);
        var person = new Person(property.Id, "Ada", "Kaya", "0555 123 45 67", "ada@example.test");
        person.SetProfile("NATURAL", "DOMESTIC", "TR", "12345678901", null, null, null, linked ? resident : null);
        var occupancy = new UnitOccupancy(property.Id, unit.Id, person.Id, OccupancyType.OWNER, period.AddYears(-1), null, true);
        var assessment = new DuesAssessment(property.Id, Guid.NewGuid(), new string('b', 64), period, amount, "TRY", due, "Aidat");
        var charge = assessment.AddCharge(unit.Id);
        await factory.WithDb(async db => {
            db.Add(property); db.Add(new PropertyMembership(property.Id, actor)); db.Add(new PropertyMembership(property.Id, resident));
            db.Add(person); db.Add(occupancy); db.Add(assessment);
            if (smsAccount)
            {
                var account = new SmsAccount(property.Id, "Site SMS", SmsProvider.MOCK, "BUILDING", 0);
                db.Add(account); db.Add(new SmsCreditTransaction(account, SmsCreditTransactionType.LOAD, 100, null, null, null, null, null));
            }
            await db.SaveChangesAsync();
        });
        return new(actor, resident, property, unit, person, charge);
    }

    private static AutomationRequest Request(string type, int[] offsets, bool sms = false, int send = 0, int quietStart = 0, int quietEnd = 0, long version = 0)
    {
        var info = AutomationCatalog.All.Single(t => t.Type == type);
        return new(type, info.DefaultName, null, null, send, quietStart, quietEnd, offsets, true, sms, false, info.DefaultTitle, info.DefaultBody, "Yönetim kurulu kararı", version);
    }

    private static async Task<AutomationResponse> Active(HttpClient client, Guid property, AutomationRequest request)
    {
        var created = await client.PostAsJsonAsync(Root(property), request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var value = (await created.Content.ReadFromJsonAsync<AutomationResponse>())!;
        Assert.Equal("PAUSED", value.Status);
        var activated = await client.PostAsJsonAsync($"{Root(property)}/{value.Id}/activate", new AutomationStatusRequest("Devreye alındı", value.Version));
        Assert.Equal(HttpStatusCode.OK, activated.StatusCode);
        return (await activated.Content.ReadFromJsonAsync<AutomationResponse>())!;
    }

    private static async Task<AutomationExecutionResponse> Run(HttpClient client, Guid property, Guid id)
    {
        var response = await client.PostAsync($"{Root(property)}/{id}/run", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AutomationExecutionResponse>())!;
    }

    private async Task<List<PersonMessage>> Messages(Guid property) =>
        await Read(db => db.PersonMessages.AsNoTracking().Where(m => m.PropertyId == property).OrderBy(m => m.CreatedAt).ToListAsync());

    private async Task<T> Read<T>(Func<BuildingFlowDbContext, Task<T>> query)
    {
        T value = default!;
        await factory.WithDb(async db => value = await query(db));
        return value;
    }

    private static Task<HttpResponseMessage> Pay(HttpClient client, Fixture x, decimal amount, long version) =>
        client.PostAsJsonAsync($"/api/v1/properties/{x.Property.Id}/payments", new CreatePaymentRequest(Guid.NewGuid(), x.Unit.Id,
            amount.ToString("0.00", CultureInfo.InvariantCulture), "TRY", Today, "CASH", null, "Tahsilat", [new AllocationRequest(x.Charge.Id, amount.ToString("0.00", CultureInfo.InvariantCulture), version)]));

    private static async Task<long> ChargeVersion(HttpClient client, Fixture x) =>
        (await client.GetFromJsonAsync<DuesChargeResponse>($"/api/v1/properties/{x.Property.Id}/dues/{x.Charge.Id}"))!.Version;

    [Fact]
    public void RecipientsAreMaskedAndAmountsUseTurkishFormat()
    {
        Assert.Equal("a******@example.test", AutomationEngine.MaskEmail("ada.kaya@example.test"));
        Assert.Equal("1.000,00 TL", AutomationEngine.Money(1000m));
    }

    [PostgresTheory] [InlineData("ACCOUNTANT")] [InlineData("RESIDENT")] [InlineData("")]
    public async Task OnlyManagementRolesReachAutomations(string role)
    {
        var x = await Seed(2); using var client = TestToken.Client(factory, x.Actor, role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Root(x.Property.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Root(x.Property.Id) + "/dispatches")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(Root(x.Property.Id), Request("DUES_DUE_REMINDER", [-2]))).StatusCode);
    }

    [PostgresFact]
    public async Task ManagerOfAnotherPropertyCannotReadOrRun()
    {
        var x = await Seed(2); var other = await Seed(2);
        using var owner = TestToken.Client(factory, x.Actor, "MANAGER");
        var rule = await Active(owner, x.Property.Id, Request("DUES_DUE_REMINDER", [-2]));
        using var stranger = TestToken.Client(factory, other.Actor, "MANAGER");
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(Root(x.Property.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"{Root(x.Property.Id)}/{rule.Id}/run", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"{Root(other.Property.Id)}/{rule.Id}/run", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"{Root(x.Property.Id)}/dispatches")).StatusCode);
    }

    [PostgresFact]
    public async Task InvalidRulesAreRejectedWithCatalogCode()
    {
        var x = await Seed(2); using var client = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        var bad = new[] {
            Request("DUES_DUE_REMINDER", [2]), Request("DUES_OVERDUE_REMINDER", [0]), Request("DUES_DUE_REMINDER", []),
            Request("DUES_DUE_REMINDER", [-1], send: 22, quietStart: 21, quietEnd: 9), Request("PAYMENT_RECEIVED", [1]),
            Request("DUES_DUE_REMINDER", [-1]) with { TitleTemplate = "{{personName}}" }, Request("DUES_DUE_REMINDER", [-1]) with { InApp = false },
            Request("DUES_DUE_REMINDER", [-1]) with { Reason = " " }, Request("DUES_DUE_REMINDER", [-1]) with { Type = "UNKNOWN" }
        };
        foreach (var request in bad)
        {
            var response = await client.PostAsJsonAsync(Root(x.Property.Id), request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("BF_AUTOMATION_INVALID", await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(Root(x.Property.Id), Request("DUES_DUE_REMINDER", [-1]))).StatusCode);
        var duplicate = await client.PostAsJsonAsync(Root(x.Property.Id), Request("DUES_DUE_REMINDER", [-2]));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains("BF_AUTOMATION_CONFLICT", await duplicate.Content.ReadAsStringAsync());
    }

    [PostgresFact]
    public async Task DueReminderReachesInAppInboxOnceAndRecordsUnavailableSms()
    {
        var x = await Seed(2); using var client = TestToken.Client(factory, x.Actor, "MANAGER");
        var rule = await Active(client, x.Property.Id, Request("DUES_DUE_REMINDER", [-3, -2, -1, 0], sms: true));
        var preview = (await client.GetFromJsonAsync<AutomationPreview>($"{Root(x.Property.Id)}/{rule.Id}/preview"))!;
        Assert.Equal(1, preview.Total); Assert.Contains("1.000,00 TL", preview.Items[0].Body); Assert.Equal(-2, preview.Items[0].OffsetDays);
        Assert.Empty(await Messages(x.Property.Id));
        var first = await Run(client, x.Property.Id, rule.Id);
        Assert.Equal("SUCCEEDED", first.Status); Assert.Equal(1, first.Evaluated); Assert.Equal(1, first.Dispatched); Assert.Equal(1, first.Skipped);
        var messages = await Messages(x.Property.Id);
        var message = Assert.Single(messages);
        Assert.Equal(x.Resident, message.RecipientUserId); Assert.Contains("son ödemeye 2 gün kaldı", message.Title); Assert.Contains("22 no.lu", message.Body);
        var again = await Run(client, x.Property.Id, rule.Id);
        Assert.Equal(0, again.Dispatched); Assert.Equal(0, again.Skipped);
        Assert.Single(await Messages(x.Property.Id));
        var history = (await client.GetFromJsonAsync<PagedRows<NotificationDispatchResponse>>($"{Root(x.Property.Id)}/dispatches"))!;
        Assert.Equal(2, history.TotalCount);
        var sms = history.Items.Single(d => d.Channel == "SMS");
        Assert.Equal("FAILED", sms.Status); Assert.Equal(SmsQueue.NoAccount, sms.ReasonCode); Assert.DoesNotContain("123 45 67", sms.RecipientMasked);
        Assert.DoesNotContain("Ada", JsonSerializer.Serialize(history));
        using var resident = TestToken.Client(factory, x.Resident, "RESIDENT");
        var inbox = (await resident.GetFromJsonAsync<PagedRows<PersonMessageResponse>>("/api/v1/notifications"))!;
        Assert.Contains(inbox.Items, m => m.Id == message.Id);
    }

    [PostgresFact]
    public async Task PartialPaymentUsesCurrentBalanceAndFullPaymentStopsReminders()
    {
        var x = await Seed(-1); using var client = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.Created, (await Pay(client, x, 400m, await ChargeVersion(client, x))).StatusCode);
        var rule = await Active(client, x.Property.Id, Request("DUES_OVERDUE_REMINDER", [1, 7, 15]));
        await Run(client, x.Property.Id, rule.Id);
        var message = Assert.Single(await Messages(x.Property.Id));
        Assert.Contains("600,00 TL", message.Body); Assert.Contains("1 gün önce", message.Body);

        var paid = await Seed(-7); using var other = TestToken.Client(factory, paid.Actor, "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.Created, (await Pay(other, paid, 1000m, await ChargeVersion(other, paid))).StatusCode);
        var paidRule = await Active(other, paid.Property.Id, Request("DUES_OVERDUE_REMINDER", [7]));
        var run = await Run(other, paid.Property.Id, paidRule.Id);
        Assert.Equal(0, run.Evaluated); Assert.Empty(await Messages(paid.Property.Id));
    }

    [PostgresFact]
    public async Task PausedRuleCannotRunAndRevisionsAreKept()
    {
        var x = await Seed(0); using var client = TestToken.Client(factory, x.Actor, "MANAGER");
        var rule = await Active(client, x.Property.Id, Request("DUES_DUE_REMINDER", [0]));
        var paused = (await (await client.PostAsJsonAsync($"{Root(x.Property.Id)}/{rule.Id}/pause", new AutomationStatusRequest("Tatil", rule.Version))).Content.ReadFromJsonAsync<AutomationResponse>())!;
        Assert.Equal("PAUSED", paused.Status);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"{Root(x.Property.Id)}/{rule.Id}/run", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"{Root(x.Property.Id)}/{rule.Id}", Request("DUES_DUE_REMINDER", [-1, 0], version: rule.Version))).StatusCode);
        var updated = await client.PutAsJsonAsync($"{Root(x.Property.Id)}/{rule.Id}", Request("DUES_DUE_REMINDER", [-1, 0], version: paused.Version));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var revisions = (await client.GetFromJsonAsync<List<AutomationRevisionResponse>>($"{Root(x.Property.Id)}/{rule.Id}/revisions"))!;
        Assert.Equal(["UPDATED", "PAUSED", "ACTIVATED", "CREATED"], revisions.Select(r => r.Action));
        Assert.Empty(await Messages(x.Property.Id));
    }

    [PostgresFact]
    public async Task UnlinkedOrMovedOutRecipientIsSkippedWithReason()
    {
        var x = await Seed(0, linked: false); using var client = TestToken.Client(factory, x.Actor, "MANAGER");
        var rule = await Active(client, x.Property.Id, Request("DUES_DUE_REMINDER", [0]));
        var run = await Run(client, x.Property.Id, rule.Id);
        Assert.Equal(0, run.Dispatched); Assert.Equal(1, run.Skipped);
        var row = Assert.Single((await client.GetFromJsonAsync<PagedRows<NotificationDispatchResponse>>($"{Root(x.Property.Id)}/dispatches"))!.Items);
        Assert.Equal("NO_APP_ACCOUNT", row.ReasonCode);

        var moved = await Seed(0);
        await factory.WithDb(async db => {
            var link = await db.UnitOccupancies.SingleAsync(o => o.PersonId == moved.Person.Id);
            db.Entry(link).Property(o => o.EndDate).CurrentValue = Today.AddDays(-1); await db.SaveChangesAsync();
        });
        using var manager = TestToken.Client(factory, moved.Actor, "MANAGER");
        var movedRule = await Active(manager, moved.Property.Id, Request("DUES_DUE_REMINDER", [0]));
        await Run(manager, moved.Property.Id, movedRule.Id);
        Assert.Equal("NO_RECIPIENT", Assert.Single((await manager.GetFromJsonAsync<PagedRows<NotificationDispatchResponse>>($"{Root(moved.Property.Id)}/dispatches"))!.Items).ReasonCode);
        Assert.Empty(await Messages(moved.Property.Id));
    }

    [PostgresFact]
    public async Task PaymentReceivedNotifiesDebtorWithUnitBalance()
    {
        var x = await Seed(5); using var client = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        var rule = await Active(client, x.Property.Id, Request("PAYMENT_RECEIVED", []));
        Assert.Equal(HttpStatusCode.Created, (await Pay(client, x, 250m, await ChargeVersion(client, x))).StatusCode);
        var run = await Run(client, x.Property.Id, rule.Id);
        Assert.Equal(1, run.Dispatched);
        var message = Assert.Single(await Messages(x.Property.Id));
        Assert.Equal("Ödemeniz alındı", message.Title); Assert.Contains("250,00 TL", message.Body); Assert.Contains("750,00 TL", message.Body);
        Assert.Equal(0, (await Run(client, x.Property.Id, rule.Id)).Dispatched);
    }

    [PostgresFact]
    public async Task QueuedReminderSmsIsWithdrawnWhenDebtIsPaidBeforeDispatch()
    {
        var x = await Seed(1, smsAccount: true); using var client = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        var rule = await Active(client, x.Property.Id, Request("DUES_DUE_REMINDER", [-1], sms: true));
        await Run(client, x.Property.Id, rule.Id);
        var queued = await Read(db => db.SmsMessages.AsNoTracking().SingleAsync(m => m.PropertyId == x.Property.Id));
        Assert.Equal(SmsMessageStatus.QUEUED, queued.Status);
        Assert.Equal(HttpStatusCode.Created, (await Pay(client, x, 1000m, await ChargeVersion(client, x))).StatusCode);
        await factory.WithDb(async db => {
            var guard = new AutomationSmsGuard(db);
            Assert.Equal("CHARGE_PAID", await guard.BlockReasonAsync(queued.Id, CancellationToken.None));
            await db.SaveChangesAsync();
        });
        var row = (await client.GetFromJsonAsync<PagedRows<NotificationDispatchResponse>>($"{Root(x.Property.Id)}/dispatches?channel=SMS"))!.Items.Single();
        Assert.Equal("CANCELLED", row.Status); Assert.Equal("CHARGE_PAID", row.ReasonCode);
    }

    [PostgresFact]
    public async Task DispatcherRejectsWithdrawnSmsAndNeverCallsProvider()
    {
        var x = await Seed(1, smsAccount: true); using var client = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        var rule = await Active(client, x.Property.Id, Request("DUES_DUE_REMINDER", [-1], sms: true));
        await Run(client, x.Property.Id, rule.Id);
        Assert.Equal(HttpStatusCode.Created, (await Pay(client, x, 1000m, await ChargeVersion(client, x))).StatusCode);
        using (var services = factory.Services.CreateScope())
            await services.ServiceProvider.GetRequiredService<SmsDispatcher>().DispatchPendingAsync(CancellationToken.None);
        var sms = await Read(db => db.SmsMessages.AsNoTracking().SingleAsync(m => m.PropertyId == x.Property.Id));
        Assert.Equal(SmsMessageStatus.FAILED, sms.Status); Assert.Equal("CHARGE_PAID", sms.ErrorCode); Assert.Null(sms.ProviderMessageId);
        var credit = await Read(db => db.SmsAccounts.AsNoTracking().SingleAsync(a => a.PropertyId == x.Property.Id));
        Assert.Equal(100, credit.CreditBalance);
    }

    [PostgresFact]
    public async Task ConcurrentSchedulersProduceOneReminderAndOneExecution()
    {
        var x = await Seed(-15); using var client = TestToken.Client(factory, x.Actor, "MANAGER");
        var rule = await Active(client, x.Property.Id, Request("DUES_OVERDUE_REMINDER", [15]));
        async Task<int> Tick() { using var scope = factory.Services.CreateScope(); return await scope.ServiceProvider.GetRequiredService<IAutomationScheduler>().RunDueAsync(CancellationToken.None); }
        await Task.WhenAll(Tick(), Tick(), Tick());
        await Tick();
        Assert.Single(await Messages(x.Property.Id));
        var executions = (await client.GetFromJsonAsync<PagedRows<AutomationExecutionResponse>>($"{Root(x.Property.Id)}/executions?automationId={rule.Id}"))!;
        var execution = Assert.Single(executions.Items);
        Assert.Equal("SCHEDULE", execution.Trigger); Assert.Equal("SUCCEEDED", execution.Status);
        var overview = (await client.GetFromJsonAsync<AutomationOverview>(Root(x.Property.Id)))!;
        Assert.Equal(1, overview.ActiveCount); Assert.Equal(1, overview.Channels.Single(c => c.Channel == "IN_APP").Delivered);
        Assert.False(overview.Health.PushAvailable);
    }
}
