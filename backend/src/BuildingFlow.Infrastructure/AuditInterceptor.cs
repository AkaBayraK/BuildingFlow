using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingFlow.Infrastructure;

public sealed class AuditInterceptor(ICurrentUserService currentUser, TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Audit(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Audit(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Audit(DbContext? context)
    {
        if (context is null) return;

        var now = clock.GetUtcNow();
        var actor = currentUser.IsAuthenticated ? currentUser.UserId ?? "system" : "system";

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Property(entity => entity.IsDeleted).CurrentValue = true;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedAt).CurrentValue = now;
                entry.Property(entity => entity.CreatedBy).CurrentValue = actor;
                entry.Property(entity => entity.Version).CurrentValue = 1;
            }
            else
            {
                entry.Property(entity => entity.CreatedAt).IsModified = false;
                entry.Property(entity => entity.CreatedBy).IsModified = false;
                entry.Property(entity => entity.Version).CurrentValue = checked(entry.Property(entity => entity.Version).OriginalValue + 1);
            }

            entry.Property(entity => entity.ModifiedAt).CurrentValue = now;
            entry.Property(entity => entity.ModifiedBy).CurrentValue = actor;
        }
    }
}
