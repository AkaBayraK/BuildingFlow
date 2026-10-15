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
