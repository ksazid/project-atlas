using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260807135000_ActionDecisions")]
public sealed class ActionDecisionsMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ActionDecisionRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                GoalId = table.Column<Guid>(type: "uuid", nullable: true),
                KnowledgePackVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                KnowledgePackKey = table.Column<string>(type: "text", nullable: false),
                KnowledgePackVersion = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                ReasonCode = table.Column<string>(type: "text", nullable: true),
                OwnerNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                DecidedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                OpportunityVersionBeforeDecision = table.Column<uint>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ActionDecisionRecords", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_ActionDecisionRecords_BusinessId_OpportunityId_DecidedAt",
            table: "ActionDecisionRecords",
            columns: new[] { "BusinessId", "OpportunityId", "DecidedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ActionDecisionRecords_OpportunityId_DecidedAt",
            table: "ActionDecisionRecords",
            columns: new[] { "OpportunityId", "DecidedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ActionDecisionRecords");
    }
}
