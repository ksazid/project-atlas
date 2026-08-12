using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260812113000_FeedbackSupport")]
public sealed class FeedbackSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FeedbackRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                SubmittedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                OpportunityId = table.Column<Guid>(type: "uuid", nullable: true),
                ContextKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                Usefulness = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                Message = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeedbackRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_FeedbackRecords_Businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "Businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_FeedbackRecords_Opportunities_OpportunityId",
                    column: x => x.OpportunityId,
                    principalTable: "Opportunities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_FeedbackRecords_UserAccounts_SubmittedByAccountId",
                    column: x => x.SubmittedByAccountId,
                    principalTable: "UserAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FeedbackRecords_BusinessId_CreatedAt",
            table: "FeedbackRecords",
            columns: new[] { "BusinessId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_FeedbackRecords_OpportunityId",
            table: "FeedbackRecords",
            column: "OpportunityId");

        migrationBuilder.CreateIndex(
            name: "IX_FeedbackRecords_SubmittedByAccountId",
            table: "FeedbackRecords",
            column: "SubmittedByAccountId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FeedbackRecords");
    }
}