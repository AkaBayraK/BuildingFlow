using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BuildingFlow.Infrastructure;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class MetWeatherTests
{
    private sealed class Clock:TimeProvider { public DateTimeOffset Now=new(2026,10,21,10,0,0,TimeSpan.Zero);public override DateTimeOffset GetUtcNow()=>Now; }
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> respond):HttpMessageHandler
    {public int Calls;protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){Calls++;Assert.Equal("api.met.no",request.RequestUri!.Host);return Task.FromResult(respond(request));}}
    private static string Json(DateTimeOffset now,decimal temperature=18.5m,string unit="celsius")=>JsonSerializer.Serialize(new{properties=new{meta=new{updated_at=now,units=new{air_temperature=unit}},timeseries=new[]{new{time=now,data=new{instant=new{details=new{air_temperature=temperature}},next_1_hours=new{summary=new{symbol_code="partlycloudy_day"}}}}}}});
    private static HttpResponseMessage Response(DateTimeOffset now,string json,int minutes=1){var r=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json)};r.Content.Headers.Expires=now.AddMinutes(minutes);r.Content.Headers.LastModified=now;return r;}
    [Fact] public async Task CitySingleFlightExpires304AndProviderBackoffPreserveLastSuccess()
    {
        var clock=new Clock();var initial=clock.Now;var stage=0;
        var handler=new Handler(request=>{
            if(stage==0){Assert.Null(request.Headers.IfModifiedSince);return Response(clock.Now,Json(clock.Now));}
            Assert.Equal(initial,request.Headers.IfModifiedSince);
            if(stage==1){var r=new HttpResponseMessage(HttpStatusCode.NotModified);r.Content.Headers.Expires=clock.Now.AddMinutes(2);return r;}
            var error=new HttpResponseMessage(HttpStatusCode.TooManyRequests);error.Headers.RetryAfter=new RetryConditionHeaderValue(TimeSpan.FromMinutes(20));return error;
        });
        var provider=new MetWeatherProvider(()=>new HttpClient(handler,false),clock);
        var rows=await Task.WhenAll(Enumerable.Range(0,20).Select(_=>provider.Get("istanbul",default)));Assert.Equal(1,handler.Calls);Assert.All(rows,r=>{Assert.Equal("FRESH",r.Status);Assert.Equal(18.5m,r.TemperatureC);Assert.Equal("İstanbul",r.City);});
        clock.Now=clock.Now.AddMinutes(2);stage=1;var fresh=await provider.Get("istanbul",default);Assert.Equal(2,handler.Calls);Assert.Equal("FRESH",fresh.Status);Assert.Equal(clock.Now,fresh.CheckedAt);
        clock.Now=clock.Now.AddMinutes(3);stage=2;var stale=await provider.Get("istanbul",default);Assert.Equal(3,handler.Calls);Assert.Equal("STALE",stale.Status);Assert.Equal(18.5m,stale.TemperatureC);Assert.Equal(clock.Now.AddMinutes(20),stale.NextRefreshAt);
        await provider.Get("istanbul",default);Assert.Equal(3,handler.Calls);
        await Assert.ThrowsAsync<ArgumentException>(()=>provider.Get("https://localhost",default));Assert.Equal(3,handler.Calls);
    }
    [Fact] public async Task UnavailableAndCorruptDataDoNotExposeProviderBodyOrRetryOnEveryClick()
    {
        var clock=new Clock();var handler=new Handler(_=>new(HttpStatusCode.OK){Content=new StringContent("NOT_JSON_PRIVATE_PROVIDER_DETAIL")});
        var provider=new MetWeatherProvider(()=>new HttpClient(handler,false),clock);var first=await provider.Get("artvin",default);Assert.Equal("UNAVAILABLE",first.Status);Assert.Null(first.TemperatureC);Assert.Null(first.CheckedAt);Assert.DoesNotContain("PRIVATE",JsonSerializer.Serialize(first));
        await provider.Get("artvin",default);Assert.Equal(1,handler.Calls);
    }
    [Theory][InlineData(66,"celsius")][InlineData(-101,"celsius")][InlineData(20,"kelvin")]
    public void InvalidTemperaturesOrUnitsAreRejected(decimal temp,string unit){var now=new Clock().Now;using var json=JsonDocument.Parse(Json(now,temp,unit));Assert.Throws<InvalidDataException>(()=>MetWeatherProvider.Parse(json.RootElement,"İstanbul",now));}
    [Fact] public void MissingCurrentForecastIsRejected(){var now=new Clock().Now;using var json=JsonDocument.Parse(Json(now.AddHours(-3)));Assert.Throws<InvalidDataException>(()=>MetWeatherProvider.Parse(json.RootElement,"İstanbul",now));}
}
