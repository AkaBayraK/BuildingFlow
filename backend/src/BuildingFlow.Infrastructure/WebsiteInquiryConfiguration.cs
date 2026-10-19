using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class WebsiteInquiryConfiguration:IEntityTypeConfiguration<WebsiteInquiry>{public void Configure(EntityTypeBuilder<WebsiteInquiry>b){BaseEntityConfiguration.Configure(b,"website_inquiries");
 b.Property(x=>x.PropertyId).HasColumnName("property_id");
 b.Property(x=>x.SiteId).HasColumnName("site_id");
 b.Property(x=>x.ListingId).HasColumnName("listing_id");
 b.Property(x=>x.FullName).HasColumnName("full_name").HasMaxLength(100);
 b.Property(x=>x.Phone).HasColumnName("phone").HasMaxLength(40);
 b.Property(x=>x.Email).HasColumnName("email").HasMaxLength(200);
 b.Property(x=>x.Message).HasColumnName("message").HasMaxLength(2000);
 b.Property(x=>x.PrivacyNoticeVersion).HasColumnName("privacy_notice_version").HasMaxLength(64);
 b.Property(x=>x.IpHash).HasColumnName("ip_hash").HasMaxLength(64);
 b.Property(x=>x.RetainUntil).HasColumnName("retain_until");
 b.Property(x=>x.Status).HasColumnName("status").HasMaxLength(10);

 b.HasOne<WebsiteSite>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.SiteId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.HasOne<PropertyListing>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.SiteId,x.ListingId}).HasPrincipalKey(x=>new{x.PropertyId,x.SiteId,x.Id}).OnDelete(DeleteBehavior.Restrict);b.HasIndex(x=>new{x.SiteId,x.CreatedAt});b.HasIndex(x=>new{x.IpHash,x.CreatedAt});b.ToTable(t=>t.HasCheckConstraint("ck_website_inquiry_status","status IN ('NEW','HANDLED','SPAM')"));}}
