namespace BuildingFlow.Application;
public sealed record ManagerRow(Guid Id,string Title,string Detail,string? Amount=null,string? State=null);
public sealed record ManagerSummary(int Buildings,int Units,int Residents,int Announcements,int Requests,string Balance,string Overdue);
public sealed record ManagerSnapshot(string UserId,string ContextKey,DateTimeOffset AsOf,string PropertyName,string Address,string Section,int Page,int Total,ManagerSummary Summary,ManagerRow[] Rows);
public interface IMobileManagerStore { Task<ManagerSnapshot> ReadAsync(Guid property,string role,string section,int page,CancellationToken token); }
