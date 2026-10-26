using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class SmsConsentApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static SaveLegalSenderRequest Sender(long version = 0, bool optOut = true) =>
        new("Gül Sitesi Yönetimi", "0123456789012345", "0212 000 00 00", "RET GUL", "4567", optOut, false, null, true, version);

    [PostgresFact] public async Task ManagerReadsButOnlySiteAdminConfiguresLegalSenderWithVersion()
    {
        var x = await Seed();
        using var manager = TestToken.Client(factory, x.Actor, "MANAGER");
        Assert.Null((await manager.GetFromJsonAsync<LegalSenderView>(Root(x.Property) + "/legal-sender"))!.Sender);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync(Root(x.Property) + "/legal-sender", Sender())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync("/api/v1/sms/platform-sender", Sender())).StatusCode);
        using var admin = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Root(x.Property) + "/legal-sender", Sender() with { OptOutKeyword = "STOP" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync(Root(x.Property) + "/legal-sender", Sender() with { MersisNumber = "123" })).StatusCode);
        var saved = (await (await admin.PutAsJsonAsync(Root(x.Property) + "/legal-sender", Sender())).Content.ReadFromJsonAsync<LegalSenderResponse>())!;
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync(Root(x.Property) + "/legal-sender", Sender())).StatusCode);
        var updated = (await (await admin.PutAsJsonAsync(Root(x.Property) + "/legal-sender", Sender(saved.Version) with { Title = "Gül Yönetim" })).Content.ReadFromJsonAsync<LegalSenderResponse>())!;
        Assert.Equal("Gül Yönetim", updated.Title); Assert.True(updated.Version > saved.Version);
        using var stranger = TestToken.Client(factory, Guid.NewGuid().ToString(), "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync(Root(x.Property) + "/legal-sender")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PutAsJsonAsync(Root(x.Property) + "/legal-sender", Sender(updated.Version))).StatusCode);
    }

    [PostgresFact] public async Task PreviewShowsFooterLengthAndBlockReason()
    {
        var x = await Seed();
        using var admin = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        var plain = await Preview(admin, x.Property, new("MANUAL", "Su kesintisi"));
        Assert.True(plain.Allowed); Assert.Equal("", plain.Footer); Assert.Equal("GSM7", plain.Encoding); Assert.Equal(1, plain.Segments);
        var noSender = await Preview(admin, x.Property, new("MARKETING", "Kampanya"));
        Assert.False(noSender.Allowed); Assert.Equal(SmsPolicyErrors.LegalSenderMissing, noSender.BlockedCode);
        await admin.PutAsJsonAsync(Root(x.Property) + "/legal-sender", Sender());
        var marketing = await Preview(admin, x.Property, new("MARKETING", "Kampanya"));
        Assert.True(marketing.Allowed); Assert.Contains("RET GUL", marketing.Body); Assert.Equal("UCS2", marketing.Encoding); Assert.Equal(2, marketing.Segments);
        var recipient = await Preview(admin, x.Property, new("MARKETING", "Kampanya", "0555 123 45 67"));
        Assert.Equal(SmsPolicyErrors.CommercialConsentRequired, recipient.BlockedCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Root(x.Property) + "/preview", new SmsPreviewRequest("UNKNOWN", "x"))).StatusCode);
    }

    [PostgresFact] public async Task AccountApprovesWithdrawsAndAdminSeesMaskedHistoryAndOptOut()
    {
        var x = await Seed();
        using var admin = TestToken.Client(factory, x.Actor, "SITE_ADMIN");
        var sender = (await (await admin.PutAsJsonAsync(Root(x.Property) + "/legal-sender", Sender())).Content.ReadFromJsonAsync<LegalSenderResponse>())!;
        var resident = Guid.NewGuid().ToString();
        await factory.WithDb(async db => { db.Add(new PropertyMembership(x.Property, resident)); await db.SaveChangesAsync(); });
        using var me = TestToken.Client(factory, resident, "RESIDENT");
        var text = CommercialConsentTexts.Current;
        var unverified = await me.GetFromJsonAsync<CommunicationPreferences>(Prefs);
        Assert.Null(unverified!.VerifiedPhoneMasked); Assert.Equal("NONE", unverified.Items.Single(i => i.SenderId == sender.Id).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await me.PutAsJsonAsync($"{Prefs}/{sender.Id}", new CommunicationPreferenceCommand(Guid.NewGuid(), true, text.Version, text.BodySha256))).StatusCode);
        await factory.WithDb(async db =>
        {
            var proof = new AccountPhoneVerification(resident); proof.Stage("0555 123 45 67"); proof.Confirm(proof.PendingKey!, DateTimeOffset.UtcNow); db.Add(proof); await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Conflict, (await me.PutAsJsonAsync($"{Prefs}/{sender.Id}", new CommunicationPreferenceCommand(Guid.NewGuid(), true, text.Version, new string('0', 64)))).StatusCode);
        var approved = await me.PutAsJsonAsync($"{Prefs}/{sender.Id}", new CommunicationPreferenceCommand(Guid.NewGuid(), true, text.Version, text.BodySha256, "MOBILE"));
        Assert.Equal("APPROVED", (await approved.Content.ReadFromJsonAsync<CommunicationPreferenceItem>())!.Status);
        Assert.Equal("+90********67", (await me.GetFromJsonAsync<CommunicationPreferences>(Prefs))!.VerifiedPhoneMasked);
        Assert.True((await Preview(admin, x.Property, new("MARKETING", "Kampanya", "+905551234567"))).Allowed);
        await me.PutAsJsonAsync($"{Prefs}/{sender.Id}", new CommunicationPreferenceCommand(Guid.NewGuid(), false));
        var history = await me.GetFromJsonAsync<PagedRows<CommercialConsentState>>($"{Prefs}/{sender.Id}/history");
        Assert.Equal(["REJECTED", "APPROVED"], history!.Items.Select(i => i.Status)); Assert.Equal(["WEB", "MOBILE"], history.Items.Select(i => i.Source));
        Assert.Equal(SmsPolicyErrors.OptedOut, (await Preview(admin, x.Property, new("MARKETING", "Kampanya", "+905551234567"))).BlockedCode);
        var report = await admin.GetStringAsync(Root(x.Property) + "/commercial-consents");
        Assert.DoesNotContain("5551234567", report); Assert.Contains("+90********67", report);
        var optOut = await admin.PostAsJsonAsync(Root(x.Property) + "/opt-outs", new AdminOptOutRequest(Guid.NewGuid(), "0555 999 88 77"));
        Assert.Equal("ADMIN", (await optOut.Content.ReadFromJsonAsync<CommercialConsentState>())!.Source);
        using var other = TestToken.Client(factory, Guid.NewGuid().ToString(), "RESIDENT");
        Assert.DoesNotContain((await other.GetFromJsonAsync<CommunicationPreferences>(Prefs))!.Items, i => i.SenderId == sender.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"{Prefs}/{sender.Id}", new CommunicationPreferenceCommand(Guid.NewGuid(), false))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"{Prefs}/{sender.Id}/history")).StatusCode);
    }

    [PostgresFact] public async Task AnonymousAndResidentCannotReachAdminSurfaces()
    {
        var x = await Seed();
        using var resident = TestToken.Client(factory, x.Actor, "RESIDENT");
        Assert.Equal(HttpStatusCode.Forbidden, (await resident.GetAsync(Root(x.Property) + "/commercial-consents")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await resident.PostAsJsonAsync(Root(x.Property) + "/opt-outs", new AdminOptOutRequest(Guid.NewGuid(), "05551234567"))).StatusCode);
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Prefs)).StatusCode);
    }

    private const string Prefs = "/api/v1/account/communication-preferences";
    private static async Task<SmsPreviewResponse> Preview(HttpClient client, Guid property, SmsPreviewRequest request) =>
        (await (await client.PostAsJsonAsync(Root(property) + "/preview", request)).Content.ReadFromJsonAsync<SmsPreviewResponse>())!;
    private static string Root(Guid property) => $"/api/v1/properties/{property}/sms";
    private async Task<(string Actor, Guid Property)> Seed()
    {
        var actor = Guid.NewGuid().ToString(); var property = new Property("Consent API " + actor[..8], "City", "District", "Address", PropertyType.APARTMENT);
        await factory.WithDb(async db => { db.Add(property); db.Add(new PropertyMembership(property.Id, actor)); await db.SaveChangesAsync(); });
        return (actor, property.Id);
    }
}
