using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class PropertyListingConfiguration:IEntityTypeConfiguration<PropertyListing>{public void Configure(EntityTypeBuilder<PropertyListing>b){
 BaseEntityConfiguration.Configure(b,"property_listings");
 b.Property(x=>x.PropertyId).HasColumnName("property_id");
 b.Property(x=>x.SiteId).HasColumnName("site_id");
 b.Property(x=>x.UnitId).HasColumnName("unit_id");
 b.Property(x=>x.RequestId).HasColumnName("request_id");
 b.Property(x=>x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
 b.Property(x=>x.Kind).HasColumnName("kind").HasMaxLength(10);
 b.Property(x=>x.Title).HasColumnName("title").HasMaxLength(120);
 b.Property(x=>x.Description).HasColumnName("description").HasMaxLength(4000);
 b.Property(x=>x.Price).HasColumnName("price").HasPrecision(14,2);
 b.Property(x=>x.RoomLayout).HasColumnName("room_layout").HasMaxLength(10);
 b.Property(x=>x.GrossArea).HasColumnName("gross_area").HasPrecision(8,2);
 b.Property(x=>x.NetArea).HasColumnName("net_area").HasPrecision(8,2);
 b.Property(x=>x.FloorLabel).HasColumnName("floor_label").HasMaxLength(40);
 b.Property(x=>x.BuildingLabel).HasColumnName("building_label").HasMaxLength(60);
 b.Property(x=>x.Features).HasColumnName("features").HasColumnType("jsonb");
 b.Property(x=>x.Status).HasColumnName("status").HasMaxLength(20);
 b.Property(x=>x.PublishUntil).HasColumnName("publish_until");
 b.Property(x=>x.ConsentKind).HasColumnName("consent_kind").HasMaxLength(30);
 b.Property(x=>x.ConsentObjectKey).HasColumnName("consent_object_key").HasMaxLength(160);
 b.Property(x=>x.ConsentSha256).HasColumnName("consent_sha256").HasMaxLength(64);
 b.Property(x=>x.ConsentContentType).HasColumnName("consent_content_type").HasMaxLength(30);
 b.Property(x=>x.ConsentSize).HasColumnName("consent_size");
 b.Property(x=>x.AgentLicenseNo).HasColumnName("agent_license_no").HasMaxLength(60);
 b.Property(x=>x.ComplianceNote).HasColumnName("compliance_note").HasMaxLength(500);
 b.Property(x=>x.VerificationReference).HasColumnName("verification_reference").HasMaxLength(200);

 b.HasOne<WebsiteSite>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.SiteId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
 b.HasOne<Unit>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.UnitId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
 b.HasAlternateKey(x=>new{x.PropertyId,x.SiteId,x.Id});b.HasAlternateKey(x=>new{x.PropertyId,x.Id});b.HasIndex(x=>new{x.SiteId,x.RequestId}).IsUnique();
 b.ToTable(t=>t.HasCheckConstraint("ck_property_listing_values","kind IN ('SALE','RENT') AND price>0 AND gross_area>0 AND net_area>0 AND net_area<=gross_area AND room_layout ~ '^[0-9]{1,2}\\+[0-9]$' AND status IN ('DRAFT','PENDING_APPROVAL','APPROVED','PUBLISHED','WITHDRAWN','EXPIRED') AND (status <> 'PUBLISHED' OR (publish_until IS NOT NULL AND verification_reference IS NOT NULL AND consent_object_key IS NOT NULL AND compliance_note IS NOT NULL))"));
}}
internal sealed class ListingPhotoConfiguration:IEntityTypeConfiguration<ListingPhoto>{public void Configure(EntityTypeBuilder<ListingPhoto>b){BaseEntityConfiguration.Configure(b,"listing_photos");b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.SiteId).HasColumnName("site_id");b.Property(x=>x.ListingId).HasColumnName("listing_id");b.Property(x=>x.MediaId).HasColumnName("media_id");b.Property(x=>x.SortOrder).HasColumnName("sort_order");b.HasOne<PropertyListing>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.SiteId,x.ListingId}).HasPrincipalKey(x=>new{x.PropertyId,x.SiteId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.HasOne<WebsiteMedia>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.SiteId,x.MediaId}).HasPrincipalKey(x=>new{x.PropertyId,x.SiteId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>new{x.ListingId,x.MediaId}).IsUnique().HasFilter("is_deleted=false");b.HasIndex(x=>new{x.ListingId,x.SortOrder}).IsUnique().HasFilter("is_deleted=false");b.ToTable(t=>t.HasCheckConstraint("ck_listing_photo_order","sort_order BETWEEN 0 AND 19"));}}
internal sealed class ListingEventConfiguration:IEntityTypeConfiguration<ListingEvent>{public void Configure(EntityTypeBuilder<ListingEvent>b){BaseEntityConfiguration.Configure(b,"listing_events");b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.ListingId).HasColumnName("listing_id");b.Property(x=>x.Action).HasColumnName("action").HasMaxLength(30);b.Property(x=>x.Note).HasColumnName("note").HasMaxLength(500);b.Property(x=>x.ListingVersion).HasColumnName("listing_version");b.HasOne<PropertyListing>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.ListingId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);}}
