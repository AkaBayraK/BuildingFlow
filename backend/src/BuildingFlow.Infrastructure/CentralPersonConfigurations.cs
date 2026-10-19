using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class PersonTenantConfiguration:IEntityTypeConfiguration<PersonTenant> {
 public void Configure(EntityTypeBuilder<PersonTenant> b){BaseEntityConfiguration.Configure(b,"person_tenants");b.Property(x=>x.Name).HasColumnName("name").HasMaxLength(200);}
}
internal sealed class PersonUserAccountConfiguration:IEntityTypeConfiguration<PersonUserAccount> {
 public void Configure(EntityTypeBuilder<PersonUserAccount> b){BaseEntityConfiguration.Configure(b,"person_user_accounts");b.Property(x=>x.Issuer).HasColumnName("issuer").HasMaxLength(500);b.Property(x=>x.Subject).HasColumnName("subject").HasMaxLength(200);b.HasIndex(x=>new{x.Issuer,x.Subject}).IsUnique();}
}
internal sealed class PersonTenantMembershipConfiguration:IEntityTypeConfiguration<PersonTenantMembership> {
 public void Configure(EntityTypeBuilder<PersonTenantMembership> b){BaseEntityConfiguration.Configure(b,"person_tenant_memberships");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.UserAccountId).HasColumnName("user_account_id");b.HasAlternateKey(x=>new{x.TenantId,x.UserAccountId});b.HasOne<PersonTenant>().WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);b.HasOne<PersonUserAccount>().WithMany().HasForeignKey(x=>x.UserAccountId).OnDelete(DeleteBehavior.Restrict);}
}
internal sealed class CentralPersonConfiguration:IEntityTypeConfiguration<CentralPerson> {
 public void Configure(EntityTypeBuilder<CentralPerson> b){
  BaseEntityConfiguration.Configure(b,"persons");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.HasAlternateKey(x=>new{x.TenantId,x.Id});b.HasOne<PersonTenant>().WithMany().HasForeignKey(x=>x.TenantId).OnDelete(DeleteBehavior.Restrict);
  b.Property(x=>x.Kind).HasColumnName("kind").HasMaxLength(10);b.Property(x=>x.Citizenship).HasColumnName("citizenship").HasMaxLength(10);b.Property(x=>x.CountryCode).HasColumnName("country_code").HasMaxLength(2);b.Property(x=>x.IdentityNumber).HasColumnName("identity_number").HasMaxLength(50);
  b.Property(x=>x.FirstName).HasColumnName("first_name").HasMaxLength(100);b.Property(x=>x.LastName).HasColumnName("last_name").HasMaxLength(100);b.Property(x=>x.BirthDate).HasColumnName("birth_date");b.Property(x=>x.Gender).HasColumnName("gender").HasMaxLength(20);b.Property(x=>x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(30);b.Property(x=>x.Email).HasColumnName("email").HasMaxLength(254);
  b.HasIndex(x=>new{x.TenantId,x.LastName,x.FirstName});
  b.ToTable(t=>t.HasCheckConstraint("ck_central_person_profile","length(btrim(first_name)) BETWEEN 1 AND 100 AND (kind='LEGAL' OR length(btrim(last_name)) BETWEEN 1 AND 100) AND kind IN ('NATURAL','LEGAL') AND citizenship IN ('DOMESTIC','FOREIGN') AND country_code ~ '^[A-Z]{2}$' AND length(btrim(identity_number)) BETWEEN 1 AND 50 AND ((citizenship='DOMESTIC' AND country_code='TR' AND identity_number ~ CASE WHEN kind='NATURAL' THEN '^[1-9][0-9]{10}$' ELSE '^[0-9]{10}$' END) OR (citizenship='FOREIGN' AND country_code<>'TR' AND (kind='LEGAL' OR identity_number ~ '^[A-Za-z0-9-]{6,30}$'))) AND (kind='NATURAL' OR (birth_date IS NULL AND gender IS NULL)) AND (gender IS NULL OR gender IN ('FEMALE','MALE','OTHER','UNSPECIFIED')) AND (birth_date IS NULL OR birth_date>=DATE '1900-01-01')"));
 }
}
internal sealed class CentralPersonAccountLinkConfiguration:IEntityTypeConfiguration<CentralPersonAccountLink> {
 public void Configure(EntityTypeBuilder<CentralPersonAccountLink> b){BaseEntityConfiguration.Configure(b,"person_account_links");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.UserAccountId).HasColumnName("user_account_id");b.Property(x=>x.PersonId).HasColumnName("person_id");b.Property(x=>x.LinkedAt).HasColumnName("linked_at");b.Property(x=>x.EndedAt).HasColumnName("ended_at");
  b.HasOne<CentralPerson>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PersonId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<PersonTenantMembership>().WithMany().HasForeignKey(x=>new{x.TenantId,x.UserAccountId}).HasPrincipalKey(x=>new{x.TenantId,x.UserAccountId}).OnDelete(DeleteBehavior.Restrict);
  b.HasIndex(x=>new{x.TenantId,x.UserAccountId}).IsUnique().HasFilter("is_deleted=false AND is_enabled=true AND ended_at IS NULL");b.HasIndex(x=>new{x.TenantId,x.PersonId}).IsUnique().HasFilter("is_deleted=false AND is_enabled=true AND ended_at IS NULL");b.ToTable(t=>t.HasCheckConstraint("ck_person_link_dates","ended_at IS NULL OR ended_at>=linked_at"));
 }
}
internal sealed class CentralPersonOperationConfiguration:IEntityTypeConfiguration<CentralPersonOperation> {
 public void Configure(EntityTypeBuilder<CentralPersonOperation> b){BaseEntityConfiguration.Configure(b,"person_operation_receipts");b.Property(x=>x.TenantId).HasColumnName("tenant_id");b.Property(x=>x.ActorIssuer).HasColumnName("actor_issuer").HasMaxLength(500);b.Property(x=>x.ActorSubject).HasColumnName("actor_subject").HasMaxLength(200);b.Property(x=>x.OperationId).HasColumnName("operation_id");b.Property(x=>x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);b.Property(x=>x.PersonId).HasColumnName("person_id");b.Property(x=>x.AppliedVersion).HasColumnName("applied_version");b.HasIndex(x=>new{x.TenantId,x.ActorIssuer,x.ActorSubject,x.OperationId}).IsUnique();b.HasOne<CentralPerson>().WithMany().HasForeignKey(x=>new{x.TenantId,x.PersonId}).HasPrincipalKey(x=>new{x.TenantId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.ToTable(t=>t.HasCheckConstraint("ck_person_receipt_version","applied_version>0"));}
}
