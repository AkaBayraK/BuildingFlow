namespace BuildingFlow.Domain;

public enum SupplierWorkOrderStatus { DRAFT, ASSIGNED, ACCEPTED, IN_PROGRESS, COMPLETED, VERIFIED, CLOSED, REJECTED, CANCELLED }
public enum SupplierWorkOrderSource { QUOTE, REQUEST, MAINTENANCE }

public sealed class SupplierWorkOrder : BaseEntity
{
    private SupplierWorkOrder() { }
    public SupplierWorkOrder(SupplierInquiry inquiry, SupplierInvitation invitation, SupplierQuote quote)
    {
        if (inquiry.Status != SupplierInquiryStatus.AWARDED || quote.Status != SupplierQuoteStatus.APPROVED || quote.InquiryId != inquiry.Id || invitation.InquiryId != inquiry.Id || quote.InvitationId != invitation.Id || quote.TenantId != inquiry.TenantId || quote.PropertyId != inquiry.PropertyId)
            throw new InvalidOperationException("An awarded quote in the same scope is required.");
        TenantId = inquiry.TenantId; PropertyId = inquiry.PropertyId; SupplierId = invitation.SupplierId; AreaServiceId = invitation.AreaServiceId;
        InquiryId = inquiry.Id; QuoteId = quote.Id; Title = inquiry.Title; Amount = quote.GrossAmount; Currency = quote.Currency;
        Source = SupplierWorkOrderSource.QUOTE; SourceSnapshot = inquiry.Description; Description = inquiry.Description;
    }
    public SupplierWorkOrder(Guid tenant, Guid supplier, Guid property, Guid areaService, SupplierWorkOrderSource source, Guid sourceId, long sourceVersion, string sourceSnapshot, string title, string description, DateTimeOffset? dueAt)
    {
        SupplierText.Scope(tenant, supplier); SupplierText.Scope(property, areaService);
        if (source is not (SupplierWorkOrderSource.REQUEST or SupplierWorkOrderSource.MAINTENANCE) || sourceId == Guid.Empty || sourceVersion < 1) throw new ArgumentException("A persisted request or maintenance source is required.");
        var cleanTitle = SupplierQuoteRules.Text(title, nameof(title), 200);
        var snapshot = WorkText(sourceSnapshot, nameof(sourceSnapshot), 4000);
        TenantId = tenant; SupplierId = supplier; PropertyId = property; AreaServiceId = areaService; Source = source; SourceVersion = sourceVersion;
        RequestId = source == SupplierWorkOrderSource.REQUEST ? sourceId : null; MaintenancePlanId = source == SupplierWorkOrderSource.MAINTENANCE ? sourceId : null;
        Title = cleanTitle; SourceSnapshot = snapshot; Edit(description, dueAt);
    }
    public Guid TenantId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid AreaServiceId { get; private set; }
    public Guid? InquiryId { get; private set; }
    public Guid? QuoteId { get; private set; }
    public Guid? RequestId { get; private set; }
    public Guid? MaintenancePlanId { get; private set; }
    public long? SourceVersion { get; private set; }
    public SupplierWorkOrderSource Source { get; private set; }
    public string SourceSnapshot { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public decimal? Amount { get; private set; }
    public string? Currency { get; private set; }
    public SupplierWorkOrderStatus Status { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public string? CompletionNote { get; private set; }
    public string? DecisionReason { get; private set; }
    public void Edit(string description, DateTimeOffset? dueAt)
    {
        if (Status != SupplierWorkOrderStatus.DRAFT) throw new InvalidOperationException("Only a draft scope and SLA can be edited.");
        var clean = WorkText(description, nameof(description), 4000);
        if (dueAt.HasValue && dueAt.Value.Year is < 2000 or > 2100) throw new ArgumentException("Invalid SLA deadline.");
        Description = clean; DueAt = dueAt.HasValue ? UtcInstant(dueAt.Value) : null;
    }
    public static bool CanTransition(SupplierWorkOrderStatus from, SupplierWorkOrderStatus to) => (from, to) is
        (SupplierWorkOrderStatus.DRAFT, SupplierWorkOrderStatus.ASSIGNED or SupplierWorkOrderStatus.CANCELLED) or
        (SupplierWorkOrderStatus.ASSIGNED, SupplierWorkOrderStatus.ACCEPTED or SupplierWorkOrderStatus.REJECTED or SupplierWorkOrderStatus.CANCELLED) or
        (SupplierWorkOrderStatus.ACCEPTED, SupplierWorkOrderStatus.IN_PROGRESS or SupplierWorkOrderStatus.CANCELLED) or
        (SupplierWorkOrderStatus.IN_PROGRESS, SupplierWorkOrderStatus.COMPLETED or SupplierWorkOrderStatus.CANCELLED) or
        (SupplierWorkOrderStatus.COMPLETED, SupplierWorkOrderStatus.VERIFIED or SupplierWorkOrderStatus.CANCELLED) or
        (SupplierWorkOrderStatus.VERIFIED, SupplierWorkOrderStatus.CLOSED);
    public void Transition(SupplierWorkOrderStatus next, string reason, DateTimeOffset now)
    {
        if (!CanTransition(Status, next)) throw new InvalidOperationException("Invalid work order transition.");
        var clean = WorkText(reason, nameof(reason), next == SupplierWorkOrderStatus.COMPLETED ? 4000 : 1000);
        var instant = UtcInstant(now);
        if (instant.Year is < 2000 or > 2100 || (next == SupplierWorkOrderStatus.ASSIGNED && (!DueAt.HasValue || DueAt <= instant))) throw new ArgumentException("Assignment requires a future SLA deadline.");
        var previous = ClosedAt ?? VerifiedAt ?? CompletedAt ?? StartedAt ?? AcceptedAt ?? AssignedAt;
        if (previous > instant) throw new ArgumentException("Work order time cannot move backwards.");
        switch (next)
        {
            case SupplierWorkOrderStatus.ASSIGNED: AssignedAt = instant; break;
            case SupplierWorkOrderStatus.ACCEPTED: AcceptedAt = instant; break;
            case SupplierWorkOrderStatus.IN_PROGRESS: StartedAt = instant; break;
            case SupplierWorkOrderStatus.COMPLETED: CompletedAt = instant; CompletionNote = clean; break;
            case SupplierWorkOrderStatus.VERIFIED: VerifiedAt = instant; break;
            case SupplierWorkOrderStatus.CLOSED: ClosedAt = instant; break;
            case SupplierWorkOrderStatus.REJECTED:
            case SupplierWorkOrderStatus.CANCELLED: DecisionReason = clean; break;
        }
        Status = next;
    }
    public static DateTimeOffset UtcInstant(DateTimeOffset value) { var utc = value.ToUniversalTime(); return utc.AddTicks(-(utc.Ticks % 10)); }
    public bool AllowsPhotos => Status is SupplierWorkOrderStatus.DRAFT or SupplierWorkOrderStatus.ASSIGNED or SupplierWorkOrderStatus.ACCEPTED or SupplierWorkOrderStatus.IN_PROGRESS;
    public static string WorkText(string value, string field, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, field); var clean = value.Trim();
        if (clean.Length > max || clean.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t'))) throw new ArgumentException("Invalid work text.", field);
        return clean;
    }
}

public sealed class SupplierWorkOrderPhoto : BaseEntity
{
    private SupplierWorkOrderPhoto() { }
    public SupplierWorkOrderPhoto(SupplierWorkOrder order, SupplierDocument document, string caption)
    {
        if (!order.AllowsPhotos || document.TenantId != order.TenantId || document.SupplierId != order.SupplierId || document.PropertyId != order.PropertyId) throw new InvalidOperationException("A photo must belong to the work order scope.");
        TenantId = order.TenantId; SupplierId = order.SupplierId; PropertyId = order.PropertyId; WorkOrderId = order.Id; DocumentId = document.Id;
        Caption = SupplierWorkOrder.WorkText(caption, nameof(caption), 1000);
    }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public Guid DocumentId { get; private set; }
    public string Caption { get; private set; } = null!;
}
public sealed class SupplierWorkOrderRevision : BaseEntity
{
    private SupplierWorkOrderRevision() { }
    public SupplierWorkOrderRevision(SupplierWorkOrder order, Guid operation, string issuer, string subject, string fingerprint, string action, string snapshot, long appliedVersion)
    {
        if (operation == Guid.Empty || appliedVersion < 1 || action is not ("CREATE" or "EDIT" or "ASSIGNED" or "ACCEPTED" or "IN_PROGRESS" or "COMPLETED" or "VERIFIED" or "CLOSED" or "REJECTED" or "CANCELLED" or "PHOTO")) throw new ArgumentException("Invalid work order revision.");
        TenantId = order.TenantId; SupplierId = order.SupplierId; PropertyId = order.PropertyId; WorkOrderId = order.Id; OperationId = operation;
        ActorIssuer = Required(issuer, nameof(issuer), 500); ActorSubject = Required(subject, nameof(subject), 200); Fingerprint = Required(fingerprint, nameof(fingerprint), 64); Action = action; Snapshot = Required(snapshot, nameof(snapshot), 20000); AppliedVersion = appliedVersion;
    }
    public Guid TenantId { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public Guid OperationId { get; private set; }
    public string ActorIssuer { get; private set; } = null!;
    public string ActorSubject { get; private set; } = null!;
    public string Fingerprint { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string Snapshot { get; private set; } = null!;
    public long AppliedVersion { get; private set; }
}
