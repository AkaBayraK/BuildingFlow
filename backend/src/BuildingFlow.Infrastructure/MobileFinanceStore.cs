using System.Data;
using System.Globalization;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class MobileFinanceStore(BuildingFlowDbContext db,IMobileSessionStore sessions,ICurrentUserService actor,LatePenaltyEngine penalties,TimeProvider clock):IMobileFinanceStore {
 private static string Money(decimal value)=>value.ToString("0.00",CultureInfo.InvariantCulture);
 public async Task<FinanceSnapshot> ReadAsync(Guid property,Guid? unit,string role,string view,int page,Guid? charge,CancellationToken token){
  if(!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId))throw new UnauthorizedAccessException();
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
  var resident=role is "OWNER" or "TENANT";
  if(resident){
   var session=await sessions.ReadAsync(actor.UserId,actor.Username,Array.Empty<string>(),token);
   if(unit==null || session.HasMore || !session.Contexts.Any(c=>c.PropertyId==property && c.UnitId==unit && c.Role==role))throw new UnauthorizedAccessException();
  }else if(unit!=null || role is not ("SUPER_ADMIN" or "SITE_ADMIN" or "MANAGER" or "ACCOUNTANT") || !actor.IsInRole(role))throw new UnauthorizedAccessException();
  var p=await db.Properties.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==property && p.IsEnabled && (!resident && role=="SUPER_ADMIN" || db.PropertyMemberships.Any(m=>m.PropertyId==property && m.UserId==actor.UserId && m.IsEnabled)),token);
  if(p is null)throw new UnauthorizedAccessException();
  var now=clock.GetUtcNow();var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now,TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
  var charges=resident?MobileFinanceQueries.ResidentCharges(db,property,unit!.Value,role,actor.UserId):db.DuesCharges.AsNoTracking().Where(c=>c.PropertyId==property && c.IsEnabled);
  var summary=new MobileFinanceSummary(Money(await charges.Where(c=>!c.IsCancelled).SumAsync(c=>(decimal?)c.Amount,token)??0m),Money(await charges.Where(c=>!c.IsCancelled).SumAsync(c=>(decimal?)c.PaidAmount,token)??0m),Money(await charges.Where(c=>!c.IsCancelled).SumAsync(c=>(decimal?)(c.Amount-c.PaidAmount),token)??0m),Money(await charges.Where(c=>!c.IsCancelled && c.DueDate<today).SumAsync(c=>(decimal?)(c.Amount-c.PaidAmount),token)??0m),Money(await db.LatePenaltyAccruals.Where(a=>a.PropertyId==property && a.Status==LatePenaltyAccrualStatus.ACTIVE && charges.Any(c=>c.Id==a.ChargeId)).SumAsync(a=>(decimal?)(a.Amount-a.PaidAmount),token)??0m));
  if(charge!=null){if(!await charges.AnyAsync(c=>c.Id==charge,token))throw new UnauthorizedAccessException();charges=charges.Where(c=>c.Id==charge);}
  var skip=(page-1)*50;FinanceCharge[] chargeRows=[];FinancePayment[] paymentRows=[];FinancePenaltyLine[]? penaltyLines=null;int total;
  if(view=="charges"){
   total=await charges.CountAsync(token);
   var rows=await charges.OrderByDescending(c=>c.Period).ThenBy(c=>c.Id).Skip(skip).Take(50).Select(c=>new {Charge=c,Number=db.Units.Where(u=>u.Id==c.UnitId && u.PropertyId==property).Select(u=>u.Number).FirstOrDefault()}).ToArrayAsync(token);
   // Penalty shown on mobile is the server's crystallized balance plus today's pending amount; mobile never calculates it.
   var inputs=await penalties.InputsAsync(property,token);var states=await penalties.StatesAsync(property,rows.Select(x=>x.Charge).ToArray(),null,false,token);
   decimal Pending(Guid id)=>LatePenaltyCalculator.Round(LatePenaltyEngine.Pending(states[id],inputs,today).Sum(x=>x.Amount));
   chargeRows=rows.Select(x=>new FinanceCharge(new(x.Charge.Id,x.Charge.Period.ToString("yyyy-MM",CultureInfo.InvariantCulture),Money(x.Charge.Amount),Money(x.Charge.PaidAmount),Money(x.Charge.Balance),x.Charge.DueDate,x.Charge.Status(today),Money(states[x.Charge.Id].Accruals.Sum(a=>a.Balance)+Pending(x.Charge.Id)),x.Charge.Kind.ToString(),x.Charge.Category?.ToString()),x.Number??"Arşiv")).ToArray();
   if(charge!=null && rows.Length==1){var state=states[rows[0].Charge.Id];var accrued=await db.LatePenaltyAccruals.AsNoTracking().Include(a=>a.Segments).Where(a=>a.PropertyId==property && a.ChargeId==charge && a.Status!=LatePenaltyAccrualStatus.SUPERSEDED).ToListAsync(token);
    penaltyLines=accrued.SelectMany(a=>a.Segments.Select(g=>new FinancePenaltyLine(g.FromDate,g.ToDate,g.Days,Money(g.Principal),LatePenaltyService.Rate(g.MonthlyRate),Money(LatePenaltyCalculator.Round(g.Amount)),a.Status==LatePenaltyAccrualStatus.WAIVED?"WAIVED":"ACCRUED")))
     .Concat(LatePenaltyEngine.Pending(state,inputs,today).Select(g=>new FinancePenaltyLine(g.From,g.To,g.Days,Money(g.Principal),LatePenaltyService.Rate(g.MonthlyRate),Money(LatePenaltyCalculator.Round(g.Amount)),"PENDING"))).OrderBy(l=>l.From).ToArray();}
  }else{
   var payments=from allocation in db.PaymentAllocations.AsNoTracking() join payment in db.Payments on allocation.PaymentId equals payment.Id join c in charges on allocation.ChargeId equals c.Id where allocation.PropertyId==property && allocation.UnitId==c.UnitId && allocation.IsEnabled && payment.PropertyId==property && payment.UnitId==c.UnitId && payment.IsEnabled select new {allocation.Id,ChargeId=c.Id,c.UnitId,c.Period,allocation.Amount,payment.PaidDate,payment.IsCancelled,allocation.Kind};
   total=await payments.CountAsync(token);
   var rows=await payments.OrderByDescending(x=>x.PaidDate).ThenBy(x=>x.Id).Skip(skip).Take(50).Select(x=>new {Row=x,Number=db.Units.Where(u=>u.Id==x.UnitId && u.PropertyId==property).Select(u=>u.Number).FirstOrDefault()}).ToArrayAsync(token);
   paymentRows=rows.Select(x=>new FinancePayment(x.Row.Id,x.Row.ChargeId,x.Number??"Arşiv",x.Row.Period.ToString("yyyy-MM",CultureInfo.InvariantCulture),Money(x.Row.Amount),x.Row.PaidDate,x.Row.IsCancelled,x.Row.Kind.ToString())).ToArray();
  }
  await tx.CommitAsync(token);
  return new(actor.UserId,$"{property}:{(unit.HasValue?unit.Value.ToString():"-")}:{role}",now,p.Name,view,page,charge,total,summary,chargeRows,paymentRows,penaltyLines);
 }
}
