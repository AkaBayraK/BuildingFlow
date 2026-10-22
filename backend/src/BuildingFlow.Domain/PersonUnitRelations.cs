using System.Numerics;
namespace BuildingFlow.Domain;

public interface IPersonUnitRelation
{
    Guid TenantId { get; } Guid PropertyId { get; } Guid UnitId { get; } Guid PersonId { get; }
    DateOnly StartDate { get; } DateOnly? EndDate { get; } bool IsPrimaryContact { get; }
    OccupancyType Kind { get; } long? ShareNumerator { get; } long? ShareDenominator { get; }
}

public static class PersonUnitRules
{
    public static void Validate(IPersonUnitRelation value)
    {
        if (value.TenantId == Guid.Empty || value.PropertyId == Guid.Empty || value.UnitId == Guid.Empty || value.PersonId == Guid.Empty)
            throw new ArgumentException("Relation scope is required.");
        if (value.StartDate == DateOnly.MinValue || value.EndDate < value.StartDate)
            throw new ArgumentException("Invalid relation dates.");
        if ((value.ShareNumerator is null) != (value.ShareDenominator is null) ||
            value.ShareNumerator is <= 0 || value.ShareDenominator is <= 0 || value.ShareNumerator > value.ShareDenominator ||
            (value.Kind != OccupancyType.OWNER && value.ShareNumerator != null))
            throw new ArgumentException("Invalid ownership share.");
    }
    public static bool Overlaps(IPersonUnitRelation a, IPersonUnitRelation b) =>
        (a.EndDate == null || b.StartDate <= a.EndDate) && (b.EndDate == null || a.StartDate <= b.EndDate);
    // Validate all date boundaries, including finite intervals. Unknown shares remain unknown, never zero or equal shares.
    public static void ValidateSet(IEnumerable<IPersonUnitRelation> source)
    {
        var rows = source.ToArray(); foreach (var row in rows) Validate(row);
        foreach (var group in rows.GroupBy(r => (r.TenantId,r.PropertyId,r.UnitId)))
        {
            var unit = group.ToArray();
            for (var i=0;i<unit.Length;i++) for (var j=i+1;j<unit.Length;j++)
                if (Overlaps(unit[i],unit[j]) && ((unit[i].PersonId==unit[j].PersonId && unit[i].Kind==unit[j].Kind) ||
                    (unit[i].IsPrimaryContact && unit[j].IsPrimaryContact)))
                    throw new InvalidOperationException("Overlapping relation or primary contact.");
            var known = unit.Where(r=>r.Kind==OccupancyType.OWNER && r.ShareNumerator!=null).ToArray();
            foreach (var day in known.Select(r=>r.StartDate).Distinct())
            {
                BigInteger numerator=0,denominator=1;
                foreach (var r in known.Where(r=>r.StartDate<=day && (r.EndDate==null || r.EndDate>=day)))
                {
                    numerator=numerator*r.ShareDenominator!.Value+r.ShareNumerator!.Value*denominator;
                    denominator*=r.ShareDenominator.Value;
                    var gcd=BigInteger.GreatestCommonDivisor(numerator,denominator);numerator/=gcd;denominator/=gcd;
                }
                if(numerator>denominator)throw new InvalidOperationException("Ownership shares exceed one.");
            }
        }
    }
}

public sealed class PersonPropertyScope : BaseEntity
{
    private PersonPropertyScope() { }
    public PersonPropertyScope(Guid tenantId,Guid propertyId)
    { if(tenantId==Guid.Empty || propertyId==Guid.Empty)throw new ArgumentException("Scope required.");TenantId=tenantId;PropertyId=propertyId; }
    public Guid TenantId {get;private set;} public Guid PropertyId {get;private set;}
}

// One canonical per legacy UUID initially. This mapping is a shadow checkpoint, never an independent v1 write owner.
public sealed class LegacyPersonMap : BaseEntity
{
    private LegacyPersonMap() { }
    public LegacyPersonMap(Guid tenantId,Person source,Guid personId,string localProfile)
    {TenantId=tenantId;PropertyId=source.PropertyId;LegacyPersonId=source.Id;PersonId=personId;SourceVersion=source.Version;LocalProfile=localProfile;}
    public Guid TenantId {get;private set;} public Guid PropertyId {get;private set;}
    public Guid LegacyPersonId {get;private set;} public Guid PersonId {get;private set;}
    public long SourceVersion {get;private set;}
    public string LocalProfile {get;private set;}=null!;
}

public sealed class LegacyOccupancyMap : BaseEntity
{
    private LegacyOccupancyMap() { }
    public LegacyOccupancyMap(Guid tenantId,UnitOccupancy source,Guid? ownership,Guid? tenancy,Guid? residency)
    {TenantId=tenantId;PropertyId=source.PropertyId;UnitId=source.UnitId;LegacyOccupancyId=source.Id;SourceVersion=source.Version;OwnershipId=ownership;TenancyId=tenancy;ResidencyId=residency;}
    public Guid TenantId {get;private set;} public Guid PropertyId {get;private set;} public Guid UnitId {get;private set;}
    public Guid LegacyOccupancyId {get;private set;} public long SourceVersion {get;private set;}
    public Guid? OwnershipId {get;private set;} public Guid? TenancyId {get;private set;} public Guid? ResidencyId {get;private set;}
}

public sealed class UnitOwnership : VerifiableRecord, IPersonUnitRelation
{
    private UnitOwnership() { }
    public UnitOwnership(Guid tenantId,Guid propertyId,Guid unitId,Guid personId,DateOnly startDate,DateOnly? endDate=null,bool isPrimaryContact=false,long? shareNumerator=null,long? shareDenominator=null)
    {TenantId=tenantId;PropertyId=propertyId;UnitId=unitId;PersonId=personId;StartDate=startDate;EndDate=endDate;IsPrimaryContact=isPrimaryContact;ShareNumerator=shareNumerator;ShareDenominator=shareDenominator;PersonUnitRules.Validate(this);}
    public Guid TenantId {get;private set;} public Guid PropertyId {get;private set;} public Guid UnitId {get;private set;} public Guid PersonId {get;private set;}
    public DateOnly StartDate {get;private set;} public DateOnly? EndDate {get;private set;} public bool IsPrimaryContact {get;private set;}
    public OccupancyType Kind => OccupancyType.OWNER;
    public long? ShareNumerator {get;private set;} public long? ShareDenominator {get;private set;}
    public void End(DateOnly date)
    {if(EndDate!=null)throw new InvalidOperationException("Ended relation is immutable.");if(date<StartDate)throw new ArgumentException("Invalid end date.");EndDate=date;}
}

// A tenancy is the lease party row: joint tenants share one LeaseContract, a renewal is a new contract.
public sealed class UnitTenancy : VerifiableRecord, IPersonUnitRelation
{
    private UnitTenancy() { }
    public UnitTenancy(Guid tenantId,Guid propertyId,Guid unitId,Guid personId,DateOnly startDate,DateOnly? endDate=null,bool isPrimaryContact=false)
    {TenantId=tenantId;PropertyId=propertyId;UnitId=unitId;PersonId=personId;StartDate=startDate;EndDate=endDate;IsPrimaryContact=isPrimaryContact;PersonUnitRules.Validate(this);}
    public UnitTenancy(LeaseContract contract,Guid personId,DateOnly startDate,DateOnly? endDate=null,bool isPrimaryContact=false)
        :this(contract.TenantId,contract.PropertyId,contract.UnitId,personId,startDate,endDate,isPrimaryContact){AttachLease(contract);}
    public Guid? LeaseContractId {get;private set;}
    public void AttachLease(LeaseContract contract)
    {
        if(LeaseContractId!=null)throw new InvalidOperationException("Lease party is immutable.");
        if((contract.TenantId,contract.PropertyId,contract.UnitId)!=(TenantId,PropertyId,UnitId))throw new ArgumentException("Lease scope mismatch.");
        if(!contract.Covers(StartDate,EndDate))throw new ArgumentException("Lease party is outside the contract period.");
        LeaseContractId=contract.Id;
    }
    public Guid TenantId {get;private set;} public Guid PropertyId {get;private set;} public Guid UnitId {get;private set;} public Guid PersonId {get;private set;}
    public DateOnly StartDate {get;private set;} public DateOnly? EndDate {get;private set;} public bool IsPrimaryContact {get;private set;}
    public OccupancyType Kind => OccupancyType.TENANT;
    public long? ShareNumerator => null; public long? ShareDenominator => null;
    public void End(DateOnly date)
    {if(EndDate!=null)throw new InvalidOperationException("Ended relation is immutable.");if(date<StartDate)throw new ArgumentException("Invalid end date.");EndDate=date;}
}

public sealed class UnitResidency : VerifiableRecord, IPersonUnitRelation
{
    private UnitResidency() { }
    public UnitResidency(Guid tenantId,Guid propertyId,Guid unitId,Guid personId,DateOnly startDate,DateOnly? endDate=null,bool isPrimaryContact=false)
    {TenantId=tenantId;PropertyId=propertyId;UnitId=unitId;PersonId=personId;StartDate=startDate;EndDate=endDate;IsPrimaryContact=isPrimaryContact;PersonUnitRules.Validate(this);}
    public Guid TenantId {get;private set;} public Guid PropertyId {get;private set;} public Guid UnitId {get;private set;} public Guid PersonId {get;private set;}
    public DateOnly StartDate {get;private set;} public DateOnly? EndDate {get;private set;} public bool IsPrimaryContact {get;private set;}
    public OccupancyType Kind => OccupancyType.RESIDENT;
    public long? ShareNumerator => null; public long? ShareDenominator => null;
    public void End(DateOnly date)
    {if(EndDate!=null)throw new InvalidOperationException("Ended relation is immutable.");if(date<StartDate)throw new ArgumentException("Invalid end date.");EndDate=date;}
}
