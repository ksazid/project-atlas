using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260806152000_InitialIdentityAndBusiness")]
public sealed class InitialIdentityAndBusiness : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Businesses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Category = table.Column<string>(type: "text", nullable: false),
                Country = table.Column<string>(type: "text", nullable: false),
                Timezone = table.Column<string>(type: "text", nullable: false),
                Currency = table.Column<string>(type: "text", nullable: false),
                PrimaryLocation = table.Column<string>(type: "text", nullable: false),
                OperatingStatus = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Businesses", x => x.Id));

        migrationBuilder.CreateTable(
            name: "UserAccounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProviderSubject = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_UserAccounts", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AuditRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                Action = table.Column<string>(type: "text", nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AuditRecords", x => x.Id));

        migrationBuilder.CreateTable(
            name: "BusinessMemberships",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Role = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessMemberships", x => x.Id);
                table.ForeignKey(
                    name: "FK_BusinessMemberships_UserAccounts_UserAccountId",
                    column: x => x.UserAccountId,
                    principalTable: "UserAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditRecords_BusinessId_OccurredAt",
            table: "AuditRecords",
            columns: new[] { "BusinessId", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_BusinessMemberships_BusinessId_UserAccountId_Role",
            table: "BusinessMemberships",
            columns: new[] { "BusinessId", "UserAccountId", "Role" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BusinessMemberships_UserAccountId",
            table: "BusinessMemberships",
            column: "UserAccountId");

        migrationBuilder.CreateIndex(
            name: "IX_UserAccounts_ProviderSubject",
            table: "UserAccounts",
            column: "ProviderSubject",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuditRecords");
        migrationBuilder.DropTable(name: "BusinessMemberships");
        migrationBuilder.DropTable(name: "Businesses");
        migrationBuilder.DropTable(name: "UserAccounts");
    }
}
