using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed class MobileOnboardingStore(BuildingFlowDbContext db, TimeProvider clock) : IMobileOnboardingStore
{
    public async Task<StoredRegistration?> RegistrationAsync(Guid requestId, CancellationToken token) =>
        await db.MobileRegistrations.AsNoTracking().Where(x => x.RequestId == requestId).Select(x => new StoredRegistration(x.Fingerprint, x.UserId, x.Username)).SingleOrDefaultAsync(token);

    public Task<int> RecentRegistrationsAsync(string clientKey, TimeSpan window, CancellationToken token)
    { var since = clock.GetUtcNow() - window; return db.MobileRegistrations.CountAsync(x => x.ClientKey == clientKey && x.CreatedAt >= since, token); }

    public async Task SaveRegistrationAsync(Guid requestId, string fingerprint, string userId, string username, string clientKey, PersonIdentity identity, CancellationToken token)
    {
        db.MobileRegistrations.Add(new MobileRegistration(requestId, fingerprint, userId, username, clientKey));
        var profile = await db.Set<MobileProfile>().SingleOrDefaultAsync(x => x.OwnerUserId == userId, token);
        if (profile is null) { profile = new MobileProfile(userId); db.Add(profile); }
        profile.Edit(identity.FirstName, identity.LastName, identity.PhoneNumber, profile.PhotoId);
        await db.SaveChangesAsync(token);
    }

    public async Task<bool> IsLockedAsync(string userId, CancellationToken token)
    {
        var since = clock.GetUtcNow() - PersonMatching.LockWindow;
        return await db.PersonLinkAttempts.CountAsync(x => x.UserId == userId && !x.Succeeded && x.CreatedAt >= since, token) >= PersonMatching.MaxFailures;
    }

    public async Task RecordFailureAsync(string userId, CancellationToken token)
    {
        db.ChangeTracker.Clear();
        db.PersonLinkAttempts.Add(new PersonLinkAttempt(userId, false, null));
        await db.SaveChangesAsync(token);
    }

    // Every rejection reason returns the same result so the endpoint cannot be used to learn which field or person exists.
    public async Task<PersonLinkResult> LinkAsync(string userId, string codeHash, PersonIdentity identity, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var candidate = await db.People.AsNoTracking().Where(p => p.PersonCodeHash == codeHash).Select(p => new { p.Id, p.PropertyId }).SingleOrDefaultAsync(token);
        if (candidate is not null) await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {candidate.PropertyId} FOR UPDATE", token);
        var person = candidate is null ? null : await db.People.SingleOrDefaultAsync(p => p.Id == candidate.Id && p.PersonCodeHash == codeHash, token);
        var property = person is null ? null : await db.Properties.SingleOrDefaultAsync(p => p.Id == person.PropertyId, token);
        var today = BusinessDate.Today(clock);
        var accepted = person is not null && property is not null && property.IsEnabled && person.IsEnabled && person.PersonKind is not null &&
            (person.LinkedUserId is null || person.LinkedUserId == userId) &&
            PersonMatching.Matches(identity, person.FirstName, person.LastName, person.PhoneNumber, person.Email) &&
            await db.UnitOccupancies.AnyAsync(o => o.PersonId == person.Id && o.PropertyId == person.PropertyId && o.IsEnabled && o.StartDate <= today && (o.EndDate == null || o.EndDate >= today) &&
                db.Units.Any(u => u.Id == o.UnitId && u.IsEnabled), token) &&
            !await db.People.AnyAsync(p => p.PropertyId == person.PropertyId && p.LinkedUserId == userId && p.Id != person.Id, token);
        if (!accepted)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            await RecordFailureAsync(userId, token);
            return new(PersonMatching.Rejected, ErrorCode: ErrorCatalog.MobileLinkRejected.Code);
        }
        var membership = await db.PropertyMemberships.SingleOrDefaultAsync(m => m.PropertyId == property!.Id && m.UserId == userId, token);
        if (membership is null) db.PropertyMemberships.Add(new PropertyMembership(property!.Id, userId));
        else if (!membership.IsEnabled) { membership.SetEnabled(true); db.Entry(membership).Property(m => m.IsEnabled).IsModified = true; }
        person!.LinkUser(userId);
        person.ClearCode();
        db.PersonLinkAttempts.Add(new PersonLinkAttempt(userId, true, person.Id));
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return new(PersonMatching.Linked, property!.Id, property.Name);
    }
}
