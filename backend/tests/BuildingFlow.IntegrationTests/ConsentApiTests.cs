using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class ConsentApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Root = "/api/v1/consents";
    private static string Code() => "TEST_" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    private static string Hash(char c) => new(c, 64);

    [PostgresFact]
    public async Task SystemTypesAreSeededAndProtected()
    {
        using var admin = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var types = (await admin.GetFromJsonAsync<ConsentTypeResponse[]>(Root + "/types"))!;
        foreach (var (code, feature) in new[] { ("KVKK_INFORMATION", (string?)null), ("PROFILE_PHOTO", "PROFILE_PHOTO"), ("REQUEST_PHOTO", "REQUEST_PHOTO") })
        {
            var type = Assert.Single(types, t => t.Code == code);
            Assert.True(type.IsSystem); Assert.Equal(feature, type.Feature);
        }
        var photo = types.Single(t => t.Code == "PROFILE_PHOTO");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"{Root}/types/{photo.Id}?version={photo.Version}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"{Root}/types/{photo.Id}", new UpdateConsentTypeRequest(photo.NameTr, photo.NameEn, photo.SortOrder, false, photo.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Root + "/types", new CreateConsentTypeRequest("PROFILE_PHOTO", "Kopya", "Copy", "EXPLICIT_CONSENT"))).StatusCode);
    }

    [PostgresTheory] [InlineData("SITE_ADMIN")] [InlineData("MANAGER")]
    public async Task OnlySuperAdminWritesTypesAndTexts(string role)
    {
        using var reader = TestToken.Client(factory, Guid.NewGuid().ToString(), role);
        var type = (await reader.GetFromJsonAsync<ConsentTypeResponse[]>(Root + "/types"))!.Single(t => t.Code == "REQUEST_PHOTO");
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsJsonAsync(Root + "/types", new CreateConsentTypeRequest(Code(), "Ad", "Name", "INFORMATION"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsJsonAsync($"{Root}/types/{type.Id}/texts", new PublishConsentTextRequest(Guid.NewGuid(), "Başlık", "Metin"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync($"{Root}/types/{type.Id}/texts")).StatusCode);
    }

    [PostgresTheory] [InlineData("ACCOUNTANT")] [InlineData("RESIDENT")] [InlineData("spoofed")] [InlineData("client-role")] [InlineData("")]
    public async Task OtherRolesCannotReadConsentData(string role)
    {
        using var client = TestToken.Client(factory, Guid.NewGuid().ToString(), role);
        foreach (var path in new[] { "/types", "/records", "/users/someone" }) Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Root + path)).StatusCode);
        using var anonymous = TestToken.Client(factory, null, "");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Root + "/types")).StatusCode);
    }

    [PostgresFact]
    public async Task TypesCanBeCreatedRenamedDisabledAndDeletedOnlyWithoutTexts()
    {
        using var admin = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Root + "/types", new CreateConsentTypeRequest("lower", "Ad", "Name", "INFORMATION"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Root + "/types", new CreateConsentTypeRequest(Code(), "Ad", "Name", "1"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(Root + "/types", new CreateConsentTypeRequest(Code(), " ", "Name", "INFORMATION"))).StatusCode);
        var created = (await (await admin.PostAsJsonAsync(Root + "/types", new CreateConsentTypeRequest(Code(), " Kamera kaydı ", "Camera", "EXPLICIT_CONSENT", 50))).Content.ReadFromJsonAsync<ConsentTypeResponse>())!;
        Assert.Equal("Kamera kaydı", created.NameTr); Assert.False(created.IsSystem); Assert.Null(created.CurrentText);
        var updated = (await (await admin.PutAsJsonAsync($"{Root}/types/{created.Id}", new UpdateConsentTypeRequest("Kamera", "Camera", 60, false, created.Version))).Content.ReadFromJsonAsync<ConsentTypeResponse>())!;
        Assert.False(updated.IsEnabled); Assert.Equal(60, updated.SortOrder);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"{Root}/types/{created.Id}", new UpdateConsentTypeRequest("Kamera", "Camera", 60, true, created.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"{Root}/types/{created.Id}/texts", new PublishConsentTextRequest(Guid.NewGuid(), "Başlık", "Metin"))).StatusCode);
        updated = (await (await admin.PutAsJsonAsync($"{Root}/types/{created.Id}", new UpdateConsentTypeRequest("Kamera", "Camera", 60, true, updated.Version))).Content.ReadFromJsonAsync<ConsentTypeResponse>())!;
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"{Root}/types/{created.Id}/texts", new PublishConsentTextRequest(Guid.NewGuid(), "Başlık", "Metin"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"{Root}/types/{created.Id}?version={updated.Version}")).StatusCode);
        var spare = (await (await admin.PostAsJsonAsync(Root + "/types", new CreateConsentTypeRequest(Code(), "Boş", "Empty", "INFORMATION"))).Content.ReadFromJsonAsync<ConsentTypeResponse>())!;
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Root}/types/{spare.Id}?version={spare.Version}")).StatusCode);
        Assert.DoesNotContain((await admin.GetFromJsonAsync<ConsentTypeResponse[]>(Root + "/types"))!, t => t.Id == spare.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Root + "/types", new CreateConsentTypeRequest(spare.Code, "Tekrar", "Again", "INFORMATION"))).StatusCode);
    }

    [PostgresFact]
    public async Task PublishingCreatesImmutableVersionsAndReplaysByOperation()
    {
        using var admin = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var type = await CreateType(admin);
        var operation = Guid.NewGuid();
        var request = new PublishConsentTextRequest(operation, " Profil fotoğrafı ", "# Başlık\r\n\r\n**Kalın** ve [site](https://example.com).\r\n");
        var first = (await (await admin.PostAsJsonAsync($"{Root}/types/{type.Id}/texts", request)).Content.ReadFromJsonAsync<ConsentTextResponse>())!;
        Assert.Equal(1, first.TextVersion); Assert.Equal("Profil fotoğrafı", first.Title);
        Assert.Equal("# Başlık\n\n**Kalın** ve [site](https://example.com).", first.Body);
        Assert.Equal(ConsentEvidence.Sha256(first.Body), first.BodySha256);
        var replay = (await (await admin.PostAsJsonAsync($"{Root}/types/{type.Id}/texts", request)).Content.ReadFromJsonAsync<ConsentTextResponse>())!;
        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"{Root}/types/{type.Id}/texts", request with { Body = "Başka" })).StatusCode);
        var second = (await (await admin.PostAsJsonAsync($"{Root}/types/{type.Id}/texts", new PublishConsentTextRequest(Guid.NewGuid(), "Yeni", "Yeni metin", false))).Content.ReadFromJsonAsync<ConsentTextResponse>())!;
        Assert.Equal(2, second.TextVersion); Assert.False(second.RequiresReconsent);
        var history = (await admin.GetFromJsonAsync<PagedRows<ConsentTextResponse>>($"{Root}/types/{type.Id}/texts"))!;
        Assert.Equal([2, 1], history.Items.Select(x => x.TextVersion));
        Assert.Equal(first.Body, (await admin.GetFromJsonAsync<ConsentTextResponse>($"{Root}/texts/{first.Id}"))!.Body);
        var listed = (await admin.GetFromJsonAsync<ConsentTypeResponse[]>(Root + "/types"))!.Single(t => t.Id == type.Id);
        Assert.Equal(2, listed.TextCount); Assert.Equal(second.Id, listed.CurrentText!.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"{Root}/types/{Guid.NewGuid()}/texts", new PublishConsentTextRequest(Guid.NewGuid(), "A", "B"))).StatusCode);
    }

    [PostgresFact]
    public async Task ConcurrentPublishesGetDistinctVersions()
    {
        using var admin = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var type = await CreateType(admin);
        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => admin.PostAsJsonAsync($"{Root}/types/{type.Id}/texts", new PublishConsentTextRequest(Guid.NewGuid(), "Sürüm", "Metin " + i))));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var versions = (await admin.GetFromJsonAsync<PagedRows<ConsentTextResponse>>($"{Root}/types/{type.Id}/texts"))!.Items.Select(x => x.TextVersion).Order();
        Assert.Equal(Enumerable.Range(1, 6), versions);
    }

    [PostgresTheory]
    [InlineData("[tıkla](javascript:alert(1))", "BF_CONSENT_LINK_INVALID")]
    [InlineData("[site](http://example.com)", "BF_CONSENT_LINK_INVALID")]
    [InlineData("  \n ", "BF_CONSENT_TEXT_INVALID")]
    [InlineData("gizli \u202e metin", "BF_CONSENT_TEXT_INVALID")]
    public async Task InvalidBodiesReturnCodedErrorsAndStoreNothing(string body, string code)
    {
        using var admin = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var type = await CreateType(admin);
        using var response = await admin.PostAsJsonAsync($"{Root}/types/{type.Id}/texts", new PublishConsentTextRequest(Guid.NewGuid(), "Başlık", body));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, problem.RootElement.GetProperty("fieldIssues").GetProperty("body")[0].GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"{Root}/types/{type.Id}/texts", new PublishConsentTextRequest(Guid.Empty, "Başlık", "Metin"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"{Root}/types/{type.Id}/texts", new PublishConsentTextRequest(Guid.NewGuid(), new string('x', 201), "Metin"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"{Root}/types/{type.Id}/texts", new PublishConsentTextRequest(Guid.NewGuid(), "Başlık", new string('x', 50_001)))).StatusCode);
        await factory.WithDb(async db => Assert.False(await db.ConsentTexts.AnyAsync(x => x.ConsentTypeId == type.Id)));
    }

    [PostgresFact]
    public async Task RecordsAreScopedStatefulAndEvidenceIsVerifiable()
    {
        using var admin = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var type = await CreateType(admin);
        var v1 = await Publish(admin, type.Id, "Metin 1", true);
        var manager = Guid.NewGuid().ToString(); var resident = Guid.NewGuid().ToString(); var outsider = Guid.NewGuid().ToString();
        var property = new Property("Rıza " + manager[..8], "City", "District", "Address", PropertyType.APARTMENT);
        var other = new Property("Diğer " + manager[..8], "City", "District", "Address", PropertyType.APARTMENT);
        await factory.WithDb(async db =>
        {
            db.AddRange(property, other);
            db.AddRange(new PropertyMembership(property.Id, manager), new PropertyMembership(property.Id, resident), new PropertyMembership(other.Id, outsider));
            await db.SaveChangesAsync();
        });
        var residentRow = await Approve(resident, "Ada Kaya", type.Id, v1.Id);
        var outsiderRow = await Approve(outsider, "Can Demir", type.Id, v1.Id);

        using var managerClient = TestToken.Client(factory, manager, "MANAGER");
        var page = (await managerClient.GetFromJsonAsync<PagedRows<ConsentRecordResponse>>($"{Root}/records?typeId={type.Id}"))!;
        var row = Assert.Single(page.Items);
        Assert.Equal(residentRow, row.Id); Assert.Equal("ACTIVE", row.State); Assert.Equal("Ada Kaya", row.ApproverName);
        Assert.Equal(HttpStatusCode.NotFound, (await managerClient.GetAsync($"{Root}/records/{outsiderRow}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await managerClient.GetAsync($"{Root}/users/{outsider}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await managerClient.GetAsync($"{Root}/records?propertyId={other.Id}")).StatusCode);
        Assert.Equal(2, (await admin.GetFromJsonAsync<PagedRows<ConsentRecordResponse>>($"{Root}/records?typeId={type.Id}"))!.TotalCount);
        Assert.Equal(outsiderRow, Assert.Single((await admin.GetFromJsonAsync<PagedRows<ConsentRecordResponse>>($"{Root}/records?typeId={type.Id}&propertyId={other.Id}"))!.Items).Id);
        Assert.Equal(residentRow, Assert.Single((await admin.GetFromJsonAsync<PagedRows<ConsentRecordResponse>>($"{Root}/records?typeId={type.Id}&search=ada%20k"))!.Items).Id);
        Assert.Empty((await admin.GetFromJsonAsync<PagedRows<ConsentRecordResponse>>($"{Root}/records?typeId={type.Id}&search=%25"))!.Items);

        var document = (await managerClient.GetFromJsonAsync<ConsentDocumentResponse>($"{Root}/records/{residentRow}"))!;
        Assert.True(document.BodyIntact); Assert.True(document.EvidenceIntact); Assert.Equal("Metin 1", document.Body);

        // A wording-only revision keeps approvals active; a revision that needs re-consent makes them outdated.
        await Publish(admin, type.Id, "Metin 1 yazım düzeltmesi", false);
        Assert.Equal("ACTIVE", (await managerClient.GetFromJsonAsync<PagedRows<ConsentRecordResponse>>($"{Root}/records?typeId={type.Id}"))!.Items[0].State);
        var v3 = await Publish(admin, type.Id, "Metin 3", true);
        Assert.Equal("OUTDATED", (await managerClient.GetFromJsonAsync<PagedRows<ConsentRecordResponse>>($"{Root}/records?typeId={type.Id}&state=OUTDATED"))!.Items.Single().State);

        // History keeps every approval and withdrawal; only the centre reads it.
        await factory.WithDb(async db =>
        {
            var active = await db.UserConsents.SingleAsync(x => x.Id == residentRow);
            active.Revoke(ConsentRevokeReason.SUPERSEDED, DateTimeOffset.UtcNow, null);
            await db.SaveChangesAsync();
        });
        var renewed = await Approve(resident, "Ada Kaya", type.Id, v3.Id);
        await factory.WithDb(async db =>
        {
            var active = await db.UserConsents.SingleAsync(x => x.Id == renewed);
            active.Revoke(ConsentRevokeReason.WITHDRAWN, DateTimeOffset.UtcNow, Guid.NewGuid());
            await db.SaveChangesAsync();
        });
        var history = (await managerClient.GetFromJsonAsync<UserConsentHistory>($"{Root}/users/{resident}"))!;
        Assert.Equal(["WITHDRAWN", "SUPERSEDED"], history.Records.Where(r => r.TypeId == type.Id).Select(r => r.State));
        var summary = history.Summary.Single(s => s.TypeId == type.Id);
        Assert.Equal("WITHDRAWN", summary.State); Assert.Null(summary.ApprovedVersion); Assert.Equal(3, summary.CurrentVersion);
        Assert.Equal("NONE", history.Summary.Single(s => s.Code == "KVKK_INFORMATION").State);
        Assert.Equal(HttpStatusCode.BadRequest, (await managerClient.GetAsync($"{Root}/records?state=GONE")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await managerClient.GetAsync($"{Root}/records?pageSize=101")).StatusCode);

        // Tampering with the stored row is detected on the document.
        await factory.WithDb(async db => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE user_consents SET approver_name = 'Başkası' WHERE id = {residentRow}"));
        Assert.False((await managerClient.GetFromJsonAsync<ConsentDocumentResponse>($"{Root}/records/{residentRow}"))!.EvidenceIntact);
    }

    [PostgresFact]
    public async Task DatabaseRejectsSecondActiveConsentAndForeignTextType()
    {
        using var admin = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var type = await CreateType(admin); var otherType = await CreateType(admin);
        var text = await Publish(admin, type.Id, "Metin", true);
        var owner = Guid.NewGuid().ToString();
        await Approve(owner, "Ada", type.Id, text.Id);
        await Assert.ThrowsAsync<DbUpdateException>(() => Approve(owner, "Ada", type.Id, text.Id));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => factory.WithDb(db => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE user_consents SET consent_type_id = {otherType.Id} WHERE owner_user_id = {owner}")));
    }

    private static async Task<ConsentTypeResponse> CreateType(HttpClient admin) =>
        (await (await admin.PostAsJsonAsync(Root + "/types", new CreateConsentTypeRequest(Code(), "Test türü", "Test type", "EXPLICIT_CONSENT"))).Content.ReadFromJsonAsync<ConsentTypeResponse>())!;

    private static async Task<ConsentTextResponse> Publish(HttpClient admin, Guid type, string body, bool reconsent) =>
        (await (await admin.PostAsJsonAsync($"{Root}/types/{type}/texts", new PublishConsentTextRequest(Guid.NewGuid(), "Başlık", body, reconsent))).Content.ReadFromJsonAsync<ConsentTextResponse>())!;

    // Approval itself is the mobile flow of the next phase; tests write it through the domain with sealed evidence.
    private async Task<Guid> Approve(string owner, string name, Guid typeId, Guid textId)
    {
        var id = Guid.Empty;
        await factory.WithDb(async db =>
        {
            var type = await db.ConsentTypes.SingleAsync(x => x.Id == typeId);
            var text = await db.ConsentTexts.SingleAsync(x => x.Id == textId);
            var consent = new UserConsent(owner, name, type, text, DateTimeOffset.UtcNow, ConsentChannel.MOBILE, "1.0.0", Guid.NewGuid(), Hash('b'));
            consent.Seal(ConsentEvidence.Compute(consent, type.Code));
            db.Add(consent); await db.SaveChangesAsync(); id = consent.Id;
        });
        return id;
    }
}
