using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260807220000_Notifications")]
public sealed class NotificationsMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotificationPreferences",
            columns: table => new
            {
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                TodayFocusEnabled = table.Column<bool>(type: "boolean", nullable: false),
                OutcomeFollowUpEnabled = table.Column<bool>(type: "boolean", nullable: false),
                WeeklyReviewEnabled = table.Column<bool>(type: "boolean", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ConcurrencyVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_NotificationPreferences", x => x.BusinessId));

        migrationBuilder.CreateTable(
            name: "NotificationRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                StableKey = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                DeepLink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ConcurrencyVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_NotificationRecords", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_NotificationRecords_BusinessId_StableKey",
            table: "NotificationRecords",
            columns: new[] { "BusinessId", "StableKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_NotificationRecords_BusinessId_ReadAt_CreatedAt",
            table: "NotificationRecords",
            columns: new[] { "BusinessId", "ReadAt", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "NotificationRecords");
        migrationBuilder.DropTable(name: "NotificationPreferences");
    }
}
