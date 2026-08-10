# VS-18 — Knowledge Pack Schema v2 Design

## Authority and goal

VS-18 implements the previously approved Atlas V2 Knowledge Pack contract for FR-06 without expanding the product beyond the approved PRD/TRD. The MVP remains backend-packaged, provider-neutral and global-ready, with Malta-first activation. Dynamic remote pack installation, paid provider dependencies, private POS connectors, release and production deployment remain out of scope.

The goal is to give Atlas Core a deterministic, versioned contract that category packs can use for intelligence while preserving the existing v1 persistence and exact-version assignment compatibility.

## Contract

Add a pure domain contract `KnowledgePackManifestV2` with schema version `2` and these bounded sections:

- identity: pack key, exact version and layer;
- applicability: supported canonical category and optional subcategory keys;
- KPI definitions;
- evidence rules;
- deterministic Opportunity patterns;
- execution templates;
- measurement suggestions;
- seasonality notes;
- guardrails.

Supported layers for this slice are `core`, `category`, `subcategory` and `local-market`. Business Context and Business Memory remain business-owned runtime inputs, not remotely installable packs.

## Validation

`KnowledgePackManifestV2Policy` is server-owned and deterministic. It rejects:

- any schema version other than 2;
- invalid pack keys, versions or layers;
- unknown category/subcategory keys;
- duplicate stable keys within a section;
- Opportunity patterns whose evidence-rule or execution-template references do not exist;
- category-layer manifests without at least one supported canonical category;
- core manifests that claim category specificity;
- empty guardrails;
- counts or text values outside bounded limits.

The policy returns structured validation errors and computes a stable SHA-256 fingerprint from canonical ordered contract values. The fingerprint is for reproducibility and diagnostics; it is not a security signature.

## Built-in Core manifest

Add `GenericBusinessKnowledgeManifestV2.Create()` as the v2 Core reference. It remains industry-agnostic and includes only generic evidence, opportunity and execution primitives. It must not mention Restaurant/Café-specific tactics.

## Compatibility

No database migration is required in VS-18. Existing `KnowledgePack`, `KnowledgePackVersion`, `KnowledgeSection` and `BusinessKnowledgeAssignment` remain the persistence authority. VS-18 introduces the v2 packaged manifest contract that later slices will resolve alongside the exact current assignment.

No existing endpoint changes in this slice.

## Testing

Add xUnit policy coverage proving:

1. the built-in Core manifest validates;
2. the Core manifest is industry-agnostic;
3. fingerprints are stable regardless of input collection order where order is not semantically meaningful;
4. duplicate keys are rejected;
5. broken pattern references are rejected;
6. category-layer manifests require known canonical categories.

Existing Knowledge Pack policy tests must remain green.

## Definition of done

VS-18 is complete when the contract and tests are implemented, deterministic CI/security/product-intake gates pass on the exact head SHA, the slice is documented and certified, and the PR is merged. Release/deployment/production enablement remain unauthorized.