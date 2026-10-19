using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

// Server-side consent checks shared by profile, request photo and consent commands. The mobile check is only user experience.
internal static class ConsentGate
{
    // Every writer that reads or changes a user's consent takes this lock last, so a withdrawal and an upload cannot interleave.
    public static Task LockAsync(BuildingFlowDbContext db, string user, CancellationToken token) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"consent:" + user},0))", token);

    // ACTIVE: an unrevoked approval with no later text that asks for re-consent.
    public static Task<bool> ActiveAsync(BuildingFlowDbContext db, string user, ConsentFeature feature, CancellationToken token) =>
        (from c in db.UserConsents
         join t in db.ConsentTypes on c.ConsentTypeId equals t.Id
         where c.OwnerUserId == user && t.Feature == feature && t.IsEnabled && c.RevokedAt == null &&
               !db.ConsentTexts.Any(x => x.ConsentTypeId == c.ConsentTypeId && x.TextVersion > c.TextVersion && x.RequiresReconsent)
         select c.Id).AnyAsync(token);

    public static async Task RequireAsync(BuildingFlowDbContext db, string user, ConsentFeature feature, CancellationToken token)
    {
        if (!await ActiveAsync(db, user, feature, token)) throw new CodedForbiddenException(ErrorCatalog.ConsentRequired.Create());
    }

    // Request photos stay with the centre after a withdrawal; the owner stops seeing every photo that was current before it.
    public static IQueryable<MobileRequestPhoto> VisibleToOwner(BuildingFlowDbContext db, IQueryable<MobileRequestPhoto> photos) =>
        photos.Where(p => !(from c in db.UserConsents.IgnoreQueryFilters()
                            join t in db.ConsentTypes.IgnoreQueryFilters() on c.ConsentTypeId equals t.Id
                            where c.OwnerUserId == p.OwnerUserId && t.Feature == ConsentFeature.REQUEST_PHOTO &&
                                  c.RevokeReason == ConsentRevokeReason.WITHDRAWN && c.RevokedAt >= p.ModifiedAt
                            select c.Id).Any());
}
