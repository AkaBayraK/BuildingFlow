using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class ExpenseMigrationTests
{
    [PostgresTheory] [InlineData(false)] [InlineData(true)]
    public async Task UpgradePreservesAuditEmptyDownIsSafeAndFinancialHistoryPreventsDataLoss(bool budgetOnly)
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var schema = "expense_migration_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(original) { SearchPath = schema + ",public" };
            await using var db = new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>().BeforeApartmentDetails().UseNpgsql(connection.ConnectionString, options => options.MigrationsHistoryTable("__EFMigrationsHistory", schema)).AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options);
            var previous = db.Database.GetMigrations().Single(value => value.EndsWith("_DuesPartyHistoryAndCancellationReasons", StringComparison.Ordinal));
            await db.GetService<IMigrator>().MigrateAsync(previous);
            var property = new Property("Historical property", "City", "District", "Address", PropertyType.APARTMENT); db.Add(property); await db.SaveChangesAsync();
            var id = property.Id;
            var before = await db.Database.SqlQuery<string>($"SELECT (to_jsonb(p) - ARRAY['neighborhood','construction_year','heating_type','has_elevator','parking_type'])::text AS \"Value\" FROM properties p WHERE id={id}").SingleAsync();
            var target = db.Database.GetMigrations().Single(value => value.EndsWith("_ExpensesAndBudgets", StringComparison.Ordinal));
            await db.GetService<IMigrator>().MigrateAsync(target); await db.GetService<IMigrator>().MigrateAsync(target);
            var after = await db.Database.SqlQuery<string>($"SELECT (to_jsonb(p) - ARRAY['neighborhood','construction_year','heating_type','has_elevator','parking_type'])::text AS \"Value\" FROM properties p WHERE id={id}").SingleAsync(); Assert.Equal(before, after);
            // This historical test targets the phase 07 migration; latest-model parity is checked separately.
            await db.GetService<IMigrator>().MigrateAsync(previous); await db.GetService<IMigrator>().MigrateAsync(target);
            foreach (var table in new[] { "expenses", "property_budgets", "budget_revisions" }) {
                var columns = await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToListAsync();
                foreach (var column in new[] { "id", "version", "created_at", "created_by", "modified_at", "modified_by", "is_deleted", "is_enabled" }) Assert.Contains(column, columns);
            }
            var budget = new PropertyBudget(id, new(2026, 10, 1), ExpenseCategory.OTHER); budget.Revise(Guid.NewGuid(), new string('a', 64), 0, "Explicit zero plan"); db.Add(budget);
            if (!budgetOnly) { var expense = new Expense(id, Guid.NewGuid(), new string('a', 64), ExpenseCategory.OTHER, 12.35m, new(2026, 10, 1), "Wrong entry", null); expense.Cancel("Correction", DateTimeOffset.UtcNow); db.Add(expense); }
            await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.GetService<IMigrator>().MigrateAsync(previous));
            Assert.Equal(target, (await db.Database.GetAppliedMigrationsAsync()).Last()); Assert.Equal(0, (await db.PropertyBudgets.AsNoTracking().SingleAsync()).Amount); Assert.Single(await db.BudgetRevisions.ToListAsync());
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE property_budgets SET amount={-1m} WHERE id={budget.Id}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE property_budgets SET is_deleted={true} WHERE id={budget.Id}"));
            var revision = Assert.Single(budget.Revisions);
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE budget_revisions SET is_enabled={false} WHERE id={revision.Id}"));
            var other = new Property("Other", "City", "District", "Address", PropertyType.APARTMENT); db.Add(other); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE budget_revisions SET property_id={other.Id} WHERE id={revision.Id}"));
            if (!budgetOnly) {
                var expense = await db.Expenses.AsNoTracking().SingleAsync();
                await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE expenses SET is_deleted={true} WHERE id={expense.Id}"));
                await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE expenses SET category={"INVALID"} WHERE id={expense.Id}"));
            }
        }
        finally { await using var cleanup = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await cleanup.ExecuteNonQueryAsync(); }
    }
    private sealed class Actor : ICurrentUserService
    { public string? UserId => "expense-migration"; public string? Username => "fixture"; public bool IsAuthenticated => true; public bool IsInRole(string role) => false; }
}
