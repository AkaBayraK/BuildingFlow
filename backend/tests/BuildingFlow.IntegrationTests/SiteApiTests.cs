using Amazon.S3;
using Amazon.S3.Model;
using BuildingFlow.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;

namespace BuildingFlow.IntegrationTests;

public sealed class SiteApiTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private const string Issuer = "http://localhost:8080/realms/buildingflow";
    private static readonly SymmetricSecurityKey Key = new(Encoding.UTF8.GetBytes(new string('s', 64))) { KeyId = "sites-test-only" };
    private static readonly CreatePropertyRequest Valid = new("Çınar Sitesi", "İstanbul", "Kadıköy", "Test Mahallesi 10", "SITE");
    private HttpClient Client(string? user = null, string role = "", bool withSubject = true)
    {
        var client = factory.CreateClient();
        if (user is null) return client;
        List<Claim> claims = [];
        if (withSubject) claims.Add(new("sub", user));
        if (role == "client-role") claims.Add(new("resource_access", "{\"buildingflow-web\":{\"roles\":[\"SUPER_ADMIN\"]}}", JsonClaimValueTypes.Json));
        else if (role == "spoofed") { claims.Add(new("buildingflow_role", "SUPER_ADMIN")); claims.Add(new("role", "SUPER_ADMIN")); }
        else claims.Add(new("realm_access", JsonSerializer.Serialize(new { roles = role.Length > 0 ? new[] { role } : [] }), JsonClaimValueTypes.Json));
        var jwt = new JwtSecurityToken(Issuer, "buildingflow-api", claims, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5), new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        return client;
    }

    [PostgresFact]
    public async Task AnonymousCannotCreateListOrRead()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/properties", Valid)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/properties")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/properties/{Guid.NewGuid()}")).StatusCode);
    }

    [PostgresTheory]
    [InlineData("SITE_ADMIN")][InlineData("MANAGER")][InlineData("ACCOUNTANT")][InlineData("RESIDENT")][InlineData("")][InlineData("client-role")][InlineData("spoofed")]
    public async Task OnlySignedSuperAdminRealmRoleCanCreate(string role)
    {
        using var client = Client(Guid.NewGuid().ToString(), role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/properties", Valid)).StatusCode);
    }

    [PostgresFact]
    public async Task CreatesSiteAndMembershipWithTrustedAuditAndLocation()
    {
        var actor = Guid.NewGuid().ToString();
        using var client = Client(actor, "SUPER_ADMIN");
        using var response = await client.PostAsJsonAsync("/api/v1/properties", new
        {
            type = "SITE", name = "  Çınar Sitesi  ", city = " İstanbul ", district = " Kadıköy ", address = " Test Mahallesi 10 ",
            id = Guid.NewGuid(), createdBy = "attacker", modifiedBy = "attacker", version = 999, isDeleted = true, userId = "attacker"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var site = (await response.Content.ReadFromJsonAsync<PropertyResponse>())!;
        Assert.Equal($"/api/v1/properties/{site.Id}", response.Headers.Location!.ToString());
        Assert.Equal("Çınar Sitesi", site.Name); Assert.Equal("İstanbul", site.City);
        Assert.Equal(1, site.Version); Assert.True(site.IsEnabled); Assert.True(site.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
        await factory.WithDb(async db =>
        {
            var saved = await db.Properties.SingleAsync(value => value.Id == site.Id);
            var membership = await db.PropertyMemberships.SingleAsync(value => value.PropertyId == site.Id);
            Assert.Equal(actor, saved.CreatedBy); Assert.Equal(actor, saved.ModifiedBy);
            Assert.False(saved.IsDeleted); Assert.Equal(actor, membership.UserId); Assert.Equal(actor, membership.CreatedBy);
        });
        // An explicit creator membership still grants read access after losing the global role.
        using var member = Client(actor, "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync(response.Headers.Location)).StatusCode);
        var list = (await member.GetFromJsonAsync<PagedProperties>("/api/v1/properties"))!;
        Assert.Equal(site.Id, Assert.Single(list.Items).Id);
    }

    [PostgresTheory]
    [InlineData("name", false)][InlineData("city", false)][InlineData("district", false)][InlineData("address", false)]
    [InlineData("name", true)][InlineData("city", true)][InlineData("district", true)][InlineData("address", true)]
    public async Task RejectsBlankOrOverlongFieldsWithValidationProblem(string field, bool tooLong)
    {
        var actor = Guid.NewGuid().ToString();
        using var client = Client(actor, "SUPER_ADMIN");
        var body = new Dictionary<string, string> { ["type"]="SITE", ["name"]="Valid", ["city"]="City", ["district"]="District", ["address"]="Address" };
        body[field] = tooLong ? new string('x', field == "name" ? 201 : field == "address" ? 501 : 101) : "  ";
        using var response = await client.PostAsJsonAsync("/api/v1/properties", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(error.RootElement.GetProperty("errors").TryGetProperty(field, out _));
        await factory.WithDb(async db => Assert.False(await db.Properties.AnyAsync(site => site.CreatedBy == actor)));
    }

    [PostgresFact]
    public async Task MissingFieldsAreReportedTogether()
    {
        using var client = Client(Guid.NewGuid().ToString(), "SUPER_ADMIN");
        using var response = await client.PostAsJsonAsync("/api/v1/properties", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(5, body.RootElement.GetProperty("errors").EnumerateObject().Count());
    }

    [PostgresTheory]
    [InlineData(0, 12)][InlineData(-1, 12)][InlineData(1, 0)][InlineData(1, 101)][InlineData(1000001, 12)]
    public async Task RejectsInvalidPagination(int page, int size)
    {
        using var client = Client(Guid.NewGuid().ToString(), "RESIDENT");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/properties?page={page}&pageSize={size}")).StatusCode);
    }

    [PostgresFact]
    public async Task MembershipScopePreventsCrossSiteAccessAndQueryBypass()
    {
        var memberId = Guid.NewGuid().ToString();
        var outsiderId = Guid.NewGuid().ToString();
        var site = new Property("Private", "City", "District", "Private Address");
        await factory.WithDb(async db => { db.Add(site); db.Add(new PropertyMembership(site.Id, memberId)); await db.SaveChangesAsync(); });
        using var member = Client(memberId, "RESIDENT");
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/api/v1/properties/{site.Id}")).StatusCode);
        using var outsider = Client(outsiderId, "SITE_ADMIN");
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/v1/properties/{site.Id}")).StatusCode);
        var list = (await outsider.GetFromJsonAsync<PagedProperties>($"/api/v1/properties?userId={memberId}&readAll=true"))!;
        Assert.Empty(list.Items); Assert.Equal(0, list.TotalCount);
        using var admin = Client(Guid.NewGuid().ToString(), "SUPER_ADMIN");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/properties/{site.Id}")).StatusCode);
    }

    [PostgresFact]
    public async Task PaginationIsStableWhenNamesAreEqual()
    {
        var user = Guid.NewGuid().ToString();
        var sites = Enumerable.Range(0,4).Select(_ => new Property("Same name", "City", "District", "Address")).ToArray();
        await factory.WithDb(async db => { db.AddRange(sites); db.AddRange(sites.Select(site => new PropertyMembership(site.Id,user))); await db.SaveChangesAsync(); });
        using var client = Client(user, "MANAGER");
        var first = (await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties?page=1&pageSize=2"))!;
        var second = (await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties?page=2&pageSize=2"))!;
        Assert.Equal(4, first.TotalCount); Assert.Equal(2, first.Items.Count); Assert.Equal(2, second.Page);
        Assert.Equal(sites.OrderBy(site => site.Id).Select(site => site.Id), first.Items.Concat(second.Items).Select(site => site.Id));
    }

    [PostgresFact]
    public async Task SoftDeletedSiteIsHiddenEvenFromSuperAdmin()
    {
        var user = Guid.NewGuid().ToString();
        var site = new Property("Deleted", "City", "District", "Address");
        await factory.WithDb(async db => { db.Add(site); db.Add(new PropertyMembership(site.Id,user)); await db.SaveChangesAsync(); db.Remove(site); await db.SaveChangesAsync(); });
        foreach (var role in new[] {"RESIDENT", "SUPER_ADMIN"})
        {
            using var client = Client(user,role);
            Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/v1/properties/{site.Id}")).StatusCode);
            var list = (await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties?pageSize=100"))!;
            Assert.DoesNotContain(list.Items,value=>value.Id==site.Id);
        }
    }

    [PostgresFact]
    public async Task DisabledOrDeletedMembershipDoesNotGrantAccess()
    {
        var user=Guid.NewGuid().ToString();
        var sites=new[] {new Property("Disabled access", "City", "District", "Address"),new Property("Deleted access", "City", "District", "Address")};
        await factory.WithDb(async db => {
            db.AddRange(sites);
            var disabled=new PropertyMembership(sites[0].Id,user); disabled.SetEnabled(false);
            var deleted=new PropertyMembership(sites[1].Id,user);
            db.AddRange(disabled,deleted);await db.SaveChangesAsync();db.Remove(deleted);await db.SaveChangesAsync();
        });
        using var client=Client(user,"SITE_ADMIN");
        Assert.Empty((await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties"))!.Items);
        foreach(var site in sites)Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/v1/properties/{site.Id}")).StatusCode);
    }

    [PostgresFact]
    public async Task MissingSiteReturns404()
    {
        using var client=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN");
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/v1/properties/{Guid.NewGuid()}")).StatusCode);
    }

    [PostgresFact]
    public async Task TokenWithoutStableSubjectCannotAccessSites()
    {
        using var client=Client("ignored","SUPER_ADMIN",false);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/v1/properties",Valid)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/v1/properties")).StatusCode);
    }


    [PostgresFact]
    public async Task SearchSortingAndPaginationKeepMembershipScope()
    {
        var user=Guid.NewGuid().ToString();
        var first=new Property("Alpha Garden", "Zulu", "District", "Address");
        var second=new Property("Alpha_Residence", "Alpha", "District", "Address");
        var hidden=new Property("Alpha Secret", "Beta", "District", "Address");
        var percent=new Property("100% Life", "Gamma", "District", "Address");
        await factory.WithDb(async db=>{
            db.AddRange(first,second,hidden,percent);
            db.AddRange(new PropertyMembership(first.Id,user),new PropertyMembership(second.Id,user),new PropertyMembership(percent.Id,user));
            await db.SaveChangesAsync();
        });
        using var client=Client(user,"RESIDENT");
        var sorted=(await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties?search=alpha&sortBy=city&sortDirection=desc&pageSize=1"))!;
        Assert.Equal(2,sorted.TotalCount);Assert.Equal(first.Id,Assert.Single(sorted.Items).Id);
        var next=(await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties?search=alpha&sortBy=city&sortDirection=desc&pageSize=1&page=2"))!;
        Assert.Equal(second.Id,Assert.Single(next.Items).Id);
        var underscore=(await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties?search=%5F"))!;
        Assert.Equal(second.Id,Assert.Single(underscore.Items).Id);
        var literalPercent=(await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties?search=%25"))!;
        Assert.Equal(percent.Id,Assert.Single(literalPercent.Items).Id);
        var empty=(await client.GetFromJsonAsync<PagedProperties>("/api/v1/properties?search=not-found"))!;Assert.Empty(empty.Items);
    }

    [PostgresTheory]
    [InlineData("sortBy", "address")][InlineData("sortBy", "name;DROP TABLE sites")][InlineData("sortDirection", "sideways")]
    public async Task UnrecognizedSortParametersAreRejected(string field,string value)
    {
        using var client=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync($"/api/v1/properties?{field}={Uri.EscapeDataString(value)}")).StatusCode);
    }

    [PostgresFact]
    public async Task OverlongSearchIsRejected()
    {
        using var client=Client(Guid.NewGuid().ToString(),"SUPER_ADMIN");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/v1/properties?search="+new string('a',201))).StatusCode);
    }

    public sealed class SiteApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        // Migrating or dropping a whole schema takes a lock per object; unbounded parallel classes exhausted the shared server's lock table (53200).
        private static readonly SemaphoreSlim SchemaGate=new(4);
        private readonly string schema="sites_api_"+Guid.NewGuid().ToString("N");
        private string? connection;
        public string StorageBucket => "buildingflow-test-" + schema.Replace("_", "-");
        public async Task InitializeAsync()
        {
            var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE");
            if (string.IsNullOrEmpty(original)) return;
            var config=new NpgsqlConnectionStringBuilder(original);
            if (config.Database is null || !config.Database.Contains("test",StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Use a dedicated test database.");
            await using var database=new NpgsqlConnection(original);await database.OpenAsync();
            await using var command=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",database);await command.ExecuteNonQueryAsync();
            config.SearchPath=schema+",public";connection=config.ConnectionString;
            await using var db=new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(connection, options=>options.MigrationsHistoryTable("__EFMigrationsHistory",schema)).Options);
            await SchemaGate.WaitAsync();try{await db.Database.MigrateAsync();}finally{SchemaGate.Release();}
        }
        public async Task WithDb(Func<BuildingFlowDbContext,Task> action)
        {
            using var scope=Services.CreateScope();
            await action(scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>());
        }
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(config=>config.AddInMemoryCollection(new Dictionary<string,string?> {
                ["ConnectionStrings:Database"]=connection ?? "Host=localhost;Database=unused_sites_tests",
                ["Database:AutoMigrate"]="false",
                ["Authentication:Authority"]=Issuer,["Authentication:Audience"]="buildingflow-api",["Authentication:MetadataAddress"]="",
                ["ObjectStorage:Endpoint"]=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_STORAGE_ENDPOINT"),
                ["ObjectStorage:AccessKey"]=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_STORAGE_ACCESS_KEY"),
                ["ObjectStorage:SecretKey"]=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_STORAGE_SECRET_KEY"),
                ["ObjectStorage:Bucket"]=StorageBucket,
                ["ObjectStorage:Region"]="us-east-1"
            }));
            return base.CreateHost(builder);
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services=>services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,options=>{
                var metadata=new OpenIdConnectConfiguration {Issuer=Issuer};metadata.SigningKeys.Add(Key);
                options.ConfigurationManager=new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            }));
        }
        async Task IAsyncLifetime.DisposeAsync()
        {
            if (connection is not null && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_STORAGE_ENDPOINT"))) {
                // Only this fixture's random bucket is cleaned; production buckets are never queried.
                using var storage = StorageTestClient.Create();
                var exists = true;
                try { await storage.GetBucketLocationAsync(new GetBucketLocationRequest { BucketName = StorageBucket }); }
                catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound) { exists = false; }
                if (exists) {
                    string? next = null;
                    do {
                        var page = await storage.ListObjectsV2Async(new ListObjectsV2Request { BucketName = StorageBucket, ContinuationToken = next });
                        foreach (var item in page.S3Objects ?? [])
                            await storage.DeleteObjectAsync(new DeleteObjectRequest { BucketName = StorageBucket, Key = item.Key });
                        next = page.IsTruncated == true ? page.NextContinuationToken : null;
                    } while (next != null);
                    await storage.DeleteBucketAsync(new DeleteBucketRequest { BucketName = StorageBucket });
                }
            }
            await DisposeAsync();
            if (connection is null) return;
            await using var db=new NpgsqlConnection(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE"));await db.OpenAsync();
            await SchemaGate.WaitAsync();try{await using var command=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",db);await command.ExecuteNonQueryAsync();}finally{SchemaGate.Release();}
        }
    }
}

public sealed class PostgresTheoryAttribute : TheoryAttribute
{
    public PostgresTheoryAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")))
            Skip="Set BUILDINGFLOW_TEST_DATABASE to a dedicated PostgreSQL test database.";
    }
}
