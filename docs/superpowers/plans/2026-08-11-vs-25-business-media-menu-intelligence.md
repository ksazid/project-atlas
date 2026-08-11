# VS-25 Business Media & Menu Intelligence — Implementation Plan

1. RED: add extraction/reconciliation tests for structured images, safe OG fallback, structured menu items/prices/menu URL and mismatched-source exclusion.
2. GREEN: extend the provider-neutral public snapshot contract with bounded media and offering collections; implement conservative extraction and accepted-source reconciliation.
3. RED: add persistence/materialisation tests covering snapshot children, Business-owned records, provenance and owner-confirmation semantics.
4. GREEN: add discovery/business persistence models and EF mappings; materialise accepted records inside the existing discovery confirmation transaction.
5. Add forward-only PostgreSQL migration for discovery media/offerings and Business media/offerings; validate on clean Postgres through CI.
6. Extend the discovery response contract with media/offerings while preserving existing facts.
7. Run full deterministic API/mobile/governance/migration/security gates and fix regressions through the Superpowers loop.
8. Refresh `atlas/expo-go-test-harness` and `atlas/test-deployment` only after exact-head gates are green; migrate/deploy the test environment and perform an isolated live discovery smoke.
9. Do not merge to main or enable production without separate Product Owner approval.
