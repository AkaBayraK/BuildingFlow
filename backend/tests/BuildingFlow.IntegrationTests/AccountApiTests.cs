using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Api;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;
// İŞ19 "Hesap bilgilerim": own account data, WEB consent channel, and who may see an account photo.
public sealed class AccountApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private async Task<(string User, Guid Property)> Member(string role = "MANAGER")
    {
        var user = Guid.NewGuid().ToString(); var property = new Property("Account " + user[..8], "City", "District", "Address");
        await factory.WithDb(async db => { db.Add(property); db.Add(new PropertyMembership(property.Id, user)); await db.SaveChangesAsync(); });
        return (user, property.Id);
    }
    private static async Task<MobileConsentResult> ApproveOnWeb(HttpClient client, string code)
    {
        var item = (await client.GetFromJsonAsync<MobileConsentList>("/api/v1/account/consents"))!.Items.Single(x => x.Status.TypeCode == code);
        var response = await client.PostAsJsonAsync("/api/v1/account/consents/commands", ConsentTestHelper.Approve(code, item.CurrentText!));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); return (await response.Content.ReadFromJsonAsync<MobileConsentResult>())!;
    }
    private static MobileProfileCommand Profile(long version, string action = "KEEP", string? photo = null) => new(Guid.NewGuid(), version, "ayşe nur", "yılmaz", null, action, photo);

    [PostgresFact] public async Task AccountShowsOwnRolesPropertiesAndProfileAndRecordsWebConsent()
    {
        var s = await Member(); using var c = TestToken.Client(factory, s.User, "MANAGER"); using var anonymous = TestToken.Client(factory, null, "");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/account")).StatusCode);
        var account = (await c.GetFromJsonAsync<AccountDto>("/api/v1/account"))!;
        Assert.Equal(s.User, account.UserId); Assert.Equal(["MANAGER"], account.Roles); Assert.Contains(account.Contexts, x => x.PropertyId == s.Property && x.Role == "MANAGER"); Assert.Equal(0, account.Profile.Version);
        var saved = (await (await c.PostAsJsonAsync("/api/v1/account/profile/commands", Profile(0))).Content.ReadFromJsonAsync<MobileProfileResult>())!;
        Assert.Equal("Ayşe Nur", saved.Profile.FirstName); Assert.Equal("YILMAZ", saved.Profile.LastName);
        Assert.Equal("YILMAZ", (await c.GetFromJsonAsync<AccountDto>("/api/v1/account"))!.Profile.LastName);
        if ((await c.GetFromJsonAsync<MobileConsentList>("/api/v1/account/consents"))!.Items.Single(x => x.Status.TypeCode == "PROFILE_PHOTO").CurrentText is null) await ConsentTestHelper.PublishAsync(factory, "PROFILE_PHOTO");
        var approved = await ApproveOnWeb(c, "PROFILE_PHOTO"); Assert.Equal(ConsentStates.Active, approved.Status.State);
        await factory.WithDb(async db => Assert.Equal(ConsentChannel.WEB, (await db.UserConsents.SingleAsync(x => x.OwnerUserId == s.User && x.RevokedAt == null)).Channel));
        // The mobile app reads the same decision and the same profile.
        Assert.Equal(ConsentStates.Active, (await ConsentTestHelper.ItemAsync(c, "PROFILE_PHOTO")).Status.State);
        Assert.Equal("Ayşe Nur", (await c.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.FirstName);
    }

    [StorageFact] public async Task AccountPhotoIsSharedWithMobileAndVisibleOnlyThroughALinkedPersonInScope()
    {
        var owner = await Member("RESIDENT"); using var c = TestToken.Client(factory, owner.User, "RESIDENT");
        if ((await c.GetFromJsonAsync<MobileConsentList>("/api/v1/account/consents"))!.Items.Single(x => x.Status.TypeCode == "PROFILE_PHOTO").CurrentText is null) await ConsentTestHelper.PublishAsync(factory, "PROFILE_PHOTO");
        var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "avatar.jpg")); var photo = Convert.ToBase64String(bytes);
        var refused = await c.PostAsJsonAsync("/api/v1/account/profile/commands", Profile(0, "REPLACE", photo));
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode); Assert.Contains("BF_CONSENT_REQUIRED", await refused.Content.ReadAsStringAsync());
        await ApproveOnWeb(c, "PROFILE_PHOTO");
        var saved = (await (await c.PostAsJsonAsync("/api/v1/account/profile/commands", Profile(0, "REPLACE", photo))).Content.ReadFromJsonAsync<MobileProfileResult>())!;
        var id = saved.Profile.PhotoId!.Value; var stored = await c.GetByteArrayAsync($"/api/v1/account/profile/photo/{id}");
        Assert.Equal(stored, await c.GetByteArrayAsync($"/api/v1/mobile/profile/photo/{id}"));

        Guid linked = Guid.Empty, unlinked = Guid.Empty;
        await factory.WithDb(async db =>
        {
            var a = new Person(owner.Property, "Ayşe", "YILMAZ", "+905551234567", "ayse@example.test"); a.SetProfile("NATURAL", "DOMESTIC", "TR", "12345678901", null, null, null, owner.User);
            var b = new Person(owner.Property, "Other", "PERSON", "+905551234568", "other@example.test"); b.SetProfile("NATURAL", "DOMESTIC", "TR", "12345678902", null, null, null, null);
            db.AddRange(a, b); linked = a.Id; unlinked = b.Id; await db.SaveChangesAsync();
        });
        var manager = Guid.NewGuid().ToString(); await factory.WithDb(async db => { db.Add(new PropertyMembership(owner.Property, manager)); await db.SaveChangesAsync(); });
        using var m = TestToken.Client(factory, manager, "MANAGER");
        var root = $"/api/v1/properties/{owner.Property}/people/";
        Assert.Equal(stored, await m.GetByteArrayAsync(root + linked + "/photo"));
        // The people list and detail carry the photo id only for the linked person with a current photo.
        var listed = (await m.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/v1/properties/{owner.Property}/people?page=1&pageSize=50")).GetProperty("items").EnumerateArray().ToDictionary(x => x.GetProperty("id").GetGuid(), x => x.GetProperty("linkedPhotoId"));
        Assert.Equal(id, listed[linked].GetGuid()); Assert.Equal(System.Text.Json.JsonValueKind.Null, listed[unlinked].ValueKind);
        Assert.Equal(id, (await m.GetFromJsonAsync<PersonResponse>(root + linked))!.LinkedPhotoId);
        Assert.Equal(HttpStatusCode.NotFound, (await m.GetAsync(root + unlinked + "/photo")).StatusCode);
        var stranger = await Member(); using var outsider = TestToken.Client(factory, stranger.User, "MANAGER");
        Assert.NotEqual(HttpStatusCode.OK, (await outsider.GetAsync(root + linked + "/photo")).StatusCode);
        using var accountant = TestToken.Client(factory, manager, "ACCOUNTANT");
        Assert.NotEqual(HttpStatusCode.OK, (await accountant.GetAsync(root + linked + "/photo")).StatusCode);
        // Another account cannot read the owner's photo through the account endpoints.
        Assert.Equal(HttpStatusCode.NotFound, (await m.GetAsync($"/api/v1/account/profile/photo/{id}")).StatusCode);
        // Withdrawing the consent removes the photo everywhere.
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/v1/account/consents/commands", ConsentTestHelper.Withdraw("PROFILE_PHOTO"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await m.GetAsync(root + linked + "/photo")).StatusCode);
        Assert.Null((await c.GetFromJsonAsync<AccountDto>("/api/v1/account"))!.Profile.PhotoId);
        Assert.Null((await m.GetFromJsonAsync<PersonResponse>(root + linked))!.LinkedPhotoId);
    }
}
