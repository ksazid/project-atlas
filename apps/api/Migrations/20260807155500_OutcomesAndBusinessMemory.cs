using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260807155500_OutcomesAndBusinessMemory")]
public sealed class OutcomesAndBusinessMemoryMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Outcomes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                GoalId = table.Column<Guid>(type: "uuid", nullable: true),
                KnowledgePackVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                KnowledgePackKey = table.Column<string>(type: "text", nullable: false),
                KnowledgePackVersion = table.Column<string>(type: "text", nullable: false),
                UsefulnessRating = table.Column<int>(type: "integer", nullable: false),
                ResultSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                TimeSpentMinutes = table.Column<int>(type: "integer", nullable: false),
                OwnerNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                MeasureName = table.Column<string>(type: "text", nullable: true),
                MeasureValue = table.Column<decimal>(type: "numeric", nullable: true),
                MeasureUnit = table.Column<string>(type: "text", nullable: true),
                EvidenceClass = table.Column<string>(type: "text", nullable: false),
                FollowUpAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CapturedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ConcurrencyVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Outcomes", x => x.Id));

        migrationBuilder.CreateTable(
            name: "BusinessMemoryItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                StableKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Category = table.Column<string>(type: "text", nullable: false),
                SourceType = table.Column<string>(type: "text", nullable: false),
                SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                IsDeletable = table.Column<bool>(type: "boolean", nullable: false),
                CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ConcurrencyVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_BusinessMemoryItems", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Outcomes_BusinessId_OpportunityId",
            table: "Outcomes",
            columns: new[] { "BusinessId", "OpportunityId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Outcomes_BusinessId_FollowUpAt",
            table: "Outcomes",
            columns: new[] { "BusinessId", "FollowUpAt" });

        migrationBuilder.CreateIndex(
            name: "IX_BusinessMemoryItems_BusinessId_StableKey",
            table: "BusinessMemoryItems",
            columns: new[] { "BusinessId", "StableKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BusinessMemoryItems_BusinessId_Category_UpdatedAt",
            table: "BusinessMemoryItems",
            columns: new[] { "BusinessId", "Category", "UpdatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BusinessMemoryItems");
        migrationBuilder.DropTable(name: "Outcomes");
    }
}
