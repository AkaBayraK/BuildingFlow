using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

// Reads the code-owned sources behind user-defined automation types. Every query is written here and filtered by the definition's Property;
// a type only selects fields/values from AutomationSources, which are compared in memory against these fixed projections.
public sealed class AutomationSourceReader(BuildingFlowDbContext db, TimeProvider clock)
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
    private sealed record Row(Guid Id, Guid? UnitId, DateOnly? Date, DateTimeOffset CreatedAt, Dictionary<string, string> Fields, Dictionary<string, string> Values, string? Requester = null);
    private static readonly Dictionary<string, string> Labels = new()
    {
        ["ELEVATOR"] = "Asansör", ["WATER"] = "Su", ["ELECTRICITY"] = "Elektrik", ["HEATING"] = "Isıtma", ["RENOVATION"] = "Tadilat", ["FAULT"] = "Arıza",
        ["CLEANING"] = "Temizlik", ["SECURITY"] = "Güvenlik", ["COMMON_AREA"] = "Ortak alan", ["SUGGESTION"] = "Öneri", ["OTHER"] = "Diğer",
        ["LOW"] = "Düşük", ["NORMAL"] = "Normal", ["HIGH"] = "Yüksek", ["RECORDED"] = "Kaydedildi", ["IN_PROGRESS"] = "İşlemde", ["RESOLVED"] = "Çözüldü", ["REJECTED"] = "Reddedildi",
        ["MAINTENANCE"] = "Bakım", ["INSPECTION"] = "Kontrol", ["DRILL"] = "Tatbikat", ["GAS"] = "Doğalgaz"
    };
    private static string Label(string code) => Labels.GetValueOrDefault(code, code);
    private static string Day(DateOnly d) => d.ToString("dd.MM.yyyy", Turkish);
    private static DateOnly Local(DateTimeOffset at) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(at, Zone).DateTime);

    internal async Task<IReadOnlyList<AutomationCandidate>> CandidatesAsync(AutomationDefinition d, AutomationTypeDefinition t, DateOnly today, DateTimeOffset activatedAt, CancellationToken token)
    {
        if (t.TriggerKind == AutomationTriggerKind.CALENDAR && !(t.DayOfMonth == today.Day || t.DayOfWeek == ((int)today.DayOfWeek == 0 ? 7 : (int)today.DayOfWeek))) return [];
        var targets = t.TriggerKind == AutomationTriggerKind.DATE_OFFSET ? d.OffsetDays.Select(o => today.AddDays(-o)).ToArray() : [];
        var since = clock.GetUtcNow().AddHours(-AutomationEngine.PaymentLookbackHours);
        if (activatedAt > since) since = activatedAt;
        var rows = await RowsAsync(d.PropertyId, t, targets, since, today, token);
        var conditions = t.ParsedConditions();
        rows = rows.Where(r => conditions.All(c => r.Fields.TryGetValue(c.Field, out var v) && (c.Operator == AutomationSources.NotIn ? !c.Values.Contains(v) : c.Values.Contains(v)))).ToList();
        rows = t.TriggerKind switch {
            AutomationTriggerKind.DATE_OFFSET => rows.Where(r => r.Date is { } date && targets.Contains(date)).ToList(),
            AutomationTriggerKind.EVENT => rows.Where(r => r.CreatedAt >= since).ToList(),
            _ => rows
        };
        if (rows.Count == 0) return [];
        var recipients = new RecipientCache(db, d.PropertyId, today);
        var result = new List<AutomationCandidate>();
        foreach (var r in rows.Take(AutomationEngine.MaxCandidates))
        {
            var people = await recipients.ResolveAsync(t.RecipientScopes, r.UnitId, r.Requester, token);
            int? offset = t.TriggerKind == AutomationTriggerKind.DATE_OFFSET && r.Date is { } date ? today.DayNumber - date.DayNumber : null;
            result.Add(new AutomationCandidate(r.UnitId, r.Values.GetValueOrDefault("unitNumber"), null, null, offset, null, r.Date, 0m, r.Values, people,
                t.SourceCode, r.Id, t.TriggerKind == AutomationTriggerKind.CALENDAR ? today : null));
        }
        return result;
    }

    private async Task<List<Row>> RowsAsync(Guid property, AutomationTypeDefinition t, DateOnly[] targets, DateTimeOffset since, DateOnly today, CancellationToken token)
    {
        var limit = AutomationEngine.MaxCandidates * 2;
        var units = await db.Units.AsNoTracking().Where(u => u.PropertyId == property).ToDictionaryAsync(u => u.Id, u => u.Number, token);
        string UnitNo(Guid? id) => id is { } v && units.TryGetValue(v, out var n) ? n : "";
        switch (t.SourceCode)
        {
            case AutomationSources.Request:
            {
                var query = db.Set<MobileRequest>().AsNoTracking().Where(r => r.PropertyId == property);
                if (t.TriggerKind == AutomationTriggerKind.EVENT) query = query.Where(r => r.CreatedAt >= since);
                if (t.TriggerKind == AutomationTriggerKind.DATE_OFFSET && targets.Length > 0)
                {
                    var from = new DateTimeOffset(targets.Min().ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-1);
                    var to = new DateTimeOffset(targets.Max().ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(2);
                    query = t.DateField == "updatedAt" ? query.Where(r => r.ModifiedAt >= from && r.ModifiedAt < to) : query.Where(r => r.CreatedAt >= from && r.CreatedAt < to);
                }
                var list = await query.OrderBy(r => r.CreatedAt).Take(limit).ToListAsync(token);
                return list.Select(r => new Row(r.Id, r.UnitId, t.DateField == "updatedAt" ? Local(r.ModifiedAt) : Local(r.CreatedAt), r.CreatedAt,
                    new() { ["category"] = r.Category, ["priority"] = r.Priority, ["status"] = r.ManagementStatus },
                    new() { ["unitNumber"] = UnitNo(r.UnitId), ["requestCategory"] = Label(r.Category), ["requestPriority"] = Label(r.Priority), ["requestStatus"] = Label(r.ManagementStatus),
                        ["createdDate"] = Day(Local(r.CreatedAt)), ["daysOpen"] = (today.DayNumber - Local(r.CreatedAt).DayNumber).ToString(Turkish) }, r.OwnerUserId)).ToList();
            }
            case AutomationSources.SupplierContract:
            {
                var query = db.Set<SupplierContract>().AsNoTracking().Where(c => c.PropertyId == property);
                if (t.TriggerKind == AutomationTriggerKind.EVENT) query = query.Where(c => c.CreatedAt >= since);
                if (t.TriggerKind == AutomationTriggerKind.DATE_OFFSET) query = t.DateField == "startDate" ? query.Where(c => targets.Contains(c.StartDate)) : query.Where(c => targets.Contains(c.EndDate));
                var list = await query.OrderBy(c => c.EndDate).Take(limit).ToListAsync(token);
                return list.Select(c => new Row(c.Id, null, t.DateField == "startDate" ? c.StartDate : c.EndDate, c.CreatedAt, new() { ["status"] = c.Status.ToString() },
                    new() { ["contractTitle"] = c.Title, ["startDate"] = Day(c.StartDate), ["endDate"] = Day(c.EndDate), ["daysLeft"] = (c.EndDate.DayNumber - today.DayNumber).ToString(Turkish),
                        ["amount"] = AutomationEngine.Money(c.Amount) })).ToList();
            }
            case AutomationSources.Unit:
                return (await db.Units.AsNoTracking().Where(u => u.PropertyId == property && u.IsEnabled).OrderBy(u => u.Number).Take(limit).ToListAsync(token))
                    .Select(u => new Row(u.Id, u.Id, null, u.CreatedAt, new() { ["type"] = u.Type.ToString() }, new() { ["unitNumber"] = u.Number })).ToList();
            case AutomationSources.Maintenance:
            {
                var query = db.MaintenancePlans.AsNoTracking().Where(m => m.PropertyId == property);
                if (t.TriggerKind == AutomationTriggerKind.EVENT) query = query.Where(m => m.CreatedAt >= since);
                if (t.TriggerKind == AutomationTriggerKind.DATE_OFFSET) query = query.Where(m => targets.Contains(m.PlannedDate));
                var list = await query.OrderBy(m => m.PlannedDate).Take(limit).ToListAsync(token);
                return list.Select(m => new Row(m.Id, null, m.PlannedDate, m.CreatedAt, new() { ["type"] = m.Type.ToString(), ["status"] = m.Status.ToString() },
                    new() { ["maintenanceTitle"] = m.Title, ["maintenanceType"] = Label(m.Type.ToString()), ["plannedDate"] = Day(m.PlannedDate), ["location"] = m.Location ?? "",
                        ["daysLeft"] = (m.PlannedDate.DayNumber - today.DayNumber).ToString(Turkish) })).ToList();
            }
            case AutomationSources.Meter:
            {
                var meters = await db.Meters.AsNoTracking().Where(m => m.PropertyId == property && m.IsEnabled)
                    .Select(m => new { Meter = m, Last = db.MeterReadings.Where(r => r.MeterId == m.Id).Max(r => (DateOnly?)r.ReadingDate) }).Take(limit).ToListAsync(token);
                return meters.Select(x => new Row(x.Meter.Id, x.Meter.UnitId, x.Last, x.Meter.CreatedAt, new() { ["kind"] = x.Meter.Kind.ToString() },
                    new() { ["meterName"] = x.Meter.Name, ["meterKind"] = Label(x.Meter.Kind.ToString()), ["lastReadingDate"] = x.Last is { } last ? Day(last) : "—", ["unitNumber"] = UnitNo(x.Meter.UnitId) })).ToList();
            }
            case AutomationSources.Reservation:
            {
                var query = db.Set<CommonAreaReservation>().AsNoTracking().Where(r => r.PropertyId == property);
                if (t.TriggerKind == AutomationTriggerKind.EVENT) query = query.Where(r => r.CreatedAt >= since);
                if (t.TriggerKind == AutomationTriggerKind.DATE_OFFSET) query = query.Where(r => targets.Contains(r.Date));
                var list = await query.OrderBy(r => r.Date).Take(limit).ToListAsync(token);
                var areaIds = list.Select(r => r.CommonAreaId).Distinct().ToArray();
                var areas = await db.Set<CommonArea>().AsNoTracking().Where(a => areaIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Name, token);
                return list.Select(r => new Row(r.Id, r.UnitId, r.Date, r.CreatedAt, new() { ["status"] = r.Status.ToString() },
                    new() { ["commonAreaName"] = areas.GetValueOrDefault(r.CommonAreaId, ""), ["reservationDate"] = Day(r.Date), ["startsAt"] = r.StartsAt.ToString("HH:mm"),
                        ["endsAt"] = r.EndsAt.ToString("HH:mm"), ["unitNumber"] = UnitNo(r.UnitId) })).ToList();
            }
            default: return [];
        }
    }

    // Resolves recipient scopes once per run; occupants must hold a current relationship, managers an active assignment and linked account.
    private sealed class RecipientCache(BuildingFlowDbContext db, Guid property, DateOnly today)
    {
        private List<(Guid UnitId, OccupancyType Type, bool Primary, Person Person)>? occupants;
        private List<AutomationRecipient>? managers;

        public async Task<IReadOnlyList<AutomationRecipient>> ResolveAsync(string[] scopes, Guid? unit, string? requester, CancellationToken token)
        {
            var result = new List<AutomationRecipient>();
            if (scopes.Any(s => s is AutomationSources.Owners or AutomationSources.Tenants or AutomationSources.Residents or AutomationSources.PrimaryContact))
            {
                occupants ??= await OccupantsAsync(token);
                foreach (var o in occupants.Where(o => unit is null || o.UnitId == unit))
                    if (scopes.Contains(AutomationSources.Owners) && o.Type == OccupancyType.OWNER || scopes.Contains(AutomationSources.Tenants) && o.Type == OccupancyType.TENANT ||
                        scopes.Contains(AutomationSources.Residents) && o.Type == OccupancyType.RESIDENT || scopes.Contains(AutomationSources.PrimaryContact) && o.Primary)
                        result.Add(AutomationRecipient.From(o.Person));
            }
            if (scopes.Contains(AutomationSources.Managers)) result.AddRange(managers ??= await ManagersAsync(token));
            if (scopes.Contains(AutomationSources.Requester) && requester is not null)
            {
                var person = await db.People.AsNoTracking().FirstOrDefaultAsync(p => p.PropertyId == property && p.LinkedUserId == requester && p.IsEnabled, token);
                result.Add(person is null ? new AutomationRecipient(UserGuid(requester), null, requester, null, null) : AutomationRecipient.From(person));
            }
            return result.DistinctBy(r => r.UserId ?? r.Id.ToString()).ToArray();
        }

        private async Task<List<(Guid, OccupancyType, bool, Person)>> OccupantsAsync(CancellationToken token)
        {
            var rows = await db.UnitOccupancies.AsNoTracking().Where(o => o.PropertyId == property && o.IsEnabled && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today) &&
                db.Units.Any(u => u.Id == o.UnitId && u.IsEnabled)).ToListAsync(token);
            var ids = rows.Select(r => r.PersonId).Distinct().ToArray();
            var people = await db.People.AsNoTracking().Where(p => p.PropertyId == property && ids.Contains(p.Id) && p.IsEnabled).ToDictionaryAsync(p => p.Id, token);
            return rows.Where(r => people.ContainsKey(r.PersonId)).Select(r => (r.UnitId, r.Type, r.IsPrimaryContact, people[r.PersonId])).ToList();
        }

        private async Task<List<AutomationRecipient>> ManagersAsync(CancellationToken token)
        {
            var assigned = await db.Set<ManagementAssignment>().AsNoTracking().Where(a => a.PropertyId == property && a.IsEnabled && a.CancelledAt == null &&
                (a.Role == "SITE_ADMIN" || a.Role == "MANAGER") && a.StartDate <= today && (a.EndDate == null || a.EndDate >= today)).Select(a => a.PersonId).Distinct().ToListAsync(token);
            if (assigned.Count == 0) return [];
            var central = await db.Set<CentralPerson>().AsNoTracking().Where(p => assigned.Contains(p.Id)).ToListAsync(token);
            var links = await (from link in db.Set<CentralPersonAccountLink>().AsNoTracking()
                               join account in db.Set<PersonUserAccount>().AsNoTracking() on link.UserAccountId equals account.Id
                               where assigned.Contains(link.PersonId) && link.EndedAt == null
                               select new { link.PersonId, account.Subject }).ToListAsync(token);
            var subjects = links.Select(l => l.Subject).ToArray();
            var local = await db.People.AsNoTracking().Where(p => p.PropertyId == property && p.LinkedUserId != null && subjects.Contains(p.LinkedUserId)).ToListAsync(token);
            return central.Select(c => {
                var user = links.FirstOrDefault(l => l.PersonId == c.Id)?.Subject;
                return new AutomationRecipient(c.Id, local.FirstOrDefault(p => p.LinkedUserId == user)?.Id, user, c.PhoneNumber, c.Email);
            }).ToList();
        }

        private static Guid UserGuid(string user) => new(SHA256.HashData(Encoding.UTF8.GetBytes("automation-user:" + user))[..16]);
    }
}
