using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class PhoneOtpConfiguration : IEntityTypeConfiguration<PhoneOtpChallenge>
{
    public void Configure(EntityTypeBuilder<PhoneOtpChallenge> b)
    {
        BaseEntityConfiguration.Configure(b,"phone_otp_challenges");b.Ignore(x=>x.Terminal);
        b.Property(x=>x.OwnerUserId).HasColumnName("owner_user_id").HasMaxLength(200);
        b.Property(x=>x.TargetHash).HasColumnName("target_hash").HasMaxLength(64);
        b.Property(x=>x.IpHash).HasColumnName("ip_hash").HasMaxLength(64);
        b.Property(x=>x.CodeMac).HasColumnName("code_mac").HasMaxLength(64);
        b.Property(x=>x.ProtectedPayload).HasColumnName("protected_payload").HasMaxLength(2048);
        b.Property(x=>x.Status).HasColumnName("status").HasMaxLength(16);
        b.Property(x=>x.ProviderMessageId).HasColumnName("provider_message_id").HasMaxLength(100);
        b.Property(x=>x.ExpiresAt).HasColumnName("expires_at");b.Property(x=>x.ResendAfter).HasColumnName("resend_after");b.Property(x=>x.Attempts).HasColumnName("attempts");
        b.HasIndex(x=>new{x.OwnerUserId,x.CreatedAt});b.HasIndex(x=>new{x.TargetHash,x.CreatedAt});b.HasIndex(x=>new{x.IpHash,x.CreatedAt});
        b.ToTable(t=>{t.HasCheckConstraint("ck_phone_otp_attempts","attempts BETWEEN 0 AND 5");t.HasCheckConstraint("ck_phone_otp_status","status IN ('RESERVED','ACCEPTED','UNKNOWN','FAILED','VERIFIED','CANCELLED','EXPIRED','LOCKED')");t.HasCheckConstraint("ck_phone_otp_lifetime","expires_at > created_at");});
    }
}
