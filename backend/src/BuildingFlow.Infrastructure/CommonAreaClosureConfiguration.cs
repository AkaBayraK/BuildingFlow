using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class CommonAreaClosureConfiguration:IEntityTypeConfiguration<CommonAreaClosure>
{
    public void Configure(EntityTypeBuilder<CommonAreaClosure> b)
    {
        BaseEntityConfiguration.Configure(b,"common_area_closures");
        b.Property(x=>x.PropertyId).HasColumnName("property_id");b.Property(x=>x.CommonAreaId).HasColumnName("common_area_id");b.Property(x=>x.MaintenancePlanId).HasColumnName("maintenance_plan_id");
        b.Property(x=>x.RequestId).HasColumnName("request_id");b.Property(x=>x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x=>x.Date).HasColumnName("date");b.Property(x=>x.StartsAt).HasColumnName("starts_at");b.Property(x=>x.EndsAt).HasColumnName("ends_at");
        b.Property(x=>x.Notice).HasColumnName("notice").HasMaxLength(1000);b.Property(x=>x.AnnouncementId).HasColumnName("announcement_id");
        b.Property(x=>x.ReleaseAnnouncementId).HasColumnName("release_announcement_id");b.Property(x=>x.ReleasedAt).HasColumnName("released_at");b.Property(x=>x.ReleaseReason).HasColumnName("release_reason").HasMaxLength(500);
        b.Property(x=>x.NotificationCount).HasColumnName("notification_count");
        b.HasOne<CommonArea>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.CommonAreaId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MaintenancePlan>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.MaintenancePlanId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Announcement>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.AnnouncementId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Announcement>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.ReleaseAnnouncementId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.PropertyId,x.RequestId}).IsUnique();
        b.HasIndex(x=>new{x.PropertyId,x.CommonAreaId,x.MaintenancePlanId}).IsUnique().HasFilter("released_at IS NULL AND is_deleted = false");
        b.HasIndex(x=>new{x.PropertyId,x.CommonAreaId,x.Date});
        b.ToTable(t=>t.HasCheckConstraint("ck_common_area_closure_valid","starts_at < ends_at AND EXTRACT(SECOND FROM starts_at)=0 AND EXTRACT(SECOND FROM ends_at)=0 AND EXTRACT(MINUTE FROM starts_at)::int % 15=0 AND EXTRACT(MINUTE FROM ends_at)::int % 15=0 AND length(trim(notice))>0 AND notification_count>=0 AND ((released_at IS NULL AND release_reason IS NULL AND release_announcement_id IS NULL) OR (released_at IS NOT NULL AND release_reason IS NOT NULL AND length(trim(release_reason))>0 AND release_announcement_id IS NOT NULL))"));
    }
}
