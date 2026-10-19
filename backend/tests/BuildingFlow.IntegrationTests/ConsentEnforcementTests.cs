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

// İŞ17: mobile consent API, photo enforcement and withdrawal consequences.
public sealed class ConsentEnforcementTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Profile = "PROFILE_PHOTO", Request = "REQUEST_PHOTO", Information = "KVKK_INFORMATION";

    private async Task<(HttpClient Client, string User, Guid Property)> Setup(string role = "MANAGER")
    {
        var user = Guid.NewGuid().ToString();
        var property = new Property("Consent fixture", "City", "District", "Address");
        await factory.WithDb(async db => { db.AddRange(property, new PropertyMembership(property.Id, user)); await db.SaveChangesAsync(); });
        return (TestToken.Client(factory, user, role), user, property.Id);
    }

    private static string Image() => Convert.ToBase64String(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "avatar.jpg")));
    private static MobileProfileCommand ProfileCommand(long version, string action = "KEEP", string? photo = null) => new(Guid.NewGuid(), version, "Rıza", "Sahibi", null, action, photo);
    private static Task<HttpResponseMessage> PushProfile(HttpClient c, MobileProfileCommand command) => c.PostAsJsonAsync("/api/v1/mobile/profile/commands", command);
    private static MobileSyncCommand RequestCommand(Guid property) => new(Guid.NewGuid(), Guid.NewGuid(), property, null, "MANAGER", "CREATE", 0, "Asansör arızası", "FAULT", "HIGH");
    private static Task<HttpResponseMessage> PushRequest(HttpClient c, MobileSyncCommand command) => c.PostAsJsonAsync("/api/v1/mobile/sync/commands", command);

    private static async Task AssertCode(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, problem.RootElement.GetProperty("code").GetString());
    }

    [PostgresFact]
    public async Task MobileApiApprovesSupersedesWithdrawsAndNeverReturnsHistory()
    {
        var s = await Setup(); using var c = s.Client;
        // A type of its own keeps version numbers independent of the other tests in this fixture schema.
        const string Profile = "LIFECYCLE_PHOTO";
        using (var owner = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN"))
            Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync("/api/v1/consents/types", new CreateConsentTypeRequest(Profile, "Yaşam döngüsü", "Lifecycle", "EXPLICIT_CONSENT"))).StatusCode);
        var empty = await ConsentTestHelper.ItemAsync(c, Profile);
        Assert.Equal(ConsentStates.None, empty.Status.State); Assert.Null(empty.CurrentText);

        var v1 = await ConsentTestHelper.PublishAsync(factory, Profile, "Profil fotoğrafı rızası v1.");
        var item = await ConsentTestHelper.ItemAsync(c, Profile);
        Assert.Equal(v1.Id, item.CurrentText!.Id); Assert.Equal(v1.BodySha256, item.CurrentText.BodySha256);
        var approve = ConsentTestHelper.Approve(Profile, item.CurrentText);
        var first = await ConsentTestHelper.CommandAsync(c, approve);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var approved = (await first.Content.ReadFromJsonAsync<MobileConsentResult>())!;
        Assert.Equal(ConsentStates.Active, approved.Status.State); Assert.Equal(1, approved.Status.ApprovedVersion); Assert.False(approved.Replayed);

        // Same UUID replays; same UUID with other content is rejected.
        Assert.True((await (await ConsentTestHelper.CommandAsync(c, approve)).Content.ReadFromJsonAsync<MobileConsentResult>())!.Replayed);
        await AssertCode(await ConsentTestHelper.CommandAsync(c, approve with { AppVersion = "2.0.0" }), HttpStatusCode.Conflict, "BF_CONSENT_OPERATION_CONFLICT");
        // Approving the same text again with a new UUID adds no history row.
        Assert.Equal(HttpStatusCode.OK, (await ConsentTestHelper.CommandAsync(c, approve with { OperationId = Guid.NewGuid() })).StatusCode);

        // A typo-fix version keeps the approval ACTIVE; a re-consent version makes it OUTDATED and the old text is refused.
        await ConsentTestHelper.PublishAsync(factory, Profile, "Profil fotoğrafı rızası v2 (yazım).", requiresReconsent: false);
        Assert.Equal(ConsentStates.Active, (await ConsentTestHelper.ItemAsync(c, Profile)).Status.State);
        await ConsentTestHelper.PublishAsync(factory, Profile, "Profil fotoğrafı rızası v3.");
        var outdated = await ConsentTestHelper.ItemAsync(c, Profile);
        Assert.Equal(ConsentStates.Outdated, outdated.Status.State); Assert.Equal(3, outdated.CurrentText!.TextVersion);
        await AssertCode(await ConsentTestHelper.CommandAsync(c, approve with { OperationId = Guid.NewGuid() }), HttpStatusCode.Conflict, "BF_CONSENT_TEXT_OUTDATED");
        await AssertCode(await ConsentTestHelper.CommandAsync(c, ConsentTestHelper.Approve(Profile, outdated.CurrentText) with { BodySha256 = v1.BodySha256 }), HttpStatusCode.Conflict, "BF_CONSENT_TEXT_OUTDATED");
        var renewed = (await (await ConsentTestHelper.CommandAsync(c, ConsentTestHelper.Approve(Profile, outdated.CurrentText))).Content.ReadFromJsonAsync<MobileConsentResult>())!;
        Assert.Equal(ConsentStates.Active, renewed.Status.State); Assert.Equal(3, renewed.Status.ApprovedVersion);

        var withdraw = ConsentTestHelper.Withdraw(Profile);
        Assert.Equal(ConsentStates.Withdrawn, (await (await ConsentTestHelper.CommandAsync(c, withdraw)).Content.ReadFromJsonAsync<MobileConsentResult>())!.Status.State);
        Assert.True((await (await ConsentTestHelper.CommandAsync(c, withdraw)).Content.ReadFromJsonAsync<MobileConsentResult>())!.Replayed);
        Assert.Equal(HttpStatusCode.OK, (await ConsentTestHelper.CommandAsync(c, ConsentTestHelper.Withdraw(Profile))).StatusCode);

        // The owner's endpoint carries state only; the centre sees approve → supersede → withdraw.
        using var raw = JsonDocument.Parse(await c.GetStringAsync("/api/v1/mobile/consents"));
        var names = raw.RootElement.EnumerateObject().Select(p => p.Name).Concat(raw.RootElement.GetProperty("items")[0].EnumerateObject().Select(p => p.Name))
            .Concat(raw.RootElement.GetProperty("items")[0].GetProperty("status").EnumerateObject().Select(p => p.Name)).ToArray();
        Assert.DoesNotContain(names, n => n.Contains("record", StringComparison.OrdinalIgnoreCase) || n.Contains("history", StringComparison.OrdinalIgnoreCase) || n.Contains("revoke", StringComparison.OrdinalIgnoreCase));
        using var admin = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var history = (await admin.GetFromJsonAsync<UserConsentHistory>("/api/v1/consents/users/" + s.User))!;
        var rows = history.Records.Where(r => r.TypeCode == Profile).OrderBy(r => r.ApprovedAt).ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(ConsentStates.Superseded, rows[0].State); Assert.Equal(1, rows[0].TextVersion); Assert.Equal("MOBILE", rows[0].Channel);
        Assert.Equal(ConsentStates.Withdrawn, rows[1].State); Assert.Equal(3, rows[1].TextVersion);
        Assert.Equal(ConsentStates.Withdrawn, history.Summary.Single(x => x.Code == Profile).State);
        var document = (await admin.GetFromJsonAsync<ConsentDocumentResponse>("/api/v1/consents/records/" + rows[1].Id))!;
        Assert.True(document.BodyIntact); Assert.True(document.EvidenceIntact); Assert.Equal("Profil fotoğrafı rızası v3.", document.Body);
    }

    [PostgresFact]
    public async Task ValidationInformationAndAccessRulesFailClosed()
    {
        var s = await Setup(); using var c = s.Client;
        var text = await ConsentTestHelper.PublishAsync(factory, Information, "# Aydınlatma metni\n\nVeri sorumlusu bilgisi.");
        var item = await ConsentTestHelper.ItemAsync(c, Information);
        Assert.Equal(ConsentStates.Active, (await (await ConsentTestHelper.CommandAsync(c, ConsentTestHelper.Approve(Information, item.CurrentText!))).Content.ReadFromJsonAsync<MobileConsentResult>())!.Status.State);
        // An information notice is an acknowledgement, not consent: it cannot be withdrawn.
        await AssertCode(await ConsentTestHelper.CommandAsync(c, ConsentTestHelper.Withdraw(Information)), HttpStatusCode.Conflict, "BF_CONSENT_NOT_WITHDRAWABLE");

        var valid = ConsentTestHelper.Approve(Information, item.CurrentText!);
        foreach (var invalid in new[] { valid with { OperationId = Guid.Empty }, valid with { Action = "GRANT" }, valid with { TypeCode = "" }, valid with { TypeCode = "UNKNOWN_TYPE" },
                     valid with { TextId = null }, valid with { BodySha256 = text.BodySha256.ToUpperInvariant() }, valid with { BodySha256 = "abc" }, valid with { AppVersion = new string('1', 41) },
                     ConsentTestHelper.Withdraw(Profile) with { TextId = text.Id } })
            Assert.Equal(HttpStatusCode.BadRequest, (await ConsentTestHelper.CommandAsync(c, invalid)).StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await c.PostAsync("/api/v1/mobile/consents/commands", new StringContent("{}"))).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await c.PostAsync("/api/v1/mobile/consents/commands", new StringContent(new string('x', 17_000), System.Text.Encoding.UTF8, "application/json"))).StatusCode);

        // Free (non-system) types are offered and can be withdrawn; a disabled type is refused.
        using var admin = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var free = (await (await admin.PostAsJsonAsync("/api/v1/consents/types", new CreateConsentTypeRequest("MARKETING_SMS", "Ticari ileti", "Marketing", "EXPLICIT_CONSENT"))).Content.ReadFromJsonAsync<ConsentTypeResponse>())!;
        await ConsentTestHelper.PublishAsync(factory, "MARKETING_SMS", "Ticari ileti izni.");
        var freeItem = await ConsentTestHelper.ItemAsync(c, "MARKETING_SMS");
        Assert.Null(freeItem.Status.Feature);
        Assert.Equal(HttpStatusCode.OK, (await ConsentTestHelper.CommandAsync(c, ConsentTestHelper.Approve("MARKETING_SMS", freeItem.CurrentText!))).StatusCode);
        Assert.Equal(ConsentStates.Withdrawn, (await (await ConsentTestHelper.CommandAsync(c, ConsentTestHelper.Withdraw("MARKETING_SMS"))).Content.ReadFromJsonAsync<MobileConsentResult>())!.Status.State);
        var current = (await admin.GetFromJsonAsync<ConsentTypeResponse[]>("/api/v1/consents/types"))!.Single(t => t.Id == free.Id);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync("/api/v1/consents/types/" + free.Id, new UpdateConsentTypeRequest(current.NameTr, current.NameEn, current.SortOrder, false, current.Version))).StatusCode);
        Assert.DoesNotContain((await c.GetFromJsonAsync<MobileConsentList>("/api/v1/mobile/consents"))!.Items, x => x.Status.TypeCode == "MARKETING_SMS");
        await AssertCode(await ConsentTestHelper.CommandAsync(c, ConsentTestHelper.Approve("MARKETING_SMS", freeItem.CurrentText!)), HttpStatusCode.Conflict, "BF_CONSENT_TYPE_DISABLED");

        // Each account only ever sees its own state; anonymous calls are rejected.
        var other = await Setup(); using var foreign = other.Client;
        Assert.Equal(ConsentStates.None, (await ConsentTestHelper.ItemAsync(foreign, Information)).Status.State);
        using var anonymous = TestToken.Client(factory, null, "");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/mobile/consents")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await ConsentTestHelper.CommandAsync(anonymous, valid)).StatusCode);
        // Consent rows are never deleted.
        await factory.WithDb(async db => Assert.Equal(2, await db.UserConsents.IgnoreQueryFilters().CountAsync(x => x.OwnerUserId == s.User)));
    }

    [PostgresFact]
    public async Task PhotoUploadsWithoutActiveConsentAreRejectedBeforeAnyStorageWork()
    {
        var s = await Setup(); using var c = s.Client;
        // No consent at all: profile and request REPLACE are refused, nothing is staged, KEEP/REMOVE and the plain request still work.
        await AssertCode(await PushProfile(c, ProfileCommand(0, "REPLACE", Image())), HttpStatusCode.Forbidden, "BF_CONSENT_REQUIRED");
        await AssertCode(await PushRequest(c, RequestCommand(s.Property) with { PhotoAction = "REPLACE", PhotoBase64 = Image() }), HttpStatusCode.Forbidden, "BF_CONSENT_REQUIRED");
        Assert.Equal(HttpStatusCode.OK, (await PushProfile(c, ProfileCommand(0))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PushProfile(c, ProfileCommand(1, "REMOVE"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PushRequest(c, RequestCommand(s.Property))).StatusCode);
        await factory.WithDb(async db =>
        {
            Assert.False(await db.Set<MobileProfilePhoto>().IgnoreQueryFilters().AnyAsync(x => x.OwnerUserId == s.User));
            Assert.False(await db.Set<MobileRequestPhoto>().IgnoreQueryFilters().AnyAsync(x => x.OwnerUserId == s.User));
        });

        // Another account's consent does not count, and an outdated or withdrawn consent is refused the same way.
        var other = await Setup(); using var foreign = other.Client;
        await ConsentTestHelper.GrantAsync(factory, foreign, Profile);
        await AssertCode(await PushProfile(c, ProfileCommand(2, "REPLACE", Image())), HttpStatusCode.Forbidden, "BF_CONSENT_REQUIRED");
        await ConsentTestHelper.GrantAsync(factory, c, Request);
        await ConsentTestHelper.PublishAsync(factory, Request, "Talep fotoğrafı rızası yeni sürüm.");
        await AssertCode(await PushRequest(c, RequestCommand(s.Property) with { PhotoAction = "REPLACE", PhotoBase64 = Image() }), HttpStatusCode.Forbidden, "BF_CONSENT_REQUIRED");
        await ConsentTestHelper.GrantAsync(factory, c, Request);
        Assert.Equal(HttpStatusCode.OK, (await ConsentTestHelper.CommandAsync(c, ConsentTestHelper.Withdraw(Request))).StatusCode);
        await AssertCode(await PushRequest(c, RequestCommand(s.Property) with { PhotoAction = "REPLACE", PhotoBase64 = Image() }), HttpStatusCode.Forbidden, "BF_CONSENT_REQUIRED");
    }

    [StorageFact]
    public async Task WithdrawingProfileConsentRemovesThePhotoAndPurgesTheObject()
    {
        var s = await Setup(); using var c = s.Client;
        await ConsentTestHelper.GrantAsync(factory, c, Profile);
        var saved = (await (await PushProfile(c, ProfileCommand(0, "REPLACE", Image()))).Content.ReadFromJsonAsync<MobileProfileResult>())!;
        var photo = saved.Profile.PhotoId!.Value;
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/v1/mobile/profile/photo/" + photo)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await ConsentTestHelper.CommandAsync(c, ConsentTestHelper.Withdraw(Profile))).StatusCode);
        var profile = (await c.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!;
        Assert.Null(profile.PhotoId); Assert.True(profile.Version > saved.Profile.Version);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/v1/mobile/profile/photo/" + photo)).StatusCode);
        // A queued edit made before the withdrawal now conflicts instead of silently restoring the photo.
        Assert.Equal(HttpStatusCode.Conflict, (await PushProfile(c, ProfileCommand(saved.Profile.Version))).StatusCode);
        await AssertCode(await PushProfile(c, ProfileCommand(profile.Version, "REPLACE", Image())), HttpStatusCode.Forbidden, "BF_CONSENT_REQUIRED");

        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMobileProfileStore>().MaintainAsync(default);
        await factory.WithDb(async db =>
        {
            Assert.Equal("PURGED", (await db.Set<MobileProfilePhoto>().SingleAsync(x => x.Id == photo)).State);
            Assert.Equal(ConsentRevokeReason.WITHDRAWN, (await db.UserConsents.SingleAsync(x => x.OwnerUserId == s.User)).RevokeReason);
        });
        using var storage = StorageTestClient.Create();
        var objects = await storage.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request { BucketName = factory.StorageBucket, Prefix = "profiles/" + s.User });
        Assert.Empty(objects.S3Objects ?? []);
    }

    [StorageFact]
    public async Task WithdrawingRequestConsentHidesPhotosFromTheOwnerButNotTheCentre()
    {
        var s = await Setup(); using var c = s.Client;
        var manager = Guid.NewGuid().ToString();
        await factory.WithDb(async db => { db.Add(new PropertyMembership(s.Property, manager)); await db.SaveChangesAsync(); });
        using var centre = TestToken.Client(factory, manager, "SITE_ADMIN");
        await ConsentTestHelper.GrantAsync(factory, c, Request);
        var command = RequestCommand(s.Property) with { PhotoAction = "REPLACE", PhotoBase64 = Image() };
        var created = (await (await PushRequest(c, command)).Content.ReadFromJsonAsync<MobileSyncResult>())!;
        Assert.NotNull(created.Record.PhotoId);
        var granted = (await centre.GetFromJsonAsync<RequestDetail>($"/api/v1/properties/{s.Property}/requests/{command.RecordId}"))!;
        Assert.Equal("GRANTED", granted.PhotoConsent!.State); Assert.Equal((await ConsentTestHelper.ItemAsync(c, Request)).Status.ApprovedVersion, granted.PhotoConsent.TextVersion); Assert.Null(granted.PhotoConsent.WithdrawnAt);

        Assert.Equal(HttpStatusCode.OK, (await ConsentTestHelper.CommandAsync(c, ConsentTestHelper.Withdraw(Request))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v1/mobile/sync/requests/{command.RecordId}/photo")).StatusCode);
        var snapshot = (await c.GetFromJsonAsync<MobileRequestSnapshot>($"/api/v1/mobile/sync/requests?propertyId={s.Property}&role=MANAGER"))!;
        var hidden = Assert.Single(snapshot.Records); Assert.Null(hidden.PhotoId); Assert.Null(hidden.PhotoSha256);
        // A replay of the original upload returns its receipt, but the photo is not offered to the owner again.
        Assert.True((await (await PushRequest(c, command)).Content.ReadFromJsonAsync<MobileSyncResult>())!.Replayed);

        // The centre keeps the photo, sees that consent was withdrawn, and the row was never deleted.
        Assert.Equal(HttpStatusCode.OK, (await centre.GetAsync($"/api/v1/mobile/sync/requests/{command.RecordId}/photo")).StatusCode);
        var detail = (await centre.GetFromJsonAsync<RequestDetail>($"/api/v1/properties/{s.Property}/requests/{command.RecordId}"))!;
        Assert.Equal("WITHDRAWN", detail.PhotoConsent!.State); Assert.NotNull(detail.PhotoConsent.WithdrawnAt); Assert.Equal(granted.PhotoConsent.TextVersion, detail.PhotoConsent.TextVersion); Assert.Equal(granted.PhotoConsent.RecordId, detail.PhotoConsent.RecordId);
        await factory.WithDb(async db => Assert.Equal("CURRENT", (await db.Set<MobileRequestPhoto>().SingleAsync(x => x.Id == created.Record.PhotoId)).State));

        // After consenting again a new photo is visible; the old one stays hidden.
        await ConsentTestHelper.GrantAsync(factory, c, Request);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v1/mobile/sync/requests/{command.RecordId}/photo")).StatusCode);
        var update = command with { OperationId = Guid.NewGuid(), Action = "UPDATE", ExpectedVersion = created.Record.Version };
        var replaced = (await (await PushRequest(c, update)).Content.ReadFromJsonAsync<MobileSyncResult>())!;
        Assert.NotNull(replaced.Record.PhotoId); Assert.NotEqual(created.Record.PhotoId, replaced.Record.PhotoId);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/v1/mobile/sync/requests/{command.RecordId}/photo")).StatusCode);
        Assert.Equal("GRANTED", (await centre.GetFromJsonAsync<RequestDetail>($"/api/v1/properties/{s.Property}/requests/{command.RecordId}"))!.PhotoConsent!.State);
    }

    [PostgresFact]
    public async Task LegacyPhotoWithoutConsentShowsNoneToTheCentre()
    {
        var s = await Setup();
        var manager = Guid.NewGuid().ToString();
        var request = new MobileRequest(Guid.NewGuid(), s.Property, null, "MANAGER", s.User, "Eski talep");
        var photo = new MobileRequestPhoto(s.User, s.Property, request.Id, Guid.NewGuid(), new string('a', 64), new string('B', 64), 10);
        photo.Current();
        await factory.WithDb(async db =>
        {
            db.AddRange(request, new PropertyMembership(s.Property, manager)); await db.SaveChangesAsync();
            db.Add(photo); await db.SaveChangesAsync();
            request.Details("FAULT", "NORMAL", photo.Id); await db.SaveChangesAsync();
        });
        using var centre = TestToken.Client(factory, manager, "SITE_ADMIN");
        var detail = (await centre.GetFromJsonAsync<RequestDetail>($"/api/v1/properties/{s.Property}/requests/{request.Id}"))!;
        Assert.Equal("NONE", detail.PhotoConsent!.State); Assert.Null(detail.PhotoConsent.RecordId);
        // Photos uploaded before İŞ17 stay visible to their owner; only a withdrawal hides them.
        var snapshot = (await s.Client.GetFromJsonAsync<MobileRequestSnapshot>($"/api/v1/mobile/sync/requests?propertyId={s.Property}&role=MANAGER"))!;
        Assert.Equal(photo.Id, Assert.Single(snapshot.Records).PhotoId);
    }
}
