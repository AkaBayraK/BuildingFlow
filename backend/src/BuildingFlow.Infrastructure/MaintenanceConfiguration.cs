using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class MaintenancePlanConfiguration : IEntityTypeConfiguration<MaintenancePlan>
{
    public void Configure(EntityTypeBuilder<MaintenancePlan> b)
    {
        BaseEntityConfiguration.Configure(b, "maintenance_plans");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(MaintenancePlan.TitleLength);
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(MaintenancePlan.DescriptionLength);
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PlannedDate).HasColumnName("planned_date");
        b.Property(x => x.Location).HasColumnName("location").HasMaxLength(MaintenancePlan.TextLength);
        b.Property(x => x.Responsible).HasColumnName("responsible").HasMaxLength(MaintenancePlan.TextLength);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasIndex(x => new { x.PropertyId, x.PlannedDate });
        b.ToTable(t => t.HasCheckConstraint("ck_maintenance_plans_valid", "type IN ('MAINTENANCE','CLEANING','INSPECTION','DRILL','OTHER') AND status IN ('PLANNED','IN_PROGRESS','COMPLETED','CANCELLED') AND length(trim(title)) > 0 AND planned_date BETWEEN DATE '2000-01-01' AND DATE '2100-12-31'"));
    }
}
