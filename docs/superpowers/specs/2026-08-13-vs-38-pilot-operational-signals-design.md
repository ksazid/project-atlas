# VS-38 — Pilot Operational Signals Design

## Goal
Give Atlas enough fresh operational evidence to produce materially useful, non-repetitive pilot recommendations without turning the MVP into a POS, ERP, data warehouse or autonomous operations platform.

VS-38 introduces the provider-neutral operational-signal boundary and proves it end-to-end with owner-supplied sales/order exports. Later live connectors must feed the same boundary instead of creating provider-specific intelligence paths.

## Product problem
Atlas currently reasons mainly from Business Profile, Goals, owner-confirmed Context, public Business discovery, Knowledge Packs, prior Opportunities and outcomes. That is sufficient for generic guidance but too weak for a realistic pilot because the engine cannot reliably observe what changed in day-to-day performance.

The pilot needs evidence such as sales movement, order volume, average order value, product/category performance, channel mix, discounts, cancellations and time-of-day demand. Without those signals, materially different Businesses can look too similar to the recommendation engine and Opportunity patterns can repeat even when cooldown logic is correct.

VS-37 repaired duplicate identity. VS-38 addresses the separate upstream evidence-density problem.

## Existing authority
- FR-05 requires Atlas to collect only context needed to improve intelligence and permits enabled sources and public data as Business Context.
- FR-07 requires Today’s Focus to be evidence-backed and practically valuable.
- FR-16 requires truthful insufficient-context/degraded behavior.
- TRD already defines provider adapters through internal ports, fresh Context Facts with provenance/freshness/confidence, background provider retries, Business isolation and provider-neutral intelligence.
- PRD explicitly excludes deep POS integrations and dashboard-first BI. VS-38 therefore proves read-only operational ingestion with owner-supplied exports rather than deep live POS integration.
- DEC-04 remains authoritative for public discovery sources. VS-38 does not reinterpret a public page as a private connector.

## New decision boundary — DEC-12
The Product Owner approved a new pilot policy direction:

> Atlas may ingest owner-authorized, read-only operational data for closed-pilot intelligence. Operational data is normalized into minimal provider-neutral Business Signals with source, freshness, confidence and provenance. Atlas must minimise or exclude end-customer PII, must not perform external business actions through these sources in the pilot, and must preserve Business isolation and owner authority. Provider-specific live APIs require their own bounded credentials/policy review before activation.

VS-38 implements only the first safe source under that policy: owner-supplied CSV/Excel-style sales/order exports. Wolt, Bolt, Square, Google Business Profile and other live provider adapters remain separate follow-up slices.

## Chosen approach
Three approaches were considered:

1. Build Wolt/Bolt/POS APIs first. This gives live data but risks blocking the pilot on provider access, credentials, commercial terms and provider-specific schemas.
2. Build only generic manual Context fields. This is cheap but does not create enough temporal or item-level evidence to solve the observed recommendation-quality problem.
3. Build a provider-neutral signal/change model first, with owner-uploaded operational exports as the first adapter. This proves the intelligence contract immediately and lets later connectors reuse the same ingestion path.

VS-38 uses approach 3.

## Architecture
Keep Atlas as one modular monolith. Add a bounded operational-ingestion path at the application/infrastructure boundary:

```text
Owner-supplied operational export
        |
        v
Operational Import Adapter
        |
        v
Normalized Business Signals
        |
        v
Derived Business Changes
        |
        v
Context / Evidence projection
        |
        v
Existing Intelligence pipeline
        |
        v
Today / Opportunity Detail
```

The Intelligence module must not parse CSV files or know source-specific column names. Source adapters normalize records before the Intelligence pipeline consumes them.

## Provider-neutral signal model
Persist only normalized information needed for Business intelligence and reproducibility.

A Business Signal contains, at minimum:
- `BusinessId`;
- stable metric key;
- numeric value;
- unit or currency where applicable;
- period start and period end;
- optional bounded dimensions such as product, category, channel or location;
- source kind and source reference;
- freshness/observed-at metadata;
- confidence;
- provenance/import identifier;
- deterministic identity for duplicate-safe re-import.

Initial metric catalogue is deliberately small:
- gross sales/revenue;
- order count;
- average order value;
- units sold;
- discount amount/rate when present;
- refund/cancellation amount/count when present;
- product/category sales and quantity when present;
- channel sales/order count when present;
- bounded hour/day aggregation when timestamp data is present.

Do not create arbitrary user-defined metric keys in VS-38.

## Derived Business Change model
Signals alone are not enough. Atlas needs explicit change/trend evidence so Opportunity generation does not merely receive more raw rows.

Derive deterministic comparison facts when both windows are sufficiently complete, for example:
- current 7 days versus previous 7 days;
- current 28 days versus previous 28 days;
- weekday/daypart/product/category/channel comparisons where source coverage supports them.

A derived change contains:
- BusinessId;
- metric key and matching dimensions;
- current value;
- comparison value;
- absolute delta;
- relative delta where mathematically valid;
- compared periods;
- evidence IDs pointing to the underlying normalized signals;
- freshness/confidence inherited conservatively from source coverage.

No causal inference is allowed. Copy and API contracts must describe observed change, not claim why it happened.

## Import contract
VS-38 accepts owner-supplied delimited text exports and may support `.csv` first even if a later slice adds direct `.xlsx` parsing. The owner must see the accepted schema/column mapping before import is committed.

The importer must support a bounded canonical shape with aliases for common export headings, including:
- transaction/order timestamp or business date;
- order/transaction identifier when present;
- gross/net amount;
- currency;
- product/item name when present;
- category when present;
- quantity when present;
- channel when present;
- discount/refund/cancellation indicators when present.

The import path must:
1. upload to the existing safe server boundary or a new bounded import endpoint;
2. validate file type/size/row count before parsing;
3. detect a supported schema or return an actionable mapping error;
4. preview counts/date range/recognized columns without persisting Business Signals;
5. require owner confirmation;
6. normalize and persist idempotently;
7. derive changes;
8. expose freshness/import status;
9. make eligible derived evidence available to the existing Intelligence pipeline.

## Data minimisation and privacy
Operational exports may contain end-customer information that Atlas does not need.

VS-38 must reject or ignore customer names, phone numbers, emails, delivery addresses, free-text notes and payment-card data. Raw operational rows must not become Business Memory or AI prompt input.

If raw upload retention is needed for transactional processing, keep it short-lived and explicitly bounded; otherwise process-and-discard. Durable storage should prefer normalized aggregate signals and import provenance over raw files.

No customer-level behavioural profiling or loyalty analysis enters VS-38.

## Business isolation and authority
Every imported record and derived signal is Business-scoped. API authorization must derive the authenticated owner membership server-side. A client-provided BusinessId cannot bypass membership validation.

Only Business owners (or a separately authorized internal pilot command if later approved) may confirm an operational import. No provider credentials are stored in VS-38.

## Owner experience
Keep the existing four-tab topology unchanged.

Add a bounded operational-data entry under Profile/Business Hub rather than creating a persistent Analytics tab. The owner experience should support:
- `Import sales data`;
- file selection/upload;
- preview of recognized date range, order count and supported columns;
- explicit confirmation;
- import success/failure;
- last imported date/freshness;
- replace/add-later guidance for overlapping periods;
- privacy copy explaining that Atlas ignores customer-identifying fields.

Do not build dashboard-first charts in this slice. Today and Opportunity Detail remain the primary intelligence surfaces.

## Intelligence integration
VS-38 must not hardcode restaurant-only Opportunity patterns in Atlas Core.

Expose normalized operational evidence to existing Context/Evidence resolution in a provider-neutral way. Knowledge Packs may use supported metric/change keys as evidence inputs. Existing eligibility remains authoritative: no factual evidence means no persisted Today Opportunity.

Operational evidence should improve candidate differentiation and ranking, but VS-38 must not rewrite the entire ranking engine. Any new Opportunity pattern or scoring policy beyond consuming these evidence keys belongs in a later signal-aware intelligence slice if needed.

## Idempotency and overlap handling
Re-importing the same export must not duplicate durable signals.

Use a deterministic import/file fingerprint plus stable normalized signal identities. For overlapping date ranges:
- exact duplicate observations are ignored;
- clearly same-source/same-period corrected data may supersede prior source observations through an explicit import version/provenance relationship;
- ambiguous conflicts remain visible as import errors or separate provenance rather than being silently averaged.

Do not mutate historical Opportunity Evidence snapshots when source data is re-imported.

## Failure and degraded states
- unsupported file/schema: no persistence; show supported columns and corrective guidance;
- oversized file/row count: reject before expensive parsing;
- malformed numeric/date values: reject affected import if confidence would be unsafe; do not silently coerce ambiguous locale values;
- mixed or unknown currencies: reject unless one canonical Business currency can be validated safely;
- partial optional columns: import only metrics supported by reliable columns;
- duplicate import: return successful idempotent result with no duplicate signals;
- derivation lacks comparison coverage: retain signals but do not fabricate a change;
- operational data is stale: evidence remains labelled stale and confidence is reduced/excluded according to existing policy;
- import service failure: existing Today content may remain safely readable; no fabricated fallback metrics.

## Testing strategy
Use strict TDD for all runtime work after the written spec and implementation plan are approved.

Required coverage:
1. authorization and Business-isolation tests;
2. file size/type/row-count validation;
3. deterministic parser fixtures for supported CSV shapes and aliases;
4. rejection of customer-identifying and unsupported sensitive columns from durable normalized output;
5. locale-safe date/decimal/currency parsing tests;
6. preview-before-confirm contract tests;
7. idempotent duplicate import and overlapping-period behavior;
8. normalized signal persistence/provenance/freshness tests;
9. deterministic 7-day/28-day change derivation and insufficient-comparison behavior;
10. Evidence projection tests proving underlying signal IDs remain traceable;
11. Today regression proving operational evidence can differentiate eligible candidates without weakening no-filler behavior;
12. mobile import flow accessibility/degraded-state tests;
13. clean PostgreSQL migration replay if persistence changes require migrations;
14. full API/mobile/preflight, Security baseline and Product Intake on the exact head.

## Success criteria
VS-38 succeeds when:
- an owner can import a realistic operational export without exposing customer PII to Atlas intelligence;
- Atlas persists provider-neutral, provenance-rich Business Signals rather than source-specific rows;
- deterministic Business Changes are derived only from sufficient comparable data;
- the existing intelligence pipeline can reference those signals/changes as factual Evidence;
- re-imports are idempotent;
- the same architecture can later accept live provider adapters without changing Knowledge Pack or Intelligence contracts;
- no external write action, provider credential, deep POS integration, data warehouse or new persistent analytics tab is introduced.

## Compatibility
VS-38 starts from merged VS-37 main `f75ce6142e88230220042c8d448111c562eb9ebb`.

Preserve:
- Today / History / Goals / Profile native navigation;
- Business Hub as Profile root;
- VS-36 public menu/media discovery;
- VS-37 evidence-aware cooldown behavior;
- owner-confirmation and provenance rules;
- existing Knowledge Pack versioning;
- no-filler Opportunity eligibility;
- Pilot Operations audit boundary;
- release and production-enable human gates.

## Explicit non-goals
- live Wolt/Bolt/Square/Google/POS connector implementation;
- provider OAuth/credential storage;
- accepting, rejecting or modifying orders;
- menu or price write-back;
- payments, refunds or financial actions;
- customer-level CRM/loyalty analytics;
- real-time streaming/event-bus ingestion;
- data warehouse/lake architecture;
- arbitrary dashboard builder;
- causal claims from correlations;
- autonomous publishing/advertising;
- new restaurant-only Atlas Core rules;
- production deployment, release, EAS/OTA or production database mutation.

## Follow-up sequence
After VS-38 proves the contract, proposed later slices are:
- Google Business Profile performance adapter;
- one restaurant commerce/POS adapter selected from actual pilot access;
- signal-aware Opportunity/Knowledge Pack enrichment if the pilot evidence shows the current patterns still underuse operational signals;
- connector management/sync-health UX;
- full pilot validation.
