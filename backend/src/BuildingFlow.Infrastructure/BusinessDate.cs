namespace BuildingFlow.Infrastructure;
internal static class BusinessDate
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    public static DateOnly Today(TimeProvider clock) => Of(clock.GetUtcNow());
    public static DateOnly Of(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);
}
