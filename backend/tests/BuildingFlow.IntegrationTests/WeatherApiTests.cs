using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class WeatherApiTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private sealed class FakeWeather:IWeatherProvider{public Task<WeatherSnapshot> Get(string city,CancellationToken token){var now=DateTimeOffset.UtcNow;return Task.FromResult(new WeatherSnapshot("İstanbul","FRESH",18.5m,"partlycloudy_day",now,now,now,now.AddMinutes(30)));}}
    [PostgresTheory][InlineData("SUPER_ADMIN")][InlineData("SITE_ADMIN")][InlineData("MANAGER")][InlineData("ACCOUNTANT")]
    public async Task FinanceRolesCanReadPersonalCityWithoutPropertyOrProviderCredentials(string role)
    {
        using var app=factory.WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.RemoveAll<IWeatherProvider>();s.AddSingleton<IWeatherProvider,FakeWeather>();}));
        using var signed=TestToken.Client(factory,Guid.NewGuid().ToString(),role);using var client=app.CreateClient();client.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;
        var response=await client.GetAsync("/api/v1/weather?city=istanbul");Assert.Equal(HttpStatusCode.OK,response.StatusCode);var value=(await response.Content.ReadFromJsonAsync<WeatherSnapshot>())!;Assert.Equal("İstanbul",value.City);Assert.Equal(18.5m,value.TemperatureC);
        var bad=await client.GetAsync("/api/v1/weather?city=https%3A%2F%2Flocalhost");Assert.Equal(HttpStatusCode.BadRequest,bad.StatusCode);Assert.Contains("BF_INVALID_REQUEST",await bad.Content.ReadAsStringAsync());
        Assert.Equal("no-store",response.Headers.CacheControl?.ToString());
    }
    [PostgresFact] public async Task AnonymousResidentAndSpoofedRoleAreRejectedBeforeAnyProviderCall()
    {
        using var anonymous=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync("/api/v1/weather?city=istanbul")).StatusCode);
        foreach(var role in new[]{"RESIDENT","spoofed"}){using var c=TestToken.Client(factory,Guid.NewGuid().ToString(),role);Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync("/api/v1/weather?city=istanbul")).StatusCode);}
    }
}
