namespace BuildingFlow.Domain;
public sealed class PersonUnitOperation:BaseEntity
{
    private PersonUnitOperation() { }
    public PersonUnitOperation(Guid tenant,Guid property,Guid person,Guid relation,string kind,string issuer,string subject,Guid operation,string fingerprint,long appliedVersion)
    {TenantId=tenant;PropertyId=property;PersonId=person;OwnershipId=kind=="OWNER"?relation:null;TenancyId=kind=="TENANT"?relation:null;ResidencyId=kind=="RESIDENT"?relation:null;Kind=kind;ActorIssuer=issuer;ActorSubject=subject;OperationId=operation;Fingerprint=fingerprint;AppliedVersion=appliedVersion;}
    public Guid TenantId {get;private set;}public Guid PropertyId {get;private set;}public Guid PersonId {get;private set;}public Guid RelationId=>OwnershipId??TenancyId??ResidencyId!.Value;public Guid? OwnershipId {get;private set;}public Guid? TenancyId {get;private set;}public Guid? ResidencyId {get;private set;}
    public string Kind {get;private set;}=null!;public string ActorIssuer {get;private set;}=null!;public string ActorSubject {get;private set;}=null!;
    public Guid OperationId {get;private set;}public string Fingerprint {get;private set;}=null!;public long AppliedVersion {get;private set;}
}
