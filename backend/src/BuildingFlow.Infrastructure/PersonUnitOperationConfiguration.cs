using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class PersonUnitOperationConfiguration:IEntityTypeConfiguration<PersonUnitOperation>
{
    public void Configure(EntityTypeBuilder<PersonUnitOperation> b)
    {
        BaseEntityConfiguration.Configure(b,"person_unit_operations");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.PersonId).HasColumnName("person_id");b.Ignore(x=>x.RelationId);b.Property(x=>x.OwnershipId).HasColumnName("ownership_id");b.Property(x=>x.TenancyId).HasColumnName("tenancy_id");b.Property(x=>x.ResidencyId).HasColumnName("residency_id");b.Property(x=>x.Kind).HasColumnName("kind").HasMaxLength(20);b.Property(x=>x.ActorIssuer).HasColumnName("actor_issuer").HasMaxLength(500);b.Property(x=>x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(200);b.Property(x=>x.OperationId).HasColumnName("operation_id");b.Property(x=>x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);b.Property(x=>x.AppliedVersion).HasColumnName("applied_version");
        b.HasIndex(x=>new{x.TenantId,x.ActorIssuer,x.ActorSubject,x.OperationId}).IsUnique();b.HasOne<CentralPerson>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PersonId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.HasOne<PersonPropertyScope>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UnitOwnership>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId,x.PersonId,x.OwnershipId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId,x.PersonId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UnitTenancy>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId,x.PersonId,x.TenancyId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId,x.PersonId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UnitResidency>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId,x.PersonId,x.ResidencyId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId,x.PersonId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t=>t.HasCheckConstraint("ck_person_unit_operation","((kind='OWNER' AND ownership_id IS NOT NULL AND tenancy_id IS NULL AND residency_id IS NULL) OR (kind='TENANT' AND tenancy_id IS NOT NULL AND ownership_id IS NULL AND residency_id IS NULL) OR (kind='RESIDENT' AND residency_id IS NOT NULL AND ownership_id IS NULL AND tenancy_id IS NULL)) AND applied_version>0 AND length(fingerprint)=64"));
    }
}
internal sealed class LeaseContractOperationConfiguration:IEntityTypeConfiguration<LeaseContractOperation>
{
    public void Configure(EntityTypeBuilder<LeaseContractOperation> b)
    {
        BaseEntityConfiguration.Configure(b,"lease_contract_operations");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.UnitId).HasColumnName("unit_id");b.Property(x=>x.ContractId).HasColumnName("contract_id");b.Property(x=>x.Action).HasColumnName("action").HasMaxLength(20);b.Property(x=>x.ActorIssuer).HasColumnName("actor_issuer").HasMaxLength(500);b.Property(x=>x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(200);b.Property(x=>x.OperationId).HasColumnName("operation_id");b.Property(x=>x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);b.Property(x=>x.AppliedVersion).HasColumnName("applied_version");
        b.HasIndex(x=>new{x.TenantId,x.ActorIssuer,x.ActorSubject,x.OperationId}).IsUnique();
        b.HasOne<LeaseContract>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId,x.UnitId,x.ContractId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId,x.UnitId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t=>t.HasCheckConstraint("ck_lease_contract_operation","action IN ('CREATE','ADD_PARTY','ATTACH','END') AND applied_version>0 AND length(fingerprint)=64"));
    }
}
