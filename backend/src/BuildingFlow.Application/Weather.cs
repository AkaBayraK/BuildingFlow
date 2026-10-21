namespace BuildingFlow.Application;
// Personal city-centre forecast; no user/property data is passed to the provider.
public sealed record WeatherSnapshot(string City,string Status,decimal? TemperatureC,string? Symbol,DateTimeOffset? ForecastAt,DateTimeOffset? UpdatedAt,DateTimeOffset? CheckedAt,DateTimeOffset NextRefreshAt);
public interface IWeatherProvider { Task<WeatherSnapshot> Get(string city,CancellationToken token); }
