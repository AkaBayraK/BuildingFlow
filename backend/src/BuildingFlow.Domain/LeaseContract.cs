namespace BuildingFlow.Domain;

public enum RelationVerificationStatus { LEGACY, PENDING, VERIFIED, REJECTED }

// LEGACY marks rows created before verification existed; it never names a verifier that did not act.
public abstract class VerifiableRecord : BaseEntity
{
    public RelationVerificationStatus VerificationStatus {get;private set;}=RelationVerificationStatus.PENDING;
    public Guid? VerifiedByAccountId {get;private set;}
    public DateTimeOffset? VerifiedAt {get;private set;}
    public Guid? EvidenceDocumentId {get;private set;}
    public void Verify(Guid account,DateTimeOffset at,Guid? evidence=null)=>Decide(RelationVerificationStatus.VERIFIED,account,at,evidence);
    public void Reject(Guid account,DateTimeOffset at,Guid? evidence=null)=>Decide(RelationVerificationStatus.REJECTED,account,at,evidence);
    public void MarkLegacy()
    {
        if(VerificationStatus!=RelationVerificationStatus.PENDING)throw new InvalidOperationException("Verification decision is immutable.");
        VerificationStatus=RelationVerificationStatus.LEGACY;
    }
    private void Decide(RelationVerificationStatus status,Guid account,DateTimeOffset at,Guid? evidence)
    {
        if(VerificationStatus is not(RelationVerificationStatus.PENDING or RelationVerificationStatus.LEGACY))throw new InvalidOperationException("Verification decision is immutable.");
        if(account==Guid.Empty || evidence==Guid.Empty || at==default)throw new ArgumentException("Verifier account and time are required.");
        VerificationStatus=status;VerifiedByAccountId=account;VerifiedAt=at;EvidenceDocumentId=evidence;
    }
}

public sealed class LeaseContract : VerifiableRecord
{
    public const int ReferenceMaxLength=100;
    private LeaseContract() { }
    public LeaseContract(Guid tenantId,Guid propertyId,Guid unitId,DateOnly startDate,DateOnly? endDate=null,string? contractReference=null)
    {
        if(tenantId==Guid.Empty || propertyId==Guid.Empty || unitId==Guid.Empty)throw new ArgumentException("Lease scope is required.");
        if(startDate==DateOnly.MinValue || endDate<startDate)throw new ArgumentException("Invalid lease dates.");
        TenantId=tenantId;PropertyId=propertyId;UnitId=unitId;StartDate=startDate;EndDate=endDate;ContractReference=Reference(contractReference);
    }
    public Guid TenantId {get;private set;} public Guid PropertyId {get;private set;} public Guid UnitId {get;private set;}
    public DateOnly StartDate {get;private set;} public DateOnly? EndDate {get;private set;}
    public string? ContractReference {get;private set;}
    public bool Covers(DateOnly start,DateOnly? end)=>start>=StartDate && (EndDate==null || end!=null && end<=EndDate);
    public bool IsActiveOn(DateOnly day)=>IsEnabled && StartDate<=day && (EndDate==null || EndDate>=day);
    public void End(DateOnly date)
    {if(EndDate!=null)throw new InvalidOperationException("Ended lease is immutable.");if(date<StartDate)throw new ArgumentException("Invalid end date.");EndDate=date;}
    private static string? Reference(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return null;value=value.Trim();
        if(value.Length>ReferenceMaxLength || value.Any(char.IsControl))throw new ArgumentException("Invalid contract reference.");return value;
    }
}

public static class LeaseContractRules
{
    public static bool Overlaps(LeaseContract a,LeaseContract b)=>(a.EndDate==null || b.StartDate<=a.EndDate) && (b.EndDate==null || a.StartDate<=b.EndDate);
    // A unit has one lease per period; joint tenants are parties of that single contract.
    public static void ValidateUnit(IEnumerable<LeaseContract> source)
    {
        foreach(var unit in source.Where(c=>!c.IsDeleted).GroupBy(c=>(c.TenantId,c.PropertyId,c.UnitId)))
        {
            var rows=unit.ToArray();
            for(var i=0;i<rows.Length;i++) for(var j=i+1;j<rows.Length;j++)
                if(rows[i].Id!=rows[j].Id && Overlaps(rows[i],rows[j]))throw new InvalidOperationException("Overlapping lease contract.");
        }
    }
    public static void ValidateParties(LeaseContract contract,IEnumerable<UnitTenancy> parties)
    {
        var rows=parties.ToArray();
        foreach(var party in rows)
            if(party.LeaseContractId!=contract.Id || (party.TenantId,party.PropertyId,party.UnitId)!=(contract.TenantId,contract.PropertyId,contract.UnitId) || !contract.Covers(party.StartDate,party.EndDate))
                throw new InvalidOperationException("Lease party is outside the contract.");
        PersonUnitRules.ValidateSet(rows);
    }
}

public sealed class LeaseContractOperation : BaseEntity
{
    public static readonly string[] Actions=["CREATE","ADD_PARTY","ATTACH","END"];
    private LeaseContractOperation() { }
    public LeaseContractOperation(Guid tenant,Guid property,Guid unit,Guid contract,string action,string issuer,string subject,Guid operation,string fingerprint,long appliedVersion)
    {
        if(tenant==Guid.Empty || property==Guid.Empty || unit==Guid.Empty || contract==Guid.Empty || operation==Guid.Empty || !Actions.Contains(action) || appliedVersion<1)throw new ArgumentException("Invalid lease operation receipt.");
        TenantId=tenant;PropertyId=property;UnitId=unit;ContractId=contract;Action=action;ActorIssuer=Required(issuer,nameof(issuer),500);ActorSubject=Required(subject,nameof(subject),200);OperationId=operation;Fingerprint=fingerprint;AppliedVersion=appliedVersion;
    }
    public Guid TenantId {get;private set;} public Guid PropertyId {get;private set;} public Guid UnitId {get;private set;} public Guid ContractId {get;private set;}
    public string Action {get;private set;}=null!; public string ActorIssuer {get;private set;}=null!; public string ActorSubject {get;private set;}=null!;
    public Guid OperationId {get;private set;} public string Fingerprint {get;private set;}=null!; public long AppliedVersion {get;private set;}
}
