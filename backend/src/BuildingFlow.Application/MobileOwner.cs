namespace BuildingFlow.Application;
public sealed record OwnerUnit(string PropertyName,string Address,string City,string District,string? BuildingName,string Number,int Floor,string Area);
public sealed record OwnerTenant(string Name,DateOnly StartDate,DateOnly? EndDate);
public sealed record OwnerCharge(Guid Id,string Period,string Amount,string Paid,string Balance,DateOnly DueDate,string Status);
public sealed record OwnerAnnouncement(Guid Id,string Title,string Content,DateTimeOffset PublishedAt);
public sealed record OwnerRequest(Guid Id,string Description,DateTimeOffset ModifiedAt,string ManagementStatus="RECORDED",string? ManagementResponse=null);
public sealed record OwnerPayment(Guid Id,string Amount,DateOnly PaidDate,bool Cancelled);
public sealed record OwnerSnapshot(string UserId,string ContextKey,DateTimeOffset AsOf,OwnerUnit Unit,OwnerTenant[] Tenants,bool MoreTenants,string Balance,string Overdue,OwnerCharge[] Charges,bool MoreCharges,OwnerAnnouncement[] Announcements,bool MoreAnnouncements,OwnerRequest[] Requests,bool MoreRequests,OwnerPayment[]? Payments=null,bool MorePayments=false);
public interface IMobileOwnerStore {Task<OwnerSnapshot> ReadAsync(Guid property,Guid unit,CancellationToken token);Task<OwnerSnapshot> ReadTenantAsync(Guid property,Guid unit,CancellationToken token);}
