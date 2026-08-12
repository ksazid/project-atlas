using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260812150000_PilotOperations")]
public sealed class PilotOperations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IntelligenceRuns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                Outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                CandidateCount = table.Column<int>(type: "integer", nullable: false),
                OpportunityId = table.Column<Guid>(type: "uuid", nullable: true),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IntelligenceRuns", x => x.Id);
                table.ForeignKey(
                    name: "FK_IntelligenceRuns_Businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "Businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_IntelligenceRuns_Opportunities_OpportunityId",
                    column: x => x.OpportunityId,
                    principalTable: "Opportunities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_IntelligenceRuns_UserAccounts_ActorUserAccountId",
                    column: x => x.ActorUserAccountId,
                    principalTable: "UserAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PilotOperationRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                OperatorUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                TargetType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PilotOperationRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_PilotOperationRecords_Businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "Businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PilotOperationRecords_UserAccounts_OperatorUserAccountId",
                    column: x => x.OperatorUserAccountId,
                    principalTable: "UserAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IntelligenceRuns_ActorUserAccountId",
            table: "IntelligenceRuns",
            column: "ActorUserAccountId");

        migrationBuilder.CreateIndex(
            name: "IX_IntelligenceRuns_BusinessId_OccurredAt",
            table: "IntelligenceRuns",
            columns: new[] { "BusinessId", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_IntelligenceRuns_OpportunityId",
            table: "IntelligenceRuns",
            column: "OpportunityId");

        migrationBuilder.CreateIndex(
            name: "IX_PilotOperationRecords_BusinessId_OccurredAt",
            table: "PilotOperationRecords",
            columns: new[] { "BusinessId", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_PilotOperationRecords_OperatorUserAccountId",
            table: "PilotOperationRecords",
            column: "OperatorUserAccountId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "IntelligenceRuns");
        migrationBuilder.DropTable(name: "PilotOperationRecords");
    }
}
