using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed record AdviceSupplier(Guid Id, string Name, int SampleCount, int OnTimeCount, bool HasActiveContract, int OpenOverdue);
public sealed record AdviceOrder(Guid SupplierId, Guid Id, DateTimeOffset? AssignedAt, DateTimeOffset DueAt);
public sealed record AdviceBill(Guid SupplierId, Guid Id, decimal Gross, decimal? ContractAmount, IReadOnlyList<decimal> History);
public sealed record AdviceData(IReadOnlyList<AdviceSupplier> Suppliers, IReadOnlyList<AdviceOrder> Orders, IReadOnlyList<AdviceBill> Bills);

public sealed partial class SupplierStore
{
    // Raw facts for the advisor, read only inside the caller's tenant/property scope; the advisor service masks them before any model sees them.
    public async Task<AdviceData?> AdviceData(Guid tenant, Guid property, string kind, Guid? category, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token) || !await PropertyScopes(tenant).AnyAsync(s => s.PropertyId == property, token) || !(await management.EvaluateAsync(tenant, property, "MANAGER", token)).Allowed) return null;
        if (category.HasValue && !await db.Set<SupplierCategory>().AnyAsync(c => c.TenantId == tenant && c.Id == category && c.IsEnabled, token)) return null;
        var now = clock.GetUtcNow(); var since = now.AddDays(-365); var today = ContractToday;
        var linked = db.Set<SupplierManagementArea>().Where(a => a.TenantId == tenant && a.PropertyId == property && a.IsEnabled).Select(a => a.SupplierId);
        var ids = await (category.HasValue
            ? db.Set<SupplierAreaService>().Where(a => a.TenantId == tenant && a.PropertyId == property && a.IsEnabled && linked.Contains(a.SupplierId) && db.Set<BuildingFlow.Domain.SupplierService>().Any(s => s.Id == a.ServiceId && s.CategoryId == category && s.IsEnabled)).Select(a => a.SupplierId)
            : linked).Distinct().ToListAsync(token);
        var active = await Rows(tenant).AsNoTracking().Where(s => ids.Contains(s.Id) && s.Status == SupplierStatus.ACTIVE).Select(s => new { s.Id, s.Name }).ToListAsync(token);
        var activeIds = active.Select(s => s.Id).ToArray();
        var done = await db.Set<SupplierWorkOrder>().AsNoTracking().Where(o => o.TenantId == tenant && activeIds.Contains(o.SupplierId) && o.CompletedAt >= since && o.DueAt != null).Select(o => new { o.SupplierId, OnTime = o.CompletedAt <= o.DueAt }).ToListAsync(token);
        var visits = await db.Set<SupplierMaintenanceOccurrence>().AsNoTracking().Where(o => o.TenantId == tenant && activeIds.Contains(o.SupplierId) && o.Status == SupplierMaintenanceOccurrenceStatus.COMPLETED && o.CompletedOn >= DateOnly.FromDateTime(since.UtcDateTime)).Select(o => new { o.SupplierId, OnTime = o.DelayDays == 0 }).ToListAsync(token);
        var open = await db.Set<SupplierWorkOrder>().AsNoTracking().Where(o => o.TenantId == tenant && o.PropertyId == property && activeIds.Contains(o.SupplierId) && o.DueAt != null && (o.Status == SupplierWorkOrderStatus.ASSIGNED || o.Status == SupplierWorkOrderStatus.ACCEPTED || o.Status == SupplierWorkOrderStatus.IN_PROGRESS)).Select(o => new AdviceOrder(o.SupplierId, o.Id, o.AssignedAt, o.DueAt!.Value)).Take(200).ToListAsync(token);
        var contracts = await db.Set<SupplierContract>().AsNoTracking().Where(c => c.TenantId == tenant && c.PropertyId == property && activeIds.Contains(c.SupplierId) && c.Status == SupplierContractStatus.ACTIVE && c.StartDate <= today && c.EndDate >= today).Select(c => new { c.SupplierId, c.Id, c.Amount }).ToListAsync(token);
        var bills = new List<AdviceBill>();
        if (kind == "COST_ANOMALY")
        {
            var pending = await db.Set<SupplierInvoice>().AsNoTracking().Where(i => i.TenantId == tenant && i.PropertyId == property && activeIds.Contains(i.SupplierId) && (i.Status == SupplierInvoiceStatus.RECORDED || i.Status == SupplierInvoiceStatus.APPROVED)).OrderByDescending(i => i.IssueDate).Take(100).Select(i => new { i.SupplierId, i.Id, Gross = i.GrossAmount, i.Category, i.ContractId, i.IssueDate }).ToListAsync(token);
            var history = await db.Set<SupplierInvoice>().AsNoTracking().Where(i => i.TenantId == tenant && i.PropertyId == property && activeIds.Contains(i.SupplierId) && (i.Status == SupplierInvoiceStatus.PAID || i.Status == SupplierInvoiceStatus.RECONCILED) && i.IssueDate >= DateOnly.FromDateTime(since.UtcDateTime)).Select(i => new { i.SupplierId, i.Category, i.GrossAmount }).ToListAsync(token);
            bills.AddRange(pending.Select(p => new AdviceBill(p.SupplierId, p.Id, p.Gross, p.ContractId.HasValue ? contracts.FirstOrDefault(c => c.Id == p.ContractId)?.Amount : null, history.Where(h => h.SupplierId == p.SupplierId && h.Category == p.Category).Select(h => h.GrossAmount).ToArray())));
        }
        await tx.CommitAsync(token);
        var suppliers = active.Select(s =>
        {
            var w = done.Where(x => x.SupplierId == s.Id).ToList(); var v = visits.Where(x => x.SupplierId == s.Id).ToList();
            return new AdviceSupplier(s.Id, s.Name, w.Count + v.Count, w.Count(x => x.OnTime) + v.Count(x => x.OnTime), contracts.Any(c => c.SupplierId == s.Id), open.Count(o => o.SupplierId == s.Id && o.DueAt < now));
        }).ToArray();
        return new(suppliers, open, bills);
    }
}

// Per-user daily budget so an advisor backed by a paid model cannot be called without bound.
public sealed class SupplierAdviceBudget(TimeProvider clock, int dailyLimit = SupplierAdviceBudget.DefaultLimit)
{
    public const int DefaultLimit = 20;
    private readonly ConcurrentDictionary<(Guid Tenant, string Actor, DateOnly Day), int> used = new();
    public int Limit => dailyLimit;
    public bool TryTake(Guid tenant, string actor, out int remaining)
    {
        var key = (tenant, actor, BusinessDate.Today(clock)); var count = used.AddOrUpdate(key, 1, (_, n) => n + 1);
        if (count > dailyLimit) { used.AddOrUpdate(key, dailyLimit, (_, _) => dailyLimit); remaining = 0; return false; }
        remaining = dailyLimit - count; return true;
    }
}

// Deterministic default model: explainable thresholds over masked numbers, no network and no free text.
public sealed class RuleBasedSupplierAdviceModel : ISupplierAdviceModel
{
    public string Source => "RULES";
    public Task<IReadOnlyList<AdviceSuggestion>> Suggest(AdviceInput input, CancellationToken token)
    {
        var result = new List<AdviceSuggestion>();
        static string Pct(int part, int whole) => whole == 0 ? "-" : (100m * part / whole).ToString("F1", CultureInfo.InvariantCulture);
        switch (input.Kind)
        {
            case "MATCH":
                foreach (var c in input.Candidates.OrderByDescending(c => c.SampleCount >= 3 ? (double)c.OnTimeCount / c.SampleCount : -1).ThenByDescending(c => c.HasActiveContract).ThenBy(c => c.OpenOverdue).ThenBy(c => c.Alias, StringComparer.Ordinal))
                {
                    var known = c.SampleCount >= 3; var rate = known ? (double)c.OnTimeCount / c.SampleCount : 0;
                    var level = !known ? "LOW" : rate >= .8 && c.OpenOverdue == 0 ? "HIGH" : rate >= .6 ? "MEDIUM" : "LOW";
                    result.Add(new(c.Alias, null, level, known ? "MATCH_TRACK_RECORD" : "MATCH_INSUFFICIENT_SAMPLE", new Dictionary<string, string> { ["onTimeRate"] = Pct(c.OnTimeCount, c.SampleCount), ["activeContract"] = c.HasActiveContract ? "true" : "false", ["openOverdue"] = c.OpenOverdue.ToString(CultureInfo.InvariantCulture) }, c.SampleCount));
                }
                break;
            case "DELAY_RISK":
                foreach (var o in input.OpenOrders)
                {
                    var c = input.Candidates.FirstOrDefault(x => x.Alias == o.Alias); var poor = c is { SampleCount: >= 3 } && (double)c.OnTimeCount / c.SampleCount < .6;
                    var level = o.HoursLeft <= 0 ? "HIGH" : o.ElapsedShare >= .75 || poor ? "MEDIUM" : "LOW";
                    if (level == "LOW") continue;
                    result.Add(new(o.Alias, o.OrderAlias, level, o.HoursLeft <= 0 ? "DELAY_SLA_PASSED" : poor ? "DELAY_SUPPLIER_TRACK_RECORD" : "DELAY_DEADLINE_CLOSE", new Dictionary<string, string> { ["hoursLeft"] = o.HoursLeft.ToString("F1", CultureInfo.InvariantCulture), ["elapsedShare"] = o.ElapsedShare.ToString("F2", CultureInfo.InvariantCulture), ["onTimeRate"] = c is null ? "-" : Pct(c.OnTimeCount, c.SampleCount) }, c?.SampleCount ?? 0));
                }
                break;
            case "COST_ANOMALY":
                foreach (var b in input.Invoices)
                {
                    var sorted = b.History.OrderBy(x => x).ToArray(); decimal? median = sorted.Length >= 3 ? sorted[sorted.Length / 2] : null;
                    var overContract = b.ContractAmount.HasValue && b.Gross > b.ContractAmount.Value; var overHistory = median.HasValue && b.Gross > median.Value * 1.5m;
                    if (!overContract && !overHistory) continue;
                    result.Add(new(b.Alias, b.InvoiceAlias, overContract && overHistory ? "HIGH" : "MEDIUM", overContract ? "COST_ABOVE_CONTRACT" : "COST_ABOVE_HISTORY", new Dictionary<string, string> { ["gross"] = b.Gross.ToString("F2", CultureInfo.InvariantCulture), ["median"] = median?.ToString("F2", CultureInfo.InvariantCulture) ?? "-", ["contract"] = b.ContractAmount?.ToString("F2", CultureInfo.InvariantCulture) ?? "-" }, b.History.Count));
                }
                break;
        }
        return Task.FromResult<IReadOnlyList<AdviceSuggestion>>(result);
    }
}

public sealed class SupplierAdviceService(SupplierStore store, ISupplierAdviceModel model, SupplierAdviceBudget budget, ICurrentUserService actor, TimeProvider clock) : ISupplierAdviceStore
{
    public const int MaxItems = 20;
    private static readonly string[] Kinds = ["MATCH", "DELAY_RISK", "COST_ANOMALY"];
    private static readonly string[] Levels = ["LOW", "MEDIUM", "HIGH"];
    private static readonly string[] Reasons = ["MATCH_TRACK_RECORD", "MATCH_INSUFFICIENT_SAMPLE", "DELAY_SLA_PASSED", "DELAY_SUPPLIER_TRACK_RECORD", "DELAY_DEADLINE_CLOSE", "COST_ABOVE_CONTRACT", "COST_ABOVE_HISTORY"];
    private static readonly string[] EvidenceKeys = ["onTimeRate", "activeContract", "openOverdue", "hoursLeft", "elapsedShare", "gross", "median", "contract"];
    private static void Invalid(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.SupplierInvalid.Create()] });
    public static AdviceInput Mask(string kind, AdviceData data, DateTimeOffset now, out Dictionary<string, Guid> suppliers, out Dictionary<string, Guid> records)
    {
        var s = new Dictionary<string, Guid>(); var r = new Dictionary<string, Guid>();
        var alias = data.Suppliers.OrderBy(x => x.Id).Select((x, i) => (x, Alias: "S" + (i + 1))).ToDictionary(x => x.x.Id, x => x.Alias);
        foreach (var (id, a) in alias) s[a] = id;
        var orders = data.Orders.Where(o => alias.ContainsKey(o.SupplierId)).OrderBy(o => o.DueAt).Select((o, i) =>
        {
            var key = "W" + (i + 1); r[key] = o.Id; var span = o.AssignedAt.HasValue ? (o.DueAt - o.AssignedAt.Value).TotalHours : 0;
            return new AdviceOpenOrder(alias[o.SupplierId], key, span <= 0 ? 1 : Math.Clamp((now - o.AssignedAt!.Value).TotalHours / span, 0, 10), Math.Round((o.DueAt - now).TotalHours, 1));
        }).ToArray();
        var bills = data.Bills.Where(b => alias.ContainsKey(b.SupplierId)).Select((b, i) => { var key = "F" + (i + 1); r[key] = b.Id; return new AdviceInvoice(alias[b.SupplierId], key, b.Gross, b.ContractAmount, b.History); }).ToArray();
        suppliers = s; records = r;
        return new(kind, data.Suppliers.Select(x => new AdviceCandidate(alias[x.Id], x.SampleCount, x.OnTimeCount, x.HasActiveContract, x.OpenOverdue)).ToArray(), kind == "DELAY_RISK" ? orders : [], kind == "COST_ANOMALY" ? bills : []);
    }
    // Model output is untrusted: only known aliases, closed codes and evidence keys survive, and any proposed action is dropped.
    public static IReadOnlyList<AdviceSuggestion> Sanitize(string kind, IReadOnlyList<AdviceSuggestion> raw, IReadOnlyDictionary<string, Guid> suppliers, IReadOnlyDictionary<string, Guid> records)
    {
        var prefix = kind == "MATCH" ? "MATCH_" : kind == "DELAY_RISK" ? "DELAY_" : "COST_";
        return raw.Where(x => x is not null && x.Action is null && suppliers.ContainsKey(x.Alias) && Levels.Contains(x.Level) && Reasons.Contains(x.ReasonCode) && x.ReasonCode.StartsWith(prefix, StringComparison.Ordinal)
                && (kind == "MATCH" ? x.RecordAlias is null : x.RecordAlias is not null && records.ContainsKey(x.RecordAlias)) && x.SampleCount is >= 0 and <= 100000
                && x.Evidence.Count <= EvidenceKeys.Length && x.Evidence.All(e => EvidenceKeys.Contains(e.Key) && e.Value.Length <= 20 && e.Value.All(c => char.IsAsciiDigit(c) || c is '.' or '-' || char.IsAsciiLetterLower(c))))
            .DistinctBy(x => (x.Alias, x.RecordAlias)).Take(MaxItems).ToArray();
    }
    public async Task<SupplierAdviceResponse?> Advise(Guid tenant, SupplierAdviceRequest c, CancellationToken token)
    {
        if (!Kinds.Contains(c.Kind) || c.PropertyId == Guid.Empty || c.CategoryId == Guid.Empty || c.Kind != "MATCH" && c.CategoryId.HasValue) Invalid("kind");
        if (!c.Consent) Invalid("consent");
        if (c.Note is { Length: > 500 }) Invalid("note");
        var data = await store.AdviceData(tenant, c.PropertyId, c.Kind, c.CategoryId, token); if (data is null) return null;
        if (!budget.TryTake(tenant, actor.Issuer + "|" + actor.UserId, out var remaining)) throw new RateLimitedException();
        // The free-text note is never forwarded to a model: it cannot widen scope or inject instructions.
        var input = Mask(c.Kind, data, clock.GetUtcNow(), out var suppliers, out var records);
        var safe = Sanitize(c.Kind, await model.Suggest(input, token), suppliers, records);
        var names = data.Suppliers.ToDictionary(x => x.Id, x => x.Name);
        return new(model.Source, safe.Select(x => new SupplierAdviceItem(c.Kind, suppliers[x.Alias], names[suppliers[x.Alias]], x.RecordAlias is null ? null : records[x.RecordAlias], x.Level, x.ReasonCode, x.Evidence, x.SampleCount)).ToArray(), remaining, true);
    }
}
public sealed class RateLimitedException() : Exception("Advice budget exceeded.");
