using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class MobileAnnouncementReadConfiguration:IEntityTypeConfiguration<MobileAnnouncementRead>{
 public void Configure(EntityTypeBuilder<MobileAnnouncementRead> b){
  BaseEntityConfiguration.Configure(b,"mobile_announcement_reads");
  b.Property(x=>x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);
  b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.AnnouncementId).HasColumnName("announcement_id");b.Property(x=>x.ReadAt).HasColumnName("read_at");
  b.HasOne<Announcement>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.AnnouncementId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasIndex(x=>new{x.OwnerUserId,x.AnnouncementId}).IsUnique();
 }
}
internal sealed class MobileNoticePreferenceConfiguration:IEntityTypeConfiguration<MobileNoticePreference>{
 public void Configure(EntityTypeBuilder<MobileNoticePreference> b){BaseEntityConfiguration.Configure(b,"mobile_notice_preferences");b.Property(x=>x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);b.Property(x=>x.HighlightsEnabled).HasColumnName("highlights_enabled");b.HasIndex(x=>x.OwnerUserId).IsUnique();}
}
