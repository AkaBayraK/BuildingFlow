using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class LeaseContractPersistenceTests(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static readonly DateOnly Start=new(2026,1,1),End=new(2026,12,31);
    private sealed record Fixture(Guid Tenant,Guid Property,Guid Unit,Guid Ahmet,Guid Ayse,Guid Account);
    private async Task<Fixture> Seed()
    {
        var tenant=new PersonTenant("Lease fixture");var property=new Property("Lease fixture","City","District","Address",PropertyType.APARTMENT);var unit=property.AddUnit("7",1,100);var account=new PersonUserAccount("http://localhost:8080/realms/buildingflow",Guid.NewGuid().ToString());
        var ahmet=new CentralPerson(tenant.Id,"NATURAL","DOMESTIC","TR","12345678901","Ahmet","Fixture",null,null,null,null);var ayse=new CentralPerson(tenant.Id,"NATURAL","DOMESTIC","TR","12345678901","Ayse","Fixture",null,null,null,null);
        await factory.WithDb(async db=>{db.AddRange(tenant,property,ahmet,ayse,account,new PersonPropertyScope(tenant.Id,property.Id));await db.SaveChangesAsync();});return new(tenant.Id,property.Id,unit.Id,ahmet.Id,ayse.Id,account.Id);
    }
    private static async Task Rejected(BuildingFlowDbContext db,Func<Task> act,params BaseEntity[] rows)
    {
        var error=await Assert.ThrowsAnyAsync<Exception>(act);var postgres=error as PostgresException??error.InnerException as PostgresException;Assert.NotNull(postgres);
        Assert.Contains(postgres!.SqlState,new[]{PostgresErrorCodes.CheckViolation,PostgresErrorCodes.ForeignKeyViolation});foreach(var row in rows)db.Entry(row).State=EntityState.Detached;db.ChangeTracker.Clear();
    }
    [PostgresFact]public async Task JointTenantsAndRenewalPersistAndPartiesOutsideContractAreRejectedAtCommit()
    {
        var s=await Seed();await factory.WithDb(async db=>
        {
            var lease=new LeaseContract(s.Tenant,s.Property,s.Unit,Start,End,"2026/7");lease.Verify(s.Account,DateTimeOffset.UtcNow);db.AddRange(lease,new UnitTenancy(lease,s.Ahmet,Start,End,true),new UnitTenancy(lease,s.Ayse,Start,End));await db.SaveChangesAsync();
            var renewal=new LeaseContract(s.Tenant,s.Property,s.Unit,End.AddDays(1));db.AddRange(renewal,new UnitTenancy(renewal,s.Ahmet,End.AddDays(1)));await db.SaveChangesAsync();
            Assert.Equal(2,await db.Set<UnitTenancy>().CountAsync(t=>t.LeaseContractId==lease.Id));Assert.Equal(RelationVerificationStatus.PENDING,(await db.Set<LeaseContract>().AsNoTracking().SingleAsync(c=>c.Id==renewal.Id)).VerificationStatus);
            await Rejected(db,()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE lease_contracts SET end_date=DATE '2027-01-31' WHERE id={renewal.Id}"));
            var free=new UnitTenancy(s.Tenant,s.Property,s.Unit,s.Ayse,End.AddDays(1),End.AddDays(5));db.Add(free);await db.SaveChangesAsync();
            await Rejected(db,()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_tenancies SET lease_contract_id={lease.Id} WHERE id={free.Id}"));
            await Rejected(db,()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_tenancies SET lease_contract_id={renewal.Id} WHERE lease_contract_id={lease.Id}"));
            var overlap=new UnitTenancy(renewal,s.Ahmet,End.AddDays(3),End.AddDays(4));db.Add(overlap);await Rejected(db,()=>db.SaveChangesAsync(),overlap);
            await Rejected(db,()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE lease_contracts SET is_deleted=true WHERE id={lease.Id}"));
            await Rejected(db,()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE lease_contracts SET end_date=DATE '2026-06-30' WHERE id={lease.Id}"));
            await Rejected(db,()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE lease_contracts SET start_date=DATE '2025-12-01' WHERE id={lease.Id}"));
            var party=await db.Set<UnitTenancy>().SingleAsync(t=>t.LeaseContractId==renewal.Id);var contract=await db.Set<LeaseContract>().SingleAsync(c=>c.Id==renewal.Id);party.End(End.AddDays(20));contract.End(End.AddDays(30));await db.SaveChangesAsync();
            await Rejected(db,()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE lease_contracts SET end_date=DATE '2027-03-01' WHERE id={renewal.Id}"));
        });
    }
    [PostgresFact]public async Task VerificationDecisionEvidenceScopeAndTenantBoundaryAreEnforcedByDatabase()
    {
        var s=await Seed();var other=await Seed();await factory.WithDb(async db=>
        {
            var foreignDoc=new StoredDocument(other.Property,Guid.NewGuid(),new string('A',64),null,ContentAudience.MANAGEMENT,"lease.pdf","application/pdf",10,new string('B',64));var ownDoc=new StoredDocument(s.Property,Guid.NewGuid(),new string('C',64),null,ContentAudience.MANAGEMENT,"lease.pdf","application/pdf",10,new string('D',64));db.AddRange(foreignDoc,ownDoc);await db.SaveChangesAsync();
            var crossDoc=new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Ahmet,Start);crossDoc.Verify(s.Account,DateTimeOffset.UtcNow,foreignDoc.Id);db.Add(crossDoc);await Rejected(db,()=>db.SaveChangesAsync(),crossDoc);
            var owner=new UnitOwnership(s.Tenant,s.Property,s.Unit,s.Ahmet,Start);owner.Verify(s.Account,DateTimeOffset.UtcNow,ownDoc.Id);db.Add(owner);await db.SaveChangesAsync();
            await Rejected(db,()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_ownerships SET verification_status='REJECTED' WHERE id={owner.Id}"));
            await Rejected(db,()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_ownerships SET verified_by_account_id={other.Account} WHERE id={owner.Id}"));
            await Rejected(db,()=>db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO unit_residencies(id,tenant_id,property_id,unit_id,person_id,start_date,is_primary_contact,verification_status,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled) VALUES ({Guid.NewGuid()},{s.Tenant},{s.Property},{s.Unit},{s.Ayse},DATE '2026-01-01',false,'VERIFIED',1,now(),'fixture',now(),'fixture',false,true)"));
            var pending=new UnitResidency(s.Tenant,s.Property,s.Unit,s.Ayse,Start);db.Add(pending);await db.SaveChangesAsync();pending.Reject(s.Account,DateTimeOffset.UtcNow);await db.SaveChangesAsync();
            await Rejected(db,()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE unit_residencies SET verification_status='PENDING',verified_by_account_id=NULL,verified_at=NULL WHERE id={pending.Id}"));
            var foreignLease=new LeaseContract(other.Tenant,s.Property,s.Unit,Start);db.Add(foreignLease);await Rejected(db,()=>db.SaveChangesAsync(),foreignLease);
            var crossUnit=new LeaseContract(s.Tenant,s.Property,other.Unit,Start);db.Add(crossUnit);await Rejected(db,()=>db.SaveChangesAsync(),crossUnit);
        });
    }
    [PostgresFact]public async Task DatabaseAllowsOneLeasePerUnitPeriodEvenUnderConcurrentWriters()
    {
        var s=await Seed();
        await factory.WithDb(async db=>{db.Add(new LeaseContract(s.Tenant,s.Property,s.Unit,Start,End));await db.SaveChangesAsync();
            var overlap=new LeaseContract(s.Tenant,s.Property,s.Unit,End,null);db.Add(overlap);var error=await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());Assert.Equal(PostgresErrorCodes.ExclusionViolation,Assert.IsType<PostgresException>(error.InnerException).SqlState);db.Entry(overlap).State=EntityState.Detached;
            db.Add(new LeaseContract(s.Tenant,s.Property,s.Unit,End.AddDays(1)));await db.SaveChangesAsync();});
        async Task<bool> Write(){var ok=false;await factory.WithDb(async db=>{var row=new LeaseContract(s.Tenant,s.Property,s.Unit,Start.AddYears(-3),Start.AddYears(-2));db.Add(row);try{await db.SaveChangesAsync();ok=true;}catch(DbUpdateException){db.Entry(row).State=EntityState.Detached;}
                catch(InvalidOperationException error) when(error.InnerException is DbUpdateException {InnerException:PostgresException {SqlState:PostgresErrorCodes.DeadlockDetected}})
                {db.Entry(row).State=EntityState.Detached;}
            });return ok;}
        Assert.Single(await Task.WhenAll(Write(),Write()),r=>r);
        await factory.WithDb(async db=>Assert.Equal(3,await db.Set<LeaseContract>().CountAsync(x=>x.UnitId==s.Unit)));
    }
    [PostgresFact]public async Task MigrationBackfillsLegacyWithoutInventedVerifierReappliesAndRefusesLossyRollback()
    {
        var original=Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;var schema="lease_"+Guid.NewGuid().ToString("N");await using var admin=new NpgsqlConnection(original);await admin.OpenAsync();await using(var c=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",admin))await c.ExecuteNonQueryAsync();
        try
        {
            var cs=new NpgsqlConnectionStringBuilder(original){SearchPath=schema+",public"}.ConnectionString;var options=new DbContextOptionsBuilder<BuildingFlowDbContext>().UseNpgsql(cs,o=>o.MigrationsHistoryTable("__EFMigrationsHistory",schema)).AddInterceptors(new AuditInterceptor(new Actor(),TimeProvider.System)).Options;await using var db=new BuildingFlowDbContext(options);
            var migrations=db.Database.GetMigrations().ToArray();var index=Array.FindIndex(migrations,m=>m.EndsWith("_LeaseContractsAndRelationVerification",StringComparison.Ordinal));Assert.True(index>0);var prior=migrations[index-1];await db.GetService<IMigrator>().MigrateAsync(prior);
            var t=new PersonTenant("Fixture");var p=new Property("Fixture","City","District","Address",PropertyType.APARTMENT);var u=p.AddUnit("1",0,100);var person=new CentralPerson(t.Id,"NATURAL","DOMESTIC","TR","12345678901","Fixture","Person",null,null,null,null);var account=new PersonUserAccount("issuer",Guid.NewGuid().ToString());db.AddRange(t,p,person,account,new PersonPropertyScope(t.Id,p.Id));await db.SaveChangesAsync();
            var ownerId=Guid.NewGuid();await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO unit_ownerships(id,tenant_id,property_id,unit_id,person_id,start_date,is_primary_contact,version,created_at,created_by,modified_at,modified_by,is_deleted,is_enabled) VALUES ({ownerId},{t.Id},{p.Id},{u.Id},{person.Id},DATE '2026-01-01',false,1,now(),'fixture',now(),'fixture',false,true)");
            var before=await db.Database.SqlQuery<string>($"SELECT row_to_json(o)::text AS \"Value\" FROM unit_ownerships o WHERE id={ownerId}").SingleAsync();
            await db.Database.MigrateAsync();await db.Database.MigrateAsync();LegacySchema.AssertCurrentModelMatchesMigrations();
            var migrated=await db.Set<UnitOwnership>().AsNoTracking().SingleAsync(o=>o.Id==ownerId);Assert.Equal(RelationVerificationStatus.LEGACY,migrated.VerificationStatus);Assert.Null(migrated.VerifiedByAccountId);Assert.Null(migrated.VerifiedAt);Assert.Null(migrated.EvidenceDocumentId);Assert.Equal(1,migrated.Version);
            var columns=await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name='lease_contracts' AND is_nullable='NO'").ToArrayAsync();foreach(var col in new[]{"id","version","created_at","created_by","modified_at","modified_by","is_deleted","is_enabled","verification_status"})Assert.Contains(col,columns);
            await db.GetService<IMigrator>().MigrateAsync(prior);Assert.Equal(before,await db.Database.SqlQuery<string>($"SELECT row_to_json(o)::text AS \"Value\" FROM unit_ownerships o WHERE id={ownerId}").SingleAsync());await db.Database.MigrateAsync();
            var lease=new LeaseContract(t.Id,p.Id,u.Id,new(2026,1,1));lease.Verify(account.Id,DateTimeOffset.UtcNow);db.Add(lease);await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync(prior));Assert.Contains(migrations[index],await db.Database.GetAppliedMigrationsAsync());Assert.True(await db.Set<LeaseContract>().AnyAsync(c=>c.Id==lease.Id));
        }
        finally{await using var cleanup=new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE",admin);await cleanup.ExecuteNonQueryAsync();}
    }
    private sealed class Actor:ICurrentUserService{public string? UserId=>"fixture";public string? Username=>"fixture";public bool IsAuthenticated=>true;public bool IsInRole(string role)=>false;}
}
