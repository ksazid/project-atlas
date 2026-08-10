# VS-20 Automatic Knowledge Bundle Resolution Implementation Plan

**Goal:** Automatically resolve Core + applicable packaged Category intelligence + confirmed Context + Local/Market + Memory into a deterministic runtime bundle.

**Architecture:** Add a pure `KnowledgeBundleResolver` using existing domain records. No database migration or public technical endpoint. VS-21 will consume the resolver from Opportunity generation.

**Tech:** C#/.NET, xUnit, VS-18 Manifest v2, VS-19 Restaurant/Café manifest.

### Task 1 — Define resolver contract with TDD
- [ ] Add `tests/api/KnowledgeBundleResolverTests.cs`.
- [ ] Prove RED because resolver types do not yet exist.

### Task 2 — Implement automatic resolution
- [ ] Add `apps/api/KnowledgeBundleResolver.cs`.
- [ ] Enforce exact current Generic Business Core assignment.
- [ ] Auto-select Restaurant/Café Category manifest when applicable.
- [ ] Use only owner-confirmed canonical subcategory evidence; never re-infer it.
- [ ] Filter Business Context to owner-confirmed values.
- [ ] Add local/market Business facts and Business Memory as separate evidence layers.
- [ ] Produce stable manifest/bundle fingerprints.
- [ ] Run focused and full tests GREEN.

### Task 3 — Certify and merge
- [ ] Run full preflight, API build/migration/tests, Security baseline and Product Intake.
- [ ] Bind certification to exact implementation SHA.
- [ ] Re-run gates on close-out head.
- [ ] Merge only when exact-head gates are green.

Release/deployment/production remain out of scope.