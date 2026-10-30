using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierQuoteTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    private sealed record Fixture(Guid Tenant, Guid Supplier, Guid SecondSupplier, Guid Property, Guid Category, Guid Area, Guid SecondArea, string User, string Reviewer);
    private async Task<Fixture> Seed()
    {
        var t = new PersonTenant("Quote fixture"); var p = new Property("Quote property", "City", "District", "Address"); var user = Guid.NewGuid().ToString(); var reviewer = Guid.NewGuid().ToString();
        var account = new PersonUserAccount("http://localhost:8080/realms/buildingflow", user); var reviewAccount = new PersonUserAccount("http://localhost:8080/realms/buildingflow", reviewer);
        var s = new Supplier(t.Id, "Quote supplier A", SupplierKind.COMPANY); s.ChangeStatus(SupplierStatus.ACTIVE, null); var second = new Supplier(t.Id, "Quote supplier B", SupplierKind.COMPANY); second.ChangeStatus(SupplierStatus.ACTIVE, null);
        var cat = new SupplierCategory(t.Id, "Elevators"); var service = new BuildingFlow.Domain.SupplierService(t.Id, s.Id, cat.Id, "Maintenance"); var other = new BuildingFlow.Domain.SupplierService(t.Id, second.Id, cat.Id, "Maintenance"); var area = new SupplierAreaService(t.Id, s.Id, p.Id, service.Id); var secondArea = new SupplierAreaService(t.Id, second.Id, p.Id, other.Id);
        await factory.WithDb(async db => { db.AddRange(t, p, account, reviewAccount, s, second, cat, service, other, area, secondArea, new PersonTenantMembership(t.Id, account.Id), new PersonTenantMembership(t.Id, reviewAccount.Id), new PersonPropertyScope(t.Id, p.Id), new PropertyMembership(p.Id, user), new PropertyMembership(p.Id, reviewer), new SupplierManagementArea(t.Id, s.Id, p.Id), new SupplierManagementArea(t.Id, second.Id, p.Id)); await db.SaveChangesAsync(); });
        return new(t.Id, s.Id, second.Id, p.Id, cat.Id, area.Id, secondArea.Id, user, reviewer);
    }
    private static async Task<T> Value<T>(HttpResponseMessage response)
    { Assert.Equal(HttpStatusCode.OK, response.StatusCode); return (await response.Content.ReadFromJsonAsync<T>())!; }
    private static SupplierInquiryRequest Create(Fixture x, Guid? op = null) => new(op ?? Guid.NewGuid(), 1, x.Property, x.Category, "Annual maintenance", "Includes parts and labor", Today.AddDays(3), [x.Area, x.SecondArea]);
    private static SupplierQuoteRequest Quote(long version, Guid invitation, string currency = "TRY", string amount = "100.00") => new(Guid.NewGuid(), version, invitation, amount, "20.00", currency, Today.AddDays(10), "All parts included");
    [PostgresFact] public async Task MultipleQuotesSeparateCurrenciesDecisionsAndConversionHaveImmutableHistoryAndCurrentReplay()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); using var reviewer = TestToken.Client(factory, x.Reviewer, "SITE_ADMIN"); var create = Create(x);
        var candidates = await Value<PagedRows<SupplierQuoteCandidate>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/quote-candidates?propertyId={x.Property}&categoryId={x.Category}")); Assert.Equal(2, candidates.TotalCount);
        var one = await Value<SupplierInquiryMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries", create)); Assert.Equal(2, one.Record.Quotes.Count); Assert.Equal(x.Tenant, one.Record.Inquiry.TenantId);
        var first = one.Record.Quotes.Single(q => q.SupplierId == x.Supplier); var second = one.Record.Quotes.Single(q => q.SupplierId == x.SecondSupplier);
        var command = Quote(one.Record.Inquiry.Version, first.InvitationId);
        var two = await Value<SupplierInquiryMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{one.Record.Inquiry.Id}/quotes", command));
        var three = await Value<SupplierInquiryMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{one.Record.Inquiry.Id}/quotes", Quote(two.Record.Inquiry.Version, second.InvitationId, "EUR", "80.00")));
        Assert.Equal(new[] { "EUR", "TRY" }, three.Record.Quotes.Select(q => q.Currency).Order()); Assert.Contains(three.Record.Quotes, q => q.GrossAmount == "120.00"); Assert.Contains(three.Record.Quotes, q => q.GrossAmount == "96.00");
        var quote = three.Record.Quotes.Single(q => q.SupplierId == x.Supplier); var approve = new SupplierInquiryAction(Guid.NewGuid(), three.Record.Inquiry.Version, "Local support and complete scope");
        var decided = await Value<SupplierInquiryMutation>(await reviewer.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{one.Record.Inquiry.Id}/quotes/{quote.QuoteId}/approve", approve)); Assert.Equal("AWARDED", decided.Record.Inquiry.Status); Assert.Contains(decided.Record.Quotes, q => q.Status == "NOT_SELECTED");
        var conversion = new SupplierInquiryAction(Guid.NewGuid(), decided.Record.Inquiry.Version);
        var converted = await Value<SupplierInquiryMutation>(await reviewer.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{one.Record.Inquiry.Id}/convert", conversion)); Assert.Equal("DRAFT", converted.Record.WorkOrder!.Status); Assert.Equal("120.00", converted.Record.WorkOrder.Amount); Assert.Equal("TRY", converted.Record.WorkOrder.Currency);
        var replay = await Value<SupplierInquiryMutation>(await reviewer.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{one.Record.Inquiry.Id}/convert", conversion)); Assert.True(replay.Replayed); Assert.Equal(converted.Record.WorkOrder.Id, replay.Record.WorkOrder!.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await reviewer.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{one.Record.Inquiry.Id}/convert", conversion with { OperationId = Guid.NewGuid(), Version = converted.Record.Inquiry.Version })).StatusCode);
        var old = await Value<SupplierInquiryMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries", create)); Assert.True(old.Replayed); Assert.Equal(converted.Record.Inquiry.Version, old.Record.Inquiry.Version); Assert.Equal(1, old.AppliedVersion);
        Assert.True((await Value<SupplierInquiryMutation>(await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{one.Record.Inquiry.Id}/quotes", command with { NetAmount = "100", VatRate = "20.0" }))).Replayed);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{one.Record.Inquiry.Id}/quotes", command with { NetAmount = "99.00" })).StatusCode);
        var history = await Value<PagedRows<SupplierContractHistory>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{one.Record.Inquiry.Id}/history")); Assert.Equal(5, history.TotalCount); Assert.Equal(new[] { "CONVERT", "APPROVE", "QUOTE", "QUOTE", "CREATE" }, history.Items.Select(h => h.Action));
        await factory.WithDb(async db => {
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_quotes SET net_amount=1 WHERE id={quote.QuoteId}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE supplier_inquiry_revisions SET snapshot='{{}}'::jsonb WHERE inquiry_id={one.Record.Inquiry.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM supplier_work_orders WHERE id={converted.Record.WorkOrder.Id}"));
        });
        Assert.Single((await Value<PagedRows<SupplierInquirySummary>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.SecondSupplier}/inquiries"))).Items);
    }
    [PostgresFact] public async Task RoleAndEntrantSeparationCannotBeBypassedEvenBySuperAdmin()
    {
        var x = await Seed(); using var manager = TestToken.Client(factory, x.User, "MANAGER"); using var ownAdmin = TestToken.Client(factory, x.User, "SUPER_ADMIN"); using var other = TestToken.Client(factory, x.Reviewer, "SITE_ADMIN");
        var inquiry = await Value<SupplierInquiryMutation>(await manager.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries", Create(x)));
        var quote = await Value<SupplierInquiryMutation>(await manager.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{inquiry.Record.Inquiry.Id}/quotes", Quote(inquiry.Record.Inquiry.Version, inquiry.Record.Quotes[0].InvitationId)));
        var row = quote.Record.Quotes.Single(q => q.QuoteId.HasValue); var action = new SupplierInquiryAction(Guid.NewGuid(), quote.Record.Inquiry.Version, "Best scope");
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{inquiry.Record.Inquiry.Id}/quotes/{row.QuoteId}/approve", action)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ownAdmin.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{inquiry.Record.Inquiry.Id}/quotes/{row.QuoteId}/approve", action)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{inquiry.Record.Inquiry.Id}/quotes", Quote(quote.Record.Inquiry.Version, row.InvitationId))).StatusCode);
        var rejected = await Value<SupplierInquiryMutation>(await other.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{inquiry.Record.Inquiry.Id}/quotes/{row.QuoteId}/reject", action)); Assert.Equal("REJECTED", rejected.Record.Quotes.Single(q => q.QuoteId.HasValue).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{inquiry.Record.Inquiry.Id}/quotes", Quote(rejected.Record.Inquiry.Version, row.InvitationId))).StatusCode);
        var cancelled = await Value<SupplierInquiryMutation>(await other.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{inquiry.Record.Inquiry.Id}/cancel", new SupplierInquiryAction(Guid.NewGuid(), rejected.Record.Inquiry.Version, "Scope changed"))); Assert.Equal("CANCELLED", cancelled.Record.Inquiry.Status);
    }
    [PostgresFact] public async Task ScopeValidationPaginationAndNegativeMoneyAreEnforced()
    {
        var x = await Seed(); var foreign = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); using var accountant = TestToken.Client(factory, x.User, "ACCOUNTANT"); using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await accountant.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries")).StatusCode); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v2/tenants/{foreign.Tenant}/suppliers/{foreign.Supplier}/inquiries")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries", Create(x) with { AreaServiceIds = [x.Area, foreign.Area] })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries", Create(x) with { AreaServiceIds = [x.Area, x.Area] })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries", Create(x) with { Deadline = Today.AddDays(-1) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries?pageSize=101")).StatusCode);
        var created = await Value<SupplierInquiryMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries", Create(x))); var url = $"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{created.Record.Inquiry.Id}"; var q = Quote(created.Record.Inquiry.Version, created.Record.Quotes[0].InvitationId);
        foreach (var bad in new[] { q with { NetAmount = "-1.00" }, q with { NetAmount = "1.001" }, q with { VatRate = "100.01" }, q with { Currency = "GBP" }, q with { ValidUntil = Today.AddDays(-1) } })
        { var r = await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{created.Record.Inquiry.Id}/quotes", bad); Assert.Contains(r.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict }); }
        await factory.WithDb(async db => { var membership = await db.PropertyMemberships.SingleAsync(m => m.PropertyId == x.Property && m.UserId == x.User); membership.SetEnabled(false); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(url)).StatusCode); Assert.Empty((await Value<PagedRows<SupplierInquirySummary>>(await c.GetAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries"))).Items);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{created.Record.Inquiry.Id}/quotes", q)).StatusCode);
    }
    [PostgresFact] public async Task ExpiredOffersCannotBeAwardedAndClosedDeadlineRejectsNewQuotes()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); using var reviewer = TestToken.Client(factory, x.Reviewer, "SITE_ADMIN");
        var inquiry = new SupplierInquiry(x.Tenant, x.Property, x.Category, "Expired fixture", "Expired scope", Today.AddDays(-1));
        var invitation = new SupplierInvitation(inquiry, x.Supplier, x.Area); var quote = new SupplierQuote(invitation, 100m, 20m, "TRY", Today.AddDays(-1), "Expired offer", "fixture", "entrant");
        await factory.WithDb(async db => { db.AddRange(inquiry, invitation, quote); await db.SaveChangesAsync(); });
       
        Assert.Equal(HttpStatusCode.Conflict, (await reviewer.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{inquiry.Id}/quotes/{quote.Id}/approve", new SupplierInquiryAction(Guid.NewGuid(), 1, "Too late"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{inquiry.Id}/quotes", Quote(1, invitation.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await reviewer.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{inquiry.Id}/quotes/{quote.Id}/reject", new SupplierInquiryAction(Guid.NewGuid(), 1, " "))).StatusCode);
        var rejected = await Value<SupplierInquiryMutation>(await reviewer.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{inquiry.Id}/quotes/{quote.Id}/reject", new SupplierInquiryAction(Guid.NewGuid(), 1, "Expired offer")));
        Assert.Equal("REJECTED", rejected.Record.Quotes.Single().Status);
    }
    [PostgresFact] public async Task ConcurrentQuoteWritesUseSingleInquiryVersionAndPreserveBothInvitationSlots()
    {
        var x = await Seed(); using var c = TestToken.Client(factory, x.User, "MANAGER"); var created = await Value<SupplierInquiryMutation>(await c.PostAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries", Create(x))); var url = $"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{created.Record.Inquiry.Id}";
        var responses = await Task.WhenAll(created.Record.Quotes.Select(q => c.PutAsJsonAsync($"/api/v2/tenants/{x.Tenant}/suppliers/{x.Supplier}/inquiries/{created.Record.Inquiry.Id}/quotes", Quote(created.Record.Inquiry.Version, q.InvitationId)))); Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK); Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        var current = await Value<SupplierInquiryDetail>(await c.GetAsync(url)); Assert.Single(current.Quotes, q => q.QuoteId.HasValue); Assert.Equal(2, current.Inquiry.Version);
    }
}
