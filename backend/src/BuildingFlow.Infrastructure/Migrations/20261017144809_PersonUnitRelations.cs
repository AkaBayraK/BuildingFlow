using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersonUnitRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql("""
CREATE VIEW person_unit_relation_checks AS
SELECT id,tenant_id,property_id,unit_id,person_id,start_date,end_date,is_primary_contact,share_numerator,share_denominator,'unit_ownerships'::text AS kind FROM unit_ownerships WHERE NOT is_deleted
UNION ALL
SELECT id,tenant_id,property_id,unit_id,person_id,start_date,end_date,is_primary_contact,NULL::bigint,NULL::bigint,'unit_tenancies'::text FROM unit_tenancies WHERE NOT is_deleted
UNION ALL
SELECT id,tenant_id,property_id,unit_id,person_id,start_date,end_date,is_primary_contact,NULL::bigint,NULL::bigint,'unit_residencies'::text FROM unit_residencies WHERE NOT is_deleted;
CREATE FUNCTION bf_person_relation_guard() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE boundary date; part record; n numeric; d numeric; a numeric; b numeric; rem numeric;
BEGIN
    IF TG_OP='UPDATE' THEN
        IF (NEW.tenant_id,NEW.property_id,NEW.unit_id,NEW.person_id,NEW.start_date,NEW.is_primary_contact)
            IS DISTINCT FROM (OLD.tenant_id,OLD.property_id,OLD.unit_id,OLD.person_id,OLD.start_date,OLD.is_primary_contact)
            OR (OLD.end_date IS NOT NULL AND NEW.end_date IS DISTINCT FROM OLD.end_date) THEN
            RAISE EXCEPTION 'Relation history is immutable' USING ERRCODE='23514';
        END IF;
        IF TG_TABLE_NAME='unit_ownerships' THEN
            IF (NEW.share_numerator,NEW.share_denominator) IS DISTINCT FROM (OLD.share_numerator,OLD.share_denominator) THEN
                RAISE EXCEPTION 'Ownership share history is immutable' USING ERRCODE='23514';
            END IF;
        END IF;
    END IF;
    IF NEW.is_deleted THEN RETURN NEW; END IF;
    PERFORM 1 FROM units WHERE property_id=NEW.property_id AND id=NEW.unit_id FOR UPDATE;
    IF EXISTS (SELECT 1 FROM person_unit_relation_checks r
        WHERE r.tenant_id=NEW.tenant_id AND r.property_id=NEW.property_id AND r.unit_id=NEW.unit_id
        AND NOT (r.kind=TG_TABLE_NAME AND r.id=NEW.id)
        AND (NEW.end_date IS NULL OR r.start_date<=NEW.end_date) AND (r.end_date IS NULL OR r.end_date>=NEW.start_date)
        AND ((r.kind=TG_TABLE_NAME AND r.person_id=NEW.person_id) OR (r.is_primary_contact AND NEW.is_primary_contact))) THEN
        RAISE EXCEPTION 'Overlapping relation or primary contact' USING ERRCODE='23514';
    END IF;
    IF TG_TABLE_NAME='unit_ownerships' THEN
        FOR boundary IN SELECT DISTINCT r.start_date FROM person_unit_relation_checks r
            WHERE r.tenant_id=NEW.tenant_id AND r.property_id=NEW.property_id AND r.unit_id=NEW.unit_id AND r.share_numerator IS NOT NULL
        LOOP
            n:=0; d:=1;
            FOR part IN SELECT r.share_numerator,r.share_denominator FROM person_unit_relation_checks r
                WHERE r.tenant_id=NEW.tenant_id AND r.property_id=NEW.property_id AND r.unit_id=NEW.unit_id
                AND r.share_numerator IS NOT NULL AND r.start_date<=boundary AND (r.end_date IS NULL OR r.end_date>=boundary)
            LOOP
                n:=n*part.share_denominator+part.share_numerator*d; d:=d*part.share_denominator;
                a:=n; b:=d;
                WHILE b<>0 LOOP rem:=mod(a,b); a:=b; b:=rem; END LOOP;
                n:=n/a; d:=d/a;
            END LOOP;
            IF n>d THEN RAISE EXCEPTION 'Ownership shares exceed one' USING ERRCODE='23514'; END IF;
        END LOOP;
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER trg_person_ownership_guard AFTER INSERT OR UPDATE ON unit_ownerships FOR EACH ROW EXECUTE FUNCTION bf_person_relation_guard();
CREATE TRIGGER trg_person_tenancy_guard AFTER INSERT OR UPDATE ON unit_tenancies FOR EACH ROW EXECUTE FUNCTION bf_person_relation_guard();
CREATE TRIGGER trg_person_residency_guard AFTER INSERT OR UPDATE ON unit_residencies FOR EACH ROW EXECUTE FUNCTION bf_person_relation_guard();
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
DO $$ BEGIN
IF EXISTS(SELECT 1 FROM person_property_scopes) OR EXISTS(SELECT 1 FROM legacy_person_maps) OR EXISTS(SELECT 1 FROM legacy_occupancy_maps)
 OR EXISTS(SELECT 1 FROM unit_ownerships) OR EXISTS(SELECT 1 FROM unit_tenancies) OR EXISTS(SELECT 1 FROM unit_residencies)
THEN RAISE EXCEPTION 'Person relation rollback would lose history'; END IF;
END $$;
DROP TRIGGER trg_person_ownership_guard ON unit_ownerships;
DROP TRIGGER trg_person_tenancy_guard ON unit_tenancies;
DROP TRIGGER trg_person_residency_guard ON unit_residencies;
DROP FUNCTION bf_person_relation_guard();
DROP VIEW person_unit_relation_checks;
""");


        }
    }
}
