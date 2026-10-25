using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class LatePenaltyApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private static string Root(Guid property) => $"/api/v1/properties/{property}/late-penalties";
    private sealed record Fixture(string Actor, Property Property, Unit Unit, DuesCharge Charge);
    private async Task<Fixture> Seed(bool applied = true, decimal amount = 3000m, int dueDaysAgo = 40)
    {
        var actor = Guid.NewGuid().ToString(); var property = new Property("Penalty " + actor, "City", "District", "Address", PropertyType.APARTMENT);
        var unit = property.AddUnit("1", 1, 100); var due = Today.AddDays(-dueDaysAgo); var period = new DateOnly(due.Year, due.Month, 1);
        var assessment = new DuesAssessment(property.Id, Guid.NewGuid(), new string('a', 64), period, amount, "TRY", due, "Aidat");
        var charge = assessment.AddCharge(unit.Id);
        await factory.WithDb(async db => {
            db.Add(property); db.Add(new PropertyMembership(property.Id, actor)); db.Add(assessment);
            if (applied) db.Add(new LatePenaltySwitch(property.Id, Guid.NewGuid(), new(2000, 1, 1), true, "Yönetim kurulu kararı"));
            await db.SaveChangesAsync();
        });
        return new(actor, property, unit, charge);
    }
    private static async Task<string?> Code(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    private static Task<LatePenaltyChargeDetail?> Detail(HttpClient client, Fixture x) => client.GetFromJsonAsync<LatePenaltyChargeDetail>(Root(x.Property.Id) + "/charges/" + x.Charge.Id);
    private static async Task<long> ChargeVersion(HttpClient client, Fixture x) =>
        (await client.GetFromJsonAsync<DuesChargeResponse>($"/api/v1/properties/{x.Property.Id}/dues/{x.Charge.Id}"))!.Version;
    private static async Task<HttpResponseMessage> Pay(HttpClient client, Fixture x, DateOnly date, long version, string? principal, string? penalty)
    {
        List<AllocationRequest> parts = [];
        if (principal is not null) parts.Add(new(x.Charge.Id, principal, version));
        if (penalty is not null) parts.Add(new(x.Charge.Id, penalty, version, "PENALTY"));
        var total = parts.Sum(value => decimal.Parse(value.Amount!, System.Globalization.CultureInfo.InvariantCulture));
        return await client.PostAsJsonAsync($"/api/v1/properties/{x.Property.Id}/payments", new CreatePaymentRequest(Guid.NewGuid(), x.Unit.Id,
            total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), "TRY", date, "CASH", null, "Tahsilat", parts));
    }

    [PostgresFact] public async Task SwitchOffByDefaultNeverAccruesAndOnlyFutureSwitchChangesAreAccepted()
    {
        var x = await Seed(applied: false); using var admin = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        var settings = (await admin.GetFromJsonAsync<LatePenaltySettings>(Root(x.Property.Id) + "/settings"))!;
        Assert.False(settings.AppliedToday); Assert.Equal("5.00", settings.StatutoryRate); Assert.Equal("DAILY_30", settings.DefaultMethod); Assert.True(settings.CanApprove);
        var run = (await (await admin.PostAsJsonAsync(Root(x.Property.Id) + "/accruals/run", new LatePenaltyRunRequest(Today))).Content.ReadFromJsonAsync<LatePenaltyRunResult>())!;
        Assert.Equal(0, run.AccrualCount);
        Assert.Equal("0.00", (await Detail(admin, x))!.PendingPenalty);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Root(x.Property.Id) + "/switches", new LatePenaltySwitchRequest(Guid.NewGuid(), true, Today.AddDays(-1), "Geçmiş"))).StatusCode);
        var request = new LatePenaltySwitchRequest(Guid.NewGuid(), true, Today, "Genel kurul kararı");
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(Root(x.Property.Id) + "/switches", request)).StatusCode);
        var replay = await admin.PostAsJsonAsync(Root(x.Property.Id) + "/switches", request);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode); Assert.True((await replay.Content.ReadFromJsonAsync<LatePenaltySwitchResponse>())!.Replayed);
        var conflict = await admin.PostAsJsonAsync(Root(x.Property.Id) + "/switches", request with { Apply = false });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode); Assert.Equal("BF_LATE_PENALTY_REQUEST_CONFLICT", await Code(conflict));
        var detail = (await Detail(admin, x))!;
        Assert.True(detail.AppliedToday); Assert.Equal("5.00", detail.PendingPenalty);
        using var manager = TestToken.Client(factory, x.Actor, "MANAGER");
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync(Root(x.Property.Id) + "/switches", request with { RequestId = Guid.NewGuid(), EffectiveFrom = Today.AddDays(5) })).StatusCode);
    }

    [PostgresFact] public async Task AccrualRunIsIdempotentAndPaymentCrystallizesThenReversesPenalty()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "ACCOUNTANT");
        var first = (await (await client.PostAsJsonAsync(Root(x.Property.Id) + "/accruals/run", new LatePenaltyRunRequest(Today.AddDays(-10)))).Content.ReadFromJsonAsync<LatePenaltyRunResult>())!;
        Assert.Equal((1, "150.00"), (first.AccrualCount, first.Amount));
        var again = (await (await client.PostAsJsonAsync(Root(x.Property.Id) + "/accruals/run", new LatePenaltyRunRequest(Today.AddDays(-10)))).Content.ReadFromJsonAsync<LatePenaltyRunResult>())!;
        Assert.Equal(0, again.AccrualCount);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Root(x.Property.Id) + "/accruals/run", new LatePenaltyRunRequest(Today.AddDays(1)))).StatusCode);
        var detail = (await Detail(client, x))!;
        Assert.Equal(("150.00", "50.00", "3200.00"), (detail.PenaltyBalance, detail.PendingPenalty, detail.TotalDue));
        var segment = Assert.Single(Assert.Single(detail.Accruals).Segments);
        Assert.Equal((30, "3000.00", "5.00", "DAILY_30"), (segment.Days, segment.Principal, segment.MonthlyRate, segment.Method));
        var preview = (await client.GetFromJsonAsync<LatePenaltyPreview>(Root(x.Property.Id) + $"/preview?unitId={x.Unit.Id}&paidDate={Today:yyyy-MM-dd}&amount=1000"))!;
        var line = Assert.Single(preview.Lines);
        Assert.Equal(("200.00", "200.00", "800.00"), (line.PenaltyDue, line.SuggestedPenalty, line.SuggestedPrincipal));

        var tooMuch = await Pay(client, x, Today, await ChargeVersion(client, x), null, "200.01");
        Assert.Equal(HttpStatusCode.Conflict, tooMuch.StatusCode); Assert.Equal("BF_LATE_PENALTY_BALANCE", await Code(tooMuch));
        var paid = await Pay(client, x, Today, await ChargeVersion(client, x), "3000.00", "200.00");
        Assert.Equal(HttpStatusCode.Created, paid.StatusCode);
        var payment = (await paid.Content.ReadFromJsonAsync<PaymentResponse>())!;
        Assert.Equal(["PRINCIPAL", "PENALTY", "PENALTY"], payment.Allocations.Select(value => value.Kind).OrderByDescending(value => value == "PRINCIPAL"));
        detail = (await Detail(client, x))!;
        Assert.Equal(("0.00", "0.00", "0.00", "200.00"), (detail.PrincipalBalance, detail.PenaltyBalance, detail.PendingPenalty, detail.PenaltyPaid));
        var report = (await client.GetFromJsonAsync<LatePenaltyReport>(Root(x.Property.Id) + "/report"))!;
        Assert.Equal("200.00", report.Years.Single(value => value.Year == Today.Year).Collected);

        var cancelled = await client.PutAsJsonAsync($"/api/v1/properties/{x.Property.Id}/payments/{payment.Id}/cancel", new CancelChargeRequest("Hatalı kayıt", payment.Version));
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        detail = (await Detail(client, x))!;
        Assert.Equal(("3000.00", "200.00", "0.00"), (detail.PrincipalBalance, detail.PenaltyBalance, detail.PenaltyPaid));
        await factory.WithDb(async db => Assert.Equal(2, await db.PaymentAllocations.CountAsync(value => value.PaymentId == payment.Id && value.Kind == AllocationKind.PENALTY)));
    }

    [PostgresFact] public async Task BackDatedPaymentRebuildsUnpaidAccrualsButPaidPenaltyBlocksRebuild()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "MANAGER");
        await client.PostAsJsonAsync(Root(x.Property.Id) + "/accruals/run", new LatePenaltyRunRequest(Today));
        Assert.Equal("200.00", (await Detail(client, x))!.PenaltyBalance);
        Assert.Equal(HttpStatusCode.Created, (await Pay(client, x, Today.AddDays(-20), await ChargeVersion(client, x), "1000.00", null)).StatusCode);
        var detail = (await Detail(client, x))!;
        Assert.Equal(("2000.00", "166.67"), (detail.PrincipalBalance, detail.PenaltyBalance));
        Assert.Contains(detail.Accruals, value => value.Status == "SUPERSEDED");
        var rebuilt = Assert.Single(detail.Accruals, value => value.Status == "ACTIVE");
        Assert.Equal(["3000.00", "2000.00"], rebuilt.Segments.Select(value => value.Principal));
        Assert.Equal(HttpStatusCode.Created, (await Pay(client, x, Today, await ChargeVersion(client, x), null, "10.00")).StatusCode);
        var blocked = await Pay(client, x, Today.AddDays(-30), await ChargeVersion(client, x), "100.00", null);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode); Assert.Equal("BF_LATE_PENALTY_REBUILD_BLOCKED", await Code(blocked));
    }

    [PostgresFact] public async Task RepeatedMonthEndJobCrystallizesOnlyOnce()
    {
        var x = await Seed(dueDaysAgo: 75); var monthEnd = new DateOnly(Today.Year, Today.Month, 1).AddDays(-1);
        for (var run = 0; run < 2; run++)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ILatePenaltyScheduler>().AccrueThroughPreviousMonthAsync(CancellationToken.None);
        }
        await factory.WithDb(async db => {
            var accrual = await db.LatePenaltyAccruals.SingleAsync(value => value.ChargeId == x.Charge.Id);
            Assert.Equal((x.Charge.DueDate.AddDays(1), monthEnd), (accrual.FromDate, accrual.ToDate));
            Assert.Equal(LatePenaltyCalculator.Round(3000m * 5m * (monthEnd.DayNumber - x.Charge.DueDate.DayNumber) / 3000m), accrual.Amount);
        });
    }

    [PostgresFact] public async Task MobileFinanceShowsServerPenaltySeparatelyWithCalculationLines()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "MANAGER");
        await client.PostAsJsonAsync(Root(x.Property.Id) + "/accruals/run", new LatePenaltyRunRequest(Today.AddDays(-10)));
        var list = (await client.GetFromJsonAsync<FinanceSnapshot>($"/api/v1/mobile/finance?propertyId={x.Property.Id}&role=MANAGER&view=charges&page=1"))!;
        var row = Assert.Single(list.Charges).Charge;
        Assert.Equal(("3000.00", "200.00", "150.00"), (row.Balance, row.Penalty, list.Summary.Penalty));
        Assert.Null(list.PenaltyLines);
        var detail = (await client.GetFromJsonAsync<FinanceSnapshot>($"/api/v1/mobile/finance?propertyId={x.Property.Id}&role=MANAGER&view=charges&page=1&chargeId={x.Charge.Id}"))!;
        Assert.Equal(["ACCRUED", "PENDING"], detail.PenaltyLines!.Select(value => value.Status));
        Assert.Equal(("150.00", "50.00"), (detail.PenaltyLines![0].Amount, detail.PenaltyLines[1].Amount));
    }

    [PostgresFact] public async Task ConcurrentPenaltyPaymentsCannotCloseTheSameBalanceTwice()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        await client.PostAsJsonAsync(Root(x.Property.Id) + "/accruals/run", new LatePenaltyRunRequest(Today));
        var version = await ChargeVersion(client, x);
        var results = await Task.WhenAll(Pay(client, x, Today, version, null, "200.00"), Pay(client, x, Today, version, null, "200.00"));
        Assert.Single(results, value => value.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, value => value.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("0.00", (await Detail(client, x))!.PenaltyBalance);
    }

    [PostgresFact] public async Task PolicyLifecycleAcceptsOnlyStatutoryRateAndEnforcesRolesOverlapAndCrystallizedHistory()
    {
        var x = await Seed(); using var admin = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        using var manager = TestToken.Client(factory, x.Actor, "MANAGER"); using var accountant = TestToken.Client(factory, x.Actor, "ACCOUNTANT");
        LatePenaltyPolicyRequest Request(DateOnly from, string rate = "5", string basis = "KMK_20") =>
            new(Guid.NewGuid(), from, null, rate, "DAILY_30", basis, "634 sayılı KMK m.20", null);
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.PostAsJsonAsync(Root(x.Property.Id) + "/policies", Request(Today.AddDays(1)))).StatusCode);
        var invalid = await manager.PostAsJsonAsync(Root(x.Property.Id) + "/policies", Request(Today.AddDays(1), "6"));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        foreach (var other in new[] { Request(Today.AddDays(1), "4", "MANAGEMENT_PLAN"), Request(Today.AddDays(1), "4"), Request(Today.AddDays(1), "5", "MANAGEMENT_PLAN"), Request(Today.AddDays(1)) with { Method = "DAILY_ACTUAL_MONTH" } })
        {
            var rejected = await manager.PostAsJsonAsync(Root(x.Property.Id) + "/policies", other);
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        }
        var statutory = (await (await manager.PostAsJsonAsync(Root(x.Property.Id) + "/policies", Request(Today.AddDays(1)))).Content.ReadFromJsonAsync<LatePenaltyPolicyResponse>())!;
        Assert.Equal(("DRAFT", "5.00"), (statutory.Status, statutory.MonthlyRate));
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync(Root(x.Property.Id) + $"/policies/{statutory.Id}/approve", new LatePenaltyStatusRequest(statutory.Version, null))).StatusCode);
        var approved = (await (await admin.PutAsJsonAsync(Root(x.Property.Id) + $"/policies/{statutory.Id}/approve", new LatePenaltyStatusRequest(statutory.Version, null))).Content.ReadFromJsonAsync<LatePenaltyPolicyResponse>())!;
        Assert.Equal("APPROVED", approved.Status); Assert.Equal(x.Actor, approved.ApprovedBy);
        var overlap = (await (await manager.PostAsJsonAsync(Root(x.Property.Id) + "/policies", Request(Today.AddDays(30)))).Content.ReadFromJsonAsync<LatePenaltyPolicyResponse>())!;
        var overlapping = await admin.PutAsJsonAsync(Root(x.Property.Id) + $"/policies/{overlap.Id}/approve", new LatePenaltyStatusRequest(overlap.Version, null));
        Assert.Equal("BF_LATE_PENALTY_OVERLAP", await Code(overlapping));
        await admin.PostAsJsonAsync(Root(x.Property.Id) + "/accruals/run", new LatePenaltyRunRequest(Today));
        var past = (await (await manager.PostAsJsonAsync(Root(x.Property.Id) + "/policies", Request(Today.AddDays(-5)) with { ValidTo = Today })).Content.ReadFromJsonAsync<LatePenaltyPolicyResponse>())!;
        var crystallized = await admin.PutAsJsonAsync(Root(x.Property.Id) + $"/policies/{past.Id}/approve", new LatePenaltyStatusRequest(past.Version, null));
        Assert.Equal("BF_LATE_PENALTY_CRYSTALLIZED", await Code(crystallized));
        var ended = await admin.PutAsJsonAsync(Root(x.Property.Id) + $"/policies/{approved.Id}/end", new LatePenaltyStatusRequest(approved.Version, "Yeni karar", Today.AddDays(10)));
        Assert.Equal(HttpStatusCode.OK, ended.StatusCode);
        var settings = (await admin.GetFromJsonAsync<LatePenaltySettings>(Root(x.Property.Id) + "/settings"))!;
        Assert.Equal(Today.AddDays(10), settings.Policies.Single(value => value.Id == approved.Id).ValidTo);
        Assert.Equal(Today, settings.CrystallizedThrough);
    }

    [PostgresFact] public async Task WaiverAndChargeCancellationKeepHistoryAndBlockPaidPenalty()
    {
        var x = await Seed(); using var admin = TestToken.Client(factory, x.Actor, "SITE_ADMIN"); using var manager = TestToken.Client(factory, x.Actor, "MANAGER");
        await admin.PostAsJsonAsync(Root(x.Property.Id) + "/accruals/run", new LatePenaltyRunRequest(Today.AddDays(-10)));
        var accrual = Assert.Single((await Detail(admin, x))!.Accruals);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync(Root(x.Property.Id) + $"/accruals/{accrual.Id}/waive", new LatePenaltyWaiveRequest(accrual.Version, "Muafiyet"))).StatusCode);
        var waived = (await (await admin.PutAsJsonAsync(Root(x.Property.Id) + $"/accruals/{accrual.Id}/waive", new LatePenaltyWaiveRequest(accrual.Version, "Yönetim kurulu muafiyeti"))).Content.ReadFromJsonAsync<LatePenaltyAccrualResponse>())!;
        Assert.Equal(("WAIVED", "0.00"), (waived.Status, waived.Balance));
        var again = await admin.PutAsJsonAsync(Root(x.Property.Id) + $"/accruals/{accrual.Id}/waive", new LatePenaltyWaiveRequest(waived.Version, "Tekrar"));
        Assert.Equal("BF_LATE_PENALTY_WAIVE", await Code(again));
        Assert.Equal("150.00", (await Detail(admin, x))!.PenaltyWaived);

        Assert.Equal(HttpStatusCode.Created, (await Pay(admin, x, Today, await ChargeVersion(admin, x), null, "10.00")).StatusCode);
        var blocked = await admin.PutAsJsonAsync($"/api/v1/properties/{x.Property.Id}/dues/{x.Charge.Id}/cancel", new CancelChargeRequest("Hatalı tahakkuk", await ChargeVersion(admin, x)));
        Assert.Equal("BF_LATE_PENALTY_CHARGE_PAID", await Code(blocked));
    }

    [PostgresFact] public async Task NonMembersAndUnprivilegedRolesCannotReadOrChangePenalties()
    {
        var x = await Seed(); using var stranger = TestToken.Client(factory, Guid.NewGuid().ToString(), "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(Root(x.Property.Id) + "/settings")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(Root(x.Property.Id) + "/overdue")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(Root(x.Property.Id) + "/charges/" + x.Charge.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsJsonAsync(Root(x.Property.Id) + "/accruals/run", new LatePenaltyRunRequest(Today))).StatusCode);
        foreach (var role in new[] { "RESIDENT", "spoofed", "" })
        {
            using var client = TestToken.Client(factory, x.Actor, role);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Root(x.Property.Id) + "/settings")).StatusCode);
        }
        using var member = TestToken.Client(factory, x.Actor, "ACCOUNTANT");
        var page = (await member.GetFromJsonAsync<LatePenaltyPage>(Root(x.Property.Id) + $"/overdue?year={x.Charge.Period.Year}"))!;
        var row = Assert.Single(page.Items);
        Assert.Equal(("3000.00", "200.00"), (row.PrincipalBalance, row.PendingPenalty));
    }
}
