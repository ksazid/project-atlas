# VS-18 Knowledge Pack Schema v2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a deterministic, backend-packaged Knowledge Pack Manifest v2 contract that later Category Intelligence slices can consume without breaking v1 persistence.

**Architecture:** Keep the existing EF Core Knowledge Pack entities unchanged. Add a focused pure-domain manifest/policy file and xUnit tests. The v2 manifest references canonical `BusinessCategoryTaxonomy` keys and uses stable-key cross-reference validation plus a canonical SHA-256 fingerprint.

**Tech Stack:** C#/.NET, xUnit, existing Atlas API domain types.

## Global Constraints

- Approved PRD/TRD remain authoritative.
- No database migration in VS-18.
- No remote/dynamic Knowledge Pack installation.
- No provider-specific or Restaurant/Café-specific logic in Core.
- No release, deployment or production enablement.

---

### Task 1: Manifest v2 policy contract

**Files:**
- Create: `tests/api/KnowledgePackManifestV2Tests.cs`
- Create: `apps/api/KnowledgePackManifestV2.cs`

**Interfaces:**
- Produces `KnowledgePackManifestV2`, `KnowledgePackManifestV2Policy`, `GenericBusinessKnowledgeManifestV2` and the bounded section records consumed by VS-19+.

- [ ] **Step 1: Write failing tests** for valid Core, stable fingerprint, duplicate keys, broken references and canonical category validation.
- [ ] **Step 2: Run API tests and verify RED** because the v2 types do not exist.
- [ ] **Step 3: Implement the minimal pure-domain contract and validator.**
- [ ] **Step 4: Run the focused tests and all API tests; verify GREEN.**
- [ ] **Step 5: Run formatting/build/governance checks.**

### Task 2: Govern and certify the slice

**Files:**
- Create: `docs/slices/VS-18.md`
- Modify: `delivery/current-slice.json`
- Create: `docs/evidence/VS-18-CERTIFICATION-2026-08-10.md`

**Interfaces:**
- Records the approved scope/implementation authority from the Product Owner's standing approval for the next five slices; release and production stay pending.

- [ ] **Step 1: Add slice traceability to FR-06.**
- [ ] **Step 2: Run `npm run governance:validate`, `npm run slice:validate`, `npm run preflight`.**
- [ ] **Step 3: Open the PR and verify CI, Security baseline and Product intake on the exact head SHA.**
- [ ] **Step 4: Record exact-SHA certification evidence.**
- [ ] **Step 5: Re-run governance-head checks and merge only when all required gates are green.**

## Self-review

- Spec coverage: all VS-18 requirements are covered by Task 1 or Task 2.
- Placeholder scan: no TBD/TODO implementation steps.
- Type consistency: all later slices consume the names declared in Task 1.