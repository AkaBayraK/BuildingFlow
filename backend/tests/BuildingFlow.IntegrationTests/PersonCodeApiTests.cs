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

public sealed class PersonCodeApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static readonly CreatePersonRequest Request = new("Ada", "Kaya", "+905551234567", "ada@example.test", "NATURAL", "DOMESTIC", "TR", "12345678901");

    [PostgresFact] public async Task CreateReturnsCodeOnceAndStoresOnlyKeyedHash()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "MANAGER");
        var response = await client.PostAsJsonAsync(Root(x.Property.Id) + "/people", Request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode); Assert.True(response.Headers.CacheControl!.NoStore);
        var created = (await response.Content.ReadFromJsonAsync<PersonResponse>())!;
        Assert.True(PersonCodePolicy.IsAcceptable(created.IssuedPersonCode!)); Assert.Equal(created.IssuedPersonCode![^2..], created.PersonCodeHint); Assert.NotNull(created.PersonCodeIssuedAt);
        var read = (await client.GetFromJsonAsync<PersonResponse>(Root(x.Property.Id) + "/people/" + created.Id))!;
        Assert.Null(read.IssuedPersonCode); Assert.Equal(created.PersonCodeHint, read.PersonCodeHint);
        var listed = await client.GetStringAsync(Root(x.Property.Id) + "/people?search=Ada");
        Assert.DoesNotContain(created.IssuedPersonCode!, listed);
        await factory.WithDb(async db => {
            var saved = await db.People.SingleAsync(value => value.Id == created.Id);
            Assert.Equal(Hasher().Hash(created.IssuedPersonCode!), saved.PersonCodeHash); Assert.NotEqual(created.IssuedPersonCode, saved.PersonCodeHash);
            var raw = await db.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM people WHERE id = {0} AND (person_code_hash = {1} OR person_code_hint = {1})", created.Id, created.IssuedPersonCode!).SingleAsync();
            Assert.Equal(0, raw);
        });
    }

    [PostgresFact] public async Task RegenerateReplacesCodeWithVersionCheck()
    {
        var x = await Seed(); using var client = TestToken.Client(factory, x.Actor, "SITE_ADMIN"); var root = Root(x.Property.Id);
        var created = (await (await client.PostAsJsonAsync(root + "/people", Request)).Content.ReadFromJsonAsync<PersonResponse>())!;
        var response = await client.PostAsJsonAsync($"{root}/people/{created.Id}/code", new RegeneratePersonCodeRequest(created.Version));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.True(response.Headers.CacheControl!.NoStore);
        var renewed = (await response.Content.ReadFromJsonAsync<PersonResponse>())!;
        Assert.NotEqual(created.IssuedPersonCode, renewed.IssuedPersonCode); Assert.Equal(created.Version + 1, renewed.Version);
        await factory.WithDb(async db => Assert.Equal(Hasher().Hash(renewed.IssuedPersonCode!), (await db.People.SingleAsync(value => value.Id == created.Id)).PersonCodeHash));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"{root}/people/{created.Id}/code", new RegeneratePersonCodeRequest(created.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"{root}/people/{created.Id}/code", new RegeneratePersonCodeRequest(0))).StatusCode);
    }

    [PostgresTheory] [InlineData("RESIDENT")] [InlineData("ACCOUNTANT")] [InlineData("")]
    public async Task OnlyPeopleManagersCanRegenerate(string role)
    {
        var x = await Seed(); using var manager = TestToken.Client(factory, x.Actor, "MANAGER"); using var client = TestToken.Client(factory, x.Actor, role);
        var created = (await (await manager.PostAsJsonAsync(Root(x.Property.Id) + "/people", Request)).Content.ReadFromJsonAsync<PersonResponse>())!;
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"{Root(x.Property.Id)}/people/{created.Id}/code", new RegeneratePersonCodeRequest(created.Version))).StatusCode);
    }

    [PostgresFact] public async Task ForeignPropertyCannotRegenerate()
    {
        var x = await Seed(); var other = await Seed(); using var global = TestToken.Client(factory, x.Actor, "SUPER_ADMIN"); using var own = TestToken.Client(factory, x.Actor, "MANAGER");
        var foreign = (await (await global.PostAsJsonAsync(Root(other.Property.Id) + "/people", Request)).Content.ReadFromJsonAsync<PersonResponse>())!;
        Assert.Equal(HttpStatusCode.NotFound, (await own.PostAsJsonAsync($"{Root(other.Property.Id)}/people/{foreign.Id}/code", new RegeneratePersonCodeRequest(foreign.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await own.PostAsJsonAsync($"{Root(x.Property.Id)}/people/{foreign.Id}/code", new RegeneratePersonCodeRequest(foreign.Version))).StatusCode);
    }

    [PostgresFact] public async Task BackfillAssignsDistinctCodesOnlyToPeopleWithout()
    {
        var x = await Seed(); Person[] legacy = []; string? kept = null;
        await factory.WithDb(async db => {
            legacy = Enumerable.Range(0, 5).Select(i => new Person(x.Property.Id, "Legacy" + i, "Kaya")).ToArray(); db.AddRange(legacy);
            var coded = new Person(x.Property.Id, "Coded", "Kaya"); kept = Hasher().Hash("482917"); coded.AssignCode(kept, "17", DateTimeOffset.UtcNow); db.Add(coded);
            await db.SaveChangesAsync();
        });
        using (var scope = factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IPeopleStore>();
            while (await store.AssignMissingCodesAsync(2, default) > 0) { }
        }
        await factory.WithDb(async db => {
            var ids = legacy.Select(value => value.Id).ToArray();
            var saved = await db.People.Where(value => ids.Contains(value.Id)).ToListAsync();
            Assert.All(saved, value => { Assert.Matches("^[0-9a-f]{64}$", value.PersonCodeHash!); Assert.Matches("^[0-9]{2}$", value.PersonCodeHint!); Assert.Equal("system", value.ModifiedBy); });
            Assert.Equal(saved.Count, saved.Select(value => value.PersonCodeHash).Distinct().Count());
            Assert.True(await db.People.AnyAsync(value => value.PersonCodeHash == kept));
        });
    }

    private static PersonCodeHasher Hasher() => new(new ConfigurationBuilder().Build());
    private static string Root(Guid property) => $"/api/v1/properties/{property}";
    private async Task<(string Actor, Property Property)> Seed()
    {
        var actor = Guid.NewGuid().ToString(); var property = new Property("Codes " + actor, "City", "District", "Address", PropertyType.APARTMENT); property.AddUnit("1", 0, 100);
        await factory.WithDb(async db => { db.Add(property); db.Add(new PropertyMembership(property.Id, actor)); await db.SaveChangesAsync(); });
        return (actor, property);
    }
}
