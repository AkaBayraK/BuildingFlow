using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class ExpenseApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private static string Month => Today.ToString("yyyy-MM");
    private static string Root(Guid property) => $"/api/v1/properties/{property}";
    private async Task<(Property Property, string Actor)> Seed()
    {
        var actor = Guid.NewGuid().ToString(); var property = new Property("Expenses " + actor, "City", "District", "Address", PropertyType.APARTMENT);
        await factory.WithDb(async db => { db.Add(property); db.Add(new PropertyMembership(property.Id, actor)); await db.SaveChangesAsync(); }); return (property, actor);
    }
    private static CreateExpenseRequest ExpenseRequest(string amount = "12.35", string category = "ENERGY") => new(Guid.NewGuid(), category, amount, "TRY", Today, " Electricity_% ", " INV-1 ");
    private static SaveBudgetRequest BudgetRequest(string amount = "20.00", long version = 0, string category = "ENERGY") => new(Guid.NewGuid(), Month, category, amount, "TRY", " Monthly plan ", version);
    private static async Task<ExpenseResponse> Create(HttpClient client, Guid property, CreateExpenseRequest request)
    {
        var response = await client.PostAsJsonAsync(Root(property) + "/expenses", request); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ExpenseResponse>())!;
    }
    private static async Task<BudgetResponse> Budget(HttpClient client, Guid property, SaveBudgetRequest request)
    {
        var response = await client.PostAsJsonAsync(Root(property) + "/budgets", request); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BudgetResponse>())!;
    }
    [PostgresTheory] [InlineData("SUPER_ADMIN")] [InlineData("SITE_ADMIN")] [InlineData("MANAGER")] [InlineData("ACCOUNTANT")]
    public async Task FinanceRolesCreateExpensesAndBudgetsWithTrustedAudit(string role)
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, role); var expense = await Create(client, x.Property.Id, ExpenseRequest());
        var budget = await Budget(client, x.Property.Id, BudgetRequest()); Assert.Equal("12.35", expense.Amount); Assert.Equal("INV-1", expense.DocumentReference); Assert.Equal("20.00", budget.Amount); Assert.Single(budget.Revisions);
        await factory.WithDb(async db => {
            var value = await db.Expenses.SingleAsync(value => value.Id == expense.Id); Assert.Equal(x.Actor, value.CreatedBy); Assert.Equal(1, value.Version);
            var plan = await db.PropertyBudgets.SingleAsync(value => value.Id == budget.Id); Assert.Equal(x.Actor, plan.CreatedBy);
            var revision = await db.BudgetRevisions.SingleAsync(value => value.BudgetId == budget.Id); Assert.Equal(x.Actor, revision.CreatedBy); Assert.Equal(1, revision.Version);
            Assert.Equal(x.Property.Version + 2, (await db.Properties.SingleAsync(value => value.Id == x.Property.Id)).Version);
        });
        if (role == "ACCOUNTANT") Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Root(x.Property.Id) + "/people")).StatusCode);
    }
    [PostgresTheory] [InlineData("0")] [InlineData("-1")] [InlineData("1.001")] [InlineData("1e2")] [InlineData("1,20")] [InlineData("100000000")]
    public async Task InvalidExpenseAmountsAreCodedAndAtomic(string amount)
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var response = await client.PostAsJsonAsync(Root(x.Property.Id) + "/expenses", ExpenseRequest(amount));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Contains("BF_DUES_ERROR_AMOUNT", await response.Content.ReadAsStringAsync());
        await factory.WithDb(async db => Assert.False(await db.Expenses.AnyAsync(value => value.PropertyId == x.Property.Id)));
    }
    [PostgresFact] public async Task ValidationRejectsCategoryCurrencyDateReferencesAndBudgetValues()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var request = ExpenseRequest();
        foreach (var invalid in new[] { request with { Category = "0" }, request with { Category = "unknown" }, request with { Currency = "USD" }, request with { OccurredDate = Today.AddDays(1) }, request with { OccurredDate = new(1999, 1, 1) }, request with { DocumentReference = new string('x', 101) }, request with { Description = " " }, request with { RequestId = Guid.Empty } })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Root(x.Property.Id) + "/expenses", invalid)).StatusCode);
        foreach (var invalid in new[] { BudgetRequest() with { Amount = "-1" }, BudgetRequest() with { Amount = "1.001" }, BudgetRequest() with { Period = "2026-13" }, BudgetRequest() with { Period = "9999-01" }, BudgetRequest() with { Version = -1 }, BudgetRequest() with { Explanation = " " } })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Root(x.Property.Id) + "/budgets", invalid)).StatusCode);
    }
    [PostgresFact] public async Task AuthenticationRolesAndPropertyScopeProtectEveryEndpoint()
    {
        var x = await Seed(); using var admin = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var expense = await Create(admin, x.Property.Id, ExpenseRequest()); var budget = await Budget(admin, x.Property.Id, BudgetRequest());
        var reads = new[] { "/expenses", "/expenses/" + expense.Id, "/budgets/" + budget.Id, "/finance-summary?period=" + Month };
        using var anonymous = factory.CreateClient(); using var resident = TestToken.Client(factory, x.Actor, "RESIDENT"); using var outsider = TestToken.Client(factory, Guid.NewGuid().ToString(), "MANAGER");
        using var spoofed = TestToken.Client(factory, x.Actor, "spoofed"); using var clientRole = TestToken.Client(factory, x.Actor, "client-role"); using var malformed = factory.CreateClient();
        malformed.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt");
        foreach (var suffix in reads) {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Root(x.Property.Id) + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await resident.GetAsync(Root(x.Property.Id) + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(Root(x.Property.Id) + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await spoofed.GetAsync(Root(x.Property.Id) + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await clientRole.GetAsync(Root(x.Property.Id) + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await malformed.GetAsync(Root(x.Property.Id) + suffix)).StatusCode);
        }
        foreach (var client in new[] { anonymous, resident, outsider, spoofed, clientRole, malformed }) {
            var expected = client == anonymous || client == malformed ? HttpStatusCode.Unauthorized : client == outsider ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden;
            Assert.Equal(expected, (await client.PostAsJsonAsync(Root(x.Property.Id) + "/expenses", ExpenseRequest())).StatusCode);
            Assert.Equal(expected, (await client.PostAsJsonAsync(Root(x.Property.Id) + "/budgets", BudgetRequest())).StatusCode);
            Assert.Equal(expected, (await client.PutAsJsonAsync(Root(x.Property.Id) + "/expenses/" + expense.Id + "/cancel", new CancelChargeRequest("Wrong entry", expense.Version))).StatusCode);
        }
        var other = await Seed(); Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(Root(other.Property.Id) + "/expenses/" + expense.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(Root(other.Property.Id) + "/budgets/" + budget.Id)).StatusCode);
    }
    [PostgresFact] public async Task ConcurrentExpenseReplayIsSingleAndChangedPayloadConflicts()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var request = ExpenseRequest(); var root = Root(x.Property.Id) + "/expenses";
        var results = await Task.WhenAll(client.PostAsJsonAsync(root, request), client.PostAsJsonAsync(root, request));
        Assert.Single(results, value => value.StatusCode == HttpStatusCode.Created); Assert.Single(results, value => value.StatusCode == HttpStatusCode.OK);
        var a = (await results[0].Content.ReadFromJsonAsync<ExpenseResponse>())!; var b = (await results[1].Content.ReadFromJsonAsync<ExpenseResponse>())!; Assert.Equal(a.Id, b.Id);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(root, request with { Amount = "012.350".TrimEnd('0'), Description = "Electricity_%", DocumentReference = "INV-1" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(root, request with { Amount = "13" })).StatusCode);
        await factory.WithDb(async db => Assert.Single(await db.Expenses.Where(value => value.PropertyId == x.Property.Id).ToListAsync()));
    }
    [PostgresFact] public async Task ConcurrentBudgetCreatesRevisionsAndReplaysDoNotRewindCurrentPlan()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var first = BudgetRequest(); var root = Root(x.Property.Id) + "/budgets";
        var responses = await Task.WhenAll(client.PostAsJsonAsync(root, first), client.PostAsJsonAsync(root, first)); Assert.All(responses, value => Assert.Equal(HttpStatusCode.OK, value.StatusCode));
        var current = (await responses[0].Content.ReadFromJsonAsync<BudgetResponse>())!; Assert.Single(current.Revisions);
        var second = BudgetRequest("0.00", current.Version); current = await Budget(client, x.Property.Id, second); Assert.Equal(2, current.Version); Assert.Equal("0.00", current.Amount); Assert.Equal(2, current.Revisions.Count);
        var replay = await Budget(client, x.Property.Id, first); Assert.True(replay.Replayed); Assert.Equal(1, replay.AppliedRevision); Assert.Equal("0.00", replay.Amount);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(root, first with { Amount = "5" })).StatusCode);
        var race = await Task.WhenAll(client.PostAsJsonAsync(root, BudgetRequest("5.10", current.Version)), client.PostAsJsonAsync(root, BudgetRequest("7.35", current.Version)));
        Assert.Single(race, value => value.StatusCode == HttpStatusCode.OK); Assert.Single(race, value => value.StatusCode == HttpStatusCode.Conflict);
        var saved = (await client.GetFromJsonAsync<BudgetResponse>(root + "/" + current.Id))!; Assert.Equal(3, saved.Version); Assert.Equal(3, saved.Revisions.Count); Assert.Equal(new[] { 3, 2, 1 }, saved.Revisions.Select(value => value.Sequence));
    }
    [PostgresFact] public async Task CompetingFirstBudgetWritesCannotCreateDuplicatePeriodCategory()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var root = Root(x.Property.Id) + "/budgets";
        var race = await Task.WhenAll(client.PostAsJsonAsync(root, BudgetRequest("3")), client.PostAsJsonAsync(root, BudgetRequest("4")));
        Assert.Single(race, value => value.StatusCode == HttpStatusCode.OK); Assert.Single(race, value => value.StatusCode == HttpStatusCode.Conflict);
        await factory.WithDb(async db => { Assert.Single(await db.PropertyBudgets.Where(value => value.PropertyId == x.Property.Id).ToListAsync()); Assert.Single(await db.BudgetRevisions.Where(value => value.PropertyId == x.Property.Id).ToListAsync()); });
    }
    [PostgresFact] public async Task CancellationRacePreservesAmountAuditAndReplayShowsCancelledHistory()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var request = ExpenseRequest(); var value = await Create(client, x.Property.Id, request); var root = Root(x.Property.Id) + "/expenses/" + value.Id + "/cancel";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(root, new CancelChargeRequest(" ", value.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(root, new CancelChargeRequest("Correction", 999))).StatusCode);
        var race = await Task.WhenAll(client.PutAsJsonAsync(root, new CancelChargeRequest("Correction", value.Version)), client.PutAsJsonAsync(root, new CancelChargeRequest("Correction", value.Version)));
        Assert.Single(race, response => response.StatusCode == HttpStatusCode.OK); Assert.Single(race, response => response.StatusCode == HttpStatusCode.Conflict);
        var saved = (await client.GetFromJsonAsync<ExpenseResponse>(Root(x.Property.Id) + "/expenses/" + value.Id))!; Assert.Equal("CANCELLED", saved.Status); Assert.Equal(value.Amount, saved.Amount); Assert.Equal(value.CreatedAt, saved.CreatedAt); Assert.Equal(2, saved.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync(root, new CancelChargeRequest("Rewrite", saved.Version))).StatusCode);
        var replay = await client.PostAsJsonAsync(Root(x.Property.Id) + "/expenses", request); Assert.Equal(HttpStatusCode.OK, replay.StatusCode); Assert.Equal("CANCELLED", (await replay.Content.ReadFromJsonAsync<ExpenseResponse>())!.Status);
    }
    [PostgresFact] public async Task MonthlySummaryUsesActualUncancelledReceiptsExpensesAndBudgetCategoryTotals()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN");
        await Create(client, x.Property.Id, ExpenseRequest("12.35")); var cancelled = await Create(client, x.Property.Id, ExpenseRequest("1.10", "CLEANING"));
        await client.PutAsJsonAsync(Root(x.Property.Id) + "/expenses/" + cancelled.Id + "/cancel", new CancelChargeRequest("Correction", cancelled.Version));
        await Create(client, x.Property.Id, ExpenseRequest("2.10", "OTHER")); await Create(client, x.Property.Id, ExpenseRequest("50") with { OccurredDate = new DateOnly(Today.Year, Today.Month, 1).AddDays(-1) });
        await Budget(client, x.Property.Id, BudgetRequest()); await Budget(client, x.Property.Id, BudgetRequest("0", category: "OTHER"));
        await factory.WithDb(async db => {
            var property = await db.Properties.SingleAsync(value => value.Id == x.Property.Id); var unit = property.AddUnit("1", 1, 100);
            var batch = new DuesAssessment(property.Id, Guid.NewGuid(), new string('a', 64), new(Today.Year, Today.Month, 1), 100m, "TRY", Today, "Accrual is not income"); var charge = batch.AddCharge(unit.Id);
            var payment = new Payment(property.Id, unit.Id, Guid.NewGuid(), new string('a', 64), 3.10m, "TRY", Today, PaymentMethod.CASH, null, "Actual receipt"); payment.Allocate(charge, 3.10m); payment.ValidateComplete();
            var reversed = new Payment(property.Id, unit.Id, Guid.NewGuid(), new string('b', 64), 1.10m, "TRY", Today, PaymentMethod.CASH, null, "Reversed receipt"); reversed.Allocate(charge, 1.10m); reversed.ValidateComplete(); reversed.Cancel("Correction", DateTimeOffset.UtcNow); charge.ReversePayment(1.10m);
            db.Add(batch); db.AddRange(payment, reversed); await db.SaveChangesAsync();
        });
        var summary = (await client.GetFromJsonAsync<FinanceSummary>(Root(x.Property.Id) + "/finance-summary?period=" + Month))!;
        Assert.Equal("3.10", summary.IncomeAmount); Assert.Equal("14.45", summary.ExpenseAmount); Assert.Equal("20.00", summary.BudgetAmount); Assert.Equal("-11.35", summary.NetAmount); Assert.Equal("5.55", summary.BudgetVariance);
        Assert.Equal(8, summary.Categories.Count); var energy = summary.Categories.Single(value => value.Category == "ENERGY"); Assert.Equal("7.65", energy.Variance); Assert.True(energy.HasBudget);
        Assert.False(summary.Categories.Single(value => value.Category == "STAFF").HasBudget); Assert.True(summary.Categories.Single(value => value.Category == "OTHER").HasBudget);
        var page = (await client.GetFromJsonAsync<ExpensePage>(Root(x.Property.Id) + "/expenses?period=" + Month + "&search=%5F%25&pageSize=1"))!;
        Assert.Equal(3, page.TotalCount); Assert.Single(page.Items); Assert.Equal("14.45", page.Summary.RecordedAmount); Assert.Equal("1.10", page.Summary.CancelledAmount);
        var filtered = (await client.GetFromJsonAsync<ExpensePage>(Root(x.Property.Id) + "/expenses?period=" + Month + "&category=OTHER"))!; Assert.Equal("2.10", filtered.Summary.RecordedAmount);
    }
    [PostgresTheory] [InlineData("page=0")] [InlineData("pageSize=101")] [InlineData("category=0")] [InlineData("period=2026-13")] [InlineData("status=PAID")] [InlineData("sortBy=secret")] [InlineData("sortDirection=sideways")]
    public async Task InvalidFiltersAreRejected(string query)
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Root(x.Property.Id) + "/expenses?" + query)).StatusCode);
    }
    [PostgresFact] public async Task InactivePropertiesAndDeletedMembershipsNeverPermitNewWritesOrLeakHistory()
    {
        var x = await Seed(); using var manager = TestToken.Client(factory, x.Actor, "MANAGER"); var request = ExpenseRequest(); var expense = await Create(manager, x.Property.Id, request); var budget = await Budget(manager, x.Property.Id, BudgetRequest());
        await factory.WithDb(async db => { (await db.Properties.SingleAsync(value => value.Id == x.Property.Id)).SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(Root(x.Property.Id) + "/expenses")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PostAsJsonAsync(Root(x.Property.Id) + "/expenses", ExpenseRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PostAsJsonAsync(Root(x.Property.Id) + "/budgets", BudgetRequest(version: budget.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PutAsJsonAsync(Root(x.Property.Id) + "/expenses/" + expense.Id + "/cancel", new CancelChargeRequest("Correction", expense.Version))).StatusCode);
        await factory.WithDb(async db => { db.Remove(await db.PropertyMemberships.SingleAsync(value => value.PropertyId == x.Property.Id && value.UserId == x.Actor)); await db.SaveChangesAsync(); });
        foreach (var suffix in new[] { "/expenses", "/expenses/" + expense.Id, "/budgets/" + budget.Id, "/finance-summary?period=" + Month }) Assert.Equal(HttpStatusCode.NotFound, (await manager.GetAsync(Root(x.Property.Id) + suffix)).StatusCode);
        await factory.WithDb(async db => { db.Remove(await db.Properties.SingleAsync(value => value.Id == x.Property.Id)); await db.SaveChangesAsync(); });
        using var admin = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(Root(x.Property.Id) + "/finance-summary?period=" + Month)).StatusCode);
    }
    [PostgresFact] public async Task FinancialHistoryBlocksPropertyDeletionEvenAfterExpenseCancellation()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); var expense = await Create(client, x.Property.Id, ExpenseRequest());
        await client.PutAsJsonAsync(Root(x.Property.Id) + "/expenses/" + expense.Id + "/cancel", new CancelChargeRequest("Correction", expense.Version));
        var property = (await client.GetFromJsonAsync<PropertyResponse>(Root(x.Property.Id)))!; Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync(Root(x.Property.Id) + "?version=" + property.Version)).StatusCode);
        var second = await Seed(); await Budget(client, second.Property.Id, BudgetRequest("0")); property = (await client.GetFromJsonAsync<PropertyResponse>(Root(second.Property.Id)))!;
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync(Root(second.Property.Id) + "?version=" + property.Version)).StatusCode);
    }
}
