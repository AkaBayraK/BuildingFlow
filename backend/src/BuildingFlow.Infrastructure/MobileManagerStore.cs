using System.Data;
using System.Globalization;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
public sealed class MobileManagerStore(BuildingFlowDbContext db,ICurrentUserService actor,TimeProvider clock):IMobileManagerStore {
 private static string Money(decimal value)=>value.ToString("0.00",CultureInfo.InvariantCulture);
 public async Task<ManagerSnapshot> ReadAsync(Guid property,string role,string section,int page,CancellationToken token){
  // A client-selected context never grants a realm role or property membership.
  if(!actor.IsAuthenticated || string.IsNullOrEmpty(actor.UserId) || role is not ("SUPER_ADMIN" or "SITE_ADMIN" or "MANAGER") || !actor.IsInRole(role))throw new UnauthorizedAccessException();
  await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,token);
  var p=await db.Properties.AsNoTracking().Where(p=>p.Id==property && p.IsEnabled && (role=="SUPER_ADMIN" || db.PropertyMemberships.Any(m=>m.PropertyId==property && m.UserId==actor.UserId && m.IsEnabled))).SingleOrDefaultAsync(token);
  if(p is null)throw new UnauthorizedAccessException();
  var now=clock.GetUtcNow();var today=DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now,TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).DateTime);
  var buildings=db.Buildings.AsNoTracking().Where(b=>b.PropertyId==property && b.IsEnabled);
  var units=db.Units.AsNoTracking().Where(u=>u.PropertyId==property && u.IsEnabled && (u.BuildingId==null || buildings.Any(b=>b.Id==u.BuildingId)));
  var occupants=from o in db.UnitOccupancies.AsNoTracking() join person in db.People on o.PersonId equals person.Id join u in units on o.UnitId equals u.Id where o.PropertyId==property && person.PropertyId==property && person.IsEnabled && o.IsEnabled && o.StartDate<=today && (o.EndDate==null || o.EndDate>=today) select new {o.Id,o.Type,PersonId=person.Id,person.FirstName,person.LastName,Number=u.Number};
  var charges=db.DuesCharges.AsNoTracking().Where(c=>c.PropertyId==property && c.IsEnabled);
  var announcements=db.Set<Announcement>().AsNoTracking().Where(a=>a.PropertyId==property && a.IsEnabled);
  var requests=db.Set<MobileRequest>().AsNoTracking().Where(x=>x.PropertyId==property && x.IsEnabled);
  var summary=new ManagerSummary(await buildings.CountAsync(token),await units.CountAsync(token),await occupants.Select(x=>x.PersonId).Distinct().CountAsync(token),await announcements.CountAsync(token),await requests.CountAsync(token),Money(await charges.Where(c=>!c.IsCancelled).SumAsync(c=>(decimal?)(c.Amount-c.PaidAmount),token)??0m),Money(await charges.Where(c=>!c.IsCancelled && c.DueDate<today).SumAsync(c=>(decimal?)(c.Amount-c.PaidAmount),token)??0m));
  var skip=(page-1)*50;ManagerRow[] rows=[];var total=0;
  switch(section){
   case "buildings":total=summary.Buildings;rows=await buildings.OrderBy(x=>x.Name).ThenBy(x=>x.Id).Skip(skip).Take(50).Select(x=>new ManagerRow(x.Id,x.Name,"Bina / blok",null,null)).ToArrayAsync(token);break;
   case "units":total=summary.Units;rows=await units.OrderBy(x=>x.BuildingId).ThenBy(x=>x.Number).ThenBy(x=>x.Id).Skip(skip).Take(50).Select(x=>new ManagerRow(x.Id,x.Number,(x.BuildingId==null?"Doğrudan bağlı bölüm":buildings.Where(b=>b.Id==x.BuildingId).Select(b=>b.Name).First())+" · Kat "+x.Floor,null,null)).ToArrayAsync(token);break;
   case "residents":total=await occupants.CountAsync(token);rows=await occupants.OrderBy(x=>x.LastName).ThenBy(x=>x.FirstName).ThenBy(x=>x.Id).Skip(skip).Take(50).Select(x=>new ManagerRow(x.Id,x.FirstName+" "+x.LastName,"Daire "+x.Number,null,x.Type.ToString())).ToArrayAsync(token);break;
   case "finance":total=await charges.CountAsync(token);var cs=await charges.OrderByDescending(x=>x.Period).ThenBy(x=>x.Id).Skip(skip).Take(50).Select(x=>new {x.Id,x.UnitId,x.Period,x.Amount,x.PaidAmount,x.IsCancelled,x.DueDate,Number=db.Units.Where(u=>u.Id==x.UnitId && u.PropertyId==property).Select(u=>u.Number).FirstOrDefault()}).ToArrayAsync(token);rows=cs.Select(x=>new ManagerRow(x.Id,x.Period.ToString("yyyy-MM",CultureInfo.InvariantCulture),"Daire "+(x.Number??"Arşiv")+" · Son ödeme "+x.DueDate.ToString("dd.MM.yyyy"),Money(x.IsCancelled?0m:x.Amount-x.PaidAmount),x.IsCancelled?"CANCELLED":x.Amount==x.PaidAmount?"PAID":x.DueDate<today?"OVERDUE":"OPEN")).ToArray();break;
   case "announcements":total=summary.Announcements;rows=await announcements.OrderByDescending(x=>x.ModifiedAt).ThenBy(x=>x.Id).Skip(skip).Take(50).Select(x=>new ManagerRow(x.Id,x.Title,x.Content,null,x.Status.ToString())).ToArrayAsync(token);break;
   case "requests":total=summary.Requests;rows=await requests.OrderByDescending(x=>x.ModifiedAt).ThenBy(x=>x.Id).Skip(skip).Take(50).Select(x=>new ManagerRow(x.Id,"Talep",x.Description,null,"RECORDED")).ToArrayAsync(token);break;
  }
  await tx.CommitAsync(token);
  return new(actor.UserId,$"{property}:-:{role}",now,p.Name,p.Address,section,page,total,summary,rows);
 }
}
