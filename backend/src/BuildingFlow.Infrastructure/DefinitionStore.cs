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

internal sealed class PropertyDefinitionSettingConfiguration : IEntityTypeConfiguration<PropertyDefinitionSetting>
{
    public void Configure(EntityTypeBuilder<PropertyDefinitionSetting> b)
    {
        BaseEntityConfiguration.Configure(b, "property_definition_settings");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.ListKey).HasColumnName("list_key").HasMaxLength(DefinitionValue.MaxKeyLength);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(DefinitionValue.MaxKeyLength);
        b.Property(x => x.IsHidden).HasColumnName("is_hidden");
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.ListKey, x.Code }).IsUnique().HasFilter("NOT is_deleted").HasDatabaseName("uidx_property_definition_settings_property_list_code");
        b.ToTable(t => t.HasCheckConstraint("chk_property_definition_settings_sort_order", $"sort_order IS NULL OR sort_order BETWEEN 0 AND {DefinitionValue.MaxSortOrder}"));
    }
}

// Write-side guard used by module stores: a new or changed code must be enabled system-wide and not hidden in the area.
// Keeping the record's current code is always allowed so old records stay editable.
public static class DefinitionPolicy
{
    public static async Task EnsureSelectableAsync(BuildingFlowDbContext db, Guid property, string listKey, string code, string? current, string field, CancellationToken token)
    {
        if (code == current || DefinitionRules.Locked(code)) return;
        var disabled = await db.Set<DefinitionValue>().AnyAsync(x => x.ListKey == listKey && x.Code == code && !x.IsEnabled, token);
        var hidden = !disabled && await db.Set<PropertyDefinitionSetting>().AnyAsync(x => x.PropertyId == property && x.ListKey == listKey && x.Code == code && x.IsHidden && x.IsEnabled, token);
        if (disabled || hidden) throw new RequestValidationException(new() { [field] = [ErrorCatalog.DefinitionUnavailable.Create()] });
    }
}

public sealed class DefinitionStore(BuildingFlowDbContext db, ILogger<DefinitionStore> logger) : IDefinitionStore
{
    private IQueryable<Property> Properties(AccessScope scope) => db.Properties.Where(p => !p.IsDeleted && (scope.ReadAll || db.PropertyMemberships.Any(m => m.PropertyId == p.Id && m.UserId == scope.UserId && m.IsEnabled && !m.IsDeleted)));

    public async Task<PropertyDefinitionsView?> PropertyListAsync(Guid propertyId, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(p => p.Id == propertyId, token)) return null;
        var system = (await db.Set<DefinitionValue>().AsNoTracking().ToListAsync(token)).ToDictionary(x => (x.ListKey, x.Code));
        var local = (await db.Set<PropertyDefinitionSetting>().AsNoTracking().Where(x => x.PropertyId == propertyId && x.IsEnabled).ToListAsync(token)).ToDictionary(x => (x.ListKey, x.Code));
        var lists = DefinitionRegistry.Current.Lists.Where(DefinitionRules.Editable).Select(list => new PropertyDefinitionListView(list.Key, list.Group, list.Level,
            list.Values.Select((code, index) => PropertyValue(list.Key, code, index, system.GetValueOrDefault((list.Key, code)), local.GetValueOrDefault((list.Key, code))))
                .OrderBy(x => x.SortOrder).ThenBy(x => list.Values.ToList().IndexOf(x.Code)).ToArray())).ToArray();
        return new(propertyId, lists);
    }

    public async Task<PropertyDefinitionValueView?> SavePropertyAsync(Guid propertyId, string listKey, string code, SavePropertyDefinitionRequest request, AccessScope access, CancellationToken token)
    {
        var list = DefinitionRegistry.Current.Lists.SingleOrDefault(x => x.Key == listKey);
        if (list is null || !list.Values.Contains(code)) return null;
        if (list.Level != nameof(DefinitionLevel.PROPERTY) || !DefinitionRules.Editable(list)) Invalid("listKey");
        if (request.IsHidden is null || request.SortOrder is < 0 or > DefinitionValue.MaxSortOrder || request.Version < 0)
            throw new RequestValidationException(new() { [request.IsHidden is null ? "isHidden" : request.Version < 0 ? "version" : "sortOrder"] = [ErrorCatalog.DefinitionInvalid.Create()] });
        if (request.IsHidden.Value && DefinitionRules.Locked(code))
            throw new RequestValidationException(new() { ["isHidden"] = [ErrorCatalog.DefinitionLocked.Create()] });
        await using var tx = await db.Database.BeginTransactionAsync(token);
        if (!await Properties(access).AnyAsync(p => p.Id == propertyId, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"property_definition_settings:" + propertyId + ":" + listKey}))", token);
        var row = await db.Set<PropertyDefinitionSetting>().SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.ListKey == listKey && x.Code == code, token);
        var index = list.Values.ToList().IndexOf(code);
        var system = await db.Set<DefinitionValue>().AsNoTracking().SingleOrDefaultAsync(x => x.ListKey == listKey && x.Code == code, token);
        if (request.Version != (row?.Version ?? 0))
        {
            if (row is not null && row.IsHidden == request.IsHidden && row.SortOrder == request.SortOrder) return PropertyValue(listKey, code, index, system, row);
            throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        }
        if (row is null) { row = new PropertyDefinitionSetting(propertyId, listKey, code); db.Add(row); }
        row.Set(request.IsHidden.Value, request.SortOrder);
        await db.SaveChangesAsync(token);
        await tx.CommitAsync(token);
        logger.LogInformation("BuildingFlow.Definitions area setting updated {ListKey} {Code} {Version}", listKey, code, row.Version);
        return PropertyValue(listKey, code, index, system, row);
    }

    private static PropertyDefinitionValueView PropertyValue(string listKey, string code, int index, DefinitionValue? system, PropertyDefinitionSetting? local)
    {
        var enabled = system?.IsEnabled ?? true; var hidden = local?.IsHidden ?? false; var locked = DefinitionRules.Locked(code);
        return new(code, system?.NameTr, system?.NameEn, system?.IconKey, system?.Tone, local?.SortOrder ?? system?.SortOrder ?? index, enabled, hidden, locked || enabled && !hidden, locked, local?.Version ?? 0);
    }

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

    public async Task<IReadOnlyList<CancellationReasonAdminRow>> CancellationReasonsAsync(CancellationToken token)
    {
        var usage = await db.DuesCharges.Where(x => x.CancellationReasonId != null).GroupBy(x => x.CancellationReasonId!.Value).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, token);
        var rows = await db.DuesCancellationReasons.AsNoTracking().OrderBy(x => x.Code).ToListAsync(token);
        return rows.Select(x => Reason(x, usage.GetValueOrDefault(x.Id))).ToArray();
    }

    public async Task<CancellationReasonAdminRow> CreateCancellationReasonAsync(SaveCancellationReasonRequest request, CancellationToken token)
    {
        var code = request.Code?.Trim() ?? "";
        var command = ReasonCommand(request, create: true);
        if (!System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Z][A-Z0-9_]{1,49}$")) ReasonInvalid("code");
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"dues_cancellation_reasons"}))", token);
        var prior = await db.DuesCancellationReasons.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Code == code, token);
        if (prior is not null)
        {
            // A retried create with identical content returns the stored reason; any other use of the code is a duplicate.
            if (!prior.IsDeleted && prior.Name == command.Name && prior.RequiresExplanation == command.RequiresExplanation && prior.IsEnabled == command.Enabled)
                return Reason(prior, await Usage(prior.Id, token));
            throw new HierarchyConflictException(ErrorCatalog.DefinitionDuplicate.Create());
        }
        var reason = new DuesCancellationReason(code, command.Name, command.RequiresExplanation); reason.SetEnabled(command.Enabled);
        db.Add(reason); await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        logger.LogInformation("BuildingFlow.Definitions cancellation reason created {Code}", code);
        return Reason(reason, 0);
    }

    public async Task<CancellationReasonAdminRow?> SaveCancellationReasonAsync(Guid id, SaveCancellationReasonRequest request, CancellationToken token)
    {
        var command = ReasonCommand(request, create: false);
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"dues_cancellation_reasons"}))", token);
        var reason = await db.DuesCancellationReasons.SingleOrDefaultAsync(x => x.Id == id, token);
        if (reason is null) return null;
        if (request.Code is not null && request.Code.Trim() != reason.Code) ReasonInvalid("code");
        if (reason.Locked && (!command.Enabled || command.RequiresExplanation != reason.RequiresExplanation))
            throw new RequestValidationException(new() { [!command.Enabled ? "isEnabled" : "requiresExplanation"] = [ErrorCatalog.DefinitionLocked.Create()] });
        if (request.Version != reason.Version)
        {
            if (reason.Name == command.Name && reason.RequiresExplanation == command.RequiresExplanation && reason.IsEnabled == command.Enabled) return Reason(reason, await Usage(id, token));
            throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        }
        reason.Edit(command.Name, command.RequiresExplanation); reason.SetEnabled(command.Enabled);
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        logger.LogInformation("BuildingFlow.Definitions cancellation reason updated {Code} {Version}", reason.Code, reason.Version);
        return Reason(reason, await Usage(id, token));
    }

    private sealed record ReasonInput(string Name, bool RequiresExplanation, bool Enabled);
    private static ReasonInput ReasonCommand(SaveCancellationReasonRequest r, bool create)
    {
        var name = r.Name?.Trim();
        var issues = new Dictionary<string, ApiErrorMessage[]>();
        if (string.IsNullOrEmpty(name) || name.Length > DuesCancellationReason.MaxNameLength || name.Any(char.IsControl)) issues["name"] = [ErrorCatalog.DefinitionInvalid.Create()];
        if (r.RequiresExplanation is null) issues["requiresExplanation"] = [ErrorCatalog.DefinitionInvalid.Create()];
        if (r.IsEnabled is null) issues["isEnabled"] = [ErrorCatalog.DefinitionInvalid.Create()];
        if (create ? r.Version != 0 : r.Version < 1) issues["version"] = [ErrorCatalog.DefinitionInvalid.Create()];
        if (issues.Count > 0) throw new RequestValidationException(issues);
        return new(name!, r.RequiresExplanation!.Value, r.IsEnabled!.Value);
    }
    private static void ReasonInvalid(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.DefinitionInvalid.Create()] });
    private Task<int> Usage(Guid id, CancellationToken token) => db.DuesCharges.CountAsync(x => x.CancellationReasonId == id, token);
    private static CancellationReasonAdminRow Reason(DuesCancellationReason x, int usage) => new(x.Id, x.Code, x.Name, x.RequiresExplanation, x.IsEnabled, x.Locked, usage, x.Version);

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
