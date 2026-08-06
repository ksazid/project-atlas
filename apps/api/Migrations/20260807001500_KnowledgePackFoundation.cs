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
            name: "KnowledgePacks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                CreatedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Version = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_KnowledgePacks", x => x.Id));

        migrationBuilder.CreateTable(
            name: "KnowledgePackVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                KnowledgePackId = table.Column<Guid>(type: "uuid", nullable: false),
                VersionNumber = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                Locale = table.Column<string>(type: "text", nullable: false),
                CreatedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ReviewedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                PublishedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ConcurrencyVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KnowledgePackVersions", x => x.Id);
                table.ForeignKey("FK_KnowledgePackVersions_KnowledgePacks_KnowledgePackId", x => x.KnowledgePackId, "KnowledgePacks", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "KnowledgeSections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                KnowledgePackVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                StableKey = table.Column<string>(type: "text", nullable: false),
                Category = table.Column<string>(type: "text", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                Content = table.Column<string>(type: "text", nullable: false),
                MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                Order = table.Column<int>(type: "integer", nullable: false),
                Locale = table.Column<string>(type: "text", nullable: false),
                TranslationGroupKey = table.Column<string>(type: "text", nullable: true),
                Source = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ConcurrencyVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KnowledgeSections", x => x.Id);
                table.ForeignKey("FK_KnowledgeSections_KnowledgePackVersions_KnowledgePackVersionId", x => x.KnowledgePackVersionId, "KnowledgePackVersions", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BusinessKnowledgeAssignments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                KnowledgePackId = table.Column<Guid>(type: "uuid", nullable: false),
                KnowledgePackVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                PackKey = table.Column<string>(type: "text", nullable: false),
                ExactVersion = table.Column<string>(type: "text", nullable: false),
                IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                AssignedByUserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ConcurrencyVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessKnowledgeAssignments", x => x.Id);
                table.ForeignKey("FK_BusinessKnowledgeAssignments_Businesses_BusinessId", x => x.BusinessId, "Businesses", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_BusinessKnowledgeAssignments_KnowledgePacks_KnowledgePackId", x => x.KnowledgePackId, "KnowledgePacks", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_BusinessKnowledgeAssignments_KnowledgePackVersions_KnowledgePackVersionId", x => x.KnowledgePackVersionId, "KnowledgePackVersions", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_KnowledgePacks_Key", "KnowledgePacks", "Key", unique: true);
        migrationBuilder.CreateIndex("IX_KnowledgePackVersions_KnowledgePackId_VersionNumber", "KnowledgePackVersions", new[] { "KnowledgePackId", "VersionNumber" }, unique: true);
        migrationBuilder.CreateIndex("IX_KnowledgeSections_KnowledgePackVersionId_StableKey", "KnowledgeSections", new[] { "KnowledgePackVersionId", "StableKey" }, unique: true);
        migrationBuilder.CreateIndex("IX_KnowledgeSections_KnowledgePackVersionId_Order", "KnowledgeSections", new[] { "KnowledgePackVersionId", "Order" }, unique: true);
        migrationBuilder.CreateIndex("IX_BusinessKnowledgeAssignments_KnowledgePackId", "BusinessKnowledgeAssignments", "KnowledgePackId");
        migrationBuilder.CreateIndex("IX_BusinessKnowledgeAssignments_KnowledgePackVersionId", "BusinessKnowledgeAssignments", "KnowledgePackVersionId");
        migrationBuilder.CreateIndex(
            name: "IX_BusinessKnowledgeAssignments_BusinessId_Current",
            table: "BusinessKnowledgeAssignments",
            column: "BusinessId",
            unique: true,
            filter: "\"IsCurrent\" = TRUE");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BusinessKnowledgeAssignments");
        migrationBuilder.DropTable(name: "KnowledgeSections");
        migrationBuilder.DropTable(name: "KnowledgePackVersions");
        migrationBuilder.DropTable(name: "KnowledgePacks");
    }
}
