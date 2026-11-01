using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;
namespace BuildingFlow.Infrastructure;

internal sealed class AssistantPromptConfiguration : IEntityTypeConfiguration<AssistantPrompt>
{
    private static readonly (string Code, string Category, string Tool, string TitleTr, string TitleEn, string TextTr, string TextEn)[] Seed =
    [
        ("TODAY_FOCUS", "OVERVIEW", AssistantTools.DailySummary, "Bugün neye bakmalıyım?", "What should I check today?", "Bugün neye bakmalıyım? Yönetim alanının günlük özetini ver.", "What should I check today? Give me the daily summary of this area."),
        ("AREA_STATUS", "OVERVIEW", AssistantTools.DailySummary, "Genel durum", "Overall status", "Yönetim alanının genel durumu nasıl?", "How is this management area doing overall?"),
        ("OCCUPANCY", "OVERVIEW", AssistantTools.DailySummary, "Doluluk", "Occupancy", "Kaç bölüm dolu, kaç bölüm boş?", "How many units are occupied and how many are vacant?"),
        ("OVERDUE_DUES", "FINANCE", AssistantTools.DuesCollection, "Geciken aidat", "Overdue dues", "Geciken aidat toplamı ne kadar, kaç borç kaydı gecikmede?", "How much dues are overdue and across how many charges?"),
        ("MONTH_COLLECTION", "FINANCE", AssistantTools.DuesCollection, "Bu ayın tahsilatı", "This month's collection", "Bu ayın aidat tahsilatı ne durumda?", "How is this month's dues collection going?"),
        ("YEAR_COLLECTION", "FINANCE", AssistantTools.DuesCollection, "Yıllık tahsilat", "Yearly collection", "Bu yıl aidat tahsilat oranı nedir?", "What is this year's dues collection rate?"),
        ("BUDGET_OVERRUN", "FINANCE", AssistantTools.BudgetVariance, "Bütçe aşımı", "Budget overrun", "Bütçeye göre hangi gider kalemi aşıldı?", "Which expense categories are over budget?"),
        ("MONTH_BALANCE", "FINANCE", AssistantTools.BudgetVariance, "Gelir-gider dengesi", "Income and expenses", "Bu ay gelir ve gider dengesi nasıl?", "How do income and expenses compare this month?"),
        ("UPCOMING_MAINTENANCE", "OPERATIONS", AssistantTools.UpcomingMaintenance, "Yaklaşan bakımlar", "Upcoming maintenance", "30 gün içinde kaç planlı bakım var, ilki ne zaman?", "How many maintenance plans are due within 30 days, and when is the first?"),
        ("MAINTENANCE_TYPES", "OPERATIONS", AssistantTools.UpcomingMaintenance, "Bakım türleri", "Maintenance types", "Yaklaşan bakımlar hangi türlerde?", "What types of maintenance are coming up?"),
        ("OPEN_REQUESTS", "REQUESTS", AssistantTools.OpenRequests, "Açık talepler", "Open requests", "Kaç açık talep var?", "How many requests are open?"),
        ("STALE_REQUESTS", "REQUESTS", AssistantTools.OpenRequests, "Bekleyen talepler", "Waiting requests", "En uzun süredir açık talepler ne kadar bekliyor?", "How long have the oldest open requests been waiting?"),
        ("REQUEST_CATEGORIES", "REQUESTS", AssistantTools.OpenRequests, "Talep kategorileri", "Request categories", "Açık talepler hangi kategorilerde yoğunlaşıyor?", "Which categories do open requests concentrate in?"),
        ("OLDEST_REQUESTS", "REQUESTS", AssistantTools.RequestBacklog, "En uzun bekleyenler", "Longest waiting", "En uzun süredir açık olan talepler hangileri?", "Which requests have been open the longest?"),
        ("REQUEST_STATUS", "REQUESTS", AssistantTools.RequestBacklog, "Talep durumları", "Request statuses", "Talepler durumlarına göre nasıl dağılıyor?", "How are requests distributed by status?"),
        ("OVERDUE_WORK_ORDERS", "OPERATIONS", AssistantTools.SupplierWatch, "Geciken iş emirleri", "Overdue work orders", "Hangi tedarikçi iş emirleri gecikiyor?", "Which supplier work orders are overdue?"),
        ("EXPIRING_CONTRACTS", "OPERATIONS", AssistantTools.SupplierWatch, "Biten sözleşmeler", "Expiring contracts", "Süresi dolmak üzere olan tedarikçi sözleşmeleri hangileri?", "Which supplier contracts are about to expire?"),
        ("PENDING_INVOICES", "FINANCE", AssistantTools.SupplierWatch, "Bekleyen faturalar", "Pending invoices", "Ödeme veya onay bekleyen tedarikçi faturaları ne durumda?", "What supplier invoices are waiting for approval or payment?"),
        ("SURVEY_RESULTS", "COMMUNICATION", AssistantTools.SurveySummary, "Anket sonuçları", "Survey results", "Son anketin sonuçlarını özetle.", "Summarise the latest survey's results."),
        ("SURVEY_PARTICIPATION", "COMMUNICATION", AssistantTools.SurveySummary, "Anket katılımı", "Survey participation", "Son ankete katılım oranı nedir?", "What was the participation rate of the latest survey?"),
        ("MY_BALANCE", "RESIDENT", ResidentAssistantTools.Balance, "Borcum ne kadar?", "What do I owe?", "Bu bölüm için toplam borcum ve geciken tutarım ne kadar?", "What is my total balance and overdue amount for this unit?"),
        ("MY_NEXT_DUE", "RESIDENT", ResidentAssistantTools.Balance, "Son ödeme tarihim", "My next due date", "Bir sonraki ödemem ne zaman ve ne kadar?", "When is my next payment due and how much is it?"),
        ("MY_PAYMENTS", "RESIDENT", ResidentAssistantTools.Balance, "Son ödemelerim", "My recent payments", "Son ödemelerim nelerdi?", "What were my recent payments?"),
        ("MY_REQUESTS", "RESIDENT", ResidentAssistantTools.Requests, "Taleplerim", "My requests", "Taleplerimin durumu nedir?", "What is the status of my requests?"),
        ("MY_ANNOUNCEMENTS", "RESIDENT", ResidentAssistantTools.Notices, "Son duyurular", "Latest announcements", "Son duyurularda ne var?", "What do the latest announcements say?"),
        ("MY_UNREAD", "RESIDENT", ResidentAssistantTools.Notices, "Okunmamış duyurular", "Unread announcements", "Kaç okunmamış duyurum var?", "How many unread announcements do I have?")
    ];

    public void Configure(EntityTypeBuilder<AssistantPrompt> b)
    {
        BaseEntityConfiguration.Configure(b, "assistant_prompts");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(AssistantPrompt.CodeLength);
        b.Property(x => x.Category).HasColumnName("category").HasMaxLength(AssistantPrompt.KeyLength);
        b.Property(x => x.ToolKey).HasColumnName("tool_key").HasMaxLength(AssistantPrompt.KeyLength);
        b.Property(x => x.TitleTr).HasColumnName("title_tr").HasMaxLength(AssistantPrompt.TitleLength);
        b.Property(x => x.TitleEn).HasColumnName("title_en").HasMaxLength(AssistantPrompt.TitleLength);
        b.Property(x => x.TextTr).HasColumnName("text_tr").HasMaxLength(AssistantPrompt.TextLength);
        b.Property(x => x.TextEn).HasColumnName("text_en").HasMaxLength(AssistantPrompt.TextLength);
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        b.HasIndex(x => x.Code).IsUnique().HasFilter("NOT is_deleted").HasDatabaseName("uidx_assistant_prompts_code");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("chk_assistant_prompts_sort_order", $"sort_order BETWEEN 0 AND {AssistantPrompt.MaxSortOrder}");
            t.HasCheckConstraint("chk_assistant_prompts_category", "category IN ('OVERVIEW','FINANCE','OPERATIONS','REQUESTS','COMMUNICATION','RESIDENT')");
            t.HasCheckConstraint("chk_assistant_prompts_tool_key", "tool_key IN ('DAILY_SUMMARY','DUES_COLLECTION','BUDGET_VARIANCE','UPCOMING_MAINTENANCE','OPEN_REQUESTS','REQUEST_BACKLOG','SUPPLIER_WATCH','SURVEY_SUMMARY','RESIDENT_BALANCE','RESIDENT_REQUESTS','RESIDENT_NOTICES')");
        });
        var at = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        b.HasData(Seed.Select((p, i) => new
        {
            Id = Guid.Parse($"a1a55150-0001-4000-8000-{i + 1:D12}"), p.Code, p.Category, ToolKey = p.Tool, p.TitleTr, p.TitleEn, p.TextTr, p.TextEn, SortOrder = (i + 1) * 10,
            Version = 1L, CreatedAt = at, ModifiedAt = at, CreatedBy = "migration", ModifiedBy = "migration", IsDeleted = false, IsEnabled = true
        }));
    }
}

internal sealed class AssistantAuditEntryConfiguration : IEntityTypeConfiguration<AssistantAuditEntry>
{
    public void Configure(EntityTypeBuilder<AssistantAuditEntry> b)
    {
        BaseEntityConfiguration.Configure(b, "assistant_audit_entries");
        b.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(AssistantAuditEntry.UserLength);
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.BusinessDay).HasColumnName("business_day");
        b.Property(x => x.RequestKind).HasColumnName("request_kind").HasMaxLength(AssistantAuditEntry.KeyLength);
        b.Property(x => x.PromptCode).HasColumnName("prompt_code").HasMaxLength(AssistantAuditEntry.KeyLength);
        b.Property(x => x.ToolKey).HasColumnName("tool_key").HasMaxLength(AssistantAuditEntry.KeyLength);
        b.Property(x => x.Source).HasColumnName("source").HasMaxLength(AssistantAuditEntry.KeyLength);
        b.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(AssistantAuditEntry.KeyLength);
        b.Property(x => x.DurationMs).HasColumnName("duration_ms");
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).HasConstraintName("fk_assistant_audit_entries_properties").OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.UserId, x.BusinessDay }).HasDatabaseName("idx_assistant_audit_entries_user_id_business_day");
        b.HasIndex(x => x.PropertyId).HasDatabaseName("idx_assistant_audit_entries_property_id");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("chk_assistant_audit_entries_request_kind", "request_kind IN ('PROMPT','FREE_TEXT','DRAFT','RESIDENT_PROMPT','RESIDENT_FREE')");
            t.HasCheckConstraint("chk_assistant_audit_entries_outcome", "outcome IN ('STARTED','ANSWERED','UNSUPPORTED','FAILED')");
            t.HasCheckConstraint("chk_assistant_audit_entries_duration", "duration_ms IS NULL OR duration_ms >= 0");
        });
    }
}

public sealed class AssistantStore(BuildingFlowDbContext db, ILogger<AssistantStore> logger) : IAssistantStore
{
    private static AssistantPromptView View(AssistantPrompt p) => new(p.Id, p.Code, p.Category, p.ToolKey, p.TitleTr, p.TitleEn, p.TextTr, p.TextEn, p.SortOrder, p.IsEnabled, p.Version);

    public async Task<IReadOnlyList<AssistantPromptView>> Prompts(bool includeDisabled, CancellationToken token)
    {
        var rows = await db.Set<AssistantPrompt>().AsNoTracking().Where(p => includeDisabled || p.IsEnabled).OrderBy(p => p.SortOrder).ThenBy(p => p.Code).ToListAsync(token);
        return rows.Select(View).ToArray();
    }

    public async Task<AssistantPromptView?> ActivePrompt(string code, CancellationToken token) =>
        await db.Set<AssistantPrompt>().AsNoTracking().Where(p => p.Code == code && p.IsEnabled).SingleOrDefaultAsync(token) is { } row ? View(row) : null;

    public async Task<AssistantPromptView> CreatePrompt(AssistantPromptCommand c, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await Lock(token);
        var prior = await db.Set<AssistantPrompt>().AsNoTracking().SingleOrDefaultAsync(p => p.Code == c.Code, token);
        if (prior is not null)
        {
            // A retried create with identical content returns the stored prompt; any other reuse of the code is a duplicate.
            if (Same(prior, c)) return View(prior);
            throw new HierarchyConflictException(ErrorCatalog.AssistantPromptDuplicate.Create());
        }
        var row = new AssistantPrompt(c.Code, c.Category, c.ToolKey, c.TitleTr, c.TitleEn, c.TextTr, c.TextEn, c.SortOrder);
        row.Set(c.Category, c.ToolKey, c.TitleTr, c.TitleEn, c.TextTr, c.TextEn, c.SortOrder, c.IsEnabled);
        db.Add(row);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        logger.LogInformation("BuildingFlow.Assistant prompt created {Code}", row.Code);
        return View(row);
    }

    public async Task<AssistantPromptView?> UpdatePrompt(Guid id, AssistantPromptCommand c, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await Lock(token);
        var row = await db.Set<AssistantPrompt>().SingleOrDefaultAsync(p => p.Id == id, token);
        if (row is null) return null;
        if (row.Code != c.Code) throw new RequestValidationException(new() { ["code"] = [ErrorCatalog.AssistantPromptInvalid.Create()] });
        if (row.Version != c.Version)
        {
            if (Same(row, c)) return View(row);
            throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        }
        row.Set(c.Category, c.ToolKey, c.TitleTr, c.TitleEn, c.TextTr, c.TextEn, c.SortOrder, c.IsEnabled);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        logger.LogInformation("BuildingFlow.Assistant prompt updated {Code} {Version}", row.Code, row.Version);
        return View(row);
    }

    public async Task<bool> DeletePrompt(Guid id, long version, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await Lock(token);
        var row = await db.Set<AssistantPrompt>().SingleOrDefaultAsync(p => p.Id == id, token);
        if (row is null) return false;
        if (row.Version != version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        db.Remove(row);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        logger.LogInformation("BuildingFlow.Assistant prompt deleted {Code}", row.Code);
        return true;
    }

    public async Task<Guid?> Reserve(string userId, Guid property, DateOnly day, int limit, string kind, string? promptCode, IReadOnlyCollection<string> counted, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(token);
        // One counter per user and day: concurrent calls cannot both pass the last free slot.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"assistant-quota:" + userId}, 0))", token);
        if (await db.Set<AssistantAuditEntry>().CountAsync(a => a.UserId == userId && a.BusinessDay == day && counted.Contains(a.RequestKind), token) >= limit) return null;
        var entry = new AssistantAuditEntry(userId, property, day, kind, promptCode);
        db.Add(entry);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return entry.Id;
    }

    public async Task Finish(Guid id, string? tool, string? source, string outcome, int durationMs, CancellationToken token)
    {
        var entry = await db.Set<AssistantAuditEntry>().FindAsync([id], token) ?? throw new InvalidOperationException("Assistant audit entry missing.");
        entry.Finish(tool, source, outcome, durationMs);
        await db.SaveChangesAsync(token);
        logger.LogInformation("BuildingFlow.Assistant call {Tool} {Source} {Outcome} {DurationMs}", tool ?? "-", source ?? "-", outcome, durationMs);
    }

    public Task<int> Used(string userId, DateOnly day, IReadOnlyCollection<string> counted, CancellationToken token) =>
        db.Set<AssistantAuditEntry>().CountAsync(a => a.UserId == userId && a.BusinessDay == day && counted.Contains(a.RequestKind), token);

    private Task Lock(CancellationToken token) => db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"assistant_prompts"}))", token);

    private static bool Same(AssistantPrompt p, AssistantPromptCommand c) =>
        p.Code == c.Code && p.Category == c.Category && p.ToolKey == c.ToolKey && p.TitleTr == c.TitleTr && p.TitleEn == c.TitleEn
        && p.TextTr == c.TextTr && p.TextEn == c.TextEn && p.SortOrder == c.SortOrder && p.IsEnabled == c.IsEnabled;
}

// Supplier facts for the assistant come only from the supplier module's own scoped reads (alerts and advice data).
public sealed class AssistantSupplierSource(BuildingFlowDbContext db, SupplierStore store) : IAssistantSupplierSource
{
    public const int PageSize = 100;
    public async Task<AssistantSupplierData> Read(Guid property, CancellationToken token)
    {
        var tenants = await db.Set<PersonPropertyScope>().AsNoTracking().Where(s => s.PropertyId == property && s.IsEnabled).Select(s => s.TenantId).Distinct().OrderBy(t => t).ToListAsync(token);
        var items = new List<AssistantSupplierItem>(); var workspaces = 0; var truncated = false;
        foreach (var tenant in tenants)
        {
            var overdue = await store.Alerts(tenant, "WORK_ORDER_OVERDUE", 1, PageSize, token);
            if (overdue is null) continue;
            var contracts = await store.Alerts(tenant, "CONTRACT_EXPIRY", 1, PageSize, token);
            var advice = await store.AdviceData(tenant, property, "COST_ANOMALY", null, token);
            workspaces++;
            foreach (var page in new[] { overdue, contracts })
            {
                if (page is null) continue;
                truncated |= page.Truncated || page.TotalCount > PageSize;
                items.AddRange(page.Items.Where(a => a.PropertyId == property).Select(a => new AssistantSupplierItem(a.Kind, a.SupplierName, a.RecordId, a.Title, a.Date, a.Days, null)));
            }
            if (advice is null) continue;
            var names = advice.Suppliers.ToDictionary(s => s.Id, s => s.Name);
            items.AddRange(advice.Bills.Where(b => names.ContainsKey(b.SupplierId)).Select(b => new AssistantSupplierItem("INVOICE_PENDING", names[b.SupplierId], b.Id, "", default, 0, b.Gross)));
        }
        return new(workspaces, items, truncated);
    }
}

// Resident assistant gate: the same ACTIVE rule as the photo features (enabled type, unrevoked, no newer text that asks for re-consent).
public sealed class AssistantConsent(BuildingFlowDbContext db) : IAssistantConsent
{
    public Task<bool> Active(string user, CancellationToken token) => ConsentGate.ActiveAsync(db, user, ConsentFeature.AI_ASSISTANT, token);
}
