# VS-38 — Pilot Operational Signals Design

## Goal
Give Atlas enough fresh operational evidence to produce materially useful, non-repetitive pilot recommendations without turning the MVP into a POS, ERP, data warehouse or autonomous operations platform.

VS-38 introduces the provider-neutral operational-signal boundary and proves it end-to-end with a persistent, folder-scoped Google Drive pilot connector. Owner device upload remains a fallback. Both sources feed the same normalization boundary instead of creating provider-specific intelligence paths.

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

> Atlas may ingest owner-authorized, read-only operational data for closed-pilot intelligence. The VS-38 primary source is one Google Drive folder explicitly shared read-only with the Atlas connector identity; no public-link sharing or whole-Drive OAuth grant is permitted. Atlas stores only the selected folder identity, connector state, file identities/fingerprints and normalized provider-neutral Business Signals. Raw files remain in Google Drive and are never retained by Atlas. Customer PII is excluded. Device CSV upload remains a fallback. Atlas must not perform external business actions through these sources and must preserve Business isolation and owner authority.

VS-38 implements Google Drive folder polling plus fallback device upload for owner-supplied CSV sales/order exports. Direct XLSX parsing, Wolt, Bolt, Square, Google Business Profile and other live provider adapters remain separate follow-up slices.

## Chosen approach
Three approaches were considered:

1. Build Wolt/Bolt/POS APIs first. This gives live data but risks blocking the pilot on provider access, credentials, commercial terms and provider-specific schemas.
2. Build only generic manual Context fields. This is cheap but does not create enough temporal or item-level evidence to solve the observed recommendation-quality problem.
3. Build a provider-neutral signal/change model first, with a persistent Google Drive folder as the primary adapter and owner device upload as fallback. This proves the intelligence contract immediately and lets later connectors reuse the same ingestion path.

VS-38 uses approach 3.

## Architecture
Keep Atlas as one modular monolith. Add a bounded operational-ingestion path at the application/infrastructure boundary:

```text
Google Drive Atlas folder / device CSV fallback
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

Derive deterministic comparison facts only when both windows are complete enough for the same metric/dimensions:
- most recent complete 7-day window versus the immediately preceding 7-day window;
- most recent complete 28-day window versus the immediately preceding 28-day window;
- weekday/daypart/product/category/channel comparisons only when the source contains reliable data for both compared windows.

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
VS-38 accepts `.csv` only. Direct `.xlsx` parsing is explicitly deferred.

The owner must see the accepted schema/recognized column mapping before import is committed. Maximum upload size is 10 MiB and maximum parsed data rows are 100,000 per import. Files exceeding either limit are rejected before durable processing.

The importer supports a bounded canonical shape with aliases for common export headings, including:
- transaction/order timestamp or business date;
- order/transaction identifier when present;
- gross/net amount;
- currency;
- product/item name when present;
- category when present;
- quantity when present;
- channel when present;
- discount/refund/cancellation indicators when present.

At minimum, a supported import must contain a reliable business date/timestamp and one monetary or count-bearing field sufficient to derive at least one approved metric. Ambiguous date or decimal formats are rejected rather than guessed.

The import path must:
1. upload through a bounded authenticated import endpoint;
2. validate MIME/extension, 10 MiB size limit and 100,000-row limit before normalization;
3. detect a supported schema or return an actionable mapping error;
4. preview recognized columns, date range, row/order counts, ignored sensitive columns and derivable metrics without persisting Business Signals;
5. require owner confirmation bound to the preview/import fingerprint;
6. normalize and persist idempotently;
7. derive supported changes;
8. expose last import and freshness status;
9. make eligible derived evidence available to the existing Intelligence pipeline.

## Google Drive Level 2 connector contract
The pilot uses a dedicated Atlas connector identity with Google Drive API access. The owner grants that identity Viewer access to exactly one Atlas folder. Atlas must reject public-link-only access and must never request Editor access. The connector does not use a whole-Drive OAuth scope or store a user Google refresh token.

Connection setup records only:
- Business ID and owner who connected it;
- selected Google Drive folder ID and display name;
- connector identity/version and read-only grant verification;
- encrypted connector configuration where any secret material is unavoidable;
- schedule, last-attempt/last-success timestamps and health/error code;
- per-file Google Drive file ID, MIME type, modified time, size and deterministic content fingerprint.

Atlas queries only direct `.csv` children of the selected folder. Nested folders, shortcuts, Google Sheets, XLSX and unrelated files are ignored. A scheduled poll and owner-triggered `Sync now` use the same idempotent job. Webhooks and real-time change subscriptions are deferred.

For each candidate file Atlas compares Google file ID, modified time, size and content fingerprint. Unchanged files are not downloaded again. New or changed CSV content is streamed through the same preview/validation/normalization boundary as device upload. Raw bytes may exist only in bounded transient memory/stream processing and are disposed after the attempt. Atlas never writes, renames, moves, shares or deletes Drive content.

If the folder grant is removed, the connector becomes `reauthorization-required`; existing normalized signals remain with truthful freshness, scheduled polling stops retrying aggressively, and the owner can reconnect or use device upload.

## Data minimisation and privacy
Operational exports may contain end-customer information that Atlas does not need.

VS-38 must never persist customer names, phone numbers, emails, delivery addresses, free-text customer/order notes or payment-card fields into normalized operational records, Business Memory or AI prompt input.

Known customer-identifying columns are ignored and reported in preview as ignored. A file containing payment-card PAN/CVV-like columns or other clearly prohibited payment-secret fields is rejected entirely rather than partially processed.

Raw CSV bytes and raw rows are process-and-discard in VS-38. They may exist only transiently for the request/import transaction and must not be retained as durable files, blobs, database payloads, logs or model inputs. Durable storage is limited to normalized aggregate signals plus import provenance/diagnostics required for reproducibility.

No customer-level behavioural profiling or loyalty analysis enters VS-38.

## Business isolation and authority
Every imported record and derived signal is Business-scoped. API authorization must derive the authenticated owner membership server-side. A client-provided BusinessId cannot bypass membership validation.

Only Business owners may connect/disconnect a folder, invoke `Sync now`, change the schedule or confirm a fallback device import in VS-38. Pilot operators do not gain these commands. Connector runtime credentials are server-side only and never exposed to the mobile app.

## Owner experience
Keep the existing four-tab topology unchanged.

Add a prominent `Business data` card under Profile/Business Hub, alongside the existing Business Context card, rather than a plain text link or persistent Analytics tab. Its primary action is `Connect Google Drive`; after connection it shows folder, sync health, freshness and `Sync now`. The owner experience supports:
- Google Drive connection guidance and one-folder confirmation;
- scheduled sync control with a bounded pilot cadence;
- `Sync now`;
- disconnect/reconnect without deleting normalized history;
- `Upload CSV from this device` as a secondary fallback;
- CSV file selection/upload for the fallback;
- preview of recognized date range, row/order count, supported columns, ignored sensitive columns and derivable metrics;
- explicit confirmation;
- import success/failure;
- last synced/imported date and source-data freshness;
- clear duplicate/overlap result;
- privacy copy explaining that Atlas ignores customer-identifying fields and does not retain the raw CSV.

Do not build dashboard-first charts in this slice. Today and Opportunity Detail remain the primary intelligence surfaces.

## Intelligence integration
VS-38 must not hardcode restaurant-only Opportunity patterns in Atlas Core.

Expose normalized operational evidence to existing Context/Evidence resolution in a provider-neutral way. Knowledge Packs may use supported metric/change keys as evidence inputs. Existing eligibility remains authoritative: no factual evidence means no persisted Today Opportunity.

Operational evidence may improve candidate differentiation through existing evidence resolution, but VS-38 must not introduce new Opportunity patterns or rewrite ranking/scoring policy. New patterns or scoring changes belong in a later signal-aware intelligence slice after the first imports prove which signals are useful.

## Idempotency and overlap handling
Re-importing the same export must not duplicate durable signals.

Use a deterministic file/import fingerprint plus stable normalized signal identities. For overlapping date ranges:
- exact duplicate observations are ignored idempotently;
- a byte-different import that produces the same normalized signal identities is also deduplicated;
- conflicting values for the same source/metric/dimensions/period are rejected as an overlap conflict in VS-38 rather than silently superseded or averaged.

A later connector/versioning slice may add explicit corrections/supersession. VS-38 does not mutate historical Opportunity Evidence snapshots when data is re-imported.

## Freshness
Freshness is derived from the most recent source business date, not upload time.

For pilot CSV evidence:
- most recent operational date within 7 days of the Intelligence Run is `fresh`;
- 8–30 days old is `stale` and may be shown only with reduced confidence/explicit stale provenance under existing eligibility policy;
- older than 30 days is retained for historical comparisons but is not eligible as current why-now evidence.

The import UI must display the most recent operational date so the owner can understand freshness.

## Failure and degraded states
- unsupported file/schema: no persistence; show supported columns and corrective guidance;
- file over 10 MiB or more than 100,000 rows: reject before normalization;
- malformed numeric/date values in required fields: reject the import; do not silently coerce ambiguous locale values;
- mixed currencies or currency conflicting with the confirmed Business currency: reject the import;
- partial optional columns: import only metrics supported by reliable recognized columns;
- prohibited payment-secret columns: reject the file;
- ordinary customer-identifying columns: ignore them and report them in preview;
- duplicate import: return successful idempotent result with no duplicate signals;
- conflicting overlapping observations: reject with an overlap-conflict result;
- derivation lacks comparison coverage: retain supported signals but do not fabricate a Business Change;
- operational data is stale: preserve truthful freshness and do not present old data as current evidence;
- Drive permission removed: mark reauthorization required and preserve truthful stale state;
- scheduled or manual sync failure: existing Today content may remain safely readable; no fabricated fallback metrics;

## Testing strategy
Use strict TDD for all runtime work after the written spec and implementation plan are approved.

Required coverage:
1. authorization and Business-isolation tests;
2. CSV-only, 10 MiB and 100,000-row validation;
3. deterministic parser fixtures for supported CSV shapes and aliases;
4. ignored customer-identifying columns and rejected prohibited payment-secret columns;
5. locale-safe date/decimal/currency parsing tests;
6. preview-before-confirm fingerprint contract tests;
7. raw-file non-retention tests/logging guards;
8. idempotent duplicate import and rejected overlap-conflict behavior;
9. normalized signal persistence/provenance/freshness tests;
10. deterministic 7-day/28-day change derivation and insufficient-comparison behavior;
11. Evidence projection tests proving underlying signal IDs remain traceable;
12. Today regression proving operational evidence can participate in existing eligible candidates without weakening no-filler behavior;
13. mobile import flow accessibility/degraded-state tests;
14. clean PostgreSQL migration replay if persistence changes require migrations;
15. full API/mobile/preflight, Security baseline and Product Intake on the exact head.
16. selected-folder isolation, Viewer-only verification and no public-link dependency;
17. file-ID/modified-time/size/fingerprint changed-file detection;
18. manual and scheduled sync parity, lease/idempotency and revoked-access behavior;
19. raw Drive response/file content redaction and non-retention guards.

## Success criteria
VS-38 succeeds when:
- an owner can connect one Viewer-shared Google Drive folder and sync realistic CSV exports without granting whole-Drive access or exposing customer PII to Atlas intelligence;
- Atlas does not durably retain the raw CSV or raw rows;
- Atlas persists provider-neutral, provenance-rich Business Signals rather than source-specific operational rows;
- deterministic Business Changes are derived only from sufficient comparable data;
- the existing intelligence pipeline can reference those signals/changes as factual Evidence;
- re-imports are idempotent and conflicting overlaps fail safely;
- the same architecture can later accept live provider adapters without changing Knowledge Pack or Intelligence contracts;
- no external write action, user Google OAuth token, deep POS integration, data warehouse or new persistent analytics tab is introduced.

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
- direct XLSX parsing;
- live Wolt/Bolt/Square/Google Business Profile/POS connector implementation;
- whole-Drive OAuth access or user Google refresh-token storage;
- accepting, rejecting or modifying orders;
- menu or price write-back;
- payments, refunds or financial actions;
- customer-level CRM/loyalty analytics;
- real-time streaming, Drive webhooks or event-bus ingestion;
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
