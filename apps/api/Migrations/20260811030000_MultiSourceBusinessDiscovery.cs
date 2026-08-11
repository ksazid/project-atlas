using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260811030000_MultiSourceBusinessDiscovery")]
public sealed class MultiSourceBusinessDiscovery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BusinessDiscoverySources",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                Order = table.Column<int>(type: "integer", nullable: false),
                IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                CanonicalUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                WarningCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                AssociationStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessDiscoverySources", x => x.Id);
                table.ForeignKey(
                    name: "FK_BusinessDiscoverySources_BusinessDiscoverySnapshots_SnapshotId",
                    column: x => x.SnapshotId,
                    principalTable: "BusinessDiscoverySnapshots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BusinessDiscoveryEvidence",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceOrder = table.Column<int>(type: "integer", nullable: false),
                Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                CanonicalUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Confidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                EvidenceClass = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                ReconciliationState = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                AssociationStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessDiscoveryEvidence", x => x.Id);
                table.ForeignKey(
                    name: "FK_BusinessDiscoveryEvidence_BusinessDiscoverySnapshots_SnapshotId",
                    column: x => x.SnapshotId,
                    principalTable: "BusinessDiscoverySnapshots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BusinessDiscoveryEvidence_BusinessDiscoverySources_SourceId",
                    column: x => x.SourceId,
                    principalTable: "BusinessDiscoverySources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BusinessDiscoverySources_SnapshotId_Order",
            table: "BusinessDiscoverySources",
            columns: new[] { "SnapshotId", "Order" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_BusinessDiscoveryEvidence_SnapshotId_Key",
            table: "BusinessDiscoveryEvidence",
            columns: new[] { "SnapshotId", "Key" });
        migrationBuilder.CreateIndex(
            name: "IX_BusinessDiscoveryEvidence_SourceId",
            table: "BusinessDiscoveryEvidence",
            column: "SourceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BusinessDiscoveryEvidence");
        migrationBuilder.DropTable(name: "BusinessDiscoverySources");
    }
}
