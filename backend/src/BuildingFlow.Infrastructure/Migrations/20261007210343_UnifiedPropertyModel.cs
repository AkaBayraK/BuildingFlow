using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace BuildingFlow.Infrastructure.Migrations;
public partial class UnifiedPropertyModel : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        // Rename rather than drop/create: identities, audit, access and historical rows survive.
        m.DropForeignKey("FK_apartments_blocks_block_id","apartments");
        m.DropForeignKey("FK_blocks_sites_site_id","blocks");
        m.DropForeignKey("FK_site_memberships_sites_site_id","site_memberships");
        m.RenameTable("sites",newName:"properties");m.RenameTable("blocks",newName:"buildings");
        m.RenameTable("apartments",newName:"units");m.RenameTable("site_memberships",newName:"property_memberships");
        m.RenameColumn("site_id","buildings","property_id");m.RenameColumn("site_id","property_memberships","property_id");m.RenameColumn("block_id","units","building_id");
        RenameKeys(m,false);
        m.RenameIndex("IX_blocks_site_id_name","IX_buildings_property_id_name","buildings");
        m.RenameIndex("IX_site_memberships_site_id_user_id","IX_property_memberships_property_id_user_id","property_memberships");
        m.RenameIndex("IX_site_memberships_user_id","IX_property_memberships_user_id","property_memberships");
        m.DropIndex("IX_apartments_block_id_number","units");
        m.AddColumn<string>("type","properties",type:"character varying(20)",maxLength:20,nullable:false,defaultValue:"SITE");
        m.AddColumn<int>("floor_count","properties",type:"integer",nullable:true);
        m.AddColumn<int>("floor_count","buildings",type:"integer",nullable:true);
        m.AddColumn<string>("type","units",type:"character varying(20)",maxLength:20,nullable:false,defaultValue:"APARTMENT");
        m.AddColumn<Guid>("property_id","units",type:"uuid",nullable:true);
        m.Sql("UPDATE units AS u SET property_id = b.property_id FROM buildings AS b WHERE u.building_id = b.id");
        m.AlterColumn<Guid>("property_id","units",type:"uuid",nullable:false,oldClrType:typeof(Guid),oldType:"uuid",oldNullable:true);
        m.AlterColumn<Guid>("building_id","units",type:"uuid",nullable:true,oldClrType:typeof(Guid),oldType:"uuid");
        m.Sql("ALTER TABLE properties ALTER COLUMN type DROP DEFAULT; ALTER TABLE units ALTER COLUMN type DROP DEFAULT;");
        m.AddUniqueConstraint("AK_buildings_property_id_id","buildings",new[]{"property_id","id"});
        m.AddCheckConstraint("ck_properties_type","properties","type IN ('APARTMENT','SITE','RESIDENCE','MIXED')");
        m.AddCheckConstraint("ck_properties_floor_count","properties","floor_count IS NULL OR (type = 'APARTMENT' AND floor_count > 0)");
        m.AddCheckConstraint("ck_buildings_floor_count","buildings","floor_count IS NULL OR floor_count > 0");
        m.AddCheckConstraint("ck_units_type","units","type IN ('APARTMENT','OFFICE','SHOP','VILLA','WAREHOUSE','WORKSHOP')");
        m.Sql("ALTER TABLE units RENAME CONSTRAINT ck_apartments_positive_area TO ck_units_positive_area");
        m.AddForeignKey("FK_buildings_properties_property_id","buildings","property_id","properties",principalColumn:"id",onDelete:ReferentialAction.Restrict);
        m.AddForeignKey("FK_property_memberships_properties_property_id","property_memberships","property_id","properties",principalColumn:"id",onDelete:ReferentialAction.Restrict);
        m.AddForeignKey("FK_units_properties_property_id","units","property_id","properties",principalColumn:"id",onDelete:ReferentialAction.Restrict);
        m.AddForeignKey("FK_units_buildings_property_id_building_id","units",new[]{"property_id","building_id"},"buildings",principalColumns:new[]{"property_id","id"},onDelete:ReferentialAction.Restrict);
        m.CreateIndex("IX_units_building_id_number","units",new[]{"building_id","number"},unique:true,filter:"is_deleted = false AND building_id IS NOT NULL");
        m.CreateIndex("IX_units_property_id_number","units",new[]{"property_id","number"},unique:true,filter:"is_deleted = false AND building_id IS NULL");
        m.CreateIndex("IX_units_property_id_building_id","units",new[]{"property_id","building_id"});
    }
    protected override void Down(MigrationBuilder m)
    {
        // Refuse lossy conversion, including soft-deleted history and new metadata.
        m.Sql("""
            DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM units WHERE building_id IS NULL OR type <> 'APARTMENT')
                    OR EXISTS (SELECT 1 FROM properties WHERE type <> 'SITE' OR floor_count IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM buildings WHERE floor_count IS NOT NULL)
                THEN RAISE EXCEPTION 'Cannot downgrade: the previous hierarchy cannot represent current data. Export and migrate explicitly.';
                END IF;
            END $$;
            """);
        m.DropForeignKey("FK_units_buildings_property_id_building_id","units");m.DropForeignKey("FK_units_properties_property_id","units");
        m.DropForeignKey("FK_buildings_properties_property_id","buildings");m.DropForeignKey("FK_property_memberships_properties_property_id","property_memberships");
        m.DropIndex("IX_units_building_id_number","units");m.DropIndex("IX_units_property_id_number","units");m.DropIndex("IX_units_property_id_building_id","units");
        m.DropUniqueConstraint("AK_buildings_property_id_id","buildings");
        m.DropCheckConstraint("ck_properties_type","properties");m.DropCheckConstraint("ck_properties_floor_count","properties");
        m.DropCheckConstraint("ck_buildings_floor_count","buildings");m.DropCheckConstraint("ck_units_type","units");
        m.Sql("ALTER TABLE units RENAME CONSTRAINT ck_units_positive_area TO ck_apartments_positive_area");
        m.AlterColumn<Guid>("building_id","units",type:"uuid",nullable:false,oldClrType:typeof(Guid),oldType:"uuid",oldNullable:true);
        m.DropColumn("property_id","units");m.DropColumn("type","units");m.DropColumn("type","properties");
        m.DropColumn("floor_count","properties");m.DropColumn("floor_count","buildings");
        m.RenameIndex("IX_buildings_property_id_name","IX_blocks_site_id_name","buildings");
        m.RenameIndex("IX_property_memberships_property_id_user_id","IX_site_memberships_site_id_user_id","property_memberships");m.RenameIndex("IX_property_memberships_user_id","IX_site_memberships_user_id","property_memberships");
        m.RenameColumn("property_id","buildings","site_id");m.RenameColumn("property_id","property_memberships","site_id");m.RenameColumn("building_id","units","block_id");
        RenameKeys(m,true);
        m.RenameTable("properties",newName:"sites");m.RenameTable("buildings",newName:"blocks");m.RenameTable("units",newName:"apartments");m.RenameTable("property_memberships",newName:"site_memberships");
        m.AddForeignKey("FK_blocks_sites_site_id","blocks","site_id","sites",principalColumn:"id",onDelete:ReferentialAction.Restrict);
        m.AddForeignKey("FK_site_memberships_sites_site_id","site_memberships","site_id","sites",principalColumn:"id",onDelete:ReferentialAction.Restrict);
        m.AddForeignKey("FK_apartments_blocks_block_id","apartments","block_id","blocks",principalColumn:"id",onDelete:ReferentialAction.Restrict);
        m.CreateIndex("IX_apartments_block_id_number","apartments",new[]{"block_id","number"},unique:true,filter:"is_deleted = false");
    }
    private static void RenameKeys(MigrationBuilder m,bool down)
    {
        foreach(var pair in new[]{("sites","properties"),("blocks","buildings"),("apartments","units"),("site_memberships","property_memberships")})
            m.Sql($"ALTER TABLE {pair.Item2} RENAME CONSTRAINT \"PK_{(down?pair.Item2:pair.Item1)}\" TO \"PK_{(down?pair.Item1:pair.Item2)}\"");
    }
}
