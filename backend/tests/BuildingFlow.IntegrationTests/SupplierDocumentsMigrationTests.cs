using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class SupplierDocumentsMigrationTests
{
    [PostgresFact]public async Task ExistingPrivateDocumentIsBackfilledWithoutLosingHistoryAndDownIsGuarded()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="supplier_documents_verify_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var create=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await create.ExecuteNonQueryAsync();
        try
        {
            var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"};var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs.ConnectionString,x=>x.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;
            await using var db=new BuildingFlowDbContext(options);var migrations=db.Database.GetMigrations().ToList();var target=migrations.Single(m=>m.EndsWith("_SupplierDocumentsContracts",StringComparison.Ordinal));var previous=migrations[migrations.IndexOf(target)-1];await db.GetService<IMigrator>().MigrateAsync(previous);
            var tenant=new PersonTenant("Legacy supplier docs");var property=new Property("Legacy","City","District","Address");var supplier=new Supplier(tenant.Id,"Legacy supplier",SupplierKind.COMPANY);
            db.AddRange(tenant,property,supplier,new PersonPropertyScope(tenant.Id,property.Id),new SupplierManagementArea(tenant.Id,supplier.Id,property.Id));await db.SaveChangesAsync();
            var document=Guid.NewGuid();var link=Guid.NewGuid();var request=Guid.NewGuid();var hash=new string('A',64);var now=DateTimeOffset.UtcNow;var objectKey=$"properties/{property.Id:N}/documents/{document:N}";
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO stored_documents(id,property_id,request_id,request_fingerprint,announcement_id,audience,file_name,content_type,size,sha256,object_key,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled)
                VALUES({document},{property.Id},{request},{hash},NULL,'MANAGEMENT','legacy.pdf','application/pdf',5,{hash},{objectKey},1,{now},'fixture',{now},'fixture',false,true)
                """);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO supplier_documents(id,tenant_id,supplier_id,property_id,document_id,expires_on,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled)
                VALUES({link},{tenant.Id},{supplier.Id},{property.Id},{document},NULL,1,{now},'fixture',{now},'fixture',false,true)
                """);
            await db.Database.MigrateAsync();await db.Database.MigrateAsync();LegacySchema.AssertCurrentModelMatchesMigrations();
            Assert.True((await db.StoredDocuments.SingleAsync(d=>d.Id==document)).IsSupplierOnly);var metadata=await db.Set<SupplierDocument>().SingleAsync(d=>d.Id==link);Assert.Equal(1,metadata.RevisionNumber);Assert.Null(metadata.PreviousDocumentId);
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(previous));Assert.True(await db.StoredDocuments.AnyAsync(d=>d.Id==document));
        }
        finally{await using var drop=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await drop.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>role=="SUPER_ADMIN";}
}
