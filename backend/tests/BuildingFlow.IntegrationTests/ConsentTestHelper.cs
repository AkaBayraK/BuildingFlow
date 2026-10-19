using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using Xunit;
namespace BuildingFlow.IntegrationTests;

// İŞ17: photo uploads need an ACTIVE consent, so photo tests publish a text in their own fixture schema and approve it first.
internal static class ConsentTestHelper
{
    public static async Task<ConsentTextResponse> PublishAsync(SiteApiTests.SiteApiFactory factory, string code, string body = "Fotoğraf rızası metni.", bool requiresReconsent = true)
    {
        using var admin = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var type = (await admin.GetFromJsonAsync<ConsentTypeResponse[]>("/api/v1/consents/types"))!.Single(t => t.Code == code);
        var response = await admin.PostAsJsonAsync($"/api/v1/consents/types/{type.Id}/texts", new PublishConsentTextRequest(Guid.NewGuid(), "Rıza " + code, body, requiresReconsent));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ConsentTextResponse>())!;
    }

    public static async Task<MobileConsentItem> ItemAsync(HttpClient client, string code) =>
        (await client.GetFromJsonAsync<MobileConsentList>("/api/v1/mobile/consents"))!.Items.Single(x => x.Status.TypeCode == code);

    public static Task<HttpResponseMessage> CommandAsync(HttpClient client, MobileConsentCommand command) => client.PostAsJsonAsync("/api/v1/mobile/consents/commands", command);

    public static MobileConsentCommand Approve(string code, MobileConsentText text) => new(Guid.NewGuid(), MobileConsentActions.Approve, code, text.Id, text.BodySha256, "1.0.0");
    public static MobileConsentCommand Withdraw(string code) => new(Guid.NewGuid(), MobileConsentActions.Withdraw, code);

    // Publishes a text only when the type has none yet, then approves the current one as the client's user.
    public static async Task GrantAsync(SiteApiTests.SiteApiFactory factory, HttpClient client, string code)
    {
        var item = await ItemAsync(client, code);
        if (item.CurrentText is null) { await PublishAsync(factory, code); item = await ItemAsync(client, code); }
        var response = await CommandAsync(client, Approve(code, item.CurrentText!));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ConsentStates.Active, (await response.Content.ReadFromJsonAsync<MobileConsentResult>())!.Status.State);
    }
}
