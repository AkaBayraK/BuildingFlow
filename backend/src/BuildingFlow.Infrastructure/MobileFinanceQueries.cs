using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
internal static class MobileFinanceQueries {
 public static IQueryable<DuesCharge> ResidentCharges(BuildingFlowDbContext db,Guid property,Guid unit,string role,string user)=>db.DuesCharges.AsNoTracking().Where(c=>c.PropertyId==property && c.UnitId==unit && c.IsEnabled && (db.DuesAssessments.Any(a=>a.Id==c.AssessmentId && a.PropertyId==property && a.DebtorType=="UNIT") && !db.Set<DuesChargeParty>().Any(party=>party.ChargeId==c.Id && party.IsDebtor) || db.Set<DuesChargeParty>().Any(party=>party.ChargeId==c.Id && party.PropertyId==property && party.UnitId==unit && (role=="OWNER"?party.Type==OccupancyType.OWNER:party.Type==OccupancyType.TENANT) && party.IsDebtor && db.People.Any(person=>person.Id==party.PersonId && person.PropertyId==property && person.LinkedUserId==user && person.IsEnabled))));
 // A resident context is valid only while the mobile session still grants that unit and role on an active Property.
 public static async Task<Property> ResidentPropertyAsync(BuildingFlowDbContext db,IMobileSessionStore sessions,ICurrentUserService actor,Guid property,Guid unit,string role,CancellationToken token){
  if(!actor.IsAuthenticated || string.IsNullOrWhiteSpace(actor.UserId) || role is not ("OWNER" or "TENANT"))throw new UnauthorizedAccessException();
  var session=await sessions.ReadAsync(actor.UserId,actor.Username,Array.Empty<string>(),token);
  if(session.HasMore || !session.Contexts.Any(c=>c.PropertyId==property && c.UnitId==unit && c.Role==role))throw new UnauthorizedAccessException();
  return await db.Properties.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==property && p.IsEnabled && db.PropertyMemberships.Any(m=>m.PropertyId==property && m.UserId==actor.UserId && m.IsEnabled),token)??throw new UnauthorizedAccessException();
 }
}
