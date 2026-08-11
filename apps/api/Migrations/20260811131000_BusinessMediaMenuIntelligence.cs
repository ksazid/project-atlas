using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260811131000_BusinessMediaMenuIntelligence")]
public sealed class BusinessMediaMenuIntelligence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BusinessDiscoveryMediaReferences",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceOrder = table.Column<int>(type: "integer", nullable: false),
                Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                RemoteUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Confidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                EvidenceClass = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                OwnerConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                AltText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessDiscoveryMediaReferences", x => x.Id);
                table.ForeignKey(
                    name: "FK_BusinessDiscoveryMediaReferences_BusinessDiscoverySnapshots_SnapshotId",
                    column: x => x.SnapshotId,
                    principalTable: "BusinessDiscoverySnapshots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BusinessDiscoveryOfferings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceOrder = table.Column<int>(type: "integer", nullable: false),
                Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Section = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                Name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                Source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Confidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                EvidenceClass = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                OwnerConfirmed = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessDiscoveryOfferings", x => x.Id);
                table.ForeignKey(
                    name: "FK_BusinessDiscoveryOfferings_BusinessDiscoverySnapshots_SnapshotId",
                    column: x => x.SnapshotId,
                    principalTable: "BusinessDiscoverySnapshots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "BusinessMediaReferences",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                SourceOrder = table.Column<int>(type: "integer", nullable: false),
                Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                RemoteUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Confidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                EvidenceClass = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                OwnerConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                AltText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessMediaReferences", x => x.Id);
                table.ForeignKey(
                    name: "FK_BusinessMediaReferences_Businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "Businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BusinessMediaReferences_BusinessDiscoverySnapshots_SourceSnapshotId",
                    column: x => x.SourceSnapshotId,
                    principalTable: "BusinessDiscoverySnapshots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "BusinessOfferings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceSnapshotId = table.Column<Guid>(type: "uuid", nullable: true),
                SourceOrder = table.Column<int>(type: "integer", nullable: false),
                Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Section = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                Name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                Source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Confidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                EvidenceClass = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                OwnerConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessOfferings", x => x.Id);
                table.ForeignKey(
                    name: "FK_BusinessOfferings_Businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "Businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BusinessOfferings_BusinessDiscoverySnapshots_SourceSnapshotId",
                    column: x => x.SourceSnapshotId,
                    principalTable: "BusinessDiscoverySnapshots",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BusinessDiscoveryMediaReferences_SnapshotId",
            table: "BusinessDiscoveryMediaReferences",
            column: "SnapshotId");
        migrationBuilder.CreateIndex(
            name: "IX_BusinessDiscoveryOfferings_SnapshotId",
            table: "BusinessDiscoveryOfferings",
            column: "SnapshotId");
        migrationBuilder.CreateIndex(
            name: "IX_BusinessDiscoveryOfferings_SnapshotId_Kind_Name",
            table: "BusinessDiscoveryOfferings",
            columns: new[] { "SnapshotId", "Kind", "Name" });
        migrationBuilder.CreateIndex(
            name: "IX_BusinessMediaReferences_BusinessId",
            table: "BusinessMediaReferences",
            column: "BusinessId");
        migrationBuilder.CreateIndex(
            name: "IX_BusinessMediaReferences_SourceSnapshotId",
            table: "BusinessMediaReferences",
            column: "SourceSnapshotId");
        migrationBuilder.CreateIndex(
            name: "IX_BusinessOfferings_BusinessId",
            table: "BusinessOfferings",
            column: "BusinessId");
        migrationBuilder.CreateIndex(
            name: "IX_BusinessOfferings_SourceSnapshotId",
            table: "BusinessOfferings",
            column: "SourceSnapshotId");
        migrationBuilder.CreateIndex(
            name: "IX_BusinessOfferings_BusinessId_Kind_Name",
            table: "BusinessOfferings",
            columns: new[] { "BusinessId", "Kind", "Name" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BusinessOfferings");
        migrationBuilder.DropTable(name: "BusinessMediaReferences");
        migrationBuilder.DropTable(name: "BusinessDiscoveryOfferings");
        migrationBuilder.DropTable(name: "BusinessDiscoveryMediaReferences");
    }
}
