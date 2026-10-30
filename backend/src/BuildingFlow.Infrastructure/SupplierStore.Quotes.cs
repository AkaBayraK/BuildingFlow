using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed partial class SupplierStore
{
    private IQueryable<SupplierInquiry> InquiryRows(Guid tenant, Guid supplier) => db.Set<SupplierInquiry>().Where(x => x.TenantId == tenant && db.Set<SupplierInvitation>().Any(i => i.TenantId == tenant && i.InquiryId == x.Id && i.PropertyId == x.PropertyId && i.SupplierId == supplier));
    private static string Money(decimal x) => x.ToString("F2", CultureInfo.InvariantCulture);
    private static decimal QuoteNumber(string? value, bool rate)
    {
        if (value is not { Length: <= 20 } || !Regex.IsMatch(value, @"^[0-9]+(?:\.[0-9]{1,2})?$") || !decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number) || (rate ? number is < 0 or > 100 : number is < .01m or > 99999999.99m))
        { InvalidSupplier(rate ? "vatRate" : "netAmount"); return 0; }
        return number;
    }
    private sealed class QuoteCandidateData
    {
        public Guid AreaServiceId { get; init; }
        public Guid SupplierId { get; init; }
        public string SupplierName { get; init; } = null!;
        public string ServiceName { get; init; } = null!;
    }
    private IQueryable<QuoteCandidateData> CandidateRows(Guid tenant, Guid property, Guid category) =>
        from a in db.Set<SupplierAreaService>()
        join s in db.Set<BuildingFlow.Domain.SupplierService>() on new { a.TenantId, a.SupplierId, Id = a.ServiceId } equals new { s.TenantId, s.SupplierId, s.Id }
        join parent in db.Set<Supplier>() on new { a.TenantId, Id = a.SupplierId } equals new { parent.TenantId, parent.Id }
        where a.TenantId == tenant && a.PropertyId == property && a.IsEnabled && s.IsEnabled && s.CategoryId == category && parent.Status == SupplierStatus.ACTIVE && db.Set<SupplierCategory>().Any(c => c.TenantId == tenant && c.Id == category && c.IsEnabled) && db.Set<SupplierManagementArea>().Any(link => link.TenantId == tenant && link.PropertyId == property && link.SupplierId == a.SupplierId && link.IsEnabled)
        select new QuoteCandidateData { AreaServiceId = a.Id, SupplierId = a.SupplierId, SupplierName = parent.Name, ServiceName = s.Name };
    public async Task<PagedRows<SupplierQuoteCandidate>?> QuoteCandidates(Guid tenant, Guid supplier, Guid property, Guid category, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize);
        if (!await AreaAllowed(tenant, supplier, property, token)) return null;
        // Sort before the constructor projection: EF cannot always translate ordering a record DTO.
        var rows = CandidateRows(tenant, property, category); var total = await rows.CountAsync(token);
        var items = await rows.OrderBy(x => x.AreaServiceId).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(token);
        return new(items.Select(x => new SupplierQuoteCandidate(x.AreaServiceId, x.SupplierId, x.SupplierName, x.ServiceName)).ToArray(), page, pageSize, total);
    }
    private static SupplierInquirySummary InquirySummary(SupplierInquiry x, string propertyName) => new(x.Id, x.TenantId, x.PropertyId, propertyName, x.CategoryId, x.Title, x.Deadline, x.Status.ToString(), x.Version);
    private async Task<SupplierInquiryDetail> InquiryResponse(SupplierInquiry row, CancellationToken token)
    {
        var propertyName = await db.Properties.Where(x => x.Id == row.PropertyId).Select(x => x.Name).SingleAsync(token);
        var rows = await (from i in db.Set<SupplierInvitation>().AsNoTracking()
                          join s in db.Set<Supplier>() on new { i.TenantId, Id = i.SupplierId } equals new { s.TenantId, s.Id }
                          join a in db.Set<SupplierAreaService>() on new { i.TenantId, i.PropertyId, i.SupplierId, Id = i.AreaServiceId } equals new { a.TenantId, a.PropertyId, a.SupplierId, a.Id }
                          join service in db.Set<BuildingFlow.Domain.SupplierService>() on new { a.TenantId, a.SupplierId, Id = a.ServiceId } equals new { service.TenantId, service.SupplierId, service.Id }
                          join quote in db.Set<SupplierQuote>() on i.Id equals quote.InvitationId into quotes
                          from q in quotes.DefaultIfEmpty()
                          where i.TenantId == row.TenantId && i.PropertyId == row.PropertyId && i.InquiryId == row.Id
                          orderby s.NameKey, i.Id
                          select new { Invitation = i, SupplierName = s.Name, ServiceName = service.Name, Quote = q }).ToArrayAsync(token);
        var values = rows.Select(x => new SupplierQuoteRow(x.Invitation.Id, x.Invitation.SupplierId, x.SupplierName, x.Invitation.AreaServiceId, x.ServiceName,
            x.Quote?.Id, x.Quote is null ? null : Money(x.Quote.NetAmount), x.Quote is null ? null : Money(x.Quote.VatRate), x.Quote is null ? null : Money(x.Quote.VatAmount), x.Quote is null ? null : Money(x.Quote.GrossAmount), x.Quote?.Currency, x.Quote?.ValidUntil, x.Quote?.Notes,
            x.Quote is null ? "AWAITING" : x.Quote.Status == SupplierQuoteStatus.PENDING && row.Status == SupplierInquiryStatus.AWARDED ? "NOT_SELECTED" : x.Quote.Status.ToString(), x.Quote?.DecisionReason, x.Quote?.IsEnteredBy(actor.Issuer!, actor.UserId!) ?? false)).ToArray();
        var order = await db.Set<SupplierWorkOrder>().AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == row.TenantId && x.PropertyId == row.PropertyId && x.InquiryId == row.Id, token);
        return new(InquirySummary(row, propertyName), row.Description, row.CancellationReason, values, order is null ? null : new(order.Id, order.QuoteId!.Value, order.Status.ToString(), order.Title, Money(order.Amount!.Value), order.Currency!, order.Version));
    }
    public async Task<PagedRows<SupplierInquirySummary>?> Inquiries(Guid tenant, Guid supplier, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token) || !await Rows(tenant).AnyAsync(x => x.Id == supplier, token)) return null;
        var scopes = PropertyScopes(tenant);
        var rows = from i in InquiryRows(tenant, supplier).AsNoTracking() join p in db.Properties on i.PropertyId equals p.Id where scopes.Any(s => s.PropertyId == i.PropertyId) select new { Record = i, PropertyName = p.Name };
        var count = await rows.CountAsync(token); var items = await rows.OrderByDescending(x => x.Record.CreatedAt).ThenBy(x => x.Record.Id).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items.Select(x => InquirySummary(x.Record, x.PropertyName)).ToArray(), page, pageSize, count);
    }
    public async Task<SupplierInquiryDetail?> Inquiry(Guid tenant, Guid supplier, Guid id, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token)) return null;
        var row = await InquiryRows(tenant, supplier).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token);
        if (row is null || !await AreaAllowed(tenant, supplier, row.PropertyId, token)) return null;
        var result = await InquiryResponse(row, token); await tx.CommitAsync(token); return result;
    }
    private async Task<SupplierInquiryMutation?> InquiryReplay(Guid tenant, Guid supplier, Guid operation, string fingerprint, CancellationToken token)
    {
        var receipt = await db.Set<SupplierInquiryRevision>().AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenant && x.ActorIssuer == actor.Issuer && x.ActorSubject == actor.UserId && x.OperationId == operation, token);
        if (receipt is null) return null;
        if (receipt.Fingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        var row = await InquiryRows(tenant, supplier).SingleOrDefaultAsync(x => x.Id == receipt.InquiryId && x.PropertyId == receipt.PropertyId, token);
        if (row is null || !await AreaAllowed(tenant, supplier, row.PropertyId, token)) return null;
        return new(await InquiryResponse(row, token), true, receipt.AppliedVersion);
    }
    private void InquiryRevision(SupplierInquiry row, Guid operation, string fingerprint, string action, object snapshot, long applied)
    {
        db.Add(new SupplierInquiryRevision(row, operation, actor.Issuer!, actor.UserId!, fingerprint, action, JsonSerializer.Serialize(snapshot), applied));
        if (db.Entry(row).State != EntityState.Added) db.Entry(row).Property(x => x.IsEnabled).IsModified = true;
    }
    public async Task<SupplierInquiryMutation?> CreateInquiry(Guid tenant, Guid supplier, SupplierInquiryRequest c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.SupplierVersion < 1 || c.PropertyId == Guid.Empty || c.CategoryId == Guid.Empty || c.AreaServiceIds is not { Length: >= 1 and <= 20 } || c.AreaServiceIds.Any(x => x == Guid.Empty) || c.AreaServiceIds.Distinct().Count() != c.AreaServiceIds.Length) InvalidSupplier("inquiry");
        SupplierInquiry row;
        try { row = new(tenant, c.PropertyId, c.CategoryId, c.Title, c.Description, c.Deadline); } catch (ArgumentException) { InvalidSupplier("inquiry"); throw; }
        await using var tx = await db.Database.BeginTransactionAsync(token); var scope = await LockArea(tenant, supplier, c.PropertyId, true, token); if (scope is null) return null;
        var ids = c.AreaServiceIds!.Order().ToArray();
        var fingerprint = Hash(new { action = "INQUIRY_CREATE", tenant, supplier, c.SupplierVersion, c.PropertyId, c.CategoryId, row.Title, row.Description, c.Deadline, ids });
        var replay = await InquiryReplay(tenant, supplier, c.OperationId, fingerprint, token); if (replay is not null) { await tx.CommitAsync(token); return replay; }
        Expected(c.SupplierVersion, scope.Value.Supplier.Version);
        if (c.Deadline < ContractToday) InvalidSupplier("deadline");
        var candidates = await CandidateRows(tenant, c.PropertyId, c.CategoryId).Where(x => ids.Contains(x.AreaServiceId)).ToArrayAsync(token);
        if (candidates.Length != ids.Length || candidates.Select(x => x.SupplierId).Distinct().Count() != ids.Length || !candidates.Any(x => x.SupplierId == supplier)) InvalidSupplier("invitations");
        db.Add(row); foreach (var item in candidates) db.Add(new SupplierInvitation(row, item.SupplierId, item.AreaServiceId));
        InquiryRevision(row, c.OperationId, fingerprint, "CREATE", new { row.Title, row.Description, row.Deadline, row.CategoryId, AreaServiceIds = ids }, 1); TouchArea(scope.Value);
        await db.SaveChangesAsync(token); var result = new SupplierInquiryMutation(await InquiryResponse(row, token), false, row.Version); await tx.CommitAsync(token); return result;
    }
    private async Task<(SupplierInquiry Inquiry, Supplier Supplier, Property Property)?> LockInquiry(Guid tenant, Guid supplier, Guid id, CancellationToken token)
    {
        if (!await Allowed(tenant, token)) return null;
        var property = await InquiryRows(tenant, supplier).Where(x => x.Id == id).Select(x => (Guid?)x.PropertyId).SingleOrDefaultAsync(token); if (property is null) return null;
        var scope = await LockArea(tenant, supplier, property.Value, false, token); if (scope is null) return null;
        var row = await InquiryRows(tenant, supplier).SingleOrDefaultAsync(x => x.Id == id, token); return row is null ? null : (row, scope.Value.Supplier, scope.Value.Property);
    }
    public async Task<SupplierInquiryMutation?> EnterQuote(Guid tenant, Guid supplier, Guid id, SupplierQuoteRequest c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.Version < 1 || c.InvitationId == Guid.Empty) InvalidSupplier("version");
        var net = QuoteNumber(c.NetAmount, false); var rate = QuoteNumber(c.VatRate, true);
        if (c.Currency is not ("TRY" or "USD" or "EUR") || c.ValidUntil.Year is < 2000 or > 2100 || string.IsNullOrWhiteSpace(c.Notes) || c.Notes.Trim().Length > 2000 || c.Notes.Any(char.IsControl) || decimal.Round(net * rate / 100m, 2, MidpointRounding.AwayFromZero) + net > 99999999.99m) InvalidSupplier("quote");
        await using var tx = await db.Database.BeginTransactionAsync(token); var scope = await LockInquiry(tenant, supplier, id, token); if (scope is null) return null; var inquiry = scope.Value.Inquiry;
        var fingerprint = Hash(new { action = "INQUIRY_QUOTE", tenant, supplier, id, c.Version, c.InvitationId, net = Money(net), rate = Money(rate), c.Currency, c.ValidUntil, notes = c.Notes.Trim() });
        var replay = await InquiryReplay(tenant, supplier, c.OperationId, fingerprint, token); if (replay is not null) { await tx.CommitAsync(token); return replay; }
        Expected(c.Version, inquiry.Version);
        if (inquiry.Status != SupplierInquiryStatus.OPEN || inquiry.Deadline < ContractToday || c.ValidUntil < ContractToday) throw new HierarchyConflictException(ErrorCatalog.SupplierQuoteState.Create());
        var invitation = await db.Set<SupplierInvitation>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.PropertyId == inquiry.PropertyId && x.InquiryId == id && x.Id == c.InvitationId, token); if (invitation is null) return null;
        if (!await CandidateRows(tenant, inquiry.PropertyId, inquiry.CategoryId).AnyAsync(x => x.AreaServiceId == invitation.AreaServiceId && x.SupplierId == invitation.SupplierId, token)) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
        var quote = await db.Set<SupplierQuote>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.PropertyId == inquiry.PropertyId && x.InquiryId == id && x.InvitationId == invitation.Id, token);
        if (quote is null) { quote = new(invitation, net, rate, c.Currency, c.ValidUntil, c.Notes, actor.Issuer!, actor.UserId!); db.Add(quote); }
        else
        {
            if (quote.Status != SupplierQuoteStatus.PENDING) throw new HierarchyConflictException(ErrorCatalog.SupplierQuoteState.Create());
            if (!quote.IsEnteredBy(actor.Issuer!, actor.UserId!)) throw new UnauthorizedAccessException();
            quote.Enter(net, rate, c.Currency, c.ValidUntil, c.Notes, actor.Issuer!, actor.UserId!); db.Entry(quote).Property(x => x.IsEnabled).IsModified = true;
        }
        InquiryRevision(inquiry, c.OperationId, fingerprint, "QUOTE", new { quote.Id, quote.InvitationId, NetAmount = Money(quote.NetAmount), VatRate = Money(quote.VatRate), VatAmount = Money(quote.VatAmount), GrossAmount = Money(quote.GrossAmount), quote.Currency, quote.ValidUntil, quote.Notes }, c.Version + 1);
        TouchArea((scope.Value.Supplier, scope.Value.Property)); await db.SaveChangesAsync(token); var result = new SupplierInquiryMutation(await InquiryResponse(inquiry, token), false, inquiry.Version); await tx.CommitAsync(token); return result;
    }
    public async Task<SupplierInquiryMutation?> ActOnInquiry(Guid tenant, Guid supplier, Guid id, Guid? quoteId, string action, SupplierInquiryAction c, CancellationToken token)
    {
        if (c.OperationId == Guid.Empty || c.Version < 1 || action is not ("APPROVE" or "REJECT" or "CANCEL" or "CONVERT") || action is "APPROVE" or "REJECT" && (!quoteId.HasValue || quoteId == Guid.Empty)) InvalidSupplier("version");
        if (action != "CONVERT" && (string.IsNullOrWhiteSpace(c.Reason) || c.Reason.Trim().Length > 1000 || c.Reason.Any(char.IsControl))) InvalidSupplier("reason");
        await using var tx = await db.Database.BeginTransactionAsync(token); var scope = await LockInquiry(tenant, supplier, id, token); if (scope is null) return null; var row = scope.Value.Inquiry;
        // Decision roles and actual area assignment are both enforced, including on replay.
        if (!actor.IsInRole("SUPER_ADMIN") && !actor.IsInRole("SITE_ADMIN") || !(await management.EvaluateAsync(tenant, row.PropertyId, "SITE_ADMIN", token)).Allowed) throw new UnauthorizedAccessException();
        var fingerprint = Hash(new { action, tenant, supplier, id, quoteId, c.Version, reason = action == "CONVERT" ? null : c.Reason!.Trim() });
        var replay = await InquiryReplay(tenant, supplier, c.OperationId, fingerprint, token); if (replay is not null) { await tx.CommitAsync(token); return replay; }
        Expected(c.Version, row.Version); SupplierQuote? quote = null; SupplierWorkOrder? order = null;
        if (action == "CANCEL")
        {
            if (row.Status != SupplierInquiryStatus.OPEN) throw new HierarchyConflictException(ErrorCatalog.SupplierQuoteState.Create()); row.Cancel(c.Reason!);
        }
        else if (action == "CONVERT")
        {
            if (row.Status != SupplierInquiryStatus.AWARDED || await db.Set<SupplierWorkOrder>().AnyAsync(x => x.TenantId == tenant && x.InquiryId == id, token)) throw new HierarchyConflictException(ErrorCatalog.SupplierQuoteState.Create());
            quote = await db.Set<SupplierQuote>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.PropertyId == row.PropertyId && x.InquiryId == id && x.Status == SupplierQuoteStatus.APPROVED, token); if (quote is null) return null;
            var invitation = await db.Set<SupplierInvitation>().SingleAsync(x => x.TenantId == tenant && x.PropertyId == row.PropertyId && x.InquiryId == id && x.Id == quote.InvitationId, token);
            if (!await CandidateRows(tenant, row.PropertyId, row.CategoryId).AnyAsync(x => x.SupplierId == invitation.SupplierId && x.AreaServiceId == invitation.AreaServiceId, token)) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
            order = new(row, invitation, quote); db.Add(order); OrderRevision(order, c.OperationId, fingerprint, "CREATE", 1);
        }
        else
        {
            if (row.Status != SupplierInquiryStatus.OPEN) throw new HierarchyConflictException(ErrorCatalog.SupplierQuoteState.Create());
            quote = await db.Set<SupplierQuote>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.PropertyId == row.PropertyId && x.InquiryId == id && x.Id == quoteId, token); if (quote is null) return null;
            if (quote.Status != SupplierQuoteStatus.PENDING || action == "APPROVE" && quote.ValidUntil < ContractToday) throw new HierarchyConflictException(ErrorCatalog.SupplierQuoteState.Create());
            if (action == "APPROVE")
            {
                if (quote.IsEnteredBy(actor.Issuer!, actor.UserId!)) throw new UnauthorizedAccessException();
                var invitation = await db.Set<SupplierInvitation>().SingleAsync(x => x.TenantId == tenant && x.PropertyId == row.PropertyId && x.InquiryId == id && x.Id == quote.InvitationId, token);
                if (!await CandidateRows(tenant, row.PropertyId, row.CategoryId).AnyAsync(x => x.SupplierId == invitation.SupplierId && x.AreaServiceId == invitation.AreaServiceId, token)) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create());
                row.Award();
            }
            quote.Decide(action == "APPROVE", c.Reason!, actor.Issuer!, actor.UserId!, ContractToday);
        }
        InquiryRevision(row, c.OperationId, fingerprint, action, new { QuoteId = quote?.Id, WorkOrderId = order?.Id, Status = row.Status.ToString(), Reason = action == "CONVERT" ? null : c.Reason!.Trim() }, c.Version + 1);
        TouchArea((scope.Value.Supplier, scope.Value.Property)); await db.SaveChangesAsync(token); var result = new SupplierInquiryMutation(await InquiryResponse(row, token), false, row.Version); await tx.CommitAsync(token); return result;
    }
    public async Task<PagedRows<SupplierContractHistory>?> InquiryHistory(Guid tenant, Guid supplier, Guid id, int page, int pageSize, CancellationToken token)
    {
        Page(page, pageSize); await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Allowed(tenant, token)) return null; var row = await InquiryRows(tenant, supplier).SingleOrDefaultAsync(x => x.Id == id, token);
        if (row is null || !await AreaAllowed(tenant, supplier, row.PropertyId, token)) return null;
        var rows = db.Set<SupplierInquiryRevision>().AsNoTracking().Where(x => x.TenantId == tenant && x.PropertyId == row.PropertyId && x.InquiryId == id);
        var count = await rows.CountAsync(token); var items = await rows.OrderByDescending(x => x.AppliedVersion).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new SupplierContractHistory(x.Id, x.Action, x.AppliedVersion, x.Snapshot, x.CreatedAt)).ToArrayAsync(token);
        await tx.CommitAsync(token); return new(items, page, pageSize, count);
    }
}
