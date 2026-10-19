using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class ConsentTypeConfiguration : IEntityTypeConfiguration<ConsentType>
{
    public void Configure(EntityTypeBuilder<ConsentType> b)
    {
        BaseEntityConfiguration.Configure(b, "consent_types");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(ConsentType.MaxCodeLength);
        b.Property(x => x.NameTr).HasColumnName("name_tr").HasMaxLength(ConsentType.MaxNameLength);
        b.Property(x => x.NameEn).HasColumnName("name_en").HasMaxLength(ConsentType.MaxNameLength);
        b.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IsSystem).HasColumnName("is_system");
        b.Property(x => x.Feature).HasColumnName("feature").HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        // Codes stay unique across soft-deleted rows too: history must never point at a reused code.
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uidx_consent_types_code");
        b.HasIndex(x => x.Feature).IsUnique().HasDatabaseName("uidx_consent_types_feature").HasFilter("feature IS NOT NULL");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("chk_consent_types_code", "code ~ '^[A-Z][A-Z0-9_]{1,39}$'");
            t.HasCheckConstraint("chk_consent_types_kind", "kind IN ('INFORMATION','EXPLICIT_CONSENT')");
            t.HasCheckConstraint("chk_consent_types_feature", "feature IS NULL OR (is_system AND feature IN ('PROFILE_PHOTO','REQUEST_PHOTO'))");
            t.HasCheckConstraint("chk_consent_types_sort_order", "sort_order BETWEEN 0 AND 10000");
        });
    }
}

internal sealed class ConsentTextConfiguration : IEntityTypeConfiguration<ConsentText>
{
    public void Configure(EntityTypeBuilder<ConsentText> b)
    {
        BaseEntityConfiguration.Configure(b, "consent_texts");
        b.Property(x => x.ConsentTypeId).HasColumnName("consent_type_id");
        b.Property(x => x.TextVersion).HasColumnName("text_version");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(ConsentText.MaxTitleLength);
        b.Property(x => x.Body).HasColumnName("body").HasMaxLength(ConsentText.MaxBodyLength);
        b.Property(x => x.BodySha256).HasColumnName("body_sha256").HasMaxLength(64);
        b.Property(x => x.RequiresReconsent).HasColumnName("requires_reconsent");
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.HasOne<ConsentType>().WithMany().HasForeignKey(x => x.ConsentTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x => new { x.Id, x.ConsentTypeId }).HasName("ak_consent_texts_id_type");
        b.HasIndex(x => new { x.ConsentTypeId, x.TextVersion }).IsUnique().HasDatabaseName("uidx_consent_texts_type_version");
        b.HasIndex(x => new { x.ConsentTypeId, x.OperationId }).IsUnique().HasDatabaseName("uidx_consent_texts_type_operation");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("chk_consent_texts_version", "text_version > 0");
            t.HasCheckConstraint("chk_consent_texts_hashes", "body_sha256 ~ '^[0-9a-f]{64}$' AND fingerprint ~ '^[0-9a-f]{64}$'");
            t.HasCheckConstraint("chk_consent_texts_body", "length(btrim(body)) > 0 AND length(btrim(title)) > 0");
        });
    }
}

internal sealed class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> b)
    {
        BaseEntityConfiguration.Configure(b, "user_consents");
        b.Property(x => x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);
        b.Property(x => x.ApproverName).HasColumnName("approver_name").HasMaxLength(UserConsent.MaxApproverLength);
        b.Property(x => x.ConsentTypeId).HasColumnName("consent_type_id");
        b.Property(x => x.ConsentTextId).HasColumnName("consent_text_id");
        b.Property(x => x.TextVersion).HasColumnName("text_version");
        b.Property(x => x.BodySha256).HasColumnName("body_sha256").HasMaxLength(64);
        b.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        b.Property(x => x.Channel).HasColumnName("channel").HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.AppVersion).HasColumnName("app_version").HasMaxLength(40);
        b.Property(x => x.OperationId).HasColumnName("operation_id");
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x => x.EvidenceSha256).HasColumnName("evidence_sha256").HasMaxLength(64);
        b.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        b.Property(x => x.RevokeReason).HasColumnName("revoke_reason").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.RevokeOperationId).HasColumnName("revoke_operation_id");
        b.HasOne<ConsentType>().WithMany().HasForeignKey(x => x.ConsentTypeId).OnDelete(DeleteBehavior.Restrict);
        // The text must belong to the same type the row claims.
        b.HasOne<ConsentText>().WithMany().HasForeignKey(x => new { x.ConsentTextId, x.ConsentTypeId }).HasPrincipalKey(x => new { x.Id, x.ConsentTypeId })
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_user_consents_text_type");
        b.HasIndex(x => new { x.OwnerUserId, x.ConsentTypeId }).IsUnique().HasDatabaseName("uidx_user_consents_active")
            .HasFilter("revoked_at IS NULL AND is_deleted = false");
        b.HasIndex(x => new { x.OwnerUserId, x.OperationId }).IsUnique().HasDatabaseName("uidx_user_consents_operation");
        b.HasIndex(x => new { x.OwnerUserId, x.RevokeOperationId }).IsUnique().HasDatabaseName("uidx_user_consents_revoke_operation").HasFilter("revoke_operation_id IS NOT NULL");
        b.HasIndex(x => new { x.ConsentTypeId, x.ApprovedAt }).HasDatabaseName("idx_user_consents_type_approved_at");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("chk_user_consents_revoked", "(revoked_at IS NULL) = (revoke_reason IS NULL) AND (revoke_operation_id IS NULL OR revoked_at IS NOT NULL)");
            t.HasCheckConstraint("chk_user_consents_revoke_reason", "revoke_reason IS NULL OR revoke_reason IN ('WITHDRAWN','SUPERSEDED')");
            t.HasCheckConstraint("chk_user_consents_channel", "channel IN ('MOBILE','WEB')");
            t.HasCheckConstraint("chk_user_consents_hashes", "body_sha256 ~ '^[0-9a-f]{64}$' AND fingerprint ~ '^[0-9a-f]{64}$' AND evidence_sha256 ~ '^[0-9a-f]{64}$'");
            t.HasCheckConstraint("chk_user_consents_dates", "text_version > 0 AND (revoked_at IS NULL OR revoked_at >= approved_at)");
        });
    }
}
