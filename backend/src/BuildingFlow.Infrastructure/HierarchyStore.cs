using System.Linq.Expressions;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BuildingFlow.Infrastructure;

public sealed class HierarchyStore(BuildingFlowDbContext db, TimeProvider? clock = null) : IHierarchyStore
{
    private static readonly Expression<Func<Building,BuildingResponse>> BlockProjection = value => new(value.Id,value.PropertyId,value.Name,value.Version,value.CreatedAt,value.IsEnabled,value.Units.Count,value.FloorCount,
        value.ConstructionYear,value.HeatingType.HasValue?value.HeatingType.Value.ToString():null,value.HasElevator,value.ParkingType.HasValue?value.ParkingType.Value.ToString():null);
    private static readonly Expression<Func<Unit,UnitResponse>> ApartmentProjection = value => new(value.Id,value.PropertyId,value.BuildingId,value.Number,value.Floor,value.GrossSquareMeters,value.Version,value.CreatedAt,value.IsEnabled,value.Type.ToString(),
        value.NetSquareMeters,value.RoomLayout.HasValue?value.RoomLayout.Value.ToString():null,value.FloorPosition.HasValue?value.FloorPosition.Value.ToString():null,value.IsMezzanine,value.BathroomCount,value.KitchenType.HasValue?value.KitchenType.Value.ToString():null,value.HasBalcony,value.HeatingType.HasValue?value.HeatingType.Value.ToString():null);
    private IQueryable<Property> Properties(AccessScope access) => access.ReadAll ? db.Properties : db.Properties.Where(site => db.PropertyMemberships.Any(member => member.PropertyId==site.Id && member.UserId==access.UserId && member.IsEnabled));
    private IQueryable<Building> Buildings(Guid site, AccessScope access)
    { var parents=Properties(access); return db.Buildings.Where(block => block.PropertyId==site && parents.Any(parent=>parent.Id==site)); }
    private IQueryable<Unit> Units(Guid site, Guid? block, AccessScope access)
    { var parents=Properties(access); var buildings=Buildings(site,access); return db.Units.Where(value=>value.PropertyId==site && value.BuildingId==block && parents.Any(parent=>parent.Id==site) && (block==null || buildings.Any(parent=>parent.Id==block))); }
    private static void Expected(BaseEntity entity, long version)
    { if(entity.Version!=version) throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create()); }
    private static void Active(BaseEntity entity)
    { if(!entity.IsEnabled) throw new HierarchyConflictException(ErrorCatalog.ParentInactive.Create()); }
    private void Touch(BaseEntity entity)
    {
        // Child commands share the parent's optimistic lock. Concurrent parent
        // disable/delete or another child command rolls back the entire SaveChanges.
        db.Entry(entity).Property(value=>value.IsEnabled).IsModified=true;
    }
    private async Task Save(CancellationToken token)
    {
        try { await db.SaveChangesAsync(token); }
        catch(DbUpdateException error) when(error.InnerException is PostgresException {SqlState:PostgresErrorCodes.UniqueViolation,ConstraintName:"IX_buildings_property_id_name"})
        { throw new HierarchyConflictException(ErrorCatalog.BuildingNameExists.Create()); }
        catch(DbUpdateException error) when(error.InnerException is PostgresException {SqlState:PostgresErrorCodes.UniqueViolation,ConstraintName:"IX_units_building_id_number" or "IX_units_property_id_number"})
        { throw new HierarchyConflictException(ErrorCatalog.BuildingUnitNumberExists.Create()); }
    }
    private static PropertyResponse SiteResult(Property value,bool canChangeType) => new(value.Id,value.Name,value.City,value.District,value.Address,value.Version,value.CreatedAt,value.IsEnabled,value.Type.ToString(),value.FloorCount,value.Buildings.Count(item=>!item.IsDeleted),value.Units.Count(item=>!item.IsDeleted),canChangeType,
        value.Neighborhood,value.ConstructionYear,value.HeatingType?.ToString(),value.HasElevator,value.ParkingType?.ToString());
    private static BuildingResponse BlockResult(Building value) => new(value.Id,value.PropertyId,value.Name,value.Version,value.CreatedAt,value.IsEnabled,value.Units.Count(item=>!item.IsDeleted),value.FloorCount,
        value.ConstructionYear,value.HeatingType?.ToString(),value.HasElevator,value.ParkingType?.ToString());
    private static UnitResponse ApartmentResult(Unit value) => new(value.Id,value.PropertyId,value.BuildingId,value.Number,value.Floor,value.GrossSquareMeters,value.Version,value.CreatedAt,value.IsEnabled,value.Type.ToString(),
        value.NetSquareMeters,value.RoomLayout?.ToString(),value.FloorPosition?.ToString(),value.IsMezzanine,value.BathroomCount,value.KitchenType?.ToString(),value.HasBalcony,value.HeatingType?.ToString());
    private static string Pattern(string text) => "%"+text.Trim().Replace("\\","\\\\").Replace("%","\\%").Replace("_","\\_")+"%";
    private Task<Property?> LoadSite(Guid id, AccessScope access, CancellationToken token) => Properties(access).Include(site=>site.Buildings).ThenInclude(block=>block.Units).Include(site=>site.Units).SingleOrDefaultAsync(site=>site.Id==id,token);

    public async Task<PropertyResponse?> UpdatePropertyAsync(Guid siteId, UpdatePropertyRequest request, AccessScope access, CancellationToken token)
    {
        var site=await LoadSite(siteId,access,token); if(site is null)return null;
        Expected(site,request.Version);
        var type=request.Type is null?site.Type:Enum.Parse<PropertyType>(request.Type);
        // History is structural data too; creator memberships do not lock the type.
        var canChangeType=!await db.Buildings.IgnoreQueryFilters().AnyAsync(value=>value.PropertyId==siteId,token)
            && !await db.Units.IgnoreQueryFilters().AnyAsync(value=>value.PropertyId==siteId,token);
        if(type!=site.Type && !canChangeType)throw new HierarchyConflictException(ErrorCatalog.PropertyTypeLocked.Create());
        if(type!=PropertyType.APARTMENT && request.FloorCount is not null)throw new HierarchyConflictException(ErrorCatalog.BuildingFloorMetadata.Create());
        if(type!=PropertyType.APARTMENT && !request.Features.IsEmpty)throw new RequestValidationException(new(){["buildingFeatures"]=[ErrorCatalog.BuildingFeaturesApartmentOnly.Create()]});
        if(request.IsEnabled==false && await PersonCenterStore.HasCurrentNative(db,siteId,null,null,BusinessDate.Today(clock??TimeProvider.System),token))throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        site.UpdateDetails(request.Name!,request.City!,request.District!,request.Address!,request.IsEnabled!.Value,request.FloorCount,type,request.Features.ToFeatures());site.SetNeighborhood(request.Neighborhood);
        Touch(site); await Save(token); return SiteResult(site,canChangeType);
    }
    public async Task<bool> DeletePropertyAsync(Guid siteId, long version, AccessScope access, CancellationToken token)
    {
        var site=await Properties(access).SingleOrDefaultAsync(value=>value.Id==siteId,token); if(site is null)return false;
        Expected(site,version);
        if(await PersonCenterStore.HasCurrentNative(db,siteId,null,null,BusinessDate.Today(clock??TimeProvider.System),token))throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        if(await db.Announcements.IgnoreQueryFilters().AnyAsync(value=>value.PropertyId==siteId,token) || await db.StoredDocuments.IgnoreQueryFilters().AnyAsync(value=>value.PropertyId==siteId,token)) throw new HierarchyConflictException(ErrorCatalog.ContentPropertyHistory.Create());
        if(await db.Meters.IgnoreQueryFilters().AnyAsync(value=>value.PropertyId==siteId,token)) throw new HierarchyConflictException(ErrorCatalog.MeterConflict.Create());
        if(await db.MaintenancePlans.IgnoreQueryFilters().AnyAsync(value=>value.PropertyId==siteId,token)) throw new HierarchyConflictException(ErrorCatalog.ContentPropertyHistory.Create());
        if(await db.Surveys.IgnoreQueryFilters().AnyAsync(value=>value.PropertyId==siteId,token)) throw new HierarchyConflictException(ErrorCatalog.ContentPropertyHistory.Create());
        if(await db.DuesAssessments.AnyAsync(value=>value.PropertyId==siteId,token) || await db.Expenses.AnyAsync(value=>value.PropertyId==siteId,token) || await db.PropertyBudgets.AnyAsync(value=>value.PropertyId==siteId,token)) throw new HierarchyConflictException(ErrorCatalog.DuesErrorPropertyHistory.Create());
        if(await db.Set<MobileRequest>().IgnoreQueryFilters().AnyAsync(value=>value.PropertyId==siteId,token)) throw new HierarchyConflictException(ErrorCatalog.PropertyHasChildren.Create());
        if(await db.Buildings.AnyAsync(value=>value.PropertyId==siteId,token) || await db.Units.AnyAsync(value=>value.PropertyId==siteId,token) || await db.People.AnyAsync(value=>value.PropertyId==siteId,token)) throw new HierarchyConflictException(ErrorCatalog.PropertyHasChildren.Create());
        db.Remove(site); await Save(token); return true;
    }
    public async Task<PagedRows<BuildingResponse>?> ListBuildingsAsync(Guid siteId, HierarchyQuery request, AccessScope access, CancellationToken token)
    {
        if(!await Properties(access).AnyAsync(value=>value.Id==siteId,token))return null;
        var query=Buildings(siteId,access).AsNoTracking();
        if(!string.IsNullOrWhiteSpace(request.Search)) { var pattern=Pattern(request.Search); query=query.Where(value=>EF.Functions.ILike(value.Name,pattern,"\\")); }
        var count=await query.CountAsync(token); var desc=request.SortDirection=="desc";
        var ordered=request.SortBy=="createdAt" ? (desc?query.OrderByDescending(value=>value.CreatedAt):query.OrderBy(value=>value.CreatedAt)) : (desc?query.OrderByDescending(value=>value.Name):query.OrderBy(value=>value.Name));
        var rows=await ordered.ThenBy(value=>value.Id).Skip((request.Page-1)*request.PageSize).Take(request.PageSize).Select(BlockProjection).ToListAsync(token);
        return new(rows,request.Page,request.PageSize,count);
    }
    public Task<BuildingResponse?> GetBuildingAsync(Guid siteId, Guid blockId, AccessScope access, CancellationToken token) => Buildings(siteId,access).AsNoTracking().Where(value=>value.Id==blockId).Select(BlockProjection).SingleOrDefaultAsync(token);
    public async Task<BuildingResponse?> CreateBuildingAsync(Guid siteId, CreateBuildingRequest request, AccessScope access, CancellationToken token)
    {
        var site=await LoadSite(siteId,access,token); if(site is null)return null; Active(site);
        if(site.Type==PropertyType.APARTMENT)throw new HierarchyConflictException(ErrorCatalog.ApartmentNoBuilding.Create());
        if(site.Buildings.Any(value=>string.Equals(value.Name,request.Name!.Trim(),StringComparison.OrdinalIgnoreCase)))throw new HierarchyConflictException(ErrorCatalog.BuildingNameExists.Create());
        var block=site.AddBuilding(request.Name!,request.FloorCount,request.Features.ToFeatures()); db.Buildings.Add(block); Touch(site); await Save(token); return BlockResult(block);
    }
    public async Task<BuildingResponse?> UpdateBuildingAsync(Guid siteId, Guid blockId, UpdateBuildingRequest request, AccessScope access, CancellationToken token)
    {
        var site=await LoadSite(siteId,access,token); var block=site?.Buildings.SingleOrDefault(value=>value.Id==blockId); if(block is null)return null;
        Active(site!); Expected(block,request.Version);
        if(site!.Buildings.Any(value=>value.Id!=blockId && string.Equals(value.Name,request.Name!.Trim(),StringComparison.OrdinalIgnoreCase)))throw new HierarchyConflictException(ErrorCatalog.BuildingNameExists.Create());
        site.UpdateBuilding(blockId,request.Name!,request.IsEnabled!.Value,request.FloorCount,request.Features.ToFeatures()); Touch(block); Touch(site); await Save(token); return BlockResult(block);
    }
    public async Task<bool> DeleteBuildingAsync(Guid siteId, Guid blockId, long version, AccessScope access, CancellationToken token)
    {
        var site=await LoadSite(siteId,access,token); var block=site?.Buildings.SingleOrDefault(value=>value.Id==blockId); if(block is null)return false;
        Active(site!); Expected(block,version);
        if(block.Units.Count>0)throw new HierarchyConflictException(ErrorCatalog.BuildingHasUnits.Create());
        db.Remove(block); Touch(site!); await Save(token); return true;
    }
    public async Task<PagedRows<UnitResponse>?> ListUnitsAsync(Guid siteId, Guid? blockId, HierarchyQuery request, AccessScope access, CancellationToken token)
    {
        if(!await Properties(access).AnyAsync(value=>value.Id==siteId,token) || (blockId is not null && !await Buildings(siteId,access).AnyAsync(value=>value.Id==blockId,token)))return null;
        var query=Units(siteId,blockId,access).AsNoTracking();
        if(request.UnitId is { } unitId)query=query.Where(value=>value.Id==unitId);
        if(!string.IsNullOrWhiteSpace(request.Search)){var pattern=Pattern(request.Search);query=query.Where(value=>EF.Functions.ILike(value.Number,pattern,"\\"));}
        var count=await query.CountAsync(token);var desc=request.SortDirection=="desc";
        var ordered=request.SortBy switch {
            "floor" => desc?query.OrderByDescending(value=>value.Floor):query.OrderBy(value=>value.Floor),
            "grossSquareMeters"=>desc?query.OrderByDescending(value=>value.GrossSquareMeters):query.OrderBy(value=>value.GrossSquareMeters),
            "createdAt"=>desc?query.OrderByDescending(value=>value.CreatedAt):query.OrderBy(value=>value.CreatedAt),
            _=>desc?query.OrderByDescending(value=>value.Number):query.OrderBy(value=>value.Number)};
        var rows=await ordered.ThenBy(value=>value.Id).Skip((request.Page-1)*request.PageSize).Take(request.PageSize).Select(ApartmentProjection).ToListAsync(token);
        return new(rows,request.Page,request.PageSize,count);
    }
    public Task<UnitResponse?> GetUnitAsync(Guid siteId, Guid? blockId, Guid apartmentId, AccessScope access, CancellationToken token) => Units(siteId,blockId,access).AsNoTracking().Where(value=>value.Id==apartmentId).Select(ApartmentProjection).SingleOrDefaultAsync(token);
    private static void UnitParent(Property property,Building? building,Guid? buildingId,UnitType type)
    {
        Active(property);if(building is not null)Active(building);
        if(property.Type==PropertyType.APARTMENT && (buildingId is not null || type!=UnitType.APARTMENT))throw new HierarchyConflictException(ErrorCatalog.ApartmentDirectUnitsOnly.Create());
        if(property.Type!=PropertyType.APARTMENT && buildingId is null)throw new HierarchyConflictException(ErrorCatalog.BuildingRequired.Create());
    }
    public async Task<UnitResponse?> CreateUnitAsync(Guid siteId, Guid? blockId, CreateUnitRequest request, AccessScope access, CancellationToken token)
    {
        var site=await LoadSite(siteId,access,token);if(site is null)return null;var block=site.Buildings.SingleOrDefault(value=>value.Id==blockId);if(blockId is not null && block is null)return null;
        var type=Enum.Parse<UnitType>(request.Type);UnitParent(site,block,blockId,type);
        var units=block is null?site.Units.Where(value=>value.BuildingId is null):block.Units;
        if(units.Any(value=>string.Equals(value.Number,request.Number!.Trim(),StringComparison.OrdinalIgnoreCase)))throw new HierarchyConflictException(ErrorCatalog.UnitNumberExists.Create());
        PropertyDetails.ValidateFloorWithinBuilding(request.Floor!.Value,block is null?site.FloorCount:block.FloorCount);var details=request.Features.ToFeatures();
        var unit=block is null?site.AddUnit(request.Number!,request.Floor!.Value,request.GrossSquareMeters!.Value,type,details):block.AddUnit(request.Number!,request.Floor!.Value,request.GrossSquareMeters!.Value,type,details);
        db.Units.Add(unit);Touch(site);if(block is not null)Touch(block);await Save(token);return ApartmentResult(unit);
    }
    public async Task<UnitResponse?> UpdateUnitAsync(Guid siteId, Guid? blockId, Guid apartmentId, UpdateUnitRequest request, AccessScope access, CancellationToken token)
    {
        var site=await LoadSite(siteId,access,token);if(site is null)return null;var block=site.Buildings.SingleOrDefault(value=>value.Id==blockId);if(blockId is not null && block is null)return null;
        var units=block is null?site.Units.Where(value=>value.BuildingId is null):block.Units;var unit=units.SingleOrDefault(value=>value.Id==apartmentId);if(unit is null)return null;
        var type=Enum.Parse<UnitType>(request.Type);UnitParent(site,block,blockId,type);Expected(unit,request.Version);
        var today=BusinessDate.Today(clock ?? TimeProvider.System);
        if(request.IsEnabled==false && await PersonCenterStore.HasCurrentNative(db,siteId,unit.Id,null,today,token))throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        if(request.IsEnabled==false && await db.UnitOccupancies.AnyAsync(value=>value.UnitId==unit.Id && (value.EndDate==null || value.EndDate>=today),token))throw new HierarchyConflictException(ErrorCatalog.UnitHasOccupancy.Create());
        if(units.Any(value=>value.Id!=apartmentId && string.Equals(value.Number,request.Number!.Trim(),StringComparison.OrdinalIgnoreCase)))throw new HierarchyConflictException(ErrorCatalog.UnitNumberExists.Create());
        PropertyDetails.ValidateFloorWithinBuilding(request.Floor!.Value,block is null?site.FloorCount:block.FloorCount);var details=request.Features.ToFeatures();
        if(block is null)site.UpdateUnit(apartmentId,request.Number!,request.Floor!.Value,request.GrossSquareMeters!.Value,request.IsEnabled!.Value,type,details);
        else block.UpdateUnit(apartmentId,request.Number!,request.Floor!.Value,request.GrossSquareMeters!.Value,request.IsEnabled!.Value,type,details);
        Touch(unit);Touch(site);if(block is not null)Touch(block);await Save(token);return ApartmentResult(unit);
    }
    public async Task<bool> DeleteUnitAsync(Guid siteId, Guid? blockId, Guid apartmentId, long version, AccessScope access, CancellationToken token)
    {
        var site=await LoadSite(siteId,access,token);if(site is null)return false;var block=site.Buildings.SingleOrDefault(value=>value.Id==blockId);if(blockId is not null && block is null)return false;
        var units=block is null?site.Units.Where(value=>value.BuildingId is null):block.Units;var unit=units.SingleOrDefault(value=>value.Id==apartmentId);if(unit is null)return false;
        UnitParent(site,block,blockId,unit.Type);Expected(unit,version);
        var today=BusinessDate.Today(clock ?? TimeProvider.System);
        if(await PersonCenterStore.HasCurrentNative(db,siteId,unit.Id,null,today,token))throw new HierarchyConflictException(ErrorCatalog.RecordChanged.Create());
        if(await db.UnitOccupancies.AnyAsync(value=>value.UnitId==unit.Id && (value.EndDate==null || value.EndDate>=today),token))throw new HierarchyConflictException(ErrorCatalog.UnitDeleteHasOccupancy.Create());
        db.Remove(unit);Touch(site);if(block is not null)Touch(block);await Save(token);return true;
    }
}
