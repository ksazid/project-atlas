# VS-20 — Automatic Knowledge Bundle Resolution Design

## Goal

Resolve the effective Atlas intelligence bundle automatically from the Business record and confirmed evidence. Business owners must not choose technical Knowledge Packs or understand pack layering.

## Effective bundle

The resolver composes these layers in deterministic order:

1. **Core packaged manifest** — the built-in Generic Business Manifest v2 matching the exact current persisted Core assignment.
2. **Category packaged manifest** — automatically included when a packaged manifest supports the Business's canonical category. In this slice that means Restaurant & Café only.
3. **Subcategory applicability** — taken only from an owner-confirmed `BusinessProfileField` named `subcategory` and only when it is canonical for the selected category. Atlas does not infer a new subcategory at resolution time.
4. **Business Context** — only owner-confirmed, non-empty `BusinessContextEntry` values.
5. **Local/Market facts** — canonical operating facts already stored on the Business: country, timezone, currency and primary location.
6. **Business Memory** — current persisted `BusinessMemoryItem` values, retained as a separate evidence layer.

Context, Local/Market and Memory are runtime evidence layers, not installable Knowledge Pack manifests.

## Exact-version behavior

The current MVP persists one exact Core `BusinessKnowledgeAssignment`. Resolution must fail closed if:

- there is no current assignment;
- the assignment belongs to another Business;
- the assignment is not the Generic Business Core pack; or
- its exact version has no matching packaged Core Manifest v2.

For the current built-in Core, the supported exact version is `1.0`.

## Provenance and reproducibility

Each packaged manifest exposes its layer, key, exact version and semantic Manifest v2 fingerprint. The resolved bundle also has a deterministic SHA-256 fingerprint based on canonical ordering of manifests and evidence facts. Reordering equivalent input collections must not change the fingerprint.

The fingerprint is provenance/diagnostic metadata, not a cryptographic signature.

## Runtime boundary

VS-20 adds a pure runtime resolver consumed by later Opportunity generation. It does not expose a pack-picker or technical Knowledge Pack endpoint and does not alter persistence. No database migration is required.

## Unsupported categories

If no packaged Category manifest exists for a supported canonical category, Atlas resolves Core + Context + Local/Market + Memory only. It does not fabricate a category pack.

## Exclusions

No dynamic/remote pack installation, provider-specific dependency, private marketplace API, paid provider, release, deployment or production enablement.