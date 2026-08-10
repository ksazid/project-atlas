# VS-18 Certification Evidence — 2026-08-10

## Certified implementation

- Slice: VS-18 — Knowledge Pack Schema v2
- Certified implementation SHA: `abe3c1bd7f5dfb6423dd9065b41486efa768d8d9`
- Pull request: #25
- Scope: FR-06 packaged Knowledge Pack Manifest Schema v2

## TDD evidence

The RED test head `374fe37a8361c063af093fb56ec2f3d961a784eb` passed repository preflight, API build and database migration, then failed at `dotnet test` specifically because the new Manifest v2 production types did not yet exist. CI run: `31345424122`.

After implementing only the bounded Manifest v2 contract, the implementation head `abe3c1bd7f5dfb6423dd9065b41486efa768d8d9` passed:

- CI run `31345640681`
  - repository preflight
  - API restore/build
  - database update against the existing migration set
  - complete API test project, including `KnowledgePackManifestV2Tests`
  - dashboard build
- Security baseline run `31345640673`
- Product Intake run `31345640677`

## Verified behavior

- Schema version is fixed at 2.
- Core, category, subcategory and local-market are the only supported pack layers.
- Applicability is validated against the canonical Atlas Business Category taxonomy.
- KPI, evidence-rule, Opportunity-pattern and execution-template stable keys are bounded and unique.
- Opportunity-pattern references must resolve to declared evidence rules and execution templates.
- Category/subcategory packs require canonical category applicability; Core cannot claim category specificity.
- Guardrails are mandatory.
- A canonical semantic ordering produces a reproducible SHA-256 fingerprint.
- The built-in Generic Business Core manifest remains category-agnostic.

## Exclusions retained

No database schema change, public endpoint, dynamic remote Knowledge Pack installation, paid/private provider integration, release, deployment or production enablement is included in VS-18.

## Merge gate

This document and the governed certification record are a close-out change after the certified implementation SHA. The resulting PR head must pass CI, Security baseline and Product Intake again before merge.