using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class MobileRequestConfiguration : IEntityTypeConfiguration<MobileRequest> {
    public void Configure(EntityTypeBuilder<MobileRequest> b) {
        BaseEntityConfiguration.Configure(b,"mobile_requests");
        b.Property(x=>x.PropertyId).HasColumnName("property_id");
        b.Property(x=>x.UnitId).HasColumnName("unit_id");
        b.Property(x=>x.ContextRole).HasColumnName("context_role").HasMaxLength(20);
        b.Property(x=>x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);
        b.Property(x=>x.Category).HasColumnName("category").HasMaxLength(20).HasDefaultValue("OTHER");
        b.Property(x=>x.Priority).HasColumnName("priority").HasMaxLength(10).HasDefaultValue("NORMAL");
        b.Property(x=>x.ManagementStatus).HasColumnName("management_status").HasMaxLength(20).HasDefaultValue("RECORDED");
        b.Property(x=>x.ManagementResponse).HasColumnName("management_response").HasMaxLength(2000);
        b.Property(x=>x.HasManagementHistory).HasColumnName("has_management_history").HasDefaultValue(false);
        b.HasIndex(x=>new{x.PropertyId,x.ManagementStatus,x.ModifiedAt});
        b.ToTable(t=>t.HasCheckConstraint("ck_request_management_status","management_status IN ('RECORDED','IN_PROGRESS','RESOLVED','REJECTED')"));
        b.Property(x=>x.PhotoId).HasColumnName("photo_id");
        b.HasOne<MobileRequestPhoto>().WithMany().HasForeignKey(x=>new{x.OwnerUserId,x.PropertyId,x.PhotoId}).HasPrincipalKey(x=>new{x.OwnerUserId,x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t=>{t.HasCheckConstraint("ck_mobile_request_category","category IN ("+string.Join(",",MobileRequest.Categories.Select(c=>"'"+c+"'"))+")");t.HasCheckConstraint("ck_mobile_request_priority","priority IN ('LOW','NORMAL','HIGH')");});
        b.Property(x=>x.Description).HasColumnName("description").HasMaxLength(2000);
        b.HasOne<Property>().WithMany().HasForeignKey(x=>x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Unit>().WithMany().HasForeignKey(x=>new {x.PropertyId,x.UnitId}).HasPrincipalKey(x=>new {x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new {x.OwnerUserId,x.PropertyId,x.UnitId,x.ContextRole});
        b.ToTable(t=>t.HasCheckConstraint("ck_mobile_requests_description","length(trim(description)) BETWEEN 1 AND 2000"));
    }
}
internal sealed class MobileOperationReceiptConfiguration : IEntityTypeConfiguration<MobileOperationReceipt> {
    public void Configure(EntityTypeBuilder<MobileOperationReceipt> b) {
        BaseEntityConfiguration.Configure(b,"mobile_operation_receipts");
        b.Property(x=>x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);
        b.Property(x=>x.OperationId).HasColumnName("operation_id");
        b.Property(x=>x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x=>x.Result).HasColumnName("result");
        b.HasIndex(x=>new {x.OwnerUserId,x.OperationId}).IsUnique();
    }
}

internal sealed class MobileRequestPhotoConfiguration:IEntityTypeConfiguration<MobileRequestPhoto>{
 public void Configure(EntityTypeBuilder<MobileRequestPhoto> b){
  BaseEntityConfiguration.Configure(b,"mobile_request_photos");
  b.Property(x=>x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);
  b.Property(x=>x.PropertyId).HasColumnName("property_id");
  b.Property(x=>x.RequestId).HasColumnName("request_id");
  b.Property(x=>x.OperationId).HasColumnName("operation_id");
  b.Property(x=>x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
  b.Property(x=>x.Sha256).HasColumnName("sha256").HasMaxLength(64);
  b.Property(x=>x.ObjectKey).HasColumnName("object_key").HasMaxLength(250);
  b.Property(x=>x.Size).HasColumnName("size");b.Property(x=>x.State).HasColumnName("state").HasMaxLength(10);
  b.HasOne<Property>().WithMany().HasForeignKey(x=>x.PropertyId).OnDelete(DeleteBehavior.Restrict);
  b.HasIndex(x=>new{x.OwnerUserId,x.OperationId}).IsUnique();b.HasIndex(x=>new{x.State,x.CreatedAt});
  b.ToTable(t=>{t.HasCheckConstraint("ck_request_photo_state","state IN ('STAGED','CURRENT','RETIRED','PURGED')");t.HasCheckConstraint("ck_request_photo_size","size BETWEEN 1 AND 262144");});
 }
}
