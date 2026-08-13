# VS-38 Google Drive Operational Signals Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect one Viewer-shared Google Drive folder, sync new/changed CSV exports into PII-free normalized Business Signals and Changes, and expose connector control under Profile with device upload as fallback.

**Architecture:** Keep the modular monolith. A provider-neutral operational ingestion service consumes transient streams from either a folder-scoped Google Drive adapter or authenticated device upload, validates and previews bounded CSV input, then persists deterministic aggregate signals, changes, file checkpoints and provenance. A single leased sync service powers both `Sync now` and scheduled polling; the mobile app never receives connector credentials or raw Drive content.

**Tech Stack:** ASP.NET Core 10 minimal APIs, EF Core 10/PostgreSQL, typed `HttpClient`, hosted background service, Expo SDK 54/React Native, Node test runner, xUnit.

## Global Constraints

- Primary pilot source is exactly one Google Drive folder shared Viewer-only with the Atlas connector identity; no public link and no whole-Drive OAuth grant.
- Query direct `.csv` children only; ignore nested folders, shortcuts, Google Sheets, XLSX and unrelated files.
- Manual `Sync now` and scheduled polling use the same idempotent path; no Drive webhooks.
- Raw CSV bytes/rows are transient only and never enter durable storage, logs, analytics, Business Memory or model input.
- Never persist customer names, phone numbers, emails, delivery addresses, notes, PAN/CVV-like values or customer-level profiles.
- Maximum file size is 10 MiB and maximum parsed rows are 100,000.
- Durable normalized metrics use only the approved catalogue and preserve Business isolation, provenance and freshness.
- Device CSV upload remains a secondary fallback.
- Preserve Today / History / Goals / Profile navigation and Business Hub as Profile root.
- No external write action, production deployment, release, EAS/OTA or production database mutation.

---

### Task 1: Activate governed VS-38 scope

**Files:**
- Modify: `delivery/current-slice.json`
- Modify: `delivery/completed-slices.json`
- Modify: `delivery/decisions.json`
- Create: `docs/slices/VS-38.md`
- Modify: `README.md`

**Interfaces:**
- Consumes: approved DEC-12 and merged VS-37 baseline `f75ce6142e88230220042c8d448111c562eb9ebb`.
- Produces: active `VS-38@1.0`, `runtime-enabled`, typed scope/policy/implementation approvals and exact allowed paths.

- [ ] Record VS-37 in completed slices without changing its certification evidence.
- [ ] Replace current slice with VS-38 at lifecycle `implementing`, risk `high`, requirements `FR-05`, `FR-07`, `FR-16`, decision `DEC-12`, and allowed API/mobile/test/delivery/docs/README paths.
- [ ] Record scope, policy and implementation approval by `ksazid` from the explicit 2026-08-13 Level 2 approval; keep certification/release/production pending.
- [ ] Write `docs/slices/VS-38.md` acceptance, privacy, security, rollback and evidence sections.
- [ ] Run `npm run governance:validate && npm run slice:validate`; expected exit 0.
- [ ] Commit governance/spec/plan activation as `docs(vs38): approve folder-scoped Drive pilot`.

### Task 2: Establish operational persistence contracts

**Files:**
- Create: `apps/api/OperationalSignals.cs`
- Modify: `apps/api/AtlasDomain.cs`
- Create: `apps/api/Migrations/20260813070000_OperationalSignals.cs`
- Create: `tests/api/OperationalSignalPersistenceTests.cs`

**Interfaces:**
- Produces: `OperationalConnector`, `OperationalFileCheckpoint`, `OperationalImport`, `BusinessSignal`, `BusinessChange`; stable metric/source/status catalogues; deterministic unique indexes.

- [ ] Write failing persistence tests proving Business scoping, one connector per Business, unique Drive file identity, unique signal identity, decimal precision and no raw payload fields.
- [ ] Run `dotnet test tests/api/Atlas.Api.Tests.csproj --filter OperationalSignalPersistenceTests`; expect failures for missing types/sets.
- [ ] Add minimal entities and EF mappings, including cascade boundaries and indexes.
- [ ] Add a forward-only migration creating connector, checkpoint, import, signal and change tables without raw CSV columns.
- [ ] Re-run the focused tests; expect pass.
- [ ] Commit as `feat(vs38): add operational signal persistence`.

### Task 3: Build the privacy-safe CSV preview and normalizer

**Files:**
- Create: `apps/api/OperationalCsv.cs`
- Create: `tests/api/OperationalCsvTests.cs`
- Create: `tests/api/Fixtures/operational/*.csv`

**Interfaces:**
- Produces: `OperationalCsvReader.PreviewAsync(Stream, Business, CancellationToken)` returning recognized/ignored columns, date range, row/order counts, metric keys and SHA-256 fingerprint; `NormalizeAsync` returning approved aggregate observations only.

- [ ] Write failing tests for aliases, quoted CSV, locale-safe dates/decimals, 10 MiB and 100,000-row limits, mixed currencies, ignored PII, rejected PAN/CVV columns and derivable metrics.
- [ ] Run focused tests and verify RED failures are caused by the missing reader.
- [ ] Implement bounded streaming parsing with no raw-row persistence or logging.
- [ ] Add deterministic preview fingerprint and approved metric catalogue enforcement.
- [ ] Re-run focused tests; expect pass and no fixture/customer values in test logs.
- [ ] Commit as `feat(vs38): normalize privacy-safe operational CSV`.

### Task 4: Persist idempotent signals and derive changes

**Files:**
- Create: `apps/api/OperationalIngestionService.cs`
- Create: `tests/api/OperationalIngestionServiceTests.cs`

**Interfaces:**
- Consumes: normalized observations from `OperationalCsvReader`.
- Produces: duplicate-safe `OperationalImport`, `BusinessSignal`, 7-day/28-day `BusinessChange`, overlap-conflict result and freshness classification.

- [ ] Write failing tests for same-file duplicate, byte-different normalized duplicate, conflicting overlap, 7-day/28-day comparisons, zero comparison, insufficient coverage and freshness thresholds.
- [ ] Verify focused RED.
- [ ] Implement one database transaction per confirmed import with stable SHA-256 signal identities.
- [ ] Derive changes only from complete matching windows and preserve underlying signal IDs.
- [ ] Re-run focused tests; expect pass.
- [ ] Commit as `feat(vs38): persist signals and derive changes`.

### Task 5: Implement folder-scoped Google Drive adapter

**Files:**
- Create: `apps/api/GoogleDriveOperationalSource.cs`
- Modify: `apps/api/Program.cs`
- Modify: `apps/api/appsettings.json`
- Create: `tests/api/GoogleDriveOperationalSourceTests.cs`

**Interfaces:**
- Produces: `IOperationalFileSource.ListAsync(folderId)` and `OpenReadAsync(fileId)`; verifies folder metadata and Viewer-only capability; returns only direct CSV children with file ID, modified time, size and checksum metadata.

- [ ] Write failing handler-backed tests proving only the configured folder is queried, `trashed=false`, direct CSV filtering, no write methods, revoked/not-found mapping and response redaction.
- [ ] Verify focused RED.
- [ ] Implement service-account access-token acquisition through a credential provider that reads server-side configuration only; never serialize credentials into API responses.
- [ ] Implement Drive Files `get/list/download` calls with explicit field masks and bounded streams.
- [ ] Re-run focused tests; expect pass.
- [ ] Commit as `feat(vs38): add read-only Drive folder source`.

### Task 6: Add connector commands, sync lease and scheduler

**Files:**
- Create: `apps/api/OperationalConnectorService.cs`
- Create: `apps/api/OperationalSyncWorker.cs`
- Modify: `apps/api/Program.cs`
- Create: `tests/api/OperationalConnectorServiceTests.cs`
- Create: `tests/api/OperationalConnectorEndpointTests.cs`

**Interfaces:**
- Produces owner-only status/connect/disconnect/schedule/sync endpoints; one idempotent `SyncBusinessAsync` path shared by manual and scheduled execution.

- [ ] Write failing authorization/isolation tests and service tests for connect validation, changed-file detection using ID+metadata+fingerprint, unchanged skip, changed process, lease contention and revoked grant.
- [ ] Verify focused RED.
- [ ] Implement owner-only endpoints and audit events without exposing folder credentials.
- [ ] Implement database-backed lease and bounded scheduled polling with cancellation and backoff.
- [ ] Re-run focused tests; expect pass.
- [ ] Commit as `feat(vs38): add persistent Drive sync orchestration`.

### Task 7: Add fallback device preview and confirmation endpoints

**Files:**
- Create: `apps/api/OperationalUploadEndpoints.cs`
- Modify: `apps/api/Program.cs`
- Create: `tests/api/OperationalUploadEndpointTests.cs`

**Interfaces:**
- Produces multipart preview endpoint and confirmation bound to a short-lived server-side preview fingerprint; no durable raw upload.

- [ ] Write failing owner isolation, size, schema, preview/confirm mismatch and raw-retention tests.
- [ ] Verify focused RED.
- [ ] Implement bounded multipart stream preview and confirmation using the common ingestion service.
- [ ] Re-run focused tests; expect pass.
- [ ] Commit as `feat(vs38): add device CSV fallback`.

### Task 8: Add the Profile Business Data connector experience

**Files:**
- Create: `apps/mobile/app/business-data.tsx`
- Create: `apps/mobile/src/features/operational-data/operational-data-api.ts`
- Create: `apps/mobile/src/features/operational-data/operational-data-model.ts`
- Create: `apps/mobile/src/features/operational-data/OperationalDataScreen.tsx`
- Modify: `apps/mobile/src/features/business-hub/BusinessHubScreen.tsx`
- Create: `tests/mobile/vs38-operational-data-model.test.mjs`
- Create: `tests/mobile/vs38-operational-data-ui.test.mjs`

**Interfaces:**
- Produces prominent Profile `Business data` card and accessible connector screen with connect guidance, folder/status/freshness, schedule, Sync now, reconnect/disconnect and secondary device upload.

- [ ] Write failing model/UI source-contract tests for disconnected, connected, syncing, stale, reauthorization and error states plus accessibility labels and 44pt targets.
- [ ] Verify Node tests RED.
- [ ] Implement API/model/screen using existing Atlas surfaces, tokens and navigation; no new tab or chart dashboard.
- [ ] Re-run focused tests; expect pass.
- [ ] Commit as `feat(vs38): add Business Data connector experience`.

### Task 9: Project operational evidence into intelligence

**Files:**
- Modify: `apps/api/KnowledgeBundleResolver.cs`
- Modify: `apps/api/OpportunityGeneration.cs`
- Create: `tests/api/OperationalEvidenceProjectionTests.cs`
- Modify: `tests/api/OpportunityReadinessRegressionTests.cs`

**Interfaces:**
- Produces provider-neutral evidence facts for eligible fresh/stale signals and changes, retaining signal IDs and excluding >30-day why-now evidence.

- [ ] Write failing evidence projection and no-filler regression tests.
- [ ] Verify focused RED.
- [ ] Implement projection without changing ranking, scoring, cooldown or adding restaurant-only patterns.
- [ ] Re-run focused and Opportunity regression tests; expect pass.
- [ ] Commit as `feat(vs38): expose operational evidence to Today`.

### Task 10: Verify, document and stop at certification approval

**Files:**
- Modify: `docs/slices/VS-38.md`
- Modify: `README.md`
- Modify only after evidence: `delivery/current-slice.json`

**Interfaces:**
- Produces tested runtime head and evidence package; certification remains pending until exact-SHA human approval.

- [ ] Run migration registry and clean PostgreSQL replay.
- [ ] Run all focused and full API/mobile test suites, TypeScript, Expo lint, build, governance, slice validation and `npm run preflight`.
- [ ] Inspect `git diff --check`, secret scan and changed-file review for raw content/token leakage.
- [ ] Record exact commands/results and runtime SHA in VS-38 evidence without marking certification approved.
- [ ] Push/update PR #62 only after local verification; do not merge, release or deploy.
- [ ] Stop for exact-SHA certification/merge approval.
