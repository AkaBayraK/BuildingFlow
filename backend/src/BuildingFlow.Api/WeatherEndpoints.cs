using System.Net;
using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
namespace BuildingFlow.Api;
internal static class WeatherEndpoints
{
    public static void AddWeather(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddHttpClient("met-weather",client=>{
            client.Timeout=TimeSpan.FromSeconds(8);
            // MET requires an application identifier; no fake contact address or user credentials.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BuildingFlow/1.0");
            if(Uri.TryCreate(configuration["Weather:ContactUrl"],UriKind.Absolute,out var contact)&&contact.Scheme=="https"&&string.IsNullOrEmpty(contact.UserInfo)&&string.IsNullOrEmpty(contact.Query)&&string.IsNullOrEmpty(contact.Fragment))client.DefaultRequestHeaders.UserAgent.ParseAdd("(+"+contact.AbsoluteUri+")");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).ConfigurePrimaryHttpMessageHandler(()=>new SocketsHttpHandler{AllowAutoRedirect=false,ConnectTimeout=TimeSpan.FromSeconds(3),AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate}).RemoveAllLoggers();
        services.AddSingleton<IWeatherProvider>(p=>new MetWeatherProvider(()=>p.GetRequiredService<IHttpClientFactory>().CreateClient("met-weather"),p.GetRequiredService<TimeProvider>()));
    }
    public static void MapWeatherEndpoints(this WebApplication app)=>app.MapGet("/api/v1/weather",async(string city,IWeatherProvider provider,CancellationToken token)=>
        MetWeatherProvider.Supports(city)?Results.Ok(await provider.Get(city,token)):Results.BadRequest()).RequireAuthorization("CanManageDues").WithTags("Dashboard");
}
