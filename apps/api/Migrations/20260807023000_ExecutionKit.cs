using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260807023000_ExecutionKit")]
public sealed class ExecutionKitMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExecutionKits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                GoalId = table.Column<Guid>(type: "uuid", nullable: true),
                KnowledgePackKey = table.Column<string>(type: "text", nullable: false),
                KnowledgePackVersion = table.Column<string>(type: "text", nullable: false),
                VersionNumber = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ConcurrencyVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ExecutionKits", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ExecutionAssets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ExecutionKitId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "text", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                Content = table.Column<string>(type: "text", nullable: false),
                IsEditable = table.Column<bool>(type: "boolean", nullable: false),
                IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                CopyCount = table.Column<int>(type: "integer", nullable: false),
                UsefulnessRating = table.Column<int>(type: "integer", nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ConcurrencyVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExecutionAssets", x => x.Id);
                table.ForeignKey("FK_ExecutionAssets_ExecutionKits_ExecutionKitId", x => x.ExecutionKitId, "ExecutionKits", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_ExecutionKits_BusinessId_OpportunityId", "ExecutionKits", new[] { "BusinessId", "OpportunityId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ExecutionAssets_ExecutionKitId_Type", "ExecutionAssets", new[] { "ExecutionKitId", "Type" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ExecutionAssets");
        migrationBuilder.DropTable(name: "ExecutionKits");
    }
}
