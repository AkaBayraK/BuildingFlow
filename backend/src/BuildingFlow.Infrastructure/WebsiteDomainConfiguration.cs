using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class WebsiteDomainConfiguration:IEntityTypeConfiguration<WebsiteDomain>{public void Configure(EntityTypeBuilder<WebsiteDomain>b){BaseEntityConfiguration.Configure(b,"website_domains");
 b.Property(x=>x.PropertyId).HasColumnName("property_id");
 b.Property(x=>x.SiteId).HasColumnName("site_id");
 b.Property(x=>x.RequestId).HasColumnName("request_id");
 b.Property(x=>x.HostName).HasColumnName("host_name").HasMaxLength(253).HasColumnType("citext");
 b.Property(x=>x.Kind).HasColumnName("kind").HasMaxLength(25);
 b.Property(x=>x.Status).HasColumnName("status").HasMaxLength(10);
 b.Property(x=>x.VerificationTokenHash).HasColumnName("verification_token_hash").HasMaxLength(64);
 b.Property(x=>x.VerifiedAt).HasColumnName("verified_at");
 b.Property(x=>x.LastCheckedAt).HasColumnName("last_checked_at");
 b.Property(x=>x.ApprovedAt).HasColumnName("approved_at");
 b.Property(x=>x.TlsCheckedAt).HasColumnName("tls_checked_at");
 b.Property(x=>x.IsPrimary).HasColumnName("is_primary");

 b.HasOne<WebsiteSite>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.SiteId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>x.HostName).IsUnique().HasFilter("is_deleted=false");b.HasIndex(x=>new{x.SiteId,x.RequestId}).IsUnique();b.HasIndex(x=>x.SiteId).IsUnique().HasFilter("is_deleted=false AND is_primary=true");b.ToTable(t=>t.HasCheckConstraint("ck_website_domain_state","status IN ('PENDING','VERIFIED','DISABLED') AND kind IN ('CUSTOM','PLATFORM_SUBDOMAIN') AND verification_token_hash ~ '^[0-9a-f]{64}$' AND (NOT is_primary OR status='VERIFIED') AND (status<>'VERIFIED' OR (verified_at IS NOT NULL AND tls_checked_at IS NOT NULL AND last_checked_at IS NOT NULL AND (kind<>'CUSTOM' OR approved_at IS NOT NULL)))"));}}
