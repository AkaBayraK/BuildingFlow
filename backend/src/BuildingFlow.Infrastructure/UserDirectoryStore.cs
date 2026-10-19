using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed class UserDirectoryStore(BuildingFlowDbContext db) : IUserDirectoryStore
{
    public async Task<IReadOnlyList<Guid>> ManagedPropertiesAsync(string actor, CancellationToken token) =>
        await db.PropertyMemberships.AsNoTracking().Where(m => m.UserId == actor && m.IsEnabled && db.Properties.Any(p => p.Id == m.PropertyId))
            .Select(m => m.PropertyId).Distinct().ToListAsync(token);

    public async Task<PagedRows<string>> MemberIdsAsync(IReadOnlyList<Guid> properties, int page, int pageSize, CancellationToken token)
    {
        var ids = db.PropertyMemberships.AsNoTracking().Where(m => properties.Contains(m.PropertyId) && m.IsEnabled).Select(m => m.UserId).Distinct();
        return new(await ids.OrderBy(id => id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token), page, pageSize, await ids.CountAsync(token));
    }

    public async Task<IReadOnlySet<string>> MembersAmongAsync(IReadOnlyList<Guid>? properties, IReadOnlyCollection<string> userIds, CancellationToken token) =>
        (await db.PropertyMemberships.AsNoTracking().Where(m => userIds.Contains(m.UserId) && m.IsEnabled && (properties == null || properties.Contains(m.PropertyId)))
            .Select(m => m.UserId).Distinct().ToListAsync(token)).ToHashSet();

    public async Task<IReadOnlyDictionary<string, (int PropertyCount, IReadOnlyList<LinkedPerson> People)>> LinksAsync(IReadOnlyCollection<string> userIds, IReadOnlyList<Guid>? properties, CancellationToken token)
    {
        var counts = await db.PropertyMemberships.AsNoTracking().Where(m => userIds.Contains(m.UserId) && m.IsEnabled && (properties == null || properties.Contains(m.PropertyId)))
            .GroupBy(m => m.UserId).Select(g => new { g.Key, Count = g.Select(m => m.PropertyId).Distinct().Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, token);
        var people = await (from person in db.People.AsNoTracking()
                            join property in db.Properties on person.PropertyId equals property.Id
                            where person.LinkedUserId != null && userIds.Contains(person.LinkedUserId) && (properties == null || properties.Contains(person.PropertyId))
                            orderby property.Name, person.LastName, person.FirstName
                            select new { person.LinkedUserId, Link = new LinkedPerson(person.Id, person.PropertyId, property.Name, (person.FirstName + " " + person.LastName).Trim(), person.Version) })
            .Take(500).ToListAsync(token);
        return userIds.ToDictionary(id => id, id => (counts.GetValueOrDefault(id), (IReadOnlyList<LinkedPerson>)people.Where(p => p.LinkedUserId == id).Select(p => p.Link).ToArray()));
    }

    public Task<bool> PropertyExistsAsync(Guid property, CancellationToken token) => db.Properties.AnyAsync(p => p.Id == property, token);

    public async Task EnsureMembershipAsync(Guid property, string userId, CancellationToken token)
    {
        await Ensure(property, userId, token);
        await db.SaveChangesAsync(token);
    }

    public async Task<LinkedPerson?> LinkAsync(Guid property, Guid personId, long version, string userId, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, token); if (parent is null) return null;
        var person = await db.People.SingleOrDefaultAsync(p => p.PropertyId == property && p.Id == personId, token); if (person is null) return null;
        if (person.Version != version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        if (!parent.IsEnabled || !person.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.OccupancyParentInactive.Create());
        if (person.LinkedUserId == userId) return Map(person, parent);
        // The people profile constraint allows an account link only on a completed person profile.
        if (person.PersonKind is null) throw new RequestValidationException(new() { ["personId"] = [ErrorCatalog.PersonProfileInvalid.Create()] });
        if (person.LinkedUserId is not null || await db.People.AnyAsync(p => p.PropertyId == property && p.LinkedUserId == userId, token))
            throw new HierarchyConflictException(ErrorCatalog.UserLinkConflict.Create());
        await Ensure(property, userId, token);
        person.LinkUser(userId);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return Map(person, parent);
    }

    public async Task<bool> UnlinkAsync(Guid property, Guid personId, long version, string userId, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (await Lock(property, token) is null) return false;
        var person = await db.People.SingleOrDefaultAsync(p => p.PropertyId == property && p.Id == personId && p.LinkedUserId == userId, token);
        if (person is null) return false;
        if (person.Version != version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        person.LinkUser(null);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return true;
    }

    private async Task<Property?> Lock(Guid property, CancellationToken token)
    {
        if (!await db.Properties.AnyAsync(p => p.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        return await db.Properties.SingleOrDefaultAsync(p => p.Id == property, token);
    }

    private async Task Ensure(Guid property, string userId, CancellationToken token)
    {
        var membership = await db.PropertyMemberships.SingleOrDefaultAsync(m => m.PropertyId == property && m.UserId == userId, token);
        if (membership is null) db.PropertyMemberships.Add(new PropertyMembership(property, userId));
        else if (!membership.IsEnabled) { membership.SetEnabled(true); db.Entry(membership).Property(m => m.IsEnabled).IsModified = true; }
    }

    private static LinkedPerson Map(Person person, Property property) => new(person.Id, property.Id, property.Name, (person.FirstName + " " + person.LastName).Trim(), person.Version);
}
