using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260807001500_KnowledgePackFoundation")]
public sealed class KnowledgePackFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "KnowledgePack",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "text", nullable: false),
                Version = table.Column<string>(type: "text", nullable: false),
                DisplayName = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_KnowledgePack", x => x.Id));

        migrationBuilder.CreateTable(
            name: "BusinessKnowledgePack",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                KnowledgePackId = table.Column<Guid>(type: "uuid", nullable: false),
                PackKey = table.Column<string>(type: "text", nullable: false),
                PackVersion = table.Column<string>(type: "text", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessKnowledgePack", x => x.Id);
                table.ForeignKey("FK_BusinessKnowledgePack_Businesses_BusinessId", x => x.BusinessId, "Businesses", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_BusinessKnowledgePack_KnowledgePack_KnowledgePackId", x => x.KnowledgePackId, "KnowledgePack", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_KnowledgePack_Key_Version", "KnowledgePack", new[] { "Key", "Version" }, unique: true);
        migrationBuilder.CreateIndex("IX_BusinessKnowledgePack_KnowledgePackId", "BusinessKnowledgePack", "KnowledgePackId");
        migrationBuilder.CreateIndex("IX_BusinessKnowledgePack_BusinessId_IsActive", "BusinessKnowledgePack", new[] { "BusinessId", "IsActive" });
        migrationBuilder.CreateIndex("IX_BusinessKnowledgePack_BusinessId_PackKey_PackVersion", "BusinessKnowledgePack", new[] { "BusinessId", "PackKey", "PackVersion" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BusinessKnowledgePack");
        migrationBuilder.DropTable(name: "KnowledgePack");
    }
}
