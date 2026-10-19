using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class MobileRegistrationConfiguration : IEntityTypeConfiguration<MobileRegistration>
{
    public void Configure(EntityTypeBuilder<MobileRegistration> b)
    {
        BaseEntityConfiguration.Configure(b, "mobile_registrations");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
        b.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(200);
        b.Property(x => x.Username).HasColumnName("username").HasMaxLength(50);
        b.Property(x => x.ClientKey).HasColumnName("client_key").HasMaxLength(64);
        b.HasIndex(x => x.RequestId).IsUnique().HasDatabaseName("uidx_mobile_registrations_request_id");
        b.HasIndex(x => new { x.ClientKey, x.CreatedAt }).HasDatabaseName("idx_mobile_registrations_client_key");
    }
}
internal sealed class PersonLinkAttemptConfiguration : IEntityTypeConfiguration<PersonLinkAttempt>
{
    public void Configure(EntityTypeBuilder<PersonLinkAttempt> b)
    {
        BaseEntityConfiguration.Configure(b, "person_link_attempts");
        b.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(200);
        b.Property(x => x.Succeeded).HasColumnName("is_succeeded");
        b.Property(x => x.PersonId).HasColumnName("person_id");
        b.HasIndex(x => new { x.UserId, x.CreatedAt }).HasDatabaseName("idx_person_link_attempts_user_id");
    }
}
