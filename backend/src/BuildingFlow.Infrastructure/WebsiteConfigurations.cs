using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class WebsiteSiteConfiguration : IEntityTypeConfiguration<WebsiteSite>
{
    public void Configure(EntityTypeBuilder<WebsiteSite> b)
    {
        BaseEntityConfiguration.Configure(b, "website_sites");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.SiteKey).HasColumnName("site_key").HasMaxLength(63).HasColumnType("citext");
        b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(120);
        b.Property(x => x.Tagline).HasColumnName("tagline").HasMaxLength(200);
        b.Property(x => x.ContactEmail).HasColumnName("contact_email").HasMaxLength(120);
        b.Property(x => x.ContactPhone).HasColumnName("contact_phone").HasMaxLength(30);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IsDemo).HasColumnName("is_demo");
        b.Property(x=>x.LogoMediaId).HasColumnName("logo_media_id");
        b.Property(x=>x.HeroMediaId).HasColumnName("hero_media_id");
        b.HasOne<WebsiteMedia>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.Id,x.LogoMediaId}).HasPrincipalKey(x=>new{x.PropertyId,x.SiteId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WebsiteMedia>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.Id,x.HeroMediaId}).HasPrincipalKey(x=>new{x.PropertyId,x.SiteId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Template).HasColumnName("template").HasMaxLength(40);
        b.Property(x => x.PrimaryColor).HasColumnName("primary_color").HasMaxLength(7);
        b.Property(x => x.AccentColor).HasColumnName("accent_color").HasMaxLength(7);
        b.HasOne<Property>().WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasIndex(x => x.PropertyId).IsUnique().HasFilter("is_deleted = false");
        b.HasIndex(x => x.SiteKey).IsUnique().HasFilter("is_deleted = false");
        b.HasIndex(x => x.RequestId).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_website_sites_values",
            "status IN ('DRAFT','PUBLISHED','SUSPENDED') AND template = 'PREMIUM_RESIDENCE' AND primary_color ~ '^#[0-9A-F]{6}$' AND accent_color ~ '^#[0-9A-F]{6}$' " +
            "AND site_key ~ '^[a-z0-9]([a-z0-9-]{1,61}[a-z0-9])$' AND length(trim(display_name)) > 0"));
    }
}

internal sealed class WebsitePageConfiguration : IEntityTypeConfiguration<WebsitePage>
{
    public void Configure(EntityTypeBuilder<WebsitePage> b)
    {
        BaseEntityConfiguration.Configure(b, "website_pages");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.SiteId).HasColumnName("site_id");
        b.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(60);
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(120);
        b.Property(x => x.MenuLabel).HasColumnName("menu_label").HasMaxLength(40);
        b.Property(x => x.ShowInMenu).HasColumnName("show_in_menu");
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        b.Property(x => x.SeoTitle).HasColumnName("seo_title").HasMaxLength(70);
        b.Property(x => x.SeoDescription).HasColumnName("seo_description").HasMaxLength(160);
        b.Ignore(x => x.IsRequired);
        b.HasOne<WebsiteSite>().WithMany().HasForeignKey(x => new { x.PropertyId, x.SiteId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.PropertyId, x.Id });
        b.HasIndex(x => new { x.SiteId, x.Slug }).IsUnique().HasFilter("is_deleted = false");
        b.HasIndex(x => new { x.SiteId, x.Kind }).IsUnique().HasFilter("is_deleted = false AND kind IN ('HOME','PRIVACY','COOKIES')").HasDatabaseName("IX_website_pages_site_id_required_kind");
        b.HasIndex(x => new { x.SiteId, x.SortOrder });
        b.ToTable(t => t.HasCheckConstraint("ck_website_pages_values",
            "kind IN ('HOME','ABOUT','UNITS','AMENITIES','NOTICES','GALLERY','CONTACT','PRIVACY','COOKIES','CUSTOM') AND sort_order BETWEEN 0 AND 1000 " +
            "AND ((kind = 'HOME' AND slug = '') OR (kind <> 'HOME' AND slug ~ '^[a-z0-9]([a-z0-9-]{0,58}[a-z0-9])?$')) AND length(trim(title)) > 0"));
    }
}

internal sealed class WebsiteSectionConfiguration : IEntityTypeConfiguration<WebsiteSection>
{
    public void Configure(EntityTypeBuilder<WebsiteSection> b)
    {
        BaseEntityConfiguration.Configure(b, "website_sections");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.PageId).HasColumnName("page_id");
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Data).HasColumnName("data").HasColumnType("jsonb");
        b.Property(x => x.IsVisible).HasColumnName("is_visible");
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        b.HasOne<WebsitePage>().WithMany().HasForeignKey(x => new { x.PropertyId, x.PageId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PageId, x.SortOrder });
        b.ToTable(t => t.HasCheckConstraint("ck_website_sections_values",
            "type IN ('HERO','FEATURES','LISTINGS','NOTICES','AMENITIES','GALLERY','STATS','CONTACT','LOGIN_CARD','RICH_TEXT','VIDEO_LINK') AND sort_order BETWEEN 0 AND 1000 " +
            "AND jsonb_typeof(data) = 'object' AND length(data::text) <= 16384"));
    }
}

internal sealed class WebsitePublicationConfiguration : IEntityTypeConfiguration<WebsitePublication>
{
    public void Configure(EntityTypeBuilder<WebsitePublication> b)
    {
        BaseEntityConfiguration.Configure(b, "website_publications");
        b.Property(x => x.PropertyId).HasColumnName("property_id");
        b.Property(x => x.SiteId).HasColumnName("site_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.Number).HasColumnName("number");
        // Plain text keeps the exact bytes whose SHA-256 is the public ETag (jsonb would reorder keys).
        b.Property(x => x.Snapshot).HasColumnName("snapshot").HasColumnType("text");
        b.Property(x => x.SnapshotSha256).HasColumnName("snapshot_sha256").HasMaxLength(64);
        b.Property(x => x.PublishedAt).HasColumnName("published_at");
        b.Property(x => x.Note).HasColumnName("note").HasMaxLength(200);
        b.Property(x => x.SourcePublicationId).HasColumnName("source_publication_id");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.HasOne<WebsiteSite>().WithMany().HasForeignKey(x => new { x.PropertyId, x.SiteId }).HasPrincipalKey(x => new { x.PropertyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WebsitePublication>().WithMany().HasForeignKey(x => x.SourcePublicationId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SiteId, x.Number }).IsUnique();
        b.HasIndex(x => new { x.SiteId, x.RequestId }).IsUnique();
        b.HasIndex(x => x.SiteId).IsUnique().HasFilter("is_active = true AND is_deleted = false").HasDatabaseName("IX_website_publications_site_id_active");
        b.ToTable(t => t.HasCheckConstraint("ck_website_publications_values", "number > 0 AND snapshot_sha256 ~ '^[0-9a-f]{64}$' AND length(snapshot) > 0"));
    }
}
