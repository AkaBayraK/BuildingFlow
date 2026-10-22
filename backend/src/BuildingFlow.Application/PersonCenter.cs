using System.Text.Json.Serialization;
namespace BuildingFlow.Application;
public sealed record PersonWorkspace(Guid Id,string Name);
public sealed record PersonPropertyChoice(Guid Id,string Name,bool CanManageAssignments,bool CanWriteRelations);
public sealed record PersonCenterRecord(CentralPersonDetail Person,bool CanEdit,Guid? LegacyPropertyId,Guid? LegacyPersonId,bool IsImported,bool SourceChanged,bool AssignmentsAvailable,bool CanCreateUnitRelations,bool CanManageAssignments);
public sealed record PersonAccountStatus(bool IsLinked,DateTimeOffset? LinkedAt,string? CodeHint=null,DateTimeOffset? CodeExpiresAt=null);
public sealed record PersonUnitChoice(Guid Id,string Name,Guid? BuildingId=null);
public sealed record PersonBuildingChoice(Guid Id,string Name);
public sealed record PersonUnitRelationRow(Guid Id,Guid TenantId,Guid PropertyId,string PropertyName,Guid UnitId,string UnitNumber,Guid PersonId,string Kind,DateOnly StartDate,DateOnly? EndDate,bool IsPrimaryContact,string? ShareNumerator,string? ShareDenominator,long Version,string Status,bool CanEnd,string VerificationStatus="LEGACY",Guid? LeaseContractId=null);
public sealed record PersonUnitRelationMutation(PersonUnitRelationRow Record,bool Replayed,long AppliedVersion);
public sealed record PersonDirectoryRow(Guid Id,Guid TenantId,string TenantName,string DisplayName,string Kind,string MaskedIdentity,bool IsEnabled,long Version,DateTimeOffset CreatedAt,Guid? PhotoId=null);
public sealed record PersonDirectoryQuery(int Page=1,int PageSize=20,string? Search=null,string SortBy="name",string SortDirection="asc",Guid? TenantId=null,Guid? PropertyId=null,string? Relation=null);
public sealed record PersonPortfolioView(Guid TenantId,Guid PersonId,DateOnly AsOf,IReadOnlyList<PortfolioProperty> Properties);
public sealed record PersonDutyRow(ManagementAssignmentRow Assignment,string PropertyName,bool CanManage);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreatePersonUnitRelationRequest(Guid OperationId,Guid PropertyId,Guid UnitId,string Kind,DateOnly StartDate,DateOnly? EndDate,bool IsPrimaryContact,string? ShareNumerator=null,string? ShareDenominator=null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EndPersonUnitRelationRequest(Guid OperationId,long ExpectedVersion,DateOnly EndDate);
public interface IPersonCenterStore
{
    Task<PagedRows<PersonWorkspace>> WorkspacesAsync(int page,int size,CancellationToken token);
    Task<PersonWorkspace?> WorkspaceAsync(Guid tenant,CancellationToken token);
    Task<PagedRows<PersonPropertyChoice>?> PropertiesAsync(Guid tenant,int page,int size,CancellationToken token);
    Task<PersonCenterRecord?> DetailAsync(Guid tenant,Guid person,CancellationToken token);
    Task<PersonAccountStatus?> AccountAsync(Guid tenant,Guid person,CancellationToken token);
    Task<PagedRows<PersonDirectoryRow>> DirectoryAsync(PersonDirectoryQuery query,CancellationToken token);
    Task<string?> PhotoAccountAsync(Guid tenant,Guid person,CancellationToken token);
    Task<PagedRows<PersonBuildingChoice>?> BuildingsAsync(Guid tenant,Guid property,int page,int size,CancellationToken token);
    Task<PagedRows<PersonUnitChoice>?> UnitsAsync(Guid tenant,Guid property,Guid? building,int page,int size,CancellationToken token);
    Task<PersonPortfolioView?> PortfolioAsync(Guid tenant,Guid person,CancellationToken token);
    Task<PagedRows<PersonUnitRelationRow>?> RelationsAsync(Guid tenant,Guid person,int page,int size,CancellationToken token);
    Task<PagedRows<PersonDutyRow>?> DutiesAsync(Guid tenant,Guid person,int page,int size,CancellationToken token);
    Task<PersonUnitRelationRow?> RelationAsync(Guid tenant,Guid person,string kind,Guid id,CancellationToken token);
    Task<PersonUnitRelationMutation?> CreateRelationAsync(Guid tenant,Guid person,CreatePersonUnitRelationRequest request,CancellationToken token);
    Task<PersonUnitRelationMutation?> EndRelationAsync(Guid tenant,Guid person,string kind,Guid id,EndPersonUnitRelationRequest request,CancellationToken token);
}
