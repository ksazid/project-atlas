# VS-35 Pilot Operations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver FR-18 Pilot Operations with strict internal-operator authority, append-only provenance, safe generation diagnostics, evidence-backed Opportunity preparation and audited withdrawal.

**Architecture:** Bounded `PilotOperations` module in the existing ASP.NET Core modular monolith plus a root Expo Router operator Stack outside owner tabs. Reuse current Business/Profile/Opportunity generator and owner flows; never impersonate owners.

**Tech Stack:** ASP.NET Core / .NET 10 / EF Core / PostgreSQL 17 / xUnit / Expo React Native / TypeScript / Expo Router / Node tests / GitHub Actions.

## Global Constraints
- Base exactly on merged VS-34 main `345045cd40a97daac547aa2b847e793a20bb5fb5`.
- FR-18 only.
- DEC-11: operators never impersonate Business owners.
- Every operator mutation uses `InternalOperator` and persists actor/target/reason/provenance/audit.
- Profile assistance uses `operator-assisted` and `OwnerConfirmed=false`.
- Opportunity preparation reuses the existing generator; no free-form recommendation authoring.
- Unsafe feedback never auto-withdraws content.
- `withdrawn` is terminal.
- Owner tabs remain Today / History / Goals / Profile.
- No new infrastructure, external helpdesk, attachments, release/deployment/EAS/OTA/production DB mutation.
- Old PR #57 is evidence only and never merges.

---

### Task 1: Re-establish persistence and policy on fresh main
**Files:** `apps/api/PilotOperations.cs`, `apps/api/AtlasDomain.cs`, migration, `tests/api/PilotOperationsPolicyTests.cs`, `tests/api/PilotOperationsPersistenceTests.cs`.

- [ ] Write/port the previously proven RED policy/persistence tests onto VS-35.
- [ ] Run exact-head CI and confirm intended failures while inherited tests remain green.
- [ ] Port the minimal compatible persistence/domain implementation from preserved PR #57 only after RED.
- [ ] Run GREEN including clean PostgreSQL 17 migration replay.
- [ ] Commit as VS-35.

Required persisted records: `IntelligenceRunRecord` and `PilotOperationRecord`; Business-scoped indexes; bounded strings; server-generated `jsonb` metadata only.

### Task 2: Generation diagnostics
**Files:** `apps/api/OpportunityFocusService.cs`, focused API tests.

- [ ] RED for `ready`, `insufficient-context`, `no-focus`, `degraded` diagnostics and candidate counts.
- [ ] GREEN by appending exactly one compact diagnostic per completed generation request without raw provider/model/error payloads.
- [ ] Re-run Today generation regressions and commit.

### Task 3: Internal queue/detail, support notes and profile assistance
**Files:** `apps/api/PilotOperations.cs`, `apps/api/Program.cs`, endpoint/service tests.

- [ ] RED for `InternalOperator` authorization, internal-account resolution, bounded queue/detail, support notes and profile correction provenance.
- [ ] GREEN: dedicated operator endpoints; deterministic attention-first ordering; `operator-assisted` profile source; `OwnerConfirmed=false`; audit and intervention records.
- [ ] Verify owners cannot invoke operator routes; commit.

### Task 4: Evidence-backed Opportunity preparation
**Files:** `apps/api/PilotOperations.cs`, Opportunity tests.

- [ ] RED for confirmed Profile/Goals/current Knowledge Pack/factual evidence requirements, stale candidate/current-active conflicts and same-Business isolation.
- [ ] GREEN by regenerating current candidates through the existing Opportunity generator and persisting the normal evidence snapshot plus operator provenance.
- [ ] Verify owner Action authority remains unchanged; commit.

### Task 5: Terminal withdrawal
**Files:** `apps/api/Opportunities.cs`, `apps/api/PilotOperations.cs`, Opportunity policy/integration tests.

- [ ] RED for `withdrawn` status, required reason, version conflict, terminal/no-reopen semantics, Today exclusion and owner non-actionability.
- [ ] GREEN with explicit `InternalOperator` withdrawal command, audit and intervention record.
- [ ] Verify feedback alone never withdraws; commit.

### Task 6: Mobile operator contracts/model
**Files:** `apps/mobile/src/api/atlas-client.ts`, `apps/mobile/src/features/pilot-operations/pilot-operations-model.ts`, mobile tests.

- [ ] RED for API types, source widening and pure presentation states.
- [ ] GREEN with bounded client/model only; no route/UI yet.
- [ ] Run full mobile/type/lint checks; commit.

### Task 7: Operator queue/review UI
**Files:** root `/operator` routes, Pilot Operations screens, mobile tests.

- [ ] RED for root Stack routes outside `(tabs)`, queue states, Business review sections, withdrawal confirmation and no fifth owner tab.
- [ ] GREEN using approved Atlas design language, native back behavior and accessibility conventions.
- [ ] Verify Today/History/Goals/Profile unchanged; commit.

### Task 8: Full verification, certification and merge
- [ ] Review changed-file boundary against FR-18/DEC-11.
- [ ] Run `npm run governance:validate` / `npm run preflight` through CI.
- [ ] Require mobile, TypeScript, Expo lint/authentic runtime, .NET Release build, clean PostgreSQL replay, API suite and dashboard build green.
- [ ] Require exact-head Security baseline + Product Intake green.
- [ ] Freeze runtime SHA, write governance-only PES certification commit, rerun post-cert exact-head gates.
- [ ] Merge only the clean VS-35 PR under the Product Owner's standing approval; close old PR #57 as superseded after successful merge.

## Self-review
- Spec coverage: FR-18 queue/detail, diagnostics, support, profile assistance, collaborative preparation, withdrawal, operator UI and audit all mapped.
- No placeholders/TODOs.
- All new behavior remains TDD-gated.
- No overlap with VS-34 Today runtime files.
