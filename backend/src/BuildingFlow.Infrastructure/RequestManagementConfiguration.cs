using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BuildingFlow.Infrastructure;
internal sealed class RequestManagementConfiguration:IEntityTypeConfiguration<RequestManagementEvent> {
 public void Configure(EntityTypeBuilder<RequestManagementEvent> b) {
  BaseEntityConfiguration.Configure(b,"request_management_events");
  b.Property(x=>x.PropertyId).HasColumnName("property_id");
  b.Property(x=>x.RequestId).HasColumnName("request_id");
  b.Property(x=>x.FromStatus).HasColumnName("from_status").HasMaxLength(20);
  b.Property(x=>x.ToStatus).HasColumnName("to_status").HasMaxLength(20);
  b.Property(x=>x.Response).HasColumnName("response").HasMaxLength(2000);
  b.Property(x=>x.ActorUserId).HasColumnName("actor_user_id").HasMaxLength(200);
  b.Property(x=>x.OperationId).HasColumnName("operation_id");
  b.Property(x=>x.Fingerprint).HasColumnName("fingerprint").HasMaxLength(64);
  b.Property(x=>x.ExpectedVersion).HasColumnName("expected_version");
  b.HasOne<MobileRequest>().WithMany().HasForeignKey(x=>new{x.PropertyId,x.RequestId}).HasPrincipalKey(x=>new{x.PropertyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasIndex(x=>new{x.ActorUserId,x.OperationId}).IsUnique();
  b.HasIndex(x=>new{x.PropertyId,x.RequestId,x.CreatedAt});
  b.ToTable(t=>{t.HasCheckConstraint("ck_request_event_response","length(trim(response)) BETWEEN 1 AND 2000 AND expected_version > 0");t.HasCheckConstraint("ck_request_event_transition","(from_status='RECORDED' AND to_status IN ('IN_PROGRESS','REJECTED')) OR (from_status='IN_PROGRESS' AND to_status IN ('RESOLVED','REJECTED')) OR (from_status IN ('RESOLVED','REJECTED') AND to_status='RECORDED')");});
 }
}
