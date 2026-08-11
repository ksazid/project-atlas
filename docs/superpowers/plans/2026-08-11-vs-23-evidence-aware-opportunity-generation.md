# VS-23 Evidence-Aware Opportunity Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the generic Today’s Focus placeholder with deterministic evidence-aware candidate generation based on the VS-20 resolved Knowledge Bundle.

**Architecture:** Add a focused `OpportunityGeneration` policy/service that accepts already-resolved bundle + confirmed profile + goals + prior opportunities, evaluates packaged manifest patterns, creates structured evidence snapshots and returns one ranked candidate or no candidate. `Opportunities.cs` remains the HTTP/persistence orchestration layer and delegates generation to the new service.

**Tech Stack:** C# / ASP.NET Core / EF Core / PostgreSQL / xUnit.

## Global Constraints

- Consume `ResolvedKnowledgeBundle` directly.
- No external AI/provider call.
- No Restaurant/Café leakage into unsupported categories.
- No filler recommendation when no pattern qualifies.
- Preserve exact pack key/version and bundle fingerprint.
- Application controls confidence; never emit High in VS-23.
- No new database table or migration.
- Do not modify VS-21/VS-22 discovery/location implementation.
- Exact-head CI, Security baseline and Product Intake are mandatory before certification.

---

### Task 1: Candidate and evidence policy

**Files:**
- Create: `tests/api/OpportunityGenerationTests.cs`
- Create: `apps/api/OpportunityGeneration.cs`

**Interfaces:**
- Consumes: `ResolvedKnowledgeBundle`, `BusinessProfile`, `IReadOnlyCollection<BusinessGoal>`, `IReadOnlyCollection<Opportunity>`, `DateTimeOffset`.
- Produces: `OpportunityGenerationResult Generate(...)`, where the selected candidate retains pattern, goal, evidence, pack/version and bundle fingerprint.

- [ ] **Step 1: Write failing tests** for Restaurant ordering-channel eligibility, missing-evidence rejection, unsupported category isolation, goal matching, no-candidate state, evidence IDs, confidence and exact pack/fingerprint retention.
- [ ] **Step 2: Run API tests and verify RED** because `OpportunityGenerator`/contracts do not exist.
- [ ] **Step 3: Implement minimal generator contracts and explicit evidence-rule mapping** using only supplied bundle/profile/goals.
- [ ] **Step 4: Run focused tests and verify GREEN.**
- [ ] **Step 5: Commit.**

### Task 2: Ranking and cooldown suppression

**Files:**
- Modify: `tests/api/OpportunityGenerationTests.cs`
- Modify: `apps/api/OpportunityGeneration.cs`

**Interfaces:**
- Consumes prior `Opportunity.EvidenceJson` snapshots.
- Produces deterministic selected candidate.

- [ ] **Step 1: Add failing tests** for goal-priority ranking, category-specific tie preference, Low/Medium ordering, lexical stable tie-break and cooldown suppression using persisted pattern key.
- [ ] **Step 2: Verify RED.**
- [ ] **Step 3: Implement deterministic ranking and safe parsing of prior pattern snapshots.**
- [ ] **Step 4: Verify GREEN plus the whole generator suite.**
- [ ] **Step 5: Commit.**

### Task 3: Structured evidence persistence snapshot

**Files:**
- Modify: `tests/api/OpportunityGenerationTests.cs`
- Modify: `apps/api/OpportunityGeneration.cs`

**Interfaces:**
- Produces schema-v1 `OpportunityGenerationSnapshot` serialized into `Opportunity.EvidenceJson`.

- [ ] **Step 1: Add failing tests** proving structured Evidence is separate from reason/why-now, manifest references are exact, assumptions/limitations are present and generated evidence references supplied bundle facts only.
- [ ] **Step 2: Verify RED.**
- [ ] **Step 3: Implement snapshot serialization/parsing helpers.**
- [ ] **Step 4: Verify GREEN.**
- [ ] **Step 5: Commit.**

### Task 4: Today’s Focus integration

**Files:**
- Modify: `apps/api/Opportunities.cs`
- Create: `tests/api/OpportunityGenerationIntegrationTests.cs`

**Interfaces:**
- `CreateDeterministicFocus` is replaced by bundle-backed generation orchestration.
- Existing GET route remains unchanged.

- [ ] **Step 1: Add failing integration tests** for Restaurant/Café category-specific persistence, no-focus with no qualifying evidence, reusing unexpired existing opportunity and tenant isolation.
- [ ] **Step 2: Verify RED.**
- [ ] **Step 3: Load Business/Profile/Goals/Core assignment/ProfileFields/Context/Memory/prior opportunities; call VS-20 resolver and generator; persist only selected candidate.**
- [ ] **Step 4: Return `insufficient-context`, `no-focus`, or `ready` deterministically; bundle-resolution failure must not 500.**
- [ ] **Step 5: Verify GREEN and existing Opportunity tests.**
- [ ] **Step 6: Commit.**

### Task 5: Opportunity detail evidence parsing

**Files:**
- Modify: `apps/api/Opportunities.cs`
- Modify: `tests/api/OpportunityDetailPolicyTests.cs`

**Interfaces:**
- `OpportunityPolicy.Detail` recognises VS-23 schema while preserving legacy fallback.

- [ ] **Step 1: Add failing tests** for VS-23 evidence items, assumptions/limitations/source categories and legacy malformed JSON fallback.
- [ ] **Step 2: Verify RED.**
- [ ] **Step 3: Implement schema-aware detail parser with safe legacy fallback.**
- [ ] **Step 4: Verify GREEN.**
- [ ] **Step 5: Commit.**

### Task 6: Governance, regression and certification candidate

**Files:**
- Create/modify: `docs/slices/VS-23.md`, `delivery/current-slice.json`, evidence docs as required.

- [ ] **Step 1: Run focused API tests.**
- [ ] **Step 2: Run all API tests.**
- [ ] **Step 3: Run repository preflight/governance via PR CI.**
- [ ] **Step 4: Run CI, Security baseline, Product Intake on exact head.**
- [ ] **Step 5: Fix every failure and repeat gates until green.**
- [ ] **Step 6: Mark certification pending with exact candidate SHA and evidence.**
- [ ] **Step 7: Rebase/update from current `main` after VS-22 merge, then rerun all gates on the new exact SHA before merge.**