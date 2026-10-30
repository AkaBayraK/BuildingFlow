using System.Text.Json.Serialization;
namespace BuildingFlow.Application;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SupplierAdviceRequest(string Kind, Guid PropertyId, Guid? CategoryId, bool Consent, string? Note);
public sealed record SupplierAdviceItem(string Kind, Guid SupplierId, string SupplierName, Guid? RecordId, string Level, string ReasonCode, IReadOnlyDictionary<string, string> Evidence, int SampleCount);
public sealed record SupplierAdviceResponse(string Source, IReadOnlyList<SupplierAdviceItem> Items, int RemainingToday, bool RequiresHumanDecision);

// What a model sees: opaque aliases and numbers only, never names, identifiers, notes or personal data.
public sealed record AdviceCandidate(string Alias, int SampleCount, int OnTimeCount, bool HasActiveContract, int OpenOverdue);
public sealed record AdviceOpenOrder(string Alias, string OrderAlias, double ElapsedShare, double HoursLeft);
public sealed record AdviceInvoice(string Alias, string InvoiceAlias, decimal Gross, decimal? ContractAmount, IReadOnlyList<decimal> History);
public sealed record AdviceInput(string Kind, IReadOnlyList<AdviceCandidate> Candidates, IReadOnlyList<AdviceOpenOrder> OpenOrders, IReadOnlyList<AdviceInvoice> Invoices);
public sealed record AdviceSuggestion(string Alias, string? RecordAlias, string Level, string ReasonCode, IReadOnlyDictionary<string, string> Evidence, int SampleCount, string? Action = null);
public interface ISupplierAdviceModel
{
    string Source { get; }
    Task<IReadOnlyList<AdviceSuggestion>> Suggest(AdviceInput input, CancellationToken token);
}
public interface ISupplierAdviceStore
{
    Task<SupplierAdviceResponse?> Advise(Guid tenant, SupplierAdviceRequest request, CancellationToken token);
}
