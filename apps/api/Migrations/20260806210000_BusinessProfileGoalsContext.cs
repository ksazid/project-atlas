using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260806210000_BusinessProfileGoalsContext")]
public sealed class BusinessProfileGoalsContext : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BusinessProfiles",
            columns: table => new
            {
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                Address = table.Column<string>(type: "text", nullable: true),
                Website = table.Column<string>(type: "text", nullable: true),
                Phone = table.Column<string>(type: "text", nullable: true),
                Email = table.Column<string>(type: "text", nullable: true),
                SocialChannels = table.Column<string>(type: "text", nullable: true),
                BusinessHours = table.Column<string>(type: "text", nullable: true),
                Language = table.Column<string>(type: "text", nullable: false),
                Source = table.Column<string>(type: "text", nullable: false),
                OwnerConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_BusinessProfiles", x => x.BusinessId));

        migrationBuilder.CreateTable(
            name: "BusinessGoals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "text", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                IsCustom = table.Column<bool>(type: "boolean", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_BusinessGoals", x => x.Id));

        migrationBuilder.CreateTable(
            name: "BusinessContextEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "text", nullable: false),
                Value = table.Column<string>(type: "text", nullable: false),
                Source = table.Column<string>(type: "text", nullable: false),
                OwnerConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_BusinessContextEntries", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_BusinessGoals_BusinessId_Priority",
            table: "BusinessGoals",
            columns: new[] { "BusinessId", "Priority" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BusinessContextEntries_BusinessId_Key",
            table: "BusinessContextEntries",
            columns: new[] { "BusinessId", "Key" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BusinessContextEntries");
        migrationBuilder.DropTable(name: "BusinessGoals");
        migrationBuilder.DropTable(name: "BusinessProfiles");
    }
}
