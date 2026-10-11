using System.Linq.Expressions;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingFlow.Infrastructure;

public sealed class PropertyStore(BuildingFlowDbContext db) : IPropertyStore
{
    private static readonly Expression<Func<Property, PropertyResponse>> Projection = site => new PropertyResponse(
        site.Id, site.Name, site.City, site.District, site.Address, site.Version, site.CreatedAt, site.IsEnabled, site.Type.ToString(), site.FloorCount, site.Buildings.Count, site.Units.Count, false,
        site.Neighborhood, site.ConstructionYear, site.HeatingType.HasValue ? site.HeatingType.Value.ToString() : null, site.HasElevator,
        site.ParkingType.HasValue ? site.ParkingType.Value.ToString() : null);

    private Task<List<Guid>> RelatedIds(Guid[] ids,CancellationToken token) => db.Buildings.IgnoreQueryFilters()
        .Where(value=>ids.Contains(value.PropertyId)).Select(value=>value.PropertyId)
        .Union(db.Units.IgnoreQueryFilters().Where(value=>ids.Contains(value.PropertyId)).Select(value=>value.PropertyId)).ToListAsync(token);

    private IQueryable<Property> Accessible(string userId, bool readAll)
    {
        var query = db.Properties.AsNoTracking(); // Existing soft-delete filter is always retained.
        return readAll ? query : query.Where(site => db.PropertyMemberships.Any(member =>
            member.PropertyId == site.Id && member.UserId == userId && member.IsEnabled));
    }
    public async Task SaveNewAsync(Property site, string userId, CancellationToken cancellationToken)
    {
        db.Properties.Add(site);
        db.PropertyMemberships.Add(new PropertyMembership(site.Id, userId));
        // Both rows and their audit fields commit in a single EF transaction.
        await db.SaveChangesAsync(cancellationToken);
    }
    public async Task<PagedProperties> ListAsync(string userId, bool readAll, PropertyListQuery request, CancellationToken cancellationToken)
    {
        var query = Accessible(userId, readAll);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{term}%";
            query = query.Where(site => EF.Functions.ILike(site.Name, pattern, "\\") ||
                EF.Functions.ILike(site.City, pattern, "\\") || EF.Functions.ILike(site.District, pattern, "\\"));
        }
        var count = await query.CountAsync(cancellationToken);
        var descending = request.SortDirection == "desc";
        IOrderedQueryable<Property> ordered = request.SortBy switch
        {
            "city" => descending ? query.OrderByDescending(site => site.City) : query.OrderBy(site => site.City),
            "createdAt" => descending ? query.OrderByDescending(site => site.CreatedAt) : query.OrderBy(site => site.CreatedAt),
            _ => descending ? query.OrderByDescending(site => site.Name) : query.OrderBy(site => site.Name)
        };
        var items = await ordered.ThenBy(site => site.Id).Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize).Select(Projection).ToListAsync(cancellationToken);
        // Keep the history query separate so IgnoreQueryFilters cannot affect
        // the main property/membership scope or visible child counts.
        var related=items.Count==0?[]:(await RelatedIds(items.Select(value=>value.Id).ToArray(),cancellationToken)).ToHashSet();
        return new(items.Select(value=>value with {CanChangeType=!related.Contains(value.Id)}).ToList(), request.Page, request.PageSize, count);
    }
    public async Task<PropertyResponse?> GetAsync(Guid id, string userId, bool readAll, CancellationToken cancellationToken)
    {
        var property=await Accessible(userId,readAll).Where(site=>site.Id==id).Select(Projection).SingleOrDefaultAsync(cancellationToken);
        if(property is null)return null;
        return property with {CanChangeType=(await RelatedIds([id],cancellationToken)).Count==0};
    }
}
