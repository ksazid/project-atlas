# VS-24 Category Intelligence End-to-End Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Validate the integrated Restaurant/Café hero journey and make bounded cross-slice fixes so discovery through evidence-aware Today/Detail/Execution behaves truthfully and safely.

**Architecture:** Keep existing modular boundaries. Add mobile Today state handling immediately because it depends only on VS-23’s stable API contract. Defer multi-source journey integration tests and public-Bolt smoke until VS-22 and VS-23 are merged/reconciled into this branch. Use deterministic fixture/service tests for CI and live public sources only as supplemental isolated-environment smoke evidence.

**Tech Stack:** Expo React Native / TypeScript / Node test runner / ASP.NET Core / EF Core / xUnit / PostgreSQL / GitHub Actions / Render test environment.

## Global Constraints

- No redesign; preserve ATLAS-DESIGN-001.
- No production deployment.
- Customer UI remains provider-neutral.
- No generic 5xx allowed on a valid hero-path action.
- No filler recommendation.
- Live third-party URLs never become deterministic CI dependencies.
- VS-24 cannot certify before VS-22 + VS-23 are integrated onto current `main`.
- Final exact-SHA CI, Security, Product Intake and live isolated smoke evidence are required before certification.

---

### Task 1: Mobile Today state contract

**Files:**
- Modify: `apps/mobile/src/api/atlas-client.ts`
- Create: `tests/mobile/today-focus-states.test.mjs`

**Interfaces:**
- `TodayFocus = ready | insufficient-context | no-focus | degraded`
- Non-ready server states carry optional stable `code` and required `message`.

- [ ] **Step 1: Write RED tests** that statically/type-contract assert all four states and prevent `no-focus`/`degraded` from being represented as ready.
- [ ] **Step 2: Run mobile tests and verify RED.**
- [ ] **Step 3: Extend the union minimally.**
- [ ] **Step 4: Run mobile tests/typecheck and verify GREEN.**
- [ ] **Step 5: Commit.**

### Task 2: Today no-focus/degraded UX

**Files:**
- Modify: `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`
- Modify: `tests/mobile/today-focus-states.test.mjs`

**Interfaces:**
- `no-focus` renders a non-error evidence-threshold empty state without decision controls.
- `degraded` renders safe retry/context recovery distinct from network error.

- [ ] **Step 1: Add RED assertions** for explicit state branches, accessible headings/live region, recovery controls, no provider copy and no decision CTA in no-focus/degraded content.
- [ ] **Step 2: Verify RED.**
- [ ] **Step 3: Implement state panels using existing tokens/styles; no layout redesign.**
- [ ] **Step 4: Run typecheck/lint/tests and verify GREEN.**
- [ ] **Step 5: Commit.**

### Task 3: Rendered Today runtime acceptance

**Files:**
- Create: `tests/mobile/today-focus-runtime.test.mjs` or extend an existing compatible runtime harness.

**Interfaces:**
- Controlled API fixture responses for `no-focus`, `degraded`, and `ready`.

- [ ] **Step 1: Add RED runtime assertions** proving state-specific visible copy/actions and ready → detail navigation.
- [ ] **Step 2: Verify RED against existing rendered app.**
- [ ] **Step 3: Add only required test-harness fixture support; do not alter product behavior to satisfy the test.**
- [ ] **Step 4: Verify GREEN and authentic runtime preflight.**
- [ ] **Step 5: Commit.**

### Task 4: Reconcile VS-22 + VS-23 into VS-24 baseline

**Files:** dependency integration only.

- [ ] **Step 1: Wait until VS-22 and VS-23 are merged/certified on `main`.**
- [ ] **Step 2: Update VS-24 branch to current `main`, resolving only real conflicts.**
- [ ] **Step 3: Confirm VS-22 confirmation regression tests and VS-23 opportunity tests are present.**
- [ ] **Step 4: Run full preflight/API tests before adding journey assertions.**
- [ ] **Step 5: Commit conflict/integration resolution if required.**

### Task 5: Deterministic Restaurant/Café journey integration

**Files:**
- Create: `tests/api/CategoryIntelligenceJourneyTests.cs`
- Modify production files only if the RED journey exposes a real cross-slice defect.

**Interfaces:**
- Consumes VS-22 fixture/reconciliation services, Business creation, progressive Context, VS-20 resolver, VS-23 focus service, Opportunity Detail and Execution Kit services/endpoints.

- [ ] **Step 1: Write RED journey test** for controlled multi-source Restaurant/Café facts → consumed snapshot/business → canonical context → goal → resolved bundle → Restaurant opportunity → detail → execution template.
- [ ] **Step 2: Add RED cases** for valid confirmation without 5xx, unrelated-source contamination prevention and Business isolation.
- [ ] **Step 3: Verify RED for actual integration gaps only.**
- [ ] **Step 4: Fix each production integration defect minimally with focused regression tests.**
- [ ] **Step 5: Verify GREEN on journey + full API suite.**
- [ ] **Step 6: Commit.**

### Task 6: Regression matrix for observed live defects

**Files:** existing/new focused tests as appropriate.

- [ ] **Step 1: Verify automated regression exists for merchant identity vs generic marketplace brand.**
- [ ] **Step 2: Verify marketplace boilerplate description cleanup.**
- [ ] **Step 3: Verify location → country/timezone/currency and Google `timeZone.id`.**
- [ ] **Step 4: Verify Google share URLs bypass ordinary scraper safely.**
- [ ] **Step 5: Verify safe Back navigation.**
- [ ] **Step 6: Verify Confirm-and-continue consumes valid snapshot without 5xx.**
- [ ] **Step 7: Verify Opportunity EF model/migration and Detail structured JSON regression.**
- [ ] **Step 8: Add only missing cross-boundary regressions and rerun full suite.**

### Task 7: Final deterministic gates

**Files:**
- Update: `docs/slices/VS-24.md`
- Update: `delivery/current-slice.json`
- Create: final evidence doc if repository convention requires it.

- [ ] **Step 1: Run branch scope/diff review.**
- [ ] **Step 2: Run exact-head CI.**
- [ ] **Step 3: Run Security baseline.**
- [ ] **Step 4: Run Product Intake/governance.**
- [ ] **Step 5: Fix every failure and repeat until all green.**

### Task 8: Isolated public-Bolt smoke

**Files:** no production code unless a live failure is reproduced by a new deterministic RED test first.

- [ ] **Step 1: Point `atlas/test-deployment` to the exact deterministic candidate SHA only after gates pass.**
- [ ] **Step 2: Deploy isolated Render test backend; no production release.**
- [ ] **Step 3: Validate a supplied Bolt Food Restaurant/Café URL through discovery, location, confirmation, questions/goals, Today, Detail and Execution.**
- [ ] **Step 4: Inspect Render logs for 5xx/provider failures.**
- [ ] **Step 5: If a defect appears, reproduce it with RED test, fix, rerun all gates, redeploy and repeat smoke.**
- [ ] **Step 6: Record final exact-SHA smoke evidence.**

### Task 9: Certification and merge handoff

- [ ] **Step 1: Freeze final exact SHA after deterministic gates + live smoke are green.**
- [ ] **Step 2: Request the mandatory exact-SHA human certification/merge approval.**
- [ ] **Step 3: Record certification evidence without rewriting previous history.**
- [ ] **Step 4: Merge only after exact-SHA approval.**
- [ ] **Step 5: Do not production-deploy as part of the merge.**