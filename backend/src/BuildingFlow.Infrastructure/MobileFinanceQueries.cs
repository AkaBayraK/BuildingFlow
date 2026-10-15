using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;
internal static class MobileFinanceQueries {
 public static IQueryable<DuesCharge> ResidentCharges(BuildingFlowDbContext db,Guid property,Guid unit,string role,string user)=>db.DuesCharges.AsNoTracking().Where(c=>c.PropertyId==property && c.UnitId==unit && c.IsEnabled && (db.DuesAssessments.Any(a=>a.Id==c.AssessmentId && a.PropertyId==property && a.DebtorType=="UNIT") && !db.Set<DuesChargeParty>().Any(party=>party.ChargeId==c.Id && party.IsDebtor) || db.Set<DuesChargeParty>().Any(party=>party.ChargeId==c.Id && party.PropertyId==property && party.UnitId==unit && (role=="OWNER"?party.Type==OccupancyType.OWNER:party.Type==OccupancyType.TENANT) && party.IsDebtor && db.People.Any(person=>person.Id==party.PersonId && person.PropertyId==property && person.LinkedUserId==user && person.IsEnabled))));
}
