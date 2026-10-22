using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;

internal sealed class ContactVerificationConfiguration : IEntityTypeConfiguration<AccountPhoneVerification>
{
    public void Configure(EntityTypeBuilder<AccountPhoneVerification> b)
    {
        BaseEntityConfiguration.Configure(b, "account_phone_verifications");
        b.Property(x => x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);
        b.Property(x => x.VerifiedPhone).HasColumnName("verified_phone").HasMaxLength(32);
        b.Property(x => x.VerifiedKey).HasColumnName("verified_key").HasMaxLength(80);
        b.Property(x => x.VerifiedAt).HasColumnName("verified_at");
        b.Property(x => x.PendingPhone).HasColumnName("pending_phone").HasMaxLength(32);
        b.Property(x => x.PendingKey).HasColumnName("pending_key").HasMaxLength(80);
        b.HasIndex(x => x.OwnerUserId).IsUnique();
        b.ToTable(t => {
            t.HasCheckConstraint("ck_account_phone_proof", "(verified_phone IS NULL) = (verified_key IS NULL) AND (verified_key IS NULL) = (verified_at IS NULL)");
            t.HasCheckConstraint("ck_account_phone_pending", "(pending_phone IS NULL) = (pending_key IS NULL)");
        });
    }
}
