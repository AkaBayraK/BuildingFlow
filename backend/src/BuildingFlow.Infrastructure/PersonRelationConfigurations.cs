using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal static class VerificationConfiguration
{
    internal static void Configure<T>(EntityTypeBuilder<T> b,string table) where T:VerifiableRecord
    {
        b.Property(x=>x.VerificationStatus).HasColumnName("verification_status").HasConversion<string>().HasMaxLength(20);
        b.Property(x=>x.VerifiedByAccountId).HasColumnName("verified_by_account_id");b.Property(x=>x.VerifiedAt).HasColumnName("verified_at");b.Property(x=>x.EvidenceDocumentId).HasColumnName("evidence_document_id");
        b.HasOne<PersonUserAccount>().WithMany().HasForeignKey(x=>x.VerifiedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<StoredDocument>().WithMany().HasForeignKey("PropertyId","EvidenceDocumentId").HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t=>t.HasCheckConstraint("ck_"+table+"_verification","(verification_status IN ('LEGACY','PENDING') AND verified_by_account_id IS NULL AND verified_at IS NULL) OR (verification_status IN ('VERIFIED','REJECTED') AND verified_by_account_id IS NOT NULL AND verified_at IS NOT NULL)"));
    }
}
internal static class PersonRelationConfiguration
{
    internal static void Configure<T>(EntityTypeBuilder<T> b,string table) where T:VerifiableRecord,IPersonUnitRelation
    {
        BaseEntityConfiguration.Configure(b,table);VerificationConfiguration.Configure(b,table);
        b.Ignore(x=>x.Kind);
        foreach(var name in new[]{"TenantId","PropertyId","UnitId","PersonId"})b.Property<Guid>(name).HasColumnName(name switch{"TenantId"=>"tenant_id","PropertyId"=>"property_id","UnitId"=>"unit_id",_=>"person_id"});
        b.Property(x=>x.StartDate).HasColumnName("start_date");b.Property(x=>x.EndDate).HasColumnName("end_date");b.Property(x=>x.IsPrimaryContact).HasColumnName("is_primary_contact");
        b.HasAlternateKey("TenantId","PropertyId","UnitId","Id");b.HasAlternateKey("TenantId","PropertyId","PersonId","Id");
        b.HasOne<PersonPropertyScope>().WithMany().HasForeignKey("TenantId","PropertyId").HasPrincipalKey(x=>new{x.TenantId,x.PropertyId}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CentralPerson>().WithMany().HasForeignKey("TenantId","PersonId").HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Unit>().WithMany().HasForeignKey("PropertyId","UnitId").HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex("TenantId","PropertyId","UnitId","PersonId").IsUnique().HasFilter("is_deleted=false AND end_date IS NULL");
        b.ToTable(t=>t.HasCheckConstraint("ck_"+table+"_dates","start_date > DATE '0001-01-01' AND (end_date IS NULL OR end_date>=start_date)"));
    }
}
internal sealed class UnitOwnershipConfiguration:IEntityTypeConfiguration<UnitOwnership>
{
    public void Configure(EntityTypeBuilder<UnitOwnership> b){PersonRelationConfiguration.Configure(b,"unit_ownerships");b.Property(x=>x.ShareNumerator).HasColumnName("share_numerator");b.Property(x=>x.ShareDenominator).HasColumnName("share_denominator");b.ToTable(t=>t.HasCheckConstraint("ck_ownership_share","(share_numerator IS NULL AND share_denominator IS NULL) OR (share_numerator IS NOT NULL AND share_denominator IS NOT NULL AND share_numerator>0 AND share_denominator>0 AND share_numerator<=share_denominator)"));}
}
internal sealed class UnitTenancyConfiguration:IEntityTypeConfiguration<UnitTenancy>
{
    public void Configure(EntityTypeBuilder<UnitTenancy> b)
    {
        PersonRelationConfiguration.Configure(b,"unit_tenancies");b.Ignore(x=>x.ShareNumerator);b.Ignore(x=>x.ShareDenominator);b.Property(x=>x.LeaseContractId).HasColumnName("lease_contract_id");
        b.HasOne<LeaseContract>().WithMany().HasForeignKey("TenantId","PropertyId","UnitId","LeaseContractId").HasPrincipalKey(x=>new{x.TenantId,x.PropertyId,x.UnitId,x.Id}).OnDelete(DeleteBehavior.Restrict);
    }
}
internal sealed class LeaseContractConfiguration:IEntityTypeConfiguration<LeaseContract>
{
    public void Configure(EntityTypeBuilder<LeaseContract> b)
    {
        BaseEntityConfiguration.Configure(b,"lease_contracts");VerificationConfiguration.Configure(b,"lease_contracts");
        b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.UnitId).HasColumnName("unit_id");
        b.Property(x=>x.StartDate).HasColumnName("start_date");b.Property(x=>x.EndDate).HasColumnName("end_date");b.Property(x=>x.ContractReference).HasColumnName("contract_reference").HasMaxLength(LeaseContract.ReferenceMaxLength);
        b.HasOne<PersonPropertyScope>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Unit>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.UnitId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.TenantId,x.PropertyId,x.UnitId,x.StartDate});
        b.ToTable(t=>{t.HasCheckConstraint("ck_lease_contracts_dates","start_date > DATE '0001-01-01' AND (end_date IS NULL OR end_date>=start_date)");t.HasCheckConstraint("ck_lease_contracts_reference","contract_reference IS NULL OR length(trim(contract_reference)) > 0");});
    }
}
internal sealed class UnitResidencyConfiguration:IEntityTypeConfiguration<UnitResidency>
{
    public void Configure(EntityTypeBuilder<UnitResidency> b){PersonRelationConfiguration.Configure(b,"unit_residencies");b.Ignore(x=>x.ShareNumerator);b.Ignore(x=>x.ShareDenominator);}
}
internal sealed class PersonPropertyScopeConfiguration:IEntityTypeConfiguration<PersonPropertyScope>
{
    public void Configure(EntityTypeBuilder<PersonPropertyScope> b){BaseEntityConfiguration.Configure(b,"person_property_scopes");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.PropertyId).HasColumnName("property_id");b.HasAlternateKey(x=>new{x.TenantId,x.PropertyId});b.HasIndex(x=>x.PropertyId).IsUnique();b.HasOne<PersonTenant>().WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);b.HasOne<Property>().WithMany().HasForeignKey(x=>x.PropertyId).OnDelete(DeleteBehavior.Restrict);}
}
internal sealed class LegacyPersonMapConfiguration:IEntityTypeConfiguration<LegacyPersonMap>
{
    public void Configure(EntityTypeBuilder<LegacyPersonMap> b){BaseEntityConfiguration.Configure(b,"legacy_person_maps");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.PersonId).HasColumnName("person_id");b.Property(x=>x.LegacyPersonId).HasColumnName("legacy_person_id");b.Property(x=>x.SourceVersion).HasColumnName("source_version");b.Property(x=>x.LocalProfile).HasColumnName("local_profile").HasColumnType("jsonb");b.HasIndex(x=>x.LegacyPersonId).IsUnique();b.HasIndex(x=>x.PersonId).IsUnique();b.HasOne<PersonPropertyScope>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId}).OnDelete(DeleteBehavior.Restrict);b.HasOne<CentralPerson>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PersonId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.HasOne<Person>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.LegacyPersonId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.ToTable(t=>t.HasCheckConstraint("ck_legacy_person_version","source_version>0"));}
}
internal sealed class LegacyOccupancyMapConfiguration:IEntityTypeConfiguration<LegacyOccupancyMap>
{
    public void Configure(EntityTypeBuilder<LegacyOccupancyMap> b){BaseEntityConfiguration.Configure(b,"legacy_occupancy_maps");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.UnitId).HasColumnName("unit_id");b.Property(x=>x.LegacyOccupancyId).HasColumnName("legacy_occupancy_id");b.Property(x=>x.SourceVersion).HasColumnName("source_version");b.Property(x=>x.OwnershipId).HasColumnName("ownership_id");b.Property(x=>x.TenancyId).HasColumnName("tenancy_id");b.Property(x=>x.ResidencyId).HasColumnName("residency_id");b.HasIndex(x=>x.LegacyOccupancyId).IsUnique();b.HasOne<UnitOccupancy>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.UnitId,x.LegacyOccupancyId}).HasPrincipalKey(x=>new{x.PropertyId,x.UnitId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UnitOwnership>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId,x.UnitId,x.OwnershipId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId,x.UnitId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UnitTenancy>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId,x.UnitId,x.TenancyId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId,x.UnitId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<UnitResidency>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PropertyId,x.UnitId,x.ResidencyId}).HasPrincipalKey(x=>new{x.TenantId,x.PropertyId,x.UnitId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t=>t.HasCheckConstraint("ck_legacy_occupancy_target","source_version>0 AND num_nonnulls(ownership_id,tenancy_id,residency_id)=1"));}
}
