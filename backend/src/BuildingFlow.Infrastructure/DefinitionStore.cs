using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;
namespace BuildingFlow.Infrastructure;

internal sealed class DefinitionValueConfiguration : IEntityTypeConfiguration<DefinitionValue>
{
    public void Configure(EntityTypeBuilder<DefinitionValue> b)
    {
        BaseEntityConfiguration.Configure(b, "definition_values");
        b.Property(x => x.ListKey).HasColumnName("list_key").HasMaxLength(DefinitionValue.MaxKeyLength);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(DefinitionValue.MaxKeyLength);
        b.Property(x => x.NameTr).HasColumnName("name_tr").HasMaxLength(DefinitionValue.MaxNameLength);
        b.Property(x => x.NameEn).HasColumnName("name_en").HasMaxLength(DefinitionValue.MaxNameLength);
        b.Property(x => x.IconKey).HasColumnName("icon_key").HasMaxLength(DefinitionValue.MaxKeyLength);
        b.Property(x => x.Tone).HasColumnName("tone").HasMaxLength(DefinitionValue.MaxKeyLength);
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        b.HasIndex(x => new { x.ListKey, x.Code }).IsUnique().HasFilter("NOT is_deleted").HasDatabaseName("uidx_definition_values_list_key_code");
        b.ToTable(t => t.HasCheckConstraint("chk_definition_values_sort_order", $"sort_order BETWEEN 0 AND {DefinitionValue.MaxSortOrder}"));
    }
}

public sealed class DefinitionStore(BuildingFlowDbContext db, ILogger<DefinitionStore> logger) : IDefinitionStore
{
    public async Task<DefinitionsView> ListAsync(CancellationToken token)
    {
        var rows = await db.Set<DefinitionValue>().AsNoTracking().ToListAsync(token);
        var byKey = rows.ToDictionary(x => (x.ListKey, x.Code));
        return new(DefinitionRegistry.Current.Lists.Select(list => View(list, byKey)).ToArray(), DefinitionRegistry.Current.Catalogs);
    }

    public async Task<DefinitionValueView?> SaveAsync(string listKey, string code, SaveDefinitionValueRequest request, CancellationToken token)
    {
        var list = DefinitionRegistry.Current.Lists.SingleOrDefault(x => x.Key == listKey);
        if (list is null || !list.Values.Contains(code)) return null;
        if (!DefinitionRules.Editable(list)) Invalid("listKey");
        var command = Normalize(request);
        if (!command.Enabled && DefinitionRules.Locked(code))
            throw new RequestValidationException(new() { ["isEnabled"] = [ErrorCatalog.DefinitionLocked.Create()] });
        await using var tx = await db.Database.BeginTransactionAsync(token);
        // One writer per list; the unique index stays the last guard for concurrent first edits.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"definition_values:" + listKey}))", token);
        var row = await db.Set<DefinitionValue>().SingleOrDefaultAsync(x => x.ListKey == listKey && x.Code == code, token);
        var current = row?.Version ?? 0;
        if (request.Version != current)
        {
            // A retried request whose content is already stored returns the stored value instead of a conflict.
            if (row is not null && Same(row, command)) return Value(code, row, list.Values.ToList().IndexOf(code));
            throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        }
        if (row is null) { row = new DefinitionValue(listKey, code); db.Add(row); }
        row.Set(command.NameTr, command.NameEn, command.IconKey, command.Tone, command.SortOrder, command.Enabled);
        await db.SaveChangesAsync(token);
        await tx.CommitAsync(token);
        logger.LogInformation("BuildingFlow.Definitions value updated {ListKey} {Code} {Version}", listKey, code, row.Version);
        return Value(code, row, 0);
    }

    private sealed record Command(string? NameTr, string? NameEn, string? IconKey, string? Tone, int SortOrder, bool Enabled);

    private static Command Normalize(SaveDefinitionValueRequest r)
    {
        static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        var issues = new Dictionary<string, ApiErrorMessage[]>();
        void Check(bool ok, string field) { if (!ok) issues[field] = [ErrorCatalog.DefinitionInvalid.Create()]; }
        var nameTr = Text(r.NameTr); var nameEn = Text(r.NameEn); var icon = Text(r.IconKey); var tone = Text(r.Tone);
        Check(nameTr is null || nameTr.Length <= DefinitionValue.MaxNameLength, "nameTr");
        Check(nameEn is null || nameEn.Length <= DefinitionValue.MaxNameLength, "nameEn");
        Check(icon is null || DefinitionRules.IconKeys.Contains(icon), "iconKey");
        Check(tone is null || DefinitionRules.Tones.Contains(tone), "tone");
        Check(r.SortOrder is >= 0 and <= DefinitionValue.MaxSortOrder, "sortOrder");
        Check(r.IsEnabled is not null, "isEnabled");
        Check(r.Version >= 0, "version");
        if (issues.Count > 0) throw new RequestValidationException(issues);
        return new(nameTr, nameEn, icon, tone, r.SortOrder!.Value, r.IsEnabled!.Value);
    }

    private static bool Same(DefinitionValue row, Command c) =>
        row.NameTr == c.NameTr && row.NameEn == c.NameEn && row.IconKey == c.IconKey && row.Tone == c.Tone && row.SortOrder == c.SortOrder && row.IsEnabled == c.Enabled;

    private static void Invalid(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.DefinitionInvalid.Create()] });

    private static DefinitionListView View(DefinitionList list, IReadOnlyDictionary<(string, string), DefinitionValue> rows) =>
        new(list.Key, list.Group, list.Level, list.Source, list.PlannedPhase, DefinitionRules.Editable(list),
            list.Values.Select((code, index) => Value(code, rows.GetValueOrDefault((list.Key, code)), index)).ToArray());

    private static DefinitionValueView Value(string code, DefinitionValue? row, int defaultOrder) => row is null
        ? new(code, null, null, null, null, defaultOrder, true, DefinitionRules.Locked(code), 0)
        : new(code, row.NameTr, row.NameEn, row.IconKey, row.Tone, row.SortOrder, row.IsEnabled, DefinitionRules.Locked(code), row.Version);
}
