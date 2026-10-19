using System.Text.Json.Serialization;
namespace BuildingFlow.Application;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateManagementAssignmentRequest(Guid OperationId,Guid PersonId,string Role,DateOnly StartDate,DateOnly? EndDate,string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EndManagementAssignmentRequest(Guid OperationId,long ExpectedVersion,DateOnly EndDate,string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CancelManagementAssignmentRequest(Guid OperationId,long ExpectedVersion,string Reason);
public sealed record ManagementAssignmentRow(Guid Id,Guid TenantId,Guid PropertyId,Guid PersonId,string Role,DateOnly StartDate,DateOnly? EndDate,string Status,long Version,DateTimeOffset? CancelledAt);
public sealed record ManagementAssignmentMutation(ManagementAssignmentRow Record,bool Replayed,long AppliedVersion);
public sealed record ManagementAssignmentHistory(Guid Id,string Action,string Reason,long AppliedVersion,DateTimeOffset CreatedAt,string CreatedBy);
public sealed record ManagementAccessDecision(string Mode,bool LegacyAllowed,bool AssignmentAllowed,bool Allowed);
public interface IManagementAccess
{
    Task<ManagementAccessDecision> EvaluateAsync(Guid tenant,Guid property,string role,CancellationToken token);
    Task<bool> TenantPeopleAllowedAsync(Guid tenant,CancellationToken token);
    string Mode(Guid tenant);
}
public interface IManagementAssignmentStore
{
    Task<PagedRows<ManagementAssignmentRow>?> ListAsync(Guid tenant,Guid property,int page,int pageSize,CancellationToken token);
    Task<ManagementAssignmentRow?> GetAsync(Guid tenant,Guid property,Guid id,CancellationToken token);
    Task<PagedRows<ManagementAssignmentHistory>?> HistoryAsync(Guid tenant,Guid property,Guid id,int page,int pageSize,CancellationToken token);
    Task<ManagementAssignmentMutation?> CreateAsync(Guid tenant,Guid property,CreateManagementAssignmentRequest request,CancellationToken token);
    Task<ManagementAssignmentMutation?> EndAsync(Guid tenant,Guid property,Guid id,EndManagementAssignmentRequest request,CancellationToken token);
    Task<ManagementAssignmentMutation?> CancelAsync(Guid tenant,Guid property,Guid id,CancelManagementAssignmentRequest request,CancellationToken token);
}
