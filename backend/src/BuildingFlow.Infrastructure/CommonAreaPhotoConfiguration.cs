using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class CommonAreaPhotoConfiguration : IEntityTypeConfiguration<CommonAreaPhoto>
{
    public void Configure(EntityTypeBuilder<CommonAreaPhoto> b)
    {
        BaseEntityConfiguration.Configure(b, "common_area_photos");
        b.Property(x => x.PropertyId).HasColumnName("property_id"); b.Property(x => x.CommonAreaId).HasColumnName("common_area_id");
        b.Property(x => x.DocumentId).HasColumnName("document_id"); b.Property(x => x.RightsConfirmed).HasColumnName("rights_confirmed");
        b.HasOne<CommonArea>().WithMany().HasForeignKey(x => new { x.PropertyId, x.CommonAreaId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<StoredDocument>().WithMany().HasForeignKey(x => new { x.PropertyId, x.DocumentId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.CommonAreaId, x.DocumentId }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_common_area_photo_rights", "rights_confirmed = true"));
    }
}
