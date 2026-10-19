namespace BuildingFlow.Application;
public sealed record SessionContext(Guid Id,string Kind,Guid TenantId,string TenantName,Guid PropertyId,string PropertyName,Guid? UnitId,string? UnitNumber,string Role,DateOnly? StartDate,DateOnly? EndDate,bool CanManagePeople,bool CanManageFinance,bool CanManageAssignments);
public sealed record SessionContextDetail(SessionContext Context,SelfPerson? Self);
public interface ISessionContextStore
{
    Task<PagedRows<SessionContext>> ListAsync(int page,int pageSize,CancellationToken token);
    Task<PagedRows<SessionContext>?> WebsiteAsync(string siteKey,string audience,int page,int pageSize,CancellationToken token);
    Task<SessionContextDetail?> GetAsync(string kind,Guid id,CancellationToken token);
    Task<bool> PropertyAllowedAsync(Guid property,string capability,CancellationToken token);
}
