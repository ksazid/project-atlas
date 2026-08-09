using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260809213000_BusinessDiscoveryProvenance")]
public sealed class BusinessDiscoveryProvenance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BusinessDiscoverySnapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_BusinessDiscoverySnapshots", x => x.Id));

        migrationBuilder.CreateTable(
            name: "BusinessProfileFields",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Confidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                EvidenceClass = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                OwnerConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_BusinessProfileFields", x => x.Id));

        migrationBuilder.CreateTable(
            name: "BusinessDiscoveryFacts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                Source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Confidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                EvidenceClass = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                OwnerConfirmed = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessDiscoveryFacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_BusinessDiscoveryFacts_BusinessDiscoverySnapshots_SnapshotId",
                    column: x => x.SnapshotId,
                    principalTable: "BusinessDiscoverySnapshots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BusinessDiscoverySnapshots_UserAccountId_CreatedAt",
            table: "BusinessDiscoverySnapshots",
            columns: new[] { "UserAccountId", "CreatedAt" });
        migrationBuilder.CreateIndex(
            name: "IX_BusinessDiscoverySnapshots_BusinessId",
            table: "BusinessDiscoverySnapshots",
            column: "BusinessId");
        migrationBuilder.CreateIndex(
            name: "IX_BusinessDiscoveryFacts_SnapshotId_Key",
            table: "BusinessDiscoveryFacts",
            columns: new[] { "SnapshotId", "Key" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_BusinessProfileFields_BusinessId_Key",
            table: "BusinessProfileFields",
            columns: new[] { "BusinessId", "Key" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BusinessDiscoveryFacts");
        migrationBuilder.DropTable(name: "BusinessProfileFields");
        migrationBuilder.DropTable(name: "BusinessDiscoverySnapshots");
    }
}
