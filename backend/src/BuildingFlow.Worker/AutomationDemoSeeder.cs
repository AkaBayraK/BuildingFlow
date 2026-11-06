using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
namespace BuildingFlow.Worker;

// Local demo data for every automation type (`dotnet run -- --seed-automation-demo`). Refuses any non-loopback database and is idempotent:
// rows carry the AUTO-DEMO tag and are created only once. Real SMS/e-mail are never sent: SMS uses the MOCK provider, e-mail needs SMTP.
internal static class AutomationDemoSeeder
{
    public const string Argument = "--seed-automation-demo", Tag = "AUTO-DEMO";
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static async Task<int> RunAsync(IServiceProvider services, IConfiguration configuration, string[] args)
    {
        var connection = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Database"));
        if (connection.Host is not ("localhost" or "127.0.0.1" or "::1")) { Console.Error.WriteLine("[AUTO-DEMO] Yalnız yerel (localhost) veritabanında çalışır."); return 2; }
        var propertyArg = args.SkipWhile(a => a != "--property").Skip(1).FirstOrDefault();
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider; var db = sp.GetRequiredService<BuildingFlowDbContext>();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone).DateTime);
        var property = propertyArg is not null ? await db.Properties.SingleOrDefaultAsync(p => p.Id == Guid.Parse(propertyArg))
            : await db.Properties.Where(p => p.IsEnabled).OrderBy(p => p.Name).FirstOrDefaultAsync();
        if (property is null) { Console.Error.WriteLine("[AUTO-DEMO] Yönetim alanı bulunamadı."); return 2; }
        Console.WriteLine($"[AUTO-DEMO] Yönetim alanı: {property.Name}");
        var log = new List<string>();

        await SmsAccountAsync(db, property.Id, log);
        var linked = await (from o in db.UnitOccupancies join p in db.People on o.PersonId equals p.Id
                            where o.PropertyId == property.Id && p.LinkedUserId != null && o.IsEnabled && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today)
                            select new { Occupancy = o, Person = p }).ToListAsync();
        if (linked.Count == 0) log.Add("Uyarı: uygulama hesabına bağlı kişi yok; uygulama içi bildirim düşmez.");
        var units = linked.Select(x => x.Occupancy.UnitId).Distinct().ToArray();
        var charges = await ChargesAsync(db, property.Id, today, units, linked.Select(x => (x.Occupancy, x.Person)).ToList(), log);
        await PaymentAsync(sp, db, property.Id, today, charges, log);
        var owner = linked.OrderBy(x => x.Occupancy.Type).FirstOrDefault();
        if (owner is not null) await SourcesAsync(db, property.Id, today, owner.Occupancy.UnitId, owner.Person.LinkedUserId!, log);
        await ManagerAsync(db, property.Id, today, log);
        await RulesAsync(db, property.Id, today, log);

        var scheduler = sp.GetRequiredService<IAutomationScheduler>();
        var dispatched = await scheduler.RunDueAsync(CancellationToken.None);
        log.Add($"Otomasyon çalıştı: {dispatched} bildirim iletildi/kuyruğa alındı.");
        var sms = 0; int step;
        do { step = await sp.GetRequiredService<SmsDispatcher>().DispatchPendingAsync(CancellationToken.None); sms += step; } while (step >= SmsDispatcher.BatchSize);
        log.Add($"MOCK SMS gönderici {sms} mesajı işledi (gerçek SMS yok).");
        try
        {
            var published = 0;
            do { step = await sp.GetRequiredService<OutboxRelay>().PublishPendingAsync(CancellationToken.None); published += step; } while (step >= OutboxRelay.BatchSize);
            log.Add($"RabbitMQ'ya {published} olay yayımlandı (kuyruk: notification.requested.v1).");
        }
        catch (Exception error) { log.Add($"RabbitMQ'ya yayım yapılamadı ({error.GetType().Name}); Worker açıldığında outbox kendiliğinden yayımlar."); }
        foreach (var line in log) Console.WriteLine("[AUTO-DEMO] " + line);
        return 0;
    }

    private static string Fp(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Tag + "|" + text))).ToLowerInvariant();

    private static async Task SmsAccountAsync(BuildingFlowDbContext db, Guid property, List<string> log)
    {
        if (await db.SmsAccounts.AnyAsync(a => a.PropertyId == property)) { log.Add("SMS hesabı zaten var; değiştirilmedi."); return; }
        var account = new SmsAccount(property, "Demo SMS (MOCK)", SmsProvider.MOCK, "BUILDFLOW", 50);
        db.Add(account); db.Add(new SmsCreditTransaction(account, SmsCreditTransactionType.LOAD, 1000, null, null, Tag, "Demo kontör", null));
        await db.SaveChangesAsync(); log.Add("MOCK SMS hesabı ve 1000 demo kontör eklendi.");
    }

    private static async Task<List<DuesCharge>> ChargesAsync(BuildingFlowDbContext db, Guid property, DateOnly today, Guid[] units,
        List<(UnitOccupancy Link, Person Person)> linked, List<string> log)
    {
        var result = new List<DuesCharge>();
        foreach (var (days, amount, label) in new[] { (2, 1000m, "vadeye 2 gün"), (0, 500m, "son ödeme günü"), (-7, 750m, "7 gün gecikmiş") })
        {
            var due = today.AddDays(days); var description = $"{Tag} {label}";
            var existing = await db.DuesCharges.Where(c => c.PropertyId == property && c.DueDate == due && units.Contains(c.UnitId) &&
                db.Set<DuesAssessment>().Any(a => a.Id == c.AssessmentId && a.Description == description)).ToListAsync();
            if (existing.Count > 0) { result.AddRange(existing); continue; }
            if (units.Length == 0) continue;
            var assessment = new DuesAssessment(property, Guid.NewGuid(), Fp(description + due), new DateOnly(due.Year, due.Month, 1), amount, "TRY", due, description, "UNIT",
                ChargeKind.EXTRA, ChargeCategory.OTHER);
            foreach (var unit in units)
            {
                var charge = assessment.AddCharge(unit); result.Add(charge);
                var debtor = linked.Where(x => x.Link.UnitId == unit && x.Link.Type != OccupancyType.RESIDENT).OrderBy(x => x.Link.Type).FirstOrDefault();
                if (debtor.Person is not null)
                    try { charge.RecordParties([new DuesChargeParty(charge, debtor.Link, debtor.Person, true)]); } catch (ArgumentException) { }
            }
            db.Add(assessment); await db.SaveChangesAsync();
            log.Add($"Ek tahakkuk: {label} ({amount:N2} TL) × {units.Length} bölüm.");
        }
        return result;
    }

    private static async Task PaymentAsync(IServiceProvider sp, BuildingFlowDbContext db, Guid property, DateOnly today, List<DuesCharge> charges, List<string> log)
    {
        var target = charges.Where(c => c.DueDate == today.AddDays(2)).OrderBy(c => c.UnitId).FirstOrDefault();
        if (target is null) return;
        var fresh = await db.DuesCharges.AsNoTracking().SingleAsync(c => c.Id == target.Id);
        if (fresh.PaidAmount > 0) { log.Add("Demo kısmi tahsilat zaten var."); return; }
        try
        {
            await sp.GetRequiredService<IPaymentStore>().CreateAsync(property, new PaymentCommand(Guid.NewGuid(), fresh.UnitId, 250m, today, PaymentMethod.CASH, null,
                $"{Tag} kısmi tahsilat", [new AllocationCommand(fresh.Id, 250m, fresh.Version)]), new AccessScope("automation-demo", true), CancellationToken.None);
            log.Add("Kısmi tahsilat 250,00 TL kaydedildi (ödeme alındı bildirimi + güncel kalan 750,00 TL).");
        }
        catch (Exception error) { db.ChangeTracker.Clear(); log.Add($"Demo tahsilat eklenemedi ({error.GetType().Name}); ödeme bildirimi için ekrandan tahsilat girin."); }
    }

    private static async Task SourcesAsync(BuildingFlowDbContext db, Guid property, DateOnly today, Guid unit, string user, List<string> log)
    {
        if (!await db.Set<MobileRequest>().AnyAsync(r => r.PropertyId == property && r.Description.StartsWith(Tag)))
        {
            var request = new MobileRequest(Guid.NewGuid(), property, unit, "OWNER", user, $"{Tag}: Asansör 2. katta duruyor, kapı açılmıyor.");
            request.Details("ELEVATOR", "HIGH", null); db.Add(request); log.Add("Asansör arıza talebi eklendi.");
        }
        if (!await db.MaintenancePlans.AnyAsync(m => m.PropertyId == property && m.Title.StartsWith(Tag)))
        {
            db.Add(new MaintenancePlan(property, $"{Tag} Asansör periyodik bakımı", null, MaintenanceType.MAINTENANCE, today.AddDays(2), "Blok girişi", null, MaintenanceStatus.PLANNED));
            log.Add("2 gün sonraya bakım planı eklendi.");
        }
        if (!await db.Meters.AnyAsync(m => m.PropertyId == property && m.Name.StartsWith(Tag)))
        {
            var meter = new Meter(property, unit, Guid.NewGuid(), Fp("meter" + property), $"{Tag} Su sayacı", MeterKind.WATER);
            var reading = new MeterReading(property, meter.Id, today.AddDays(-30));
            db.Add(meter); db.Add(reading); db.Add(reading.Revise(Guid.NewGuid(), Fp("reading" + property), 123.456m, "Demo okuma"));
            log.Add("Son okuması 30 gün önce olan demo sayaç eklendi.");
        }
        await db.SaveChangesAsync();
        if (!await db.Set<CommonAreaReservation>().AnyAsync(r => r.PropertyId == property && r.Note == Tag))
        {
            var area = await db.Set<CommonArea>().Where(a => a.PropertyId == property && a.IsEnabled).OrderBy(a => a.Name).FirstOrDefaultAsync();
            if (area is null) log.Add("Ortak alan yok; rezervasyon kaynağı boş kalacak.");
            else
                try { db.Add(new CommonAreaReservation(property, area.Id, Guid.NewGuid(), Fp("reservation" + property), unit, today.AddDays(1), new TimeOnly(18, 0), new TimeOnly(20, 0), 2, Tag)); await db.SaveChangesAsync(); log.Add($"Yarın için '{area.Name}' rezervasyonu eklendi."); }
                catch (Exception error) when (error is ArgumentException or DbUpdateException) { db.ChangeTracker.Clear(); log.Add($"Rezervasyon eklenemedi ({error.GetType().Name})."); }
        }
        if (!await db.Set<SupplierContract>().AnyAsync(c => c.PropertyId == property)) log.Add("Tedarikçi sözleşmesi yok; sözleşme türü 0 kayıt bulur (tür yine tanımlanır).");
    }

    // Links the property's first management account without a Person row to a demo central person with an active MANAGER assignment.
    private static async Task ManagerAsync(BuildingFlowDbContext db, Guid property, DateOnly today, List<string> log)
    {
        if (await db.Set<ManagementAssignment>().AnyAsync(a => a.PropertyId == property && a.IsEnabled && a.CancelledAt == null && (a.EndDate == null || a.EndDate >= today)))
        { log.Add("Aktif yönetici ataması zaten var; yönetici bildirimleri ona gider."); return; }
        var tenant = await db.Set<PersonPropertyScope>().Where(s => s.PropertyId == property).Select(s => (Guid?)s.TenantId).FirstOrDefaultAsync();
        var user = await db.PropertyMemberships.Where(m => m.PropertyId == property && m.IsEnabled && !db.People.Any(p => p.LinkedUserId == m.UserId))
            .OrderBy(m => m.CreatedAt).Select(m => m.UserId).FirstOrDefaultAsync();
        if (tenant is null || user is null) { log.Add("Kişi kiracısı veya yönetici hesabı bulunamadı; yönetici bildirimi atlanır."); return; }
        var account = await db.Set<PersonUserAccount>().FirstOrDefaultAsync(a => a.Subject == user);
        if (account is null) { account = new PersonUserAccount("http://localhost:8080/realms/buildingflow", user); db.Add(account); }
        if (!await db.Set<PersonTenantMembership>().AnyAsync(m => m.TenantId == tenant && m.UserAccountId == account.Id)) db.Add(new PersonTenantMembership(tenant.Value, account.Id));
        var central = new CentralPerson(tenant.Value, "NATURAL", "DOMESTIC", "TR", Tckn(), "Demo", "Yönetici", null, null, "+905550000001", "demo.yonetici@ornek.test");
        db.Add(central); await db.SaveChangesAsync();
        db.Add(new CentralPersonAccountLink(tenant.Value, account.Id, central.Id, DateTimeOffset.UtcNow));
        db.Add(new ManagementAssignment(tenant.Value, property, central.Id, "MANAGER", today.AddDays(-1)));
        await db.SaveChangesAsync();
        log.Add($"Demo yönetici ataması eklendi (hesap {user[..8]}…); yönetici bildirimleri web bildirim kutusuna düşer.");
    }

    private static async Task RulesAsync(BuildingFlowDbContext db, Guid property, DateOnly today, List<string> log)
    {
        var weekday = (int)today.DayOfWeek == 0 ? 7 : (int)today.DayOfWeek;
        var custom = new (string Code, AutomationTypeSettings Settings)[]
        {
            ("request", new("Asansör/su arızası bildirimi", "Wrench", AutomationSources.Request, [new("category", "IN", ["ELEVATOR", "WATER"])], AutomationTriggerKind.EVENT, null, [], null, null,
                [AutomationSources.Managers, AutomationSources.Requester], "Yeni {{requestCategory}} talebi", "{{unitNumber}} no.lu bölümden {{requestPriority}} öncelikli {{requestCategory}} talebi kaydedildi ({{createdDate}}).")),
            ("contract", new("Sözleşme bitiş hatırlatması", "FileSignature", AutomationSources.SupplierContract, [new("status", "EQ", ["ACTIVE"])], AutomationTriggerKind.DATE_OFFSET, "endDate", [-30, -15, -7], null, null,
                [AutomationSources.Managers], "Sözleşme bitiyor: {{contractTitle}}", "{{contractTitle}} sözleşmesi {{endDate}} tarihinde bitiyor ({{daysLeft}} gün).")),
            ("unit", new("Haftalık sayaç okuma duyurusu", "Megaphone", AutomationSources.Unit, [], AutomationTriggerKind.CALENDAR, null, [], null, weekday,
                [AutomationSources.PrimaryContact], "Sayaç okuma günü", "{{propertyName}}: {{unitNumber}} no.lu bölüm için bugün ({{today}}) sayaç okuma günüdür.")),
            ("maintenance", new("Planlı bakım duyurusu", "CalendarCheck2", AutomationSources.Maintenance, [new("status", "EQ", ["PLANNED"])], AutomationTriggerKind.DATE_OFFSET, "plannedDate", [-2], null, null,
                [AutomationSources.Managers, AutomationSources.Owners], "Planlı bakım: {{maintenanceTitle}}", "{{plannedDate}} tarihinde {{location}} alanında {{maintenanceTitle}} yapılacak ({{daysLeft}} gün kaldı).")),
            ("meter", new("Sayaç okuması gecikti", "Gauge", AutomationSources.Meter, [], AutomationTriggerKind.DATE_OFFSET, "lastReadingDate", [30], null, null,
                [AutomationSources.Owners], "Sayaç okuması bekleniyor", "{{unitNumber}} no.lu bölümdeki {{meterName}} için son okuma {{lastReadingDate}}; lütfen okuma bildirin.")),
            ("reservation", new("Rezervasyon hatırlatması", "CalendarDays", AutomationSources.Reservation, [new("status", "EQ", ["CONFIRMED"])], AutomationTriggerKind.DATE_OFFSET, "date", [-1], null, null,
                [AutomationSources.Owners, AutomationSources.Managers], "Yarın rezervasyonunuz var", "{{commonAreaName}} rezervasyonunuz {{reservationDate}} {{startsAt}}–{{endsAt}} arasında."))
        };
        var now = DateTimeOffset.UtcNow;
        foreach (var (code, settings) in custom)
        {
            var typeCode = $"auto-demo-{code}-{property.ToString("N")[..8]}";
            var type = await db.Set<AutomationTypeDefinition>().SingleOrDefaultAsync(t => t.Code == typeCode);
            if (type is null)
            {
                if (AutomationSources.Problem(settings) is { } problem) throw new InvalidOperationException("Demo type is invalid: " + problem);
                type = new AutomationTypeDefinition(property, typeCode, settings); type.Publish(); db.Add(type);
                db.Add(new AutomationTypeRevision(type, "PUBLISHED", Tag, "{}")); await db.SaveChangesAsync();
                log.Add($"Özel tür yayında: {settings.Name}");
            }
            await RuleAsync(db, property, AutomationType.CUSTOM, type, new(settings.Name, null, null, 0, 0, 0, settings.DefaultOffsets, true, true, false, settings.DefaultTitle, settings.DefaultBody), now, log);
        }
        foreach (var info in AutomationCatalog.All)
        {
            var type = Enum.Parse<AutomationType>(info.Type);
            await RuleAsync(db, property, type, null, new(info.DefaultName, null, null, 0, 0, 0, info.DefaultOffsets, true, true, type == AutomationType.DUES_DUE_REMINDER,
                info.DefaultTitle, info.DefaultBody), now, log);
        }
    }

    private static async Task RuleAsync(BuildingFlowDbContext db, Guid property, AutomationType type, AutomationTypeDefinition? custom, AutomationSettings settings, DateTimeOffset now, List<string> log)
    {
        var exists = custom is null ? await db.Set<AutomationDefinition>().AnyAsync(d => d.PropertyId == property && d.Type == type)
            : await db.Set<AutomationDefinition>().AnyAsync(d => d.PropertyId == property && d.CustomTypeId == custom.Id);
        if (exists) { log.Add($"Kural zaten var, değiştirilmedi: {settings.Name}"); return; }
        var rule = new AutomationDefinition(property, type, settings, custom);
        rule.Activate(now.AddMinutes(-5));
        db.Add(rule); db.Add(new AutomationRevision(rule, "ACTIVATED", Tag, "{}")); await db.SaveChangesAsync();
        log.Add($"Kural aktif: {settings.Name} (uygulama içi + SMS{(settings.Email ? " + e-posta" : "")}, sessiz saat yok)");
    }

    // A checksum-valid but fictional Turkish identity number for the demo manager.
    private static string Tckn()
    {
        var d = new int[11]; d[0] = RandomNumberGenerator.GetInt32(1, 10);
        for (var i = 1; i < 9; i++) d[i] = RandomNumberGenerator.GetInt32(0, 10);
        d[9] = ((d[0] + d[2] + d[4] + d[6] + d[8]) * 7 - (d[1] + d[3] + d[5] + d[7])) % 10; if (d[9] < 0) d[9] += 10;
        d[10] = d.Take(10).Sum() % 10;
        return string.Concat(d.Select(x => x.ToString(CultureInfo.InvariantCulture)));
    }
}
