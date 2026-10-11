using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
namespace BuildingFlow.Infrastructure;

public static class DatabaseMigration
{
    // Shared by every API instance and the migration job so only one process changes the schema at a time.
    public const long AdvisoryLockKey = 0x4246_4D49_4752_4154;

    public static async Task<IReadOnlyList<string>> ApplyPendingAsync(BuildingFlowDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (db.Database.HasPendingModelChanges())
            throw new InvalidOperationException("Entity model has changes without a migration. Run: bash scripts/db.sh sync");
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using (var acquire = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", connection))
            {
                acquire.Parameters.AddWithValue("key", AdvisoryLockKey);
                await acquire.ExecuteNonQueryAsync(cancellationToken);
            }
            try
            {
                var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
                if (pending.Length == 0)
                {
                    logger.LogInformation("BuildingFlow.Migrations: database schema is up to date");
                    return pending;
                }
                logger.LogInformation("BuildingFlow.Migrations: applying {Count} migration(s): {Migrations}", pending.Length, string.Join(", ", pending));
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("BuildingFlow.Migrations: applied {Count} migration(s)", pending.Length);
                return pending;
            }
            finally
            {
                await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection);
                release.Parameters.AddWithValue("key", AdvisoryLockKey);
                await release.ExecuteNonQueryAsync(CancellationToken.None);
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
