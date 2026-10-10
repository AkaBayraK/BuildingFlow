using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> b)
    {
        BaseEntityConfiguration.Configure(b, "announcements");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        b.Property(x => x.Content).HasColumnName("content").HasMaxLength(10000);
        b.Property(x => x.Audience).HasColumnName("audience").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PublishedAt).HasColumnName("published_at");
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasIndex(x => new { x.PropertyId, x.Status, x.CreatedAt });
        b.ToTable(t => {
            t.HasCheckConstraint("ck_announcements_content", "length(trim(title)) > 0 AND length(trim(content)) > 0 AND audience IN ('ALL_MEMBERS','MANAGEMENT')");
            t.HasCheckConstraint("ck_announcements_publication", "(status = 'DRAFT' AND published_at IS NULL) OR (status IN ('PUBLISHED','ARCHIVED') AND published_at IS NOT NULL)");
        });
    }
}
internal sealed class StoredDocumentConfiguration : IEntityTypeConfiguration<StoredDocument>
{
    public void Configure(EntityTypeBuilder<StoredDocument> b)
    {
        BaseEntityConfiguration.Configure(b, "stored_documents");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.RequestFingerprint).HasColumnName("request_fingerprint").HasMaxLength(64);
        b.Property(x => x.AnnouncementId).HasColumnName("announcement_id");
        b.Property(x => x.Audience).HasColumnName("audience").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(200);
        b.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(50);
        b.Property(x => x.Size).HasColumnName("size");
        b.Property(x => x.Sha256).HasColumnName("sha256").HasMaxLength(64);
        b.Property(x => x.ObjectKey).HasColumnName("object_key").HasMaxLength(200);
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Announcement>().WithMany().HasForeignKey(x => new { x.PropertyId, x.AnnouncementId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PropertyId, x.RequestId }).IsUnique();
        b.HasIndex(x => x.ObjectKey).IsUnique();
        b.HasIndex(x => new { x.PropertyId, x.AnnouncementId, x.CreatedAt });
        b.ToTable(t => t.HasCheckConstraint("ck_documents_file", "size BETWEEN 1 AND 10485760 AND content_type IN ('application/pdf','image/jpeg','image/png') AND audience IN ('ALL_MEMBERS','MANAGEMENT') AND length(sha256) = 64 AND length(request_fingerprint) = 64 AND length(trim(file_name)) > 0"));
    }
}
