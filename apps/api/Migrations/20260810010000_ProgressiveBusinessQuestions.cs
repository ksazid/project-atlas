using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260810010000_ProgressiveBusinessQuestions")]
public sealed class ProgressiveBusinessQuestions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BusinessQuestionProgress",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                CatalogueKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                CatalogueVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                QuestionKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                AnsweredContextKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ConcurrencyVersion = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessQuestionProgress", x => x.Id);
                table.ForeignKey(
                    name: "FK_BusinessQuestionProgress_Businesses_BusinessId",
                    column: x => x.BusinessId,
                    principalTable: "Businesses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BusinessQuestionProgress_BusinessId_CatalogueKey_CatalogueVersion_QuestionKey",
            table: "BusinessQuestionProgress",
            columns: new[] { "BusinessId", "CatalogueKey", "CatalogueVersion", "QuestionKey" },
            unique: true);

        migrationBuilder.Sql("""
CREATE TRIGGER atlas_businessquestionprogress_version
BEFORE INSERT OR UPDATE ON "BusinessQuestionProgress"
FOR EACH ROW EXECUTE FUNCTION atlas_set_xid_version('ConcurrencyVersion');
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
DROP TRIGGER IF EXISTS atlas_businessquestionprogress_version ON "BusinessQuestionProgress";
""");
        migrationBuilder.DropTable(name: "BusinessQuestionProgress");
    }
}
