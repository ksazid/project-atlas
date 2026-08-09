# VS-16 Business Discovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver URL-first Business discovery for public websites/Wolt/Bolt with server-owned provenance, owner confirmation, secure fetching, manual fallback, and an Atlas-aligned onboarding experience.

**Architecture:** Keep the existing modular monolith and discovery endpoint, split provider-neutral discovery policy/parsing/persistence into focused units, persist account-scoped snapshots before Business creation, and add an atomic `from-discovery` creation path that writes canonical Business/Profile values plus provenance. Mobile remains Expo Router and uses the existing BrandMark/tokens.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL, Expo React Native / TypeScript, Node test runner, xUnit, GitHub Actions.

## Global Constraints

- HTTPS public sources only; no private/authenticated connectors.
- Reject localhost/private/reserved network targets and automatic redirects.
- Never fabricate public facts, ratings, reviews, phone, hours, popularity or business performance.
- Public observations require owner confirmation before becoming authoritative Business/Profile values.
- Keep manual Business creation backward compatible.
- Keep BrandMark as the only temporary-mark boundary; no screen-level third-party logo URLs/labels.
- Use shared Atlas design tokens; no new UI dependency.
- Do not implement Knowledge Pack v2, progressive category questions, Restaurant recipes, release or deployment in VS-16.

---

### Task 1: Activate governed VS-16 scope

**Files:**
- Create: `docs/slices/VS-16.md`
- Modify: `delivery/current-slice.json`
- Modify: `delivery/decisions.json`

**Interfaces:**
- Consumes: approved PRD/TRD/DESIGN and `planning/CATEGORY-INTELLIGENCE-FOUNDATION.md`.
- Produces: active runtime-enabled VS-16 governance record with FR-02/FR-03/FR-05 traceability and release/production pending.

- [ ] Write the slice specification with explicit allowed/protected paths, security boundary and acceptance criteria.
- [ ] Record the approved product decision: VS-16 supports ordinary HTTPS business websites plus Wolt/Bolt through a provider-neutral public snapshot; private connectors remain out of scope.
- [ ] Update `delivery/current-slice.json` to VS-16, scope/implementation approved, certification/release/production pending.
- [ ] Run `npm run planning:validate && npm run governance:validate` in CI after the commit; expected PASS.
- [ ] Commit governance activation.

### Task 2: TDD the URL safety, taxonomy and extraction policy

**Files:**
- Create: `tests/api/BusinessDiscoveryPolicyTests.cs`
- Modify: `apps/api/BusinessDiscovery.cs`

**Interfaces:**
- Produces: `PublicBusinessUrlPolicy`, provider-neutral `PublicBusinessSnapshot`/`PublicBusinessFact`, conservative HTML/JSON-LD extraction, canonical taxonomy matching.

- [ ] Add failing tests proving HTTP/non-HTTPS, credentials, localhost, RFC1918, loopback, link-local, carrier-grade NAT, documentation/test ranges, IPv6 loopback/link-local/ULA/multicast and IPv4-mapped private addresses are rejected.
- [ ] Add failing tests proving ordinary safe HTTPS hosts are accepted at policy level.
- [ ] Add failing tests for JSON-LD > OpenGraph > title/description extraction precedence and missing facts remaining absent.
- [ ] Add failing tests for Wolt/Bolt Restaurant & Cafe proposal and generic-site taxonomy inference/fallback.
- [ ] Implement minimal URL/IP policy and parser to pass tests.
- [ ] Preserve `AllowAutoRedirect=false`, 8-second timeout and bounded body behavior.
- [ ] Commit policy/parser implementation.

### Task 3: Persist server-owned discovery snapshots and provenance

**Files:**
- Modify: `apps/api/AtlasDomain.cs`
- Modify: `apps/api/BusinessDiscovery.cs`
- Create: `apps/api/Migrations/20260809213000_BusinessDiscoveryProvenance.cs`
- Modify: `apps/api/Migrations/AtlasDbContextModelSnapshot.cs`
- Create: `tests/api/BusinessDiscoveryPersistenceTests.cs`

**Interfaces:**
- Produces: `BusinessDiscoverySnapshot`, `BusinessDiscoveryFact`, `BusinessProfileField` EF entities and account/business ownership rules.

- [ ] Add failing InMemory tests for snapshot ownership, unconsumed state, fact provenance fields and consumed snapshots not reusable.
- [ ] Add entities/DbSets/indexes/length limits and relationships.
- [ ] Persist authenticated owner account + snapshot/facts in `POST /api/v1/business-discovery`.
- [ ] Generate/add forward-only migration and model snapshot changes matching the EF model.
- [ ] Commit persistence/migration.

### Task 4: Add atomic owner-confirmed Business creation from discovery

**Files:**
- Modify: `apps/api/BusinessDiscovery.cs`
- Modify: `apps/api/Program.cs`
- Modify: `apps/api/AtlasDomain.cs`
- Create: `tests/api/BusinessDiscoveryConfirmationTests.cs`

**Interfaces:**
- Produces: `POST /api/v1/businesses/from-discovery`, `CreateBusinessFromDiscoveryRequest`, provenance classification policy.

- [ ] Add failing tests: another owner cannot consume a snapshot; a consumed snapshot cannot be reused; unchanged confirmed public facts remain `public-observed`; owner edits become `owner-reported`; manual-only values have no fabricated public provenance.
- [ ] Extract/reuse the current initial-Business creation transaction/Generic Pack assignment so manual and discovery paths stay behaviorally aligned.
- [ ] Implement snapshot ownership/required-field/taxonomy validation and atomic Business/Profile/provenance creation.
- [ ] Mark snapshot consumed and audit discovery confirmation/business creation.
- [ ] Keep existing `POST /api/v1/businesses` unchanged for manual fallback.
- [ ] Commit confirmation API.

### Task 5: Align the mobile discovery model and remove prototype fabrication

**Files:**
- Modify: `apps/mobile/src/api/business-discovery.ts`
- Modify: `apps/mobile/src/api/atlas-client.ts`
- Create: `apps/mobile/src/features/business-discovery/business-discovery-model.ts`
- Create: `tests/mobile/business-discovery-model.test.mjs`

**Interfaces:**
- Produces: snapshot/fact client types, confirmation request builder, category helpers and draft-preservation model.

- [ ] Add failing model tests for partial snapshots, required missing fields, owner edits, confirmation classification, category choices, retry-safe draft preservation and no fabricated fallbacks.
- [ ] Implement minimal typed model/helpers and API methods for discover/categories/create-from-discovery.
- [ ] Commit mobile model/API layer.

### Task 6: Rebuild Welcome and Business setup on Atlas visual primitives

**Files:**
- Modify: `apps/mobile/app/welcome.tsx`
- Modify: `apps/mobile/app/create-business.tsx`
- Modify: `apps/mobile/src/theme/tokens.ts` only if a missing semantic token is required.

**Interfaces:**
- Consumes: Task 5 model/API; existing `BrandMark`; existing `tokens`.
- Produces: URL-first discover → review/edit/confirm → create flow plus manual fallback.

- [ ] Update Welcome copy to explain understand → opportunities → act → measure → learn without claiming guaranteed growth.
- [ ] Replace create-business one-off colors with shared Atlas tokens and keep BrandMark only as Atlas product identity.
- [ ] Remove every Starbucks/demo branch, invented rating/review count/phone/hours/photos/fallback label and claims about scanning reviews/social sources.
- [ ] Make manual setup explicit from the first URL screen.
- [ ] Render only observed/owner-entered facts; label source/confidence non-color-only; require explicit owner confirmation.
- [ ] Use canonical taxonomy selection instead of unrestricted category free text.
- [ ] Preserve URL/draft on timeout/create failure and provide retry/manual fallback.
- [ ] Preserve session businessId + existing post-create navigation.
- [ ] Ensure 44pt targets, keyboard-safe scrolling, semantic labels, accessibility busy/error state and reduced-motion-safe loading.
- [ ] Commit mobile experience.

### Task 7: Add CI-only authentic runtime acceptance for VS-16

**Files:**
- Create: `tests/mobile/business-discovery-runtime.test.mjs`
- Create: `docs/evidence/VS-16-RUNTIME-2026-08-09.md` after successful run evidence is known.

**Interfaces:**
- Produces: authentic Expo Web evidence/screenshots under `dashboard/runtime-vs16` and machine-readable runtime summary.

- [ ] Build a local API fixture for discover/categories/create-from-discovery without production calls.
- [ ] Exercise phone 390×844 and tablet 768×1024 flows: loading, unsafe/invalid URL, source failure/retry, partial discovery, public confirmation, owner edit, manual fallback, create failure/draft retention, success/navigation.
- [ ] Assert no Starbucks/demo copy, no horizontal overflow, minimum 44px interactive targets, visible provenance and disabled/busy state.
- [ ] Capture runtime screenshots + summary inside `dashboard/runtime-vs16` so existing CI artifact upload retains them.
- [ ] Commit runtime verifier.

### Task 8: Full PES/Loop verification, certification and merge

**Files:**
- Modify: `delivery/current-slice.json`
- Modify: `docs/evidence/VS-16-RUNTIME-2026-08-09.md`
- PR metadata only otherwise.

**Interfaces:**
- Consumes: exact implementation head SHA and CI/Security/Product Intake evidence.
- Produces: certified VS-16 and merged main only if all gates pass.

- [ ] Run PR CI: planning/governance/dashboard/platform/mobile validation, authentic runtime test, .NET restore/build, clean EF migration, API tests and dashboard artifact.
- [ ] Require Security baseline PASS and Product Intake PASS at the exact same head SHA.
- [ ] If any gate fails, invoke systematic-debugging/CI debugging, patch the root cause and repeat exact-head gates.
- [ ] Record runtime artifact hashes/evidence and certification approval bound to exact 40-char SHA.
- [ ] Run `npm run governance:validate`/preflight via CI on certification head.
- [ ] Mark PR ready and merge only with expected-head guard after certification.
- [ ] Verify post-merge `main` CI succeeds on the merge SHA.
- [ ] Leave release and production-enable pending/not authorized.
