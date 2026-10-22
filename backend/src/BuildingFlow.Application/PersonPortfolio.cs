namespace BuildingFlow.Application;
public sealed record PortfolioLease(Guid ContractId,DateOnly StartDate,DateOnly? EndDate,string? ContractReference,string VerificationStatus);
public sealed record PortfolioRelationSource(Guid RelationId,string Kind,Guid PersonId,Guid PropertyId,string PropertyName,Guid? BuildingId,string? BuildingName,Guid UnitId,string UnitNumber,DateOnly StartDate,DateOnly? EndDate,bool IsEnabled,string VerificationStatus,PortfolioLease? Lease);
public sealed record PortfolioRelation(Guid RelationId,string Kind,DateOnly StartDate,DateOnly? EndDate,string VerificationStatus,PortfolioLease? Lease);
public sealed record PortfolioUnit(Guid UnitId,string UnitNumber,string[] Capacities,string Status,PortfolioLease? ActiveLease,PortfolioRelation[] Relations);
public sealed record PortfolioBuilding(Guid? BuildingId,string? BuildingName,PortfolioUnit[] Units);
public sealed record PortfolioProperty(Guid PropertyId,string PropertyName,PortfolioBuilding[] Buildings);

// Input holds the person's rows plus other people's rows of the same units; only the person's rows are returned.
public static class PersonPortfolio
{
    public const string OwnerOccupied="OWNER_OCCUPIED",RentedOut="RENTED_OUT",OwnerOnly="OWNER_ONLY",TenantResident="TENANT_RESIDENT",TenantNotResident="TENANT_NOT_RESIDENT",ResidentOnly="RESIDENT_ONLY";
    private static readonly string[] Kinds=["OWNER","TENANT","RESIDENT"];
    // Pending or rejected rows never grant a capacity; legacy rows predate verification and stay effective.
    public static bool IsEffective(PortfolioRelationSource r,DateOnly today)=>r.IsEnabled && r.VerificationStatus is "VERIFIED" or "LEGACY" && r.StartDate<=today && (r.EndDate==null || r.EndDate>=today);
    public static PortfolioProperty[] Build(Guid person,IEnumerable<PortfolioRelationSource> source,DateOnly today)
    {
        var active=source.Where(r=>IsEffective(r,today)).ToArray();
        var units=active.Where(r=>r.PersonId==person).GroupBy(r=>(r.PropertyId,r.UnitId)).Select(g=>Unit(person,g.ToArray(),active.Where(r=>r.PropertyId==g.Key.PropertyId && r.UnitId==g.Key.UnitId && r.PersonId!=person).ToArray()));
        return units.GroupBy(u=>(u.Head.PropertyId,u.Head.PropertyName)).OrderBy(g=>g.Key.PropertyName,StringComparer.Ordinal).ThenBy(g=>g.Key.PropertyId)
            .Select(p=>new PortfolioProperty(p.Key.PropertyId,p.Key.PropertyName,p.GroupBy(u=>(u.Head.BuildingId,u.Head.BuildingName)).OrderBy(b=>b.Key.BuildingName,StringComparer.Ordinal).ThenBy(b=>b.Key.BuildingId)
                .Select(b=>new PortfolioBuilding(b.Key.BuildingId,b.Key.BuildingName,b.OrderBy(u=>u.Value.UnitNumber,StringComparer.Ordinal).ThenBy(u=>u.Value.UnitId).Select(u=>u.Value).ToArray())).ToArray())).ToArray();
    }
    private static (PortfolioRelationSource Head,PortfolioUnit Value) Unit(Guid person,PortfolioRelationSource[] own,PortfolioRelationSource[] others)
    {
        var capacities=Kinds.Where(k=>own.Any(r=>r.Kind==k)).ToArray();bool Has(string kind)=>capacities.Contains(kind);
        var otherTenancy=others.Where(r=>r.Kind=="TENANT").OrderByDescending(r=>r.StartDate).ThenBy(r=>r.RelationId).FirstOrDefault();
        var status=Has("OWNER")?(Has("RESIDENT")?OwnerOccupied:otherTenancy!=null?RentedOut:OwnerOnly):Has("TENANT")?(Has("RESIDENT")?TenantResident:TenantNotResident):ResidentOnly;
        var ownLease=own.Where(r=>r.Kind=="TENANT" && r.Lease!=null).OrderByDescending(r=>r.StartDate).Select(r=>r.Lease).FirstOrDefault();
        var lease=ownLease??(status==RentedOut?otherTenancy!.Lease:null);
        var head=own[0];
        var relations=own.OrderBy(r=>Array.IndexOf(Kinds,r.Kind)).ThenBy(r=>r.StartDate).ThenBy(r=>r.RelationId).Select(r=>new PortfolioRelation(r.RelationId,r.Kind,r.StartDate,r.EndDate,r.VerificationStatus,r.Lease)).ToArray();
        return (head,new PortfolioUnit(head.UnitId,head.UnitNumber,capacities,status,lease,relations));
    }
}
