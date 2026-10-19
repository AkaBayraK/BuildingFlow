using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class MobileOnboardingTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static readonly PersonCodeHasher Hasher = new(new ConfigurationBuilder().Build());
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));

    [PostgresFact] public async Task MatchingCodeAndDetailsLinkTenantAndUnlockSession()
    {
        var x = await Seed(); var user = Guid.NewGuid().ToString(); using var client = TestToken.Client(factory, user, "");
        var response = await client.PostAsJsonAsync("/api/v1/mobile/person-links", new PersonLinkRequest(x.Code, "  ayşe ", "YILMAZ", "0555 123 45 67", "Ayse@Example.test"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<PersonLinkResult>())!;
        Assert.Equal(PersonMatching.Linked, result.Status); Assert.Equal(x.Property, result.PropertyId);
        await factory.WithDb(async db => {
            var person = await db.People.SingleAsync(p => p.Id == x.Person);
            Assert.Equal(user, person.LinkedUserId); Assert.Null(person.PersonCodeHash); Assert.Null(person.PersonCodeHint);
            Assert.True(await db.PropertyMemberships.AnyAsync(m => m.PropertyId == x.Property && m.UserId == user && m.IsEnabled));
        });
        var session = await client.GetStringAsync("/api/v1/mobile/session");
        Assert.Contains("\"role\":\"TENANT\"", session); Assert.Contains(x.Property.ToString(), session);
        using var other = TestToken.Client(factory, Guid.NewGuid().ToString(), "");
        Assert.Contains("BF_MOBILE_LINK_REJECTED", await (await other.PostAsJsonAsync("/api/v1/mobile/person-links", new PersonLinkRequest(x.Code, "Ayşe", "Yılmaz", "+905551234567", null))).Content.ReadAsStringAsync());
    }

    [PostgresFact] public async Task MismatchesAreUniformAndLockAfterFiveFailures()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, Guid.NewGuid().ToString(), "");
        var attempts = new[] {
            new PersonLinkRequest(x.Code, "Ayşe", "Kaya", "+905551234567", "ayse@example.test"),
            new PersonLinkRequest(x.Code, "Ayşe", "Yılmaz", "+905559999999", "ayse@example.test"),
            new PersonLinkRequest(x.Code, "Ayşe", "Yılmaz", "+905551234567", "other@example.test"),
            new PersonLinkRequest(Wrong(x.Code), "Ayşe", "Yılmaz", "+905551234567", "ayse@example.test"),
            new PersonLinkRequest("12", "Ayşe", "Yılmaz", "+905551234567", "ayse@example.test") };
        var bodies = new List<string>();
        foreach (var attempt in attempts)
        {
            var response = await client.PostAsJsonAsync("/api/v1/mobile/person-links", attempt);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); bodies.Add(await response.Content.ReadAsStringAsync());
        }
        Assert.All(bodies, body => { Assert.Contains("BF_MOBILE_LINK_REJECTED", body); Assert.DoesNotContain("Yılmaz", body); });
        var locked = await client.PostAsJsonAsync("/api/v1/mobile/person-links", new PersonLinkRequest(x.Code, "Ayşe", "Yılmaz", "+905551234567", "ayse@example.test"));
        Assert.Contains("BF_MOBILE_LINK_LOCKED", await locked.Content.ReadAsStringAsync());
        await factory.WithDb(async db => Assert.Null((await db.People.SingleAsync(p => p.Id == x.Person)).LinkedUserId));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/mobile/person-links", new PersonLinkRequest(x.Code, "", "Y", "123", null))).StatusCode);
    }

    [PostgresFact] public async Task EndedOccupancyOrForeignLinkIsRejected()
    {
        var ended = await Seed(endDate: Today.AddDays(-1)); var taken = await Seed(linkedTo: "someone-else");
        using var client = TestToken.Client(factory, Guid.NewGuid().ToString(), "");
        foreach (var x in new[] { ended, taken })
            Assert.Contains("BF_MOBILE_LINK_REJECTED", await (await client.PostAsJsonAsync("/api/v1/mobile/person-links", new PersonLinkRequest(x.Code, "Ayşe", "Yılmaz", "+905551234567", "ayse@example.test"))).Content.ReadAsStringAsync());
    }

    [PostgresFact] public async Task AnonymousRegistrationNeedsIdentityServerAndLinkNeedsSignIn()
    {
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/mobile/person-links", new PersonLinkRequest("482917", "A", "B", "+905551234567", null))).StatusCode);
        var response = await anonymous.PostAsJsonAsync("/api/v1/mobile/registrations", Registration(Guid.NewGuid(), "new.user"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [PostgresFact] public async Task RegistrationCreatesAccountProfileAndOptionalLink()
    {
        var x = await Seed(); var keycloak = new Keycloak(); var request = Registration(Guid.NewGuid(), "ayse.y", x.Code) with { Email = "AYSE@example.test" };
        await WithService(keycloak, async service => {
            var result = await service.RegisterAsync(request, "client-a", default);
            Assert.Equal("ayse.y", result.Username); Assert.Equal(PersonMatching.Linked, result.PersonLink.Status);
            Assert.False(keycloak.Temporary[result.UserId]);
            var replay = await service.RegisterAsync(request, "client-a", default);
            Assert.True(replay.Replayed); Assert.Equal(result.UserId, replay.UserId); Assert.Single(keycloak.Users);
            await Assert.ThrowsAsync<HierarchyConflictException>(() => service.RegisterAsync(request with { FirstName = "Changed" }, "client-a", default));
            await Assert.ThrowsAsync<HierarchyConflictException>(() => service.RegisterAsync(Registration(Guid.NewGuid(), "ayse.y"), "client-a", default));
            await Assert.ThrowsAsync<RequestValidationException>(() => service.RegisterAsync(Registration(Guid.NewGuid(), "pw.mismatch") with { PasswordConfirmation = "Different12345" }, "client-a", default));
            await Assert.ThrowsAsync<RequestValidationException>(() => service.RegisterAsync(Registration(Guid.NewGuid(), "weak.pw") with { Password = "weakpassword", PasswordConfirmation = "weakpassword" }, "client-a", default));
            await factory.WithDb(async db => {
                var profile = await db.Set<MobileProfile>().SingleAsync(p => p.OwnerUserId == result.UserId);
                Assert.Equal("Ayşe", profile.FirstName); Assert.Equal("+905551234567", profile.Phone);
                Assert.Equal(result.UserId, (await db.People.SingleAsync(p => p.Id == x.Person)).LinkedUserId);
            });
        });
    }

    [PostgresFact] public async Task RegistrationWithoutCodeOrWrongCodeStillCreatesAccountAndLimitsClients()
    {
        var keycloak = new Keycloak();
        await WithService(keycloak, async service => {
            Assert.Equal(PersonMatching.NotRequested, (await service.RegisterAsync(Registration(Guid.NewGuid(), "no.code0"), "client-b", default)).PersonLink.Status);
            Assert.Equal(PersonMatching.Rejected, (await service.RegisterAsync(Registration(Guid.NewGuid(), "bad.code", "482917"), "client-b", default)).PersonLink.Status);
            for (var i = 1; i < 4; i++) await service.RegisterAsync(Registration(Guid.NewGuid(), "no.code" + i), "client-b", default);
            await Assert.ThrowsAsync<HierarchyConflictException>(() => service.RegisterAsync(Registration(Guid.NewGuid(), "limited"), "client-b", default));
            Assert.Equal(PersonMatching.NotRequested, (await service.RegisterAsync(Registration(Guid.NewGuid(), "other.client"), "client-c", default)).PersonLink.Status);
        });
        Assert.Equal(6, keycloak.Users.Count);
    }

    [PostgresFact] public async Task FailedProfileSaveRemovesKeycloakAccount()
    {
        var keycloak = new Keycloak { FailPassword = true };
        await WithService(keycloak, async service =>
            await Assert.ThrowsAsync<IdentityUnavailableException>(() => service.RegisterAsync(Registration(Guid.NewGuid(), "rollback"), "client-d", default)));
        Assert.Empty(keycloak.Users);
    }

    [PostgresFact] public async Task BackfillDoesNotReissueConsumedCodes()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, Guid.NewGuid().ToString(), "");
        await client.PostAsJsonAsync("/api/v1/mobile/person-links", new PersonLinkRequest(x.Code, "Ayşe", "Yılmaz", "+905551234567", "ayse@example.test"));
        using (var scope = factory.Services.CreateScope()) while (await scope.ServiceProvider.GetRequiredService<IPeopleStore>().AssignMissingCodesAsync(200, default) > 0) { }
        await factory.WithDb(async db => Assert.Null((await db.People.SingleAsync(p => p.Id == x.Person)).PersonCodeHash));
    }

    private static string Wrong(string code) => code[..5] + (code[5] == '9' ? '8' : (char)(code[5] + 1));
    private static MobileRegistrationRequest Registration(Guid id, string username, string? code = null) =>
        new(id, username, "Strong-Pass123", "Strong-Pass123", "Ayşe", "Yılmaz", "0555 123 45 67", username + "@example.test", code);

    private async Task WithService(Keycloak keycloak, Func<MobileOnboardingService, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        await action(new MobileOnboardingService(keycloak, new MobileOnboardingStore(scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>(), TimeProvider.System), Hasher, new Anonymous()));
    }

    private async Task<(Guid Property, Guid Person, string Code)> Seed(DateOnly? endDate = null, string? linkedTo = null)
    {
        var property = new Property("Onboarding " + Guid.NewGuid().ToString("N")[..8], "City", "District", "Address", PropertyType.APARTMENT);
        var unit = property.AddUnit("1", 0, 100);
        var person = new Person(property.Id, "Ayşe", "Yılmaz", "+90 555 123 45 67", "ayse@example.test");
        person.SetProfile("NATURAL", "DOMESTIC", "TR", "12345678901", null, null, null, linkedTo);
        string code;
        do { code = PersonCodePolicy.Generate(System.Security.Cryptography.RandomNumberGenerator.GetInt32); }
        while (!PersonCodePolicy.IsAcceptable(code));
        person.AssignCode(Hasher.Hash(code), PersonCodePolicy.Hint(code), DateTimeOffset.UtcNow);
        await factory.WithDb(async db => {
            db.Add(property);
            if (linkedTo is not null) db.Add(new PropertyMembership(property.Id, linkedTo));
            db.Add(person); await db.SaveChangesAsync();
            db.Add(new UnitOccupancy(property.Id, unit.Id, person.Id, OccupancyType.TENANT, Today.AddDays(-30), endDate)); await db.SaveChangesAsync();
        });
        return (property.Id, person.Id, code);
    }

    private sealed class Anonymous : ICurrentUserService { public string? UserId => null; public string? Username => null; public bool IsAuthenticated => false; public bool IsInRole(string role) => false; }
    private sealed class Keycloak : IKeycloakAdmin
    {
        public Dictionary<string, KeycloakUserDraft> Users { get; } = [];
        public Dictionary<string, bool> Temporary { get; } = [];
        public bool FailPassword { get; init; }
        public Task<(IReadOnlyList<KeycloakUser> Users, int Total)> SearchAsync(string? search, int first, int max, CancellationToken token) => throw new NotSupportedException();
        public Task<KeycloakUser?> GetAsync(string id, CancellationToken token) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> RealmRolesAsync(string id, CancellationToken token) => throw new NotSupportedException();
        public Task<string> CreateAsync(KeycloakUserDraft draft, CancellationToken token)
        { if (Users.Values.Any(u => u.Username == draft.Username)) throw new IdentityConflictException(); var id = Guid.NewGuid().ToString(); Users[id] = draft; return Task.FromResult(id); }
        public Task UpdateAsync(string id, KeycloakUserDraft draft, CancellationToken token) => throw new NotSupportedException();
        public Task ChangeRealmRolesAsync(string id, IReadOnlyCollection<string> add, IReadOnlyCollection<string> remove, CancellationToken token) => throw new NotSupportedException();
        public Task SetPasswordAsync(string id, string password, bool temporary, CancellationToken token) { if (FailPassword) throw new IdentityUnavailableException(); Temporary[id] = temporary; return Task.CompletedTask; }
        public Task SendActionsEmailAsync(string id, IReadOnlyCollection<string> actions, CancellationToken token) => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken token) { Users.Remove(id); return Task.CompletedTask; }
    }
}
