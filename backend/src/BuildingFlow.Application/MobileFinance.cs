namespace BuildingFlow.Application;
public sealed record FinanceCharge(OwnerCharge Charge,string UnitNumber);
public sealed record FinancePayment(Guid Id,Guid ChargeId,string UnitNumber,string Period,string Amount,DateOnly PaidDate,bool Cancelled);
public sealed record MobileFinanceSummary(string Amount,string Paid,string Balance,string Overdue);
public sealed record FinanceSnapshot(string UserId,string ContextKey,DateTimeOffset AsOf,string PropertyName,string View,int Page,Guid? ChargeId,int Total,MobileFinanceSummary Summary,FinanceCharge[] Charges,FinancePayment[] Payments);
public interface IMobileFinanceStore {Task<FinanceSnapshot> ReadAsync(Guid property,Guid? unit,string role,string view,int page,Guid? charge,CancellationToken token);}
