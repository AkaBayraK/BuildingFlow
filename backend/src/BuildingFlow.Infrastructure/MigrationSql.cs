using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;
namespace BuildingFlow.Infrastructure;

public static class MigrationSql
{
    public static DbContextOptionsBuilder<TContext> UseBuildingFlowNpgsql<TContext>(this DbContextOptionsBuilder<TContext> options, string connection, Action<NpgsqlDbContextOptionsBuilder>? npgsql = null)
        where TContext : DbContext =>
        options.UseNpgsql(connection, npgsql).ReplaceService<IMigrationsSqlGenerator, TerminatedSqlMigrationsSqlGenerator>();

    public static DbContextOptionsBuilder UseBuildingFlowNpgsql(this DbContextOptionsBuilder options, string connection) =>
        options.UseNpgsql(connection).ReplaceService<IMigrationsSqlGenerator, TerminatedSqlMigrationsSqlGenerator>();
}

#pragma warning disable EF1001 // Npgsql exposes its generator options only through this internal interface.
public sealed class TerminatedSqlMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies, INpgsqlSingletonOptions npgsqlOptions)
    : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlOptions)
#pragma warning restore EF1001
{
    // Applied migrations contain raw SQL without a trailing ';', which breaks idempotent DO-block scripts.
    protected override void Generate(SqlOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        var sql = operation.Sql.TrimEnd();
        base.Generate(sql.EndsWith(';') ? operation : new SqlOperation { Sql = sql + ";", SuppressTransaction = operation.SuppressTransaction }, model, builder);
    }
}
