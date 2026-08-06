using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260807015000_TodaysFocus")]
public sealed class TodaysFocus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Opportunities",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                WhyItMatters = table.Column<string>(type: "text", nullable: false),
                WhyNow = table.Column<string>(type: "text", nullable: false),
                ExpectedImpact = table.Column<string>(type: "text", nullable: false),
                Effort = table.Column<string>(type: "text", nullable: false),
                Confidence = table.Column<string>(type: "text", nullable: false),
                EvidenceSummary = table.Column<string>(type: "text", nullable: false),
                EvidenceJson = table.Column<string>(type: "jsonb", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                KnowledgePackKey = table.Column<string>(type: "text", nullable: false),
                KnowledgePackVersion = table.Column<string>(type: "text", nullable: false),
                KnowledgePackVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                GoalId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DecidedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                DecisionReason = table.Column<string>(type: "text", nullable: true),
                ConcurrencyVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Opportunities", x => x.Id);
                table.ForeignKey("FK_Opportunities_Businesses_BusinessId", x => x.BusinessId, "Businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Opportunities_KnowledgePackVersions_KnowledgePackVersionId", x => x.KnowledgePackVersionId, "KnowledgePackVersions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Opportunities_BusinessGoals_GoalId", x => x.GoalId, "BusinessGoals", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex("IX_Opportunities_BusinessId_CreatedAt", "Opportunities", new[] { "BusinessId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_Opportunities_KnowledgePackVersionId", "Opportunities", "KnowledgePackVersionId");
        migrationBuilder.CreateIndex("IX_Opportunities_GoalId", "Opportunities", "GoalId");
        migrationBuilder.CreateIndex(
            name: "IX_Opportunities_BusinessId_Current",
            table: "Opportunities",
            column: "BusinessId",
            unique: true,
            filter: "\"Status\" = 'available'");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "Opportunities");
}
