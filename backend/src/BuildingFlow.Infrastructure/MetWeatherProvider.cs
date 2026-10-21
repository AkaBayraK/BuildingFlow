using System.Net;
using System.Text.Json;
using BuildingFlow.Application;
namespace BuildingFlow.Infrastructure;

// Bounded city cache and per-city single flight. Manual refresh never bypasses MET Expires.
public sealed class MetWeatherProvider(Func<HttpClient> clients,TimeProvider clock):IWeatherProvider
{
    private sealed class Entry(string city,string coordinates)
    {
        public readonly string City=city,Coordinates=coordinates;
        public readonly SemaphoreSlim Gate=new(1,1);
        public WeatherSnapshot? Value;
        public DateTimeOffset Next;
        public DateTimeOffset? Modified;
        public bool Failed;
    }
    private readonly Dictionary<string,Entry> entries=new(StringComparer.Ordinal){["istanbul"]=new("İstanbul","lat=41.01&lon=28.98"),["ankara"]=new("Ankara","lat=39.93&lon=32.86"),["izmir"]=new("İzmir","lat=38.42&lon=27.14"),["artvin"]=new("Artvin","lat=41.18&lon=41.82")};
    public static bool Supports(string city)=>city is "istanbul" or "ankara" or "izmir" or "artvin";
    public async Task<WeatherSnapshot> Get(string city,CancellationToken token)
    {
        if(!entries.TryGetValue(city,out var entry))throw new ArgumentException("Unsupported weather city.");
        await entry.Gate.WaitAsync(token);
        try
        {
            var now=clock.GetUtcNow();
            if(now>=entry.Next)
            {
                try
                {
                    using var request=new HttpRequestMessage(HttpMethod.Get,"https://api.met.no/weatherapi/locationforecast/2.0/compact?"+entry.Coordinates);
                    if(entry.Modified.HasValue)request.Headers.IfModifiedSince=entry.Modified;
                    using var client=clients();
                    using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
                    var expires=response.Content.Headers.Expires;
                    if(response.StatusCode==HttpStatusCode.NotModified&&entry.Value is not null)
                    {
                        entry.Next=expires>now?expires.Value:now.AddMinutes(30);entry.Failed=false;entry.Value=entry.Value with{CheckedAt=now};
                    }
                    else
                    {
                        if(!response.IsSuccessStatusCode)
                        {
                            var retry=response.Headers.RetryAfter;entry.Next=retry?.Date??now.Add(retry?.Delta??TimeSpan.FromMinutes(5));
                            if(entry.Next<=now)entry.Next=now.AddMinutes(5);
                            entry.Failed=true;
                        }
                        else
                        {
                            await response.Content.LoadIntoBufferAsync(1024*1024,token);
                            using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync(token),new JsonDocumentOptions{MaxDepth=32});
                            var parsed=Parse(document.RootElement,entry.City,now);
                            entry.Value=parsed;entry.Modified=response.Content.Headers.LastModified;
                            entry.Next=expires>now?expires.Value:now.AddMinutes(30);entry.Failed=false;
                        }
                    }
                }
                catch(OperationCanceledException) when(token.IsCancellationRequested){throw;}
                catch(Exception e) when(e is HttpRequestException or OperationCanceledException or JsonException or InvalidDataException or InvalidOperationException or KeyNotFoundException or FormatException)
                {entry.Failed=true;entry.Next=now.AddMinutes(5);}
            }
            var current=clock.GetUtcNow();var value=entry.Value;
            // Never present old data as a current observation. MET is a model forecast.
            var stale=entry.Failed||value?.ForecastAt<current.AddMinutes(-90)||value?.ForecastAt>current.AddMinutes(90)||value?.UpdatedAt<current.AddHours(-12);
            return value is null?new(entry.City,"UNAVAILABLE",null,null,null,null,null,entry.Next):value with{Status=stale?"STALE":"FRESH",NextRefreshAt=entry.Next};
        }
        finally{entry.Gate.Release();}
    }
    public static WeatherSnapshot Parse(JsonElement root,string city,DateTimeOffset now)
    {
        var properties=root.GetProperty("properties");var meta=properties.GetProperty("meta");
        if(meta.GetProperty("units").GetProperty("air_temperature").GetString()!="celsius")throw new InvalidDataException();
        var updated=meta.GetProperty("updated_at").GetDateTimeOffset();if(updated>now.AddMinutes(5))throw new InvalidDataException();
        var points=properties.GetProperty("timeseries").EnumerateArray().Select(p=>new{Point=p,Time=p.GetProperty("time").GetDateTimeOffset()})
            .Where(p=>p.Time>=now.AddMinutes(-90)&&p.Time<=now.AddMinutes(90)).OrderBy(p=>Math.Abs((p.Time-now).TotalMinutes)).ToArray();
        if(points.Length==0)throw new InvalidDataException();
        var point=points[0];var data=point.Point.GetProperty("data");var temp=data.GetProperty("instant").GetProperty("details").GetProperty("air_temperature").GetDecimal();
        if(temp is <-100 or >65)throw new InvalidDataException();
        string? symbol=null;if(data.TryGetProperty("next_1_hours",out var next)&&next.TryGetProperty("summary",out var summary)&&summary.TryGetProperty("symbol_code",out var code))symbol=code.GetString();
        if(symbol is not null&&(symbol.Length>60||symbol.Any(c=>!char.IsAsciiLetterLower(c)&&c!='_')))throw new InvalidDataException();
        return new(city,"FRESH",temp,symbol,point.Time,updated,now,now);
    }
}
