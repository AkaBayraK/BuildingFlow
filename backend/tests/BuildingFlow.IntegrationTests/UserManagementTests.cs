using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed partial class UserManagementTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    [PostgresTheory] [InlineData("MANAGER")] [InlineData("ACCOUNTANT")] [InlineData("RESIDENT")] [InlineData("")]
    public async Task OnlySuperAndSiteAdminsReachUserManagement(string role)
    {
        using var client = TestToken.Client(factory, Guid.NewGuid().ToString(), role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/users", new CreateUserRequest("x", "x@example.test", "A", "B"))).StatusCode);
    }

    [PostgresFact] public async Task UnconfiguredIdentityServerReturnsCodedUnavailable()
    {
        using var client = TestToken.Client(factory, Guid.NewGuid().ToString(), "SUPER_ADMIN");
        var response = await client.GetAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("BF_IDENTITY_UNAVAILABLE", await response.Content.ReadAsStringAsync());
    }

    [PostgresFact] public async Task SuperAdminCreatesUserWithRolesMembershipAndResetEmail()
    {
        var x = await Seed(); var keycloak = new FakeKeycloak();
        await WithService(keycloak, x.Actor, "SUPER_ADMIN", async service => {
            var created = await service.CreateAsync(new CreateUserRequest(" Ayse.Yilmaz ", "Ayse@Example.test", "Ayşe", "Yılmaz", true, ["SITE_ADMIN"], x.Property), default);
            Assert.Equal("ayse.yilmaz", created.Username); Assert.Equal("ayse@example.test", created.Email); Assert.Equal(["SITE_ADMIN"], created.Roles); Assert.Equal(1, created.PropertyCount);
            Assert.Equal(["UPDATE_PASSWORD"], keycloak.Emails[created.Id]);
            await Assert.ThrowsAsync<HierarchyConflictException>(() => service.CreateAsync(new CreateUserRequest("ayse.yilmaz", "other@example.test", "A", "B"), default));
            await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateAsync(new CreateUserRequest("Bad User", "a@example.test", "A", "B"), default));
            await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateAsync(new CreateUserRequest("weak.pw", "w@example.test", "A", "B", TemporaryPassword: "short1A"), default));
            await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateAsync(new CreateUserRequest("bad.role", "r@example.test", "A", "B", Roles: ["ROOT"]), default));
            var page = await service.ListAsync(1, 20, "ayse", default);
            Assert.Equal(created.Id, Assert.Single(page.Items).Id);
        });
        await factory.WithDb(async db => Assert.True(await db.PropertyMemberships.AnyAsync(m => m.PropertyId == x.Property && m.UserId == keycloak.Users.Values.Single().Id && m.IsEnabled)));
    }

    [PostgresFact] public async Task FailedMembershipStepRemovesCreatedIdentity()
    {
        var x = await Seed(); var keycloak = new FakeKeycloak { FailRoles = true };
        await WithService(keycloak, x.Actor, "SUPER_ADMIN", async service =>
            await Assert.ThrowsAsync<IdentityUnavailableException>(() => service.CreateAsync(new CreateUserRequest("orphan", "o@example.test", "O", "P", true, ["RESIDENT"], x.Property), default)));
        Assert.Empty(keycloak.Users);
    }

    [PostgresFact] public async Task SiteAdminIsScopedAndCannotEscalate()
    {
        var x = await Seed(); var other = await Seed(); var keycloak = new FakeKeycloak();
        var own = keycloak.Add("own.resident", ["RESIDENT"]); var foreign = keycloak.Add("foreign.user", ["RESIDENT"]); var peer = keycloak.Add("peer.admin", ["SITE_ADMIN"]);
        await factory.WithDb(async db => { db.AddRange(new PropertyMembership(x.Property, own), new PropertyMembership(other.Property, foreign), new PropertyMembership(x.Property, peer)); await db.SaveChangesAsync(); });
        await WithService(keycloak, x.Actor, "SITE_ADMIN", async service => {
            var page = await service.ListAsync(1, 20, null, default);
            Assert.Equal(new[] { own, peer }.Order(), page.Items.Select(u => u.Id).Order());
            Assert.False(page.Items.Single(u => u.Id == peer).CanEdit);
            Assert.DoesNotContain((await service.ListAsync(1, 20, "user", default)).Items, u => u.Id == foreign);
            Assert.Null(await service.GetAsync(foreign, default));
            await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateAsync(new CreateUserRequest("new.admin", "n@example.test", "N", "A", true, ["SITE_ADMIN"], x.Property), default));
            await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateAsync(new CreateUserRequest("no.property", "p@example.test", "N", "A", true, ["RESIDENT"]), default));
            await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateAsync(new CreateUserRequest("wrong.property", "w@example.test", "N", "A", true, ["RESIDENT"], other.Property), default));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateAsync(peer, new UpdateUserRequest("p@example.test", "P", "A", false, []), default));
            Assert.Null(await service.UpdateAsync(foreign, new UpdateUserRequest("f@example.test", "F", "U", false, []), default));
            var updated = (await service.UpdateAsync(own, new UpdateUserRequest("own@example.test", "Own", "Resident", true, ["MANAGER"]), default))!;
            Assert.Equal(["MANAGER"], updated.Roles);
        });
    }

    [PostgresFact] public async Task SuperAdminCannotLockThemselvesOut()
    {
        var x = await Seed(); var keycloak = new FakeKeycloak(); keycloak.Add("self", ["SUPER_ADMIN"], x.Actor);
        await WithService(keycloak, x.Actor, "SUPER_ADMIN", async service => {
            await Assert.ThrowsAsync<RequestValidationException>(() => service.UpdateAsync(x.Actor, new UpdateUserRequest("s@example.test", "S", "A", false, ["SUPER_ADMIN"]), default));
            await Assert.ThrowsAsync<RequestValidationException>(() => service.UpdateAsync(x.Actor, new UpdateUserRequest("s@example.test", "S", "A", true, []), default));
        });
    }

    [PostgresFact] public async Task LinkAndUnlinkPersonWithMembershipAndConflicts()
    {
        var x = await Seed(); var keycloak = new FakeKeycloak(); var user = keycloak.Add("tenant.user", []); var second = keycloak.Add("second.user", []);
        Person ada = null!, bob = null!;
        Person bare = null!;
        await factory.WithDb(async db => { ada = Profiled(x.Property, "Ada", "Kaya", "12345678901"); bob = Profiled(x.Property, "Bob", "Demir", "12345678902"); bare = new Person(x.Property, "Bare", "Person"); db.AddRange(ada, bob, bare); await db.SaveChangesAsync(); });
        await WithService(keycloak, x.Actor, "SUPER_ADMIN", async service => {
            await Assert.ThrowsAsync<HierarchyConflictException>(() => service.LinkAsync(user, new LinkPersonRequest(x.Property, ada.Id, ada.Version + 1), default));
            var linked = (await service.LinkAsync(user, new LinkPersonRequest(x.Property, ada.Id, ada.Version), default))!;
            var link = Assert.Single(linked.LinkedPeople); Assert.Equal(ada.Id, link.PersonId); Assert.Equal("Ada Kaya", link.FullName); Assert.Equal(1, linked.PropertyCount);
            await Assert.ThrowsAsync<HierarchyConflictException>(() => service.LinkAsync(second, new LinkPersonRequest(x.Property, ada.Id, link.Version), default));
            await Assert.ThrowsAsync<HierarchyConflictException>(() => service.LinkAsync(user, new LinkPersonRequest(x.Property, bob.Id, bob.Version), default));
            Assert.Null(await service.LinkAsync("missing", new LinkPersonRequest(x.Property, bob.Id, bob.Version), default));
            await Assert.ThrowsAsync<RequestValidationException>(() => service.LinkAsync(second, new LinkPersonRequest(x.Property, bare.Id, bare.Version), default));
            Assert.False(await service.UnlinkAsync(second, x.Property, ada.Id, link.Version, default));
            Assert.True(await service.UnlinkAsync(user, x.Property, ada.Id, link.Version, default));
            Assert.Empty((await service.GetAsync(user, default))!.LinkedPeople);
        });
        await factory.WithDb(async db => {
            Assert.Null((await db.People.SingleAsync(p => p.Id == ada.Id)).LinkedUserId);
            Assert.True(await db.PropertyMemberships.AnyAsync(m => m.PropertyId == x.Property && m.UserId == user));
        });
    }

    [PostgresFact] public async Task SiteAdminCannotLinkOutsideOwnProperties()
    {
        var x = await Seed(); var other = await Seed(); var keycloak = new FakeKeycloak(); var user = keycloak.Add("member", []);
        Person foreignPerson = null!;
        await factory.WithDb(async db => { db.Add(new PropertyMembership(x.Property, user)); foreignPerson = new Person(other.Property, "F", "P"); db.Add(foreignPerson); await db.SaveChangesAsync(); });
        await WithService(keycloak, x.Actor, "SITE_ADMIN", async service => {
            Assert.Null(await service.LinkAsync(user, new LinkPersonRequest(other.Property, foreignPerson.Id, foreignPerson.Version), default));
            Assert.Null(await service.LinkAsync(user, new LinkPersonRequest(x.Property, foreignPerson.Id, foreignPerson.Version), default));
            Assert.False(await service.UnlinkAsync(user, other.Property, foreignPerson.Id, foreignPerson.Version, default));
        });
    }

    [Fact] public async Task KeycloakClientCachesTokenRetriesOnceAndMapsErrors()
    {
        var handler = new RecordingHandler(); var client = new KeycloakAdminClient(new KeycloakAdminOptions { BaseUrl = "http://kc.test", ClientSecret = new string('x', 32) }, new HttpClient(handler), TimeProvider.System);
        handler.Respond = request => request.RequestUri!.AbsolutePath switch {
            "/realms/buildingflow/protocol/openid-connect/token" => Json(new { access_token = "t" + handler.Tokens++, expires_in = 300 }),
            "/admin/realms/buildingflow/users/count" => Json(3),
            "/admin/realms/buildingflow/users" when request.Method == HttpMethod.Post => new HttpResponseMessage(HttpStatusCode.Conflict),
            "/admin/realms/buildingflow/users" => Json(new[] { new { id = "u1", username = "ada", email = "a@example.test", enabled = true, emailVerified = false, createdTimestamp = 1700000000000 } }),
            "/admin/realms/buildingflow/users/missing" => new HttpResponseMessage(HttpStatusCode.NotFound),
            "/admin/realms/buildingflow/users/expired" => handler.Unauthorized++ == 0 ? new HttpResponseMessage(HttpStatusCode.Unauthorized) : Json(new { id = "expired", username = "e", enabled = true }),
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };
        var (users, total) = await client.SearchAsync("a d", 0, 20, default);
        Assert.Equal(3, total); Assert.Equal("ada", Assert.Single(users).Username); Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000), users[0].CreatedAt);
        Assert.Contains(handler.Urls, u => u.Contains("search=a%20d") && u.Contains("max=20"));
        Assert.Equal(1, handler.Tokens);
        Assert.Null(await client.GetAsync("missing", default));
        Assert.Equal("expired", (await client.GetAsync("expired", default))!.Id); Assert.Equal(2, handler.Tokens);
        await Assert.ThrowsAsync<IdentityConflictException>(() => client.CreateAsync(new("ada", "a@example.test", "A", "K", true), default));
        await Assert.ThrowsAsync<IdentityUnavailableException>(() => client.RealmRolesAsync("boom", default));
        Assert.All(handler.Urls.Zip(handler.Authorizations).Where(pair => pair.First.Contains("/admin/")), pair => Assert.StartsWith("Bearer t", pair.Second));
        await Assert.ThrowsAsync<IdentityUnavailableException>(() => new KeycloakAdminClient(new KeycloakAdminOptions(), new HttpClient(handler), TimeProvider.System).GetAsync("x", default));
    }

    private static Person Profiled(Guid property, string first, string last, string identity)
    {
        var person = new Person(property, first, last, "+905551234567", first.ToLowerInvariant() + "@example.test");
        person.SetProfile("NATURAL", "DOMESTIC", "TR", identity, null, null, null, null); return person;
    }
    [Fact] public async Task KeycloakRoleChangesUseUserScopedRoleListsOnly()
    {
        var handler = new RecordingHandler(); var client = new KeycloakAdminClient(new KeycloakAdminOptions { BaseUrl = "http://kc.test", ClientSecret = new string('x', 32) }, new HttpClient(handler), TimeProvider.System);
        var bodies = new List<string>();
        handler.Respond = request => {
            if (request.Content is not null && request.RequestUri!.AbsolutePath.EndsWith("/role-mappings/realm")) bodies.Add(request.Method + ":" + request.Content.ReadAsStringAsync().Result);
            return request.RequestUri!.AbsolutePath switch {
                "/realms/buildingflow/protocol/openid-connect/token" => Json(new { access_token = "t", expires_in = 300 }),
                "/admin/realms/buildingflow/users/u1/role-mappings/realm/available" => Json(new[] { new { id = "r-res", name = "RESIDENT" }, new { id = "r-man", name = "MANAGER" } }),
                "/admin/realms/buildingflow/users/u1/role-mappings/realm" when request.Method == HttpMethod.Get => Json(new[] { new { id = "r-acc", name = "ACCOUNTANT" } }),
                "/admin/realms/buildingflow/users/u1/role-mappings/realm" => new HttpResponseMessage(HttpStatusCode.NoContent),
                _ => new HttpResponseMessage(HttpStatusCode.Forbidden)
            };
        };
        await client.ChangeRealmRolesAsync("u1", ["RESIDENT"], ["ACCOUNTANT"], default);
        Assert.DoesNotContain(handler.Urls, url => url.Contains("/admin/realms/buildingflow/roles/"));
        Assert.Contains(bodies, b => b.StartsWith("POST") && b.Contains("r-res") && !b.Contains("r-man"));
        Assert.Contains(bodies, b => b.StartsWith("DELETE") && b.Contains("r-acc"));
        await Assert.ThrowsAsync<IdentityUnavailableException>(() => client.ChangeRealmRolesAsync("u1", ["SUPER_ADMIN"], [], default));
    }

    // Opt-in, read-only check against a real Keycloak: set BUILDINGFLOW_TEST_KEYCLOAK_ADMIN_URL and _SECRET.
    [Fact] public async Task RealKeycloakServiceAccountCanSearchUsers()
    {
        var url = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_KEYCLOAK_ADMIN_URL"); var secret = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_KEYCLOAK_ADMIN_SECRET");
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(secret)) return;
        var client = new KeycloakAdminClient(new KeycloakAdminOptions { BaseUrl = url, ClientSecret = secret }, new HttpClient(), TimeProvider.System);
        var (users, total) = await client.SearchAsync(null, 0, 5, default);
        Assert.True(total >= users.Count);
        if (users.Count > 0) Assert.NotNull(await client.RealmRolesAsync(users[0].Id, default));
    }
    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond = _ => new(HttpStatusCode.NotFound);
        public int Tokens, Unauthorized; public List<string> Urls = []; public List<string> Authorizations = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Urls.Add(request.RequestUri!.AbsoluteUri); Authorizations.Add(request.Headers.Authorization?.ToString() ?? ""); return Task.FromResult(Respond(request)); }
    }

    private async Task WithService(FakeKeycloak keycloak, string actor, string role, Func<UserService, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        await action(new UserService(keycloak, new UserDirectoryStore(scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>()), new Actor(actor, role), scope.ServiceProvider.GetRequiredService<IMobileProfileStore>()));
    }

    // İŞ19: an account photo added by its owner (with consent) is listed and readable only for accounts in the manager's scope.
    [StorageFact] public async Task UserPhotoIsVisibleOnlyWithinManagerScope()
    {
        var x = await Seed(); var other = await Seed(); var keycloak = new FakeKeycloak();
        keycloak.Add("admin", ["SITE_ADMIN"], x.Actor); var own = keycloak.Add("own.photo", ["RESIDENT"]); var foreign = keycloak.Add("foreign.photo", ["RESIDENT"]);
        await factory.WithDb(async db => { db.AddRange(new PropertyMembership(x.Property, own), new PropertyMembership(other.Property, foreign)); await db.SaveChangesAsync(); });
        var bytes = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "avatar.jpg"));
        foreach (var id in new[] { own, foreign })
        {
            using var c = TestToken.Client(factory, id, "RESIDENT"); await ConsentTestHelper.GrantAsync(factory, c, "PROFILE_PHOTO");
            var saved = await c.PostAsJsonAsync("/api/v1/account/profile/commands", new MobileProfileCommand(Guid.NewGuid(), 0, "Photo", "Owner", null, "REPLACE", Convert.ToBase64String(bytes)));
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        }
        await WithService(keycloak, x.Actor, "SITE_ADMIN", async service => {
            var listed = (await service.ListAsync(1, 20, null, default)).Items.Single(u => u.Id == own); Assert.NotNull(listed.PhotoId);
            Assert.NotNull(await service.PhotoAsync(own, default)); Assert.Null(await service.PhotoAsync(foreign, default));
        });
        await WithService(keycloak, x.Actor, "SUPER_ADMIN", async service => Assert.NotNull(await service.PhotoAsync(foreign, default)));
    }
    private async Task<(string Actor, Guid Property)> Seed()
    {
        var actor = Guid.NewGuid().ToString(); var property = new Property("Users " + actor[..8], "City", "District", "Address", PropertyType.APARTMENT);
        await factory.WithDb(async db => { db.Add(property); db.Add(new PropertyMembership(property.Id, actor)); await db.SaveChangesAsync(); });
        return (actor, property.Id);
    }
    private sealed class Actor(string id, string role) : ICurrentUserService
    {
        public string? UserId => id; public string? Username => id; public bool IsAuthenticated => true; public bool IsInRole(string value) => value == role;
    }

    private sealed class FakeKeycloak : IKeycloakAdmin
    {
        public Dictionary<string, KeycloakUser> Users { get; } = [];
        public Dictionary<string, HashSet<string>> Roles { get; } = [];
        public Dictionary<string, IReadOnlyCollection<string>> Emails { get; } = [];
        public bool FailRoles { get; init; }
        public string Add(string username, IEnumerable<string> roles, string? id = null)
        {
            id ??= Guid.NewGuid().ToString(); Users[id] = new(id, username, username + "@example.test", "F", "L", true, false, DateTimeOffset.UtcNow); Roles[id] = roles.ToHashSet(); return id;
        }
        public Task<(IReadOnlyList<KeycloakUser> Users, int Total)> SearchAsync(string? search, int first, int max, CancellationToken token)
        {
            var all = Users.Values.Where(u => search is null || u.Username.Contains(search) || (u.Email ?? "").Contains(search)).OrderBy(u => u.Username).ToArray();
            return Task.FromResult(((IReadOnlyList<KeycloakUser>)all.Skip(first).Take(max).ToArray(), all.Length));
        }
        public Task<KeycloakUser?> GetAsync(string id, CancellationToken token) => Task.FromResult(Users.GetValueOrDefault(id));
        public Task<IReadOnlyList<string>> RealmRolesAsync(string id, CancellationToken token) => Task.FromResult<IReadOnlyList<string>>(Roles.TryGetValue(id, out var r) ? r.Concat(["default-roles-buildingflow"]).ToArray() : []);
        public Task<string> CreateAsync(KeycloakUserDraft draft, CancellationToken token)
        {
            if (Users.Values.Any(u => u.Username == draft.Username || u.Email == draft.Email)) throw new IdentityConflictException();
            var id = Add(draft.Username, []); Users[id] = Users[id] with { Email = draft.Email, FirstName = draft.FirstName, LastName = draft.LastName }; return Task.FromResult(id);
        }
        public Task UpdateAsync(string id, KeycloakUserDraft draft, CancellationToken token) { Users[id] = Users[id] with { Email = draft.Email, FirstName = draft.FirstName, LastName = draft.LastName, Enabled = draft.Enabled }; return Task.CompletedTask; }
        public Task ChangeRealmRolesAsync(string id, IReadOnlyCollection<string> add, IReadOnlyCollection<string> remove, CancellationToken token)
        { if (FailRoles) throw new IdentityUnavailableException(); Roles[id].UnionWith(add); Roles[id].ExceptWith(remove); return Task.CompletedTask; }
        public Task SetPasswordAsync(string id, string password, bool temporary, CancellationToken token) => Task.CompletedTask;
        public Task SendActionsEmailAsync(string id, IReadOnlyCollection<string> actions, CancellationToken token) { Emails[id] = actions; return Task.CompletedTask; }
        public Task DeleteAsync(string id, CancellationToken token) { Users.Remove(id); Roles.Remove(id); return Task.CompletedTask; }
    }
}
