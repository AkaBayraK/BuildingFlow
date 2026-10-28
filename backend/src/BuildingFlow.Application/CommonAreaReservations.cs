using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record CommonAreaReservationRequest(Guid RequestId, Guid? UnitId, string? Date, string? StartsAt, string? EndsAt,
    int PartySize, string? Note = null, long Version = 0);
public sealed record CommonAreaReservationCancelRequest(string? Reason, long Version);
public sealed record CommonAreaReservationQuery(string? From = null, string? To = null, string? Status = null, int Page = 1, int PageSize = 50);
public sealed record CommonAreaReservationCommand(Guid RequestId, string Fingerprint, Guid? UnitId, DateOnly Date, TimeOnly StartsAt,
    TimeOnly EndsAt, int PartySize, string? Note, long Version);
public sealed record CommonAreaReservationFilter(DateOnly From, DateOnly To, CommonAreaReservationStatus? Status, int Page, int PageSize);
public sealed record CommonAreaReservationResponse(Guid Id, Guid PropertyId, Guid CommonAreaId, Guid RequestId, Guid? UnitId,
    string? UnitNumber, string Date, string StartsAt, string EndsAt, int PartySize, string? Note, string Status, string? CancelReason,
    DateTimeOffset? CancelledAt, long Version, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt, bool Replayed = false);
public sealed record CommonAreaBusyInterval(string StartsAt, string EndsAt, int Reserved, string State);
// Deliberately carries no unit, person, note or payment data: residents see only time and load.
public sealed record CommonAreaScheduleResponse(Guid CommonAreaId, string Date, string? OpensAt, string? ClosesAt, int Capacity,
    bool Reservable, IReadOnlyList<CommonAreaBusyInterval> Busy);
public interface ICommonAreaReservationStore
{
    Task<CommonAreaReportResponse?> Report(Guid property, Guid area, DateOnly from, DateOnly to, CancellationToken token);
    Task<PagedRows<CommonAreaReservationResponse>?> Reservations(Guid property, Guid area, CommonAreaReservationFilter filter, CancellationToken token);
    Task<CommonAreaReservationResponse?> SaveReservation(Guid property, Guid area, Guid? id, CommonAreaReservationCommand command, CancellationToken token);
    Task<CommonAreaReservationResponse?> CancelReservation(Guid property, Guid area, Guid id, string reason, long version, CancellationToken token);
    Task<CommonAreaScheduleResponse?> Schedule(Guid property, Guid area, DateOnly date, ActiveContextQuery? context, CancellationToken token);
}
public sealed partial class CommonAreaReservationService(ICommonAreaReservationStore store, ICurrentUserService user)
{
    private static readonly string[] Managers = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER"];
    private void Authenticated() { if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || string.IsNullOrWhiteSpace(user.Issuer)) throw new UnauthorizedAccessException(); }
    private void Manager() { Authenticated(); if (!Managers.Any(user.IsInRole)) throw new UnauthorizedAccessException(); }
    public static void Invalid(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.CommonAreaReservationInvalid.Create()] });
    public static DateOnly ParseDate(string? value, string field)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date.Year is < 2000 or > 2100) Invalid(field);
        return date;
    }
    private static TimeOnly ParseTime(string? value, string field)
    {
        if (!TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) || !CommonAreaReservation.OnSlot(time)) Invalid(field);
        return time;
    }
    public Task<PagedRows<CommonAreaReservationResponse>?> Reservations(Guid property, Guid area, CommonAreaReservationQuery q, CancellationToken token)
    {
        Manager();
        if (q.Page is < 1 or > 1000000 || q.PageSize is < 1 or > 100) Invalid("page");
        var from = q.From is null ? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)) : ParseDate(q.From, "from");
        var to = q.To is null ? from.AddDays(31) : ParseDate(q.To, "to");
        if (to < from || to.DayNumber - from.DayNumber > 92) Invalid("to");
        CommonAreaReservationStatus? status = null;
        if (q.Status is not null) { if (!Enum.GetNames<CommonAreaReservationStatus>().Contains(q.Status)) Invalid("status"); status = Enum.Parse<CommonAreaReservationStatus>(q.Status); }
        return store.Reservations(property, area, new(from, to, status, q.Page, q.PageSize), token);
    }
    public Task<CommonAreaReservationResponse?> Save(Guid property, Guid area, Guid? id, CommonAreaReservationRequest r, CancellationToken token)
    {
        Manager();
        if (r.RequestId == Guid.Empty) Invalid("requestId");
        if (id.HasValue ? r.Version < 1 : r.Version != 0) Invalid("version");
        if (r.UnitId == Guid.Empty) Invalid("unitId");
        var date = ParseDate(r.Date, "date"); var starts = ParseTime(r.StartsAt, "startsAt"); var ends = ParseTime(r.EndsAt, "endsAt");
        if (starts >= ends) Invalid("endsAt");
        if (r.PartySize is < 1 or > 10000) Invalid("partySize");
        var note = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim();
        if (note?.Length > 500) Invalid("note");
        var canonical = new { actor = user.UserId, issuer = user.Issuer, property, area, r.UnitId, date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), starts = r.StartsAt, ends = r.EndsAt, r.PartySize, note };
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical))));
        return store.SaveReservation(property, area, id, new(r.RequestId, fingerprint, r.UnitId, date, starts, ends, r.PartySize, note, r.Version), token);
    }
    public Task<CommonAreaReservationResponse?> Cancel(Guid property, Guid area, Guid id, CommonAreaReservationCancelRequest r, CancellationToken token)
    {
        Manager();
        if (r.Version < 1) Invalid("version");
        if (string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Trim().Length > 500) Invalid("reason");
        return store.CancelReservation(property, area, id, r.Reason!.Trim(), r.Version, token);
    }
    public Task<CommonAreaScheduleResponse?> Schedule(Guid property, Guid area, string? date, ActiveContextQuery? context, CancellationToken token)
    {
        if (context is null) Manager();
        else {
            Authenticated();
            if (context.Capacity is not ("OWNER" or "TENANT" or "RESIDENT") || context.PropertyId == Guid.Empty || context.UnitId is null || context.UnitId == Guid.Empty || context.RelationId == Guid.Empty) Invalid("context");
        }
        return store.Schedule(property, area, ParseDate(date, "date"), context, token);
    }
    public static IReadOnlyList<CommonAreaBusyInterval> Busy(IReadOnlyCollection<(TimeOnly Start, TimeOnly End, int Size)> rows, int capacity)
    {
        var result = new List<CommonAreaBusyInterval>();
        foreach (var (start, end, load) in Segments(rows)) {
            if (result.Count > 0 && result[^1].EndsAt == Format(start) && result[^1].Reserved == load) { result[^1] = result[^1] with { EndsAt = Format(end) }; continue; }
            result.Add(new(Format(start), Format(end), load, load >= capacity ? "FULL" : "PARTIAL"));
        }
        return result;
    }
    public static int PeakLoad(IReadOnlyCollection<(TimeOnly Start, TimeOnly End, int Size)> rows, TimeOnly start, TimeOnly end) =>
        Segments(rows).Where(s => s.Start < end && start < s.End).Select(s => s.Load).DefaultIfEmpty(0).Max();
    private static IEnumerable<(TimeOnly Start, TimeOnly End, int Load)> Segments(IReadOnlyCollection<(TimeOnly Start, TimeOnly End, int Size)> rows)
    {
        var points = rows.SelectMany(r => new[] { r.Start, r.End }).Distinct().Order().ToArray();
        for (var i = 0; i + 1 < points.Length; i++) {
            var load = rows.Where(r => r.Start < points[i + 1] && points[i] < r.End).Sum(r => r.Size);
            if (load > 0) yield return (points[i], points[i + 1], load);
        }
    }
    private static string Format(TimeOnly value) => value.ToString("HH:mm", CultureInfo.InvariantCulture);
}
