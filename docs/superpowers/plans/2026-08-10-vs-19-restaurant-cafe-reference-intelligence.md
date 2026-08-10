# VS-19 Restaurant/Café Reference Intelligence Implementation Plan

**Goal:** Add the first evidence-aware Category-layer Knowledge Pack Manifest v2 for Restaurant & Café.

**Architecture:** Implement a pure packaged manifest factory on top of VS-18. No persistence or endpoint changes.

**Tech:** C#/.NET, xUnit, Atlas Manifest v2 contract.

### Task 1 — Define contract tests
- [ ] Add `tests/api/RestaurantCafeKnowledgeManifestV2Tests.cs`.
- [ ] Verify RED because the Restaurant/Café manifest factory does not exist.

### Task 2 — Implement reference pack
- [ ] Add `apps/api/RestaurantCafeKnowledgeManifestV2.cs`.
- [ ] Include canonical category/subcategories, KPIs, evidence rules, four bounded Opportunity patterns, execution templates, measurement suggestions, seasonality and guardrails.
- [ ] Keep content provider-neutral and avoid guaranteed-outcome claims.
- [ ] Verify focused and full API tests GREEN.

### Task 3 — Certify and merge
- [ ] Run full repository preflight, API build/migrations/tests, Security baseline and Product Intake.
- [ ] Record exact implementation SHA certification evidence.
- [ ] Re-run all required gates on the close-out head.
- [ ] Merge only after exact-head gates are green.

Release/deployment/production remain out of scope.