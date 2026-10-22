namespace BuildingFlow.Application;
public sealed record MyUnitQuery(Guid PropertyId,Guid UnitId,string Capacity,Guid RelationId,int Page=1,int PageSize=20);
public sealed record MyUnitPage<T>(string ContextKey,PagedRows<T> Rows);
public sealed record MyDues(string ContextKey,MobileFinanceSummary Summary,PagedRows<OwnerCharge> Rows);
public sealed record MyPaymentRow(Guid Id,Guid ChargeId,string Period,string Amount,DateOnly PaidDate,bool Cancelled);
public sealed record MyAnnouncementRow(Guid Id,string Title,string Content,string Category,DateTimeOffset PublishedAt);
public sealed record MyDocumentRow(Guid Id,Guid AnnouncementId,string FileName,string ContentType,long Size,DateTimeOffset CreatedAt);
public sealed record MyDocumentContent(string FileName,string ContentType,Stream Content);
public sealed record MyFinanceScope(Guid TenantId,Guid PropertyId,string PropertyName,Guid UnitId,string UnitNumber,string Capacity);
public sealed record MyFinanceSummary(DateOnly AsOf,IReadOnlyList<MyFinanceScope> Scope,int ChargeCount,int SharedCharges,MobileFinanceSummary Summary);
public sealed record MyLeaseRow(Guid Id,DateOnly StartDate,DateOnly? EndDate,string? ContractReference,string Status,int PartyCount,bool IsParty);
// Every read re-resolves the selected context and its capability; the client selection grants nothing.
public interface IMyUnitStore
{
    Task<MyDues?> DuesAsync(MyUnitQuery query,CancellationToken token);
    Task<MyUnitPage<MyPaymentRow>?> PaymentsAsync(MyUnitQuery query,CancellationToken token);
    Task<MyUnitPage<MyAnnouncementRow>?> AnnouncementsAsync(MyUnitQuery query,CancellationToken token);
    Task<MyUnitPage<MyDocumentRow>?> DocumentsAsync(MyUnitQuery query,CancellationToken token);
    Task<MyUnitPage<MyLeaseRow>?> LeasesAsync(MyUnitQuery query,CancellationToken token);
    Task<MyDocumentContent?> DownloadAsync(MyUnitQuery query,Guid document,CancellationToken token);
    Task<MyFinanceSummary> FinanceSummaryAsync(CancellationToken token);
}
