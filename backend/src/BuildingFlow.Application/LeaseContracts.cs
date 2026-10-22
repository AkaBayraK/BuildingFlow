using System.Text.Json.Serialization;
namespace BuildingFlow.Application;
public sealed record LeasePartyRow(Guid TenancyId,Guid PersonId,string DisplayName,DateOnly StartDate,DateOnly? EndDate,bool IsPrimaryContact,string VerificationStatus,long Version);
public sealed record LeaseContractRow(Guid Id,Guid TenantId,Guid PropertyId,Guid UnitId,string UnitNumber,DateOnly StartDate,DateOnly? EndDate,string? ContractReference,string Status,string VerificationStatus,long Version,IReadOnlyList<LeasePartyRow> Parties);
public sealed record LeaseContractMutation(LeaseContractRow Record,bool Replayed,long AppliedVersion);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LeasePartyDraft(Guid PersonId,DateOnly StartDate,DateOnly? EndDate,bool IsPrimaryContact);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateLeaseContractRequest(Guid OperationId,DateOnly StartDate,DateOnly? EndDate,string? ContractReference,IReadOnlyList<LeasePartyDraft> Parties);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AddLeasePartyRequest(Guid OperationId,long ExpectedVersion,LeasePartyDraft Party);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AttachLeasePartyRequest(Guid OperationId,long ExpectedVersion,Guid TenancyId,long TenancyVersion);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EndLeaseContractRequest(Guid OperationId,long ExpectedVersion,DateOnly EndDate);
public interface ILeaseContractStore
{
    Task<PagedRows<LeaseContractRow>?> ListAsync(Guid tenant,Guid property,Guid unit,int page,int size,CancellationToken token);
    Task<LeaseContractRow?> GetAsync(Guid tenant,Guid property,Guid unit,Guid contract,CancellationToken token);
    Task<LeaseContractMutation?> CreateAsync(Guid tenant,Guid property,Guid unit,CreateLeaseContractRequest request,CancellationToken token);
    Task<LeaseContractMutation?> AddPartyAsync(Guid tenant,Guid property,Guid unit,Guid contract,AddLeasePartyRequest request,CancellationToken token);
    Task<LeaseContractMutation?> AttachAsync(Guid tenant,Guid property,Guid unit,Guid contract,AttachLeasePartyRequest request,CancellationToken token);
    Task<LeaseContractMutation?> EndAsync(Guid tenant,Guid property,Guid unit,Guid contract,EndLeaseContractRequest request,CancellationToken token);
}
