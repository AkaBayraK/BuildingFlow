using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class WebsiteMediaConfiguration : IEntityTypeConfiguration<WebsiteMedia>
{
 public void Configure(EntityTypeBuilder<WebsiteMedia> b){
  BaseEntityConfiguration.Configure(b,"website_media");
  b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.SiteId).HasColumnName("site_id");b.Property(x=>x.RequestId).HasColumnName("request_id");
  b.Property(x=>x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);b.Property(x=>x.ObjectKey).HasColumnName("object_key").HasMaxLength(160);b.Property(x=>x.ContentType).HasColumnName("content_type").HasMaxLength(20);
  b.Property(x=>x.Size).HasColumnName("size");b.Property(x=>x.Width).HasColumnName("width");b.Property(x=>x.Height).HasColumnName("height");b.Property(x=>x.Sha256).HasColumnName("sha256").HasMaxLength(64);
  b.Property(x=>x.AltText).HasColumnName("alt_text").HasMaxLength(200);b.Property(x=>x.RightsBasis).HasColumnName("rights_basis").HasMaxLength(20);b.Property(x=>x.RightsNote).HasColumnName("rights_note").HasMaxLength(500);b.Property(x=>x.ContainsPeople).HasColumnName("contains_people");b.Property(x=>x.PeopleConsentConfirmed).HasColumnName("people_consent_confirmed");
  b.HasOne<WebsiteSite>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.SiteId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasAlternateKey(x=>new{x.PropertyId,x.SiteId,x.Id});b.HasIndex(x=>new{x.SiteId,x.RequestId}).IsUnique();
  b.ToTable(t=>t.HasCheckConstraint("ck_website_media_values","content_type IN ('image/jpeg','image/png') AND size BETWEEN 1 AND 5242880 AND width BETWEEN 1 AND 4096 AND height BETWEEN 1 AND 4096 AND sha256 ~ '^[0-9a-f]{64}$' AND fingerprint ~ '^[0-9a-f]{64}$' AND length(trim(alt_text))>0 AND rights_basis IN ('OWN_WORK','LICENSED','PERMISSION') AND (rights_basis='OWN_WORK' OR length(trim(rights_note))>0 AND rights_note IS NOT NULL) AND (NOT contains_people OR people_consent_confirmed)"));
 }
}
