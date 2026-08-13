using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Atlas.Api.Migrations;

[DbContext(typeof(AtlasDbContext))]
[Migration("20260813070000_OperationalSignals")]
public sealed class OperationalSignals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE "OperationalConnectors" ("Id" uuid PRIMARY KEY, "BusinessId" uuid NOT NULL REFERENCES "Businesses"("Id") ON DELETE CASCADE, "SourceKind" varchar(40) NOT NULL, "FolderId" varchar(200) NOT NULL, "FolderName" varchar(240) NOT NULL, "Status" varchar(40) NOT NULL, "Schedule" varchar(40) NOT NULL, "LastAttemptAt" timestamptz NULL, "LastSuccessAt" timestamptz NULL, "LeaseUntil" timestamptz NULL, "ErrorCode" varchar(120) NULL, "CreatedAt" timestamptz NOT NULL, "UpdatedAt" timestamptz NOT NULL);
CREATE UNIQUE INDEX "IX_OperationalConnectors_BusinessId" ON "OperationalConnectors" ("BusinessId");
CREATE TABLE "OperationalFileCheckpoints" ("Id" uuid PRIMARY KEY, "BusinessId" uuid NOT NULL, "ConnectorId" uuid NOT NULL REFERENCES "OperationalConnectors"("Id") ON DELETE CASCADE, "ProviderFileId" varchar(200) NOT NULL, "FileName" varchar(240) NOT NULL, "MimeType" varchar(120) NOT NULL, "Size" bigint NOT NULL, "ProviderModifiedAt" timestamptz NOT NULL, "ContentFingerprint" varchar(128) NOT NULL, "ProcessedAt" timestamptz NOT NULL);
CREATE UNIQUE INDEX "IX_OperationalFileCheckpoints_BusinessId_ProviderFileId" ON "OperationalFileCheckpoints" ("BusinessId", "ProviderFileId");
CREATE TABLE "OperationalImports" ("Id" uuid PRIMARY KEY, "BusinessId" uuid NOT NULL REFERENCES "Businesses"("Id") ON DELETE CASCADE, "ConnectorId" uuid NULL, "FileCheckpointId" uuid NULL, "SourceKind" varchar(40) NOT NULL, "ImportFingerprint" varchar(128) NOT NULL, "Status" varchar(40) NOT NULL, "AcceptedRows" integer NOT NULL, "IgnoredColumns" integer NOT NULL, "EarliestBusinessDate" date NULL, "LatestBusinessDate" date NULL, "CreatedAt" timestamptz NOT NULL, "CompletedAt" timestamptz NOT NULL);
CREATE UNIQUE INDEX "IX_OperationalImports_BusinessId_ImportFingerprint" ON "OperationalImports" ("BusinessId", "ImportFingerprint");
CREATE TABLE "BusinessSignals" ("Id" uuid PRIMARY KEY, "BusinessId" uuid NOT NULL, "OperationalImportId" uuid NOT NULL REFERENCES "OperationalImports"("Id") ON DELETE CASCADE, "Identity" varchar(128) NOT NULL, "MetricKey" varchar(80) NOT NULL, "Value" numeric(18,4) NOT NULL, "Unit" varchar(40) NOT NULL, "Currency" varchar(3) NULL, "PeriodStart" date NOT NULL, "PeriodEnd" date NOT NULL, "DimensionsJson" jsonb NULL, "SourceKind" varchar(40) NOT NULL, "SourceReference" varchar(240) NOT NULL, "ObservedAt" timestamptz NOT NULL, "Confidence" varchar(20) NOT NULL);
CREATE UNIQUE INDEX "IX_BusinessSignals_BusinessId_Identity" ON "BusinessSignals" ("BusinessId", "Identity");
CREATE TABLE "BusinessChanges" ("Id" uuid PRIMARY KEY, "BusinessId" uuid NOT NULL REFERENCES "Businesses"("Id") ON DELETE CASCADE, "Identity" varchar(128) NOT NULL, "MetricKey" varchar(80) NOT NULL, "CurrentValue" numeric(18,4) NOT NULL, "ComparisonValue" numeric(18,4) NOT NULL, "AbsoluteDelta" numeric(18,4) NOT NULL, "RelativeDelta" numeric(18,6) NULL, "CurrentPeriodStart" date NOT NULL, "CurrentPeriodEnd" date NOT NULL, "ComparisonPeriodStart" date NOT NULL, "ComparisonPeriodEnd" date NOT NULL, "EvidenceSignalIdsJson" jsonb NOT NULL, "ObservedAt" timestamptz NOT NULL, "Confidence" varchar(20) NOT NULL);
CREATE UNIQUE INDEX "IX_BusinessChanges_BusinessId_Identity" ON "BusinessChanges" ("BusinessId", "Identity");
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("BusinessChanges");
        migrationBuilder.DropTable("BusinessSignals");
        migrationBuilder.DropTable("OperationalImports");
        migrationBuilder.DropTable("OperationalFileCheckpoints");
        migrationBuilder.DropTable("OperationalConnectors");
    }
}
