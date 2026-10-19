using System.Globalization;
using System.Data;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class MobileOwnerStore(BuildingFlowDbContext db,IMobileSessionStore sessions,ICurrentUserService actor,TimeProvider clock):IMobileOwnerStore {
 private static string Money(decimal value)=>value.ToString("0.00",CultureInfo.InvariantCulture);
 public Task<OwnerSnapshot> ReadAsync(Guid property,Guid unit,CancellationToken token)=>ReadScoped(property,unit,"OWNER",token);
 public Task<OwnerSnapshot> ReadTenantAsync(Guid property,Guid unit,CancellationToken token)=>ReadScoped(property,unit,"TENANT",token);
 private async Task<OwnerSnapshot> ReadScoped(Guid property,Guid unit,string role,CancellationToken token){
  if(!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId))throw new UnauthorizedAccessException();
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
  var now=clock.GetUtcNow();var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now,TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
  var session=await sessions.ReadAsync(actor.UserId,actor.Username,Array.Empty<string>(),token);
  if(session.HasMore || !session.Contexts.Any(x=>x.PropertyId==property && x.UnitId==unit && x.Role==role))throw new UnauthorizedAccessException();
  var p=await db.Properties.AsNoTracking().SingleAsync(x=>x.Id==property,token);var u=await db.Units.AsNoTracking().SingleAsync(x=>x.Id==unit && x.PropertyId==property,token);
  var building=u.BuildingId is Guid id?await db.Buildings.Where(x=>x.Id==id && x.PropertyId==property).Select(x=>x.Name).SingleAsync(token):null;
  var tenants=await (from o in db.UnitOccupancies.AsNoTracking() join person in db.People on o.PersonId equals person.Id where o.PropertyId==property && o.UnitId==unit && o.Type==OccupancyType.TENANT && o.IsEnabled && person.IsEnabled && person.PropertyId==property && (role=="OWNER" || person.LinkedUserId==actor.UserId) && o.StartDate<=today && (o.EndDate==null || o.EndDate>=today) orderby o.IsPrimaryContact descending,o.Id select new OwnerTenant(person.FirstName+" "+person.LastName,o.StartDate,o.EndDate)).Take(11).ToArrayAsync(token);
  // Unit-level charges and charges explicitly assigned to this linked owner's person only.
  var charges=MobileFinanceQueries.ResidentCharges(db,property,unit,role,actor.UserId);
  var balance=await charges.Where(c=>!c.IsCancelled).SumAsync(c=>(decimal?)(c.Amount-c.PaidAmount),token)??0m;
  var overdue=await charges.Where(c=>!c.IsCancelled && c.DueDate<today).SumAsync(c=>(decimal?)(c.Amount-c.PaidAmount),token)??0m;
  var chargeRows=await charges.OrderByDescending(c=>c.Period).ThenBy(c=>c.Id).Take(51).ToArrayAsync(token);
  var announcements=await db.Set<Announcement>().AsNoTracking().Where(a=>a.PropertyId==property && a.IsEnabled && a.Status==AnnouncementStatus.PUBLISHED && a.Audience==ContentAudience.ALL_MEMBERS && a.PublishedAt!=null).OrderByDescending(a=>a.PublishedAt).ThenBy(a=>a.Id).Select(a=>new OwnerAnnouncement(a.Id,a.Title,a.Content,a.PublishedAt!.Value)).Take(21).ToArrayAsync(token);
  var requests=await db.Set<MobileRequest>().AsNoTracking().Where(x=>x.OwnerUserId==actor.UserId && x.PropertyId==property && x.UnitId==unit && x.ContextRole==role && x.IsEnabled).OrderByDescending(x=>x.ModifiedAt).ThenBy(x=>x.Id).Select(x=>new OwnerRequest(x.Id,x.Description,x.ModifiedAt,x.ManagementStatus,x.ManagementResponse)).Take(21).ToArrayAsync(token);
  var payments=role=="TENANT"?await (from allocation in db.PaymentAllocations.AsNoTracking() join payment in db.Payments on allocation.PaymentId equals payment.Id where allocation.PropertyId==property && allocation.UnitId==unit && allocation.IsEnabled && payment.PropertyId==property && payment.UnitId==unit && payment.IsEnabled && charges.Any(c=>c.Id==allocation.ChargeId) orderby payment.PaidDate descending,allocation.Id select new {allocation.Id,allocation.Amount,payment.PaidDate,payment.IsCancelled}).Take(51).ToArrayAsync(token):[];
  await tx.CommitAsync(token);
  return new(actor.UserId,$"{property}:{unit}:{role}",now,new(p.Name,p.Address,p.City,p.District,building,u.Number,u.Floor,Money(u.GrossSquareMeters)),tenants.Take(10).ToArray(),tenants.Length>10,Money(balance),Money(overdue),chargeRows.Take(50).Select(c=>new OwnerCharge(c.Id,c.Period.ToString("yyyy-MM",CultureInfo.InvariantCulture),Money(c.Amount),Money(c.PaidAmount),Money(c.Balance),c.DueDate,c.Status(today))).ToArray(),chargeRows.Length>50,announcements.Take(20).ToArray(),announcements.Length>20,requests.Take(20).ToArray(),requests.Length>20,payments.Take(50).Select(p=>new OwnerPayment(p.Id,Money(p.Amount),p.PaidDate,p.IsCancelled)).ToArray(),payments.Length>50);
 }
}
