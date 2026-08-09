using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260809002000_FixGeneratedXidConcurrency")]
public sealed class FixGeneratedXidConcurrency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE OR REPLACE FUNCTION atlas_set_xid_version()
RETURNS trigger AS $$
BEGIN
    NEW := jsonb_populate_record(
        NEW,
        jsonb_build_object(TG_ARGV[0], txid_current()::text)
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER atlas_businesses_version
BEFORE INSERT OR UPDATE ON "Businesses"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('Version');

CREATE TRIGGER atlas_knowledgepacks_version
BEFORE INSERT OR UPDATE ON "KnowledgePacks"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('Version');

CREATE TRIGGER atlas_knowledgepackversions_version
BEFORE INSERT OR UPDATE ON "KnowledgePackVersions"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('ConcurrencyVersion');

CREATE TRIGGER atlas_knowledgesections_version
BEFORE INSERT OR UPDATE ON "KnowledgeSections"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('ConcurrencyVersion');

CREATE TRIGGER atlas_businessknowledgeassignments_version
BEFORE INSERT OR UPDATE ON "BusinessKnowledgeAssignments"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('ConcurrencyVersion');

CREATE TRIGGER atlas_opportunities_version
BEFORE INSERT OR UPDATE ON "Opportunities"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('ConcurrencyVersion');

CREATE TRIGGER atlas_executionkits_version
BEFORE INSERT OR UPDATE ON "ExecutionKits"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('ConcurrencyVersion');

CREATE TRIGGER atlas_executionassets_version
BEFORE INSERT OR UPDATE ON "ExecutionAssets"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('ConcurrencyVersion');

CREATE TRIGGER atlas_outcomes_version
BEFORE INSERT OR UPDATE ON "Outcomes"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('ConcurrencyVersion');

CREATE TRIGGER atlas_businessmemoryitems_version
BEFORE INSERT OR UPDATE ON "BusinessMemoryItems"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('ConcurrencyVersion');

CREATE TRIGGER atlas_notificationpreferences_version
BEFORE INSERT OR UPDATE ON "NotificationPreferences"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('ConcurrencyVersion');

CREATE TRIGGER atlas_notificationrecords_version
BEFORE INSERT OR UPDATE ON "NotificationRecords"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('ConcurrencyVersion');
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TRIGGER IF EXISTS atlas_notificationrecords_version ON "NotificationRecords";
DROP TRIGGER IF EXISTS atlas_notificationpreferences_version ON "NotificationPreferences";
DROP TRIGGER IF EXISTS atlas_businessmemoryitems_version ON "BusinessMemoryItems";
DROP TRIGGER IF EXISTS atlas_outcomes_version ON "Outcomes";
DROP TRIGGER IF EXISTS atlas_executionassets_version ON "ExecutionAssets";
DROP TRIGGER IF EXISTS atlas_executionkits_version ON "ExecutionKits";
DROP TRIGGER IF EXISTS atlas_opportunities_version ON "Opportunities";
DROP TRIGGER IF EXISTS atlas_businessknowledgeassignments_version ON "BusinessKnowledgeAssignments";
DROP TRIGGER IF EXISTS atlas_knowledgesections_version ON "KnowledgeSections";
DROP TRIGGER IF EXISTS atlas_knowledgepackversions_version ON "KnowledgePackVersions";
DROP TRIGGER IF EXISTS atlas_knowledgepacks_version ON "KnowledgePacks";
DROP TRIGGER IF EXISTS atlas_businesses_version ON "Businesses";
DROP FUNCTION IF EXISTS atlas_set_xid_version();
""");
    }
}
