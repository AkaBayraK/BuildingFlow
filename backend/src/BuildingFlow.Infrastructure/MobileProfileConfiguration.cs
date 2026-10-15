using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class MobileProfileConfiguration:IEntityTypeConfiguration<MobileProfile> {
 public void Configure(EntityTypeBuilder<MobileProfile> b) {
  BaseEntityConfiguration.Configure(b,"mobile_profiles");
  b.Property(x=>x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);
  b.Property(x=>x.FirstName).HasColumnName("first_name").HasMaxLength(80);
  b.Property(x=>x.LastName).HasColumnName("last_name").HasMaxLength(80);
  b.Property(x=>x.Phone).HasColumnName("phone").HasMaxLength(32);
  b.Property(x=>x.PhotoId).HasColumnName("photo_id");
  b.HasIndex(x=>x.OwnerUserId).IsUnique();
  b.HasOne<MobileProfilePhoto>().WithMany().HasForeignKey(x=>new{x.OwnerUserId,x.PhotoId}).HasPrincipalKey(x=>new{x.OwnerUserId,x.Id}).OnDelete(DeleteBehavior.Restrict);
 }
}
internal sealed class MobileProfilePhotoConfiguration:IEntityTypeConfiguration<MobileProfilePhoto> {
 public void Configure(EntityTypeBuilder<MobileProfilePhoto> b) {
  BaseEntityConfiguration.Configure(b,"mobile_profile_photos");
  b.Property(x=>x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);
  b.Property(x=>x.OperationId).HasColumnName("operation_id");
  b.Property(x=>x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
  b.Property(x=>x.ObjectKey).HasColumnName("object_key").HasMaxLength(250);
  b.Property(x=>x.Sha256).HasColumnName("sha256").HasMaxLength(64);
  b.Property(x=>x.Size).HasColumnName("size");
  b.Property(x=>x.State).HasColumnName("state").HasMaxLength(10);
  b.HasIndex(x=>new{x.OwnerUserId,x.OperationId}).IsUnique();
  b.HasIndex(x=>new{x.State,x.CreatedAt});
  b.ToTable(t=>{t.HasCheckConstraint("ck_profile_photo_state","state IN ('STAGED','CURRENT','RETIRED','PURGED')");t.HasCheckConstraint("ck_profile_photo_size","size BETWEEN 1 AND 262144");});
 }
}
