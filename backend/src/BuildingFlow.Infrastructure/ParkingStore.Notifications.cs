using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed partial class ParkingStore
{
    private async Task QueueLifecycleNotifications(CancellationToken token)
    {
        db.ChangeTracker.DetectChanges();
        var vehicles = db.ChangeTracker.Entries<Vehicle>().Where(e => e.State == EntityState.Added || e.Property(x => x.Status).IsModified)
            .Select(e => e.Entity).Where(x => x.PersonalDataRedactedAt == null).ToArray();
        var guests = db.ChangeTracker.Entries<GuestVehicle>().Where(e => e.State == EntityState.Added || e.Property(x => x.Status).IsModified)
            .Select(e => e.Entity).Where(x => x.PersonalDataRedactedAt == null).ToArray();
        foreach (var v in vehicles)
            await Notify(v.PropertyId, v.Id, v.UnitId, v.PersonId, "vehicle." + v.Status, v.Status switch {
                VehicleStatus.PENDING => "Araç başvurunuz alındı. / Vehicle registration received.",
                VehicleStatus.APPROVED => "Araç kaydınız onaylandı. / Vehicle registration approved.",
                VehicleStatus.REJECTED => "Araç başvurunuz reddedildi. / Vehicle registration rejected.",
                _ => "Araç kaydınız pasife alındı. / Vehicle registration deactivated."
            }, token);
        foreach (var g in guests)
            await Notify(g.PropertyId, g.Id, g.HostUnitId, null, "guest." + g.Status, g.Status switch {
                GuestVehicleStatus.PLANNED => "Misafir park kaydı oluşturuldu. / Guest parking scheduled.",
                GuestVehicleStatus.CANCELLED => "Misafir park kaydı iptal edildi. / Guest parking cancelled.",
                GuestVehicleStatus.ARRIVED => "Misafir girişi kaydedildi. / Guest entry recorded.",
                _ => "Misafir çıkışı kaydedildi. / Guest exit recorded."
            }, token);
    }

    private async Task Notify(Guid property, Guid record, Guid unit, Guid? person, string kind, string body, CancellationToken token)
    {
        var consumer = "parking.in-app." + kind;
        if (await db.ProcessedMessages.AnyAsync(x => x.Consumer == consumer && x.MessageId == record, token) ||
            db.ProcessedMessages.Local.Any(x => x.Consumer == consumer && x.MessageId == record)) return;
        var today = Today;
        var recipients = await (from p in db.People join o in db.UnitOccupancies on p.Id equals o.PersonId
            where p.PropertyId == property && p.IsEnabled && p.LinkedUserId != null && (person == null || p.Id == person) &&
                o.PropertyId == property && o.UnitId == unit && o.IsEnabled && ResidentTypes.Contains(o.Type) && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today) &&
                db.Units.Any(u => u.Id == unit && u.PropertyId == property && u.IsEnabled) &&
                db.PropertyMemberships.Any(m => m.PropertyId == property && m.UserId == p.LinkedUserId && m.IsEnabled)
            select new { p.Id, User = p.LinkedUserId! }).Distinct().ToListAsync(token);
        foreach (var recipient in recipients.DistinctBy(x => x.User))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{property:D}|{record:D}|{kind}|{recipient.Id:D}"));
            db.Add(new PersonMessage(property, recipient.Id, new Guid(hash.AsSpan(0,16)), Convert.ToHexStringLower(hash), recipient.User,
                "Otopark / Parking", body));
        }
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{property:D}|{record:D}|{kind}")));
        db.Add(new ProcessedMessage(consumer, record, property, MessageContracts.Notification, fingerprint,
            recipients.Count == 0 ? InboxStatus.SKIPPED : InboxStatus.PROCESSED, clock.GetUtcNow()));
    }

    // One property and at most 100 records per operation per call. Cursor makes the property sweep fair.
    public async Task<Guid?> RunBatch(Guid? afterProperty, CancellationToken token)
    {
        if (!string.Equals(config["Parking:Operations:Enabled"], "true", StringComparison.OrdinalIgnoreCase)) return null;
        var property = await db.ParkingPolicies.Where(p => p.IsEnabled && (afterProperty == null || p.PropertyId.CompareTo(afterProperty.Value) > 0) &&
            db.Properties.Any(x => x.Id == p.PropertyId && x.IsEnabled)).OrderBy(p => p.PropertyId).Select(p => (Guid?)p.PropertyId).FirstOrDefaultAsync(token);
        if (property is null) return null;
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property.Value} FOR UPDATE", token);
        var parent = await db.Properties.SingleOrDefaultAsync(x => x.Id == property && x.IsEnabled, token);
        var policy = await CurrentPolicy(property.Value, token);
        if (parent is null || policy is null) { await tx.CommitAsync(token); return property; }
        var now = clock.GetUtcNow(); var soon = now.AddHours(1);
        foreach (var kind in new[] { "guest.expiring", "guest.overdue" })
        {
            var consumer = "parking.in-app." + kind;
            var rows = db.GuestVehicles.Where(g => g.PropertyId == property && g.IsEnabled && g.PersonalDataRedactedAt == null &&
                !db.ProcessedMessages.Any(m => m.Consumer == consumer && m.MessageId == g.Id));
            rows = kind == "guest.expiring" ? rows.Where(g => (g.Status == GuestVehicleStatus.PLANNED || g.Status == GuestVehicleStatus.ARRIVED) && g.EndsAt > now && g.EndsAt <= soon)
                : rows.Where(g => g.Status == GuestVehicleStatus.ARRIVED && g.EndsAt <= now);
            foreach (var g in await rows.OrderBy(g => g.EndsAt).ThenBy(g => g.Id).Take(100).ToListAsync(token))
                await Notify(g.PropertyId, g.Id, g.HostUnitId, null, kind, kind == "guest.expiring"
                    ? "Misafir park süresi bir saat içinde dolacak. / Guest parking expires within one hour."
                    : "Misafir park süresi doldu; kayıtlı çıkış yok. / Guest parking expired; no exit recorded.", token);
        }
        var cutoff = now.AddDays(-policy.RetentionDays);
        foreach (var v in await db.Vehicles.Where(v => v.PropertyId == property && v.PersonalDataRedactedAt == null &&
            ((v.Status == VehicleStatus.PASSIVE && v.DeactivatedAt <= cutoff) || (v.Status == VehicleStatus.REJECTED && v.ReviewedAt <= cutoff)))
            .OrderBy(v => v.Id).Take(100).ToListAsync(token)) v.Redact(now, policy.RetentionDays);
        foreach (var g in await db.GuestVehicles.Where(g => g.PropertyId == property && g.PersonalDataRedactedAt == null &&
            ((g.Status == GuestVehicleStatus.DEPARTED && g.DepartedAt <= cutoff) || (g.Status == GuestVehicleStatus.CANCELLED && g.CancelledAt <= cutoff)))
            .OrderBy(g => g.Id).Take(100).ToListAsync(token)) g.Redact(now, policy.RetentionDays);
        await Commit(parent, tx, token); return property;
    }
}
