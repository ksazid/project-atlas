# VS-16 — URL-First Business Discovery & Owner Confirmation

Status: Approved implementation design
Date: 2026-08-09
Product authority: ATLAS-PRD-001, ATLAS-TRD-001, ATLAS-DESIGN-001, Category Intelligence Foundation locked direction

## Outcome

VS-16 changes initial Business setup from primarily manual entry to a URL-first, evidence-aware flow:

Welcome → Sign in → paste a public business URL/page → Atlas discovers supported facts → owner reviews/edits/confirms → Business is created → continue into the existing Atlas owner journey.

The slice establishes a provider-neutral Public Business Snapshot boundary and trustworthy provenance without implementing Knowledge Pack v2, category recipes, progressive intelligence questions, private provider connectors, or the first category-aware Opportunity.

## Scope

### In scope

- align Welcome messaging to the Atlas closed loop: understand → find opportunities → act → measure → learn;
- make URL-first onboarding primary and keep manual Business setup as fallback;
- support ordinary HTTPS public business websites plus existing Wolt and Bolt Food public pages;
- expose the canonical Atlas category taxonomy and searchable selection/fallback behavior;
- extract only supported public facts and retain provenance;
- require owner confirmation before public facts become authoritative Business/Profile values;
- persist discovery evidence/provenance separately from canonical Business values;
- keep unknown facts explicitly unknown instead of fabricating demo data;
- provide loading, partial-result, unsupported/unavailable source, validation, retry, edit, manual-fallback and accessible states;
- keep the existing BrandMark abstraction and shared Atlas design tokens;
- preserve authentication, Business isolation, modular-monolith architecture and existing post-creation navigation contract;
- secure generic URL fetching against SSRF, private/local network access and redirects.

### Out of scope

- private Wolt/Bolt/POS/reservation/accounting connectors;
- scraping behind authentication;
- browser automation or JavaScript-rendered-page crawling;
- broad web search/review aggregation;
- Knowledge Pack Schema v2;
- Restaurant/Cafe opportunity recipes or metric engine;
- 3–5 progressive category questions (next slice);
- menu profitability/sales/repeat-rate inference;
- release, production enablement or deployment.

## Existing implementation to preserve and improve

Atlas already has `BusinessDiscoveryService`, `/api/v1/business-discovery`, `/api/v1/business-categories`, a canonical top-level taxonomy, Wolt/Bolt HTML metadata extraction, and a discovery/confirm/manual mobile flow. VS-16 evolves those boundaries rather than replacing them.

The existing create-business UI contains prototype-only Starbucks/demo branches and fabricated rating, review count, phone, opening hours, photos and fallback labels. VS-16 removes those branches. No screen may present public facts unless they were actually observed or supplied by the owner.

## Public Business Snapshot

A discovery operation returns a snapshot with:

- snapshot id;
- provider kind (`website`, `wolt`, `bolt-food`);
- canonical/final source URL;
- observed timestamp;
- fact collection.

Each fact contains:

- stable field key;
- observed value;
- evidence class (`public-observed`);
- provider/source;
- source URL;
- observed timestamp;
- confidence (`low`, `medium`, `high`);
- owner-confirmed state (false at discovery time).

Initial supported field keys are:

- `name`;
- `category`;
- `subcategory`;
- `description`;
- `primaryLocation`;
- `country`;
- `website`;
- `phone`;
- `openingHours`;
- `orderingChannels` when directly observed.

Missing facts are omitted, not guessed. Timezone and currency remain owner inputs unless a later trusted resolver is approved; VS-16 must not infer them from the owner device and label them as public observations.

## Persistence and trust model

Discovery snapshots are persisted server-side and tied to the authenticated owner account before Business creation. This avoids trusting client-submitted provenance.

The owner confirmation command references the snapshot id and sends the canonical Business fields plus per-field owner-confirmation/edit decisions. The server verifies that the snapshot belongs to the authenticated account.

For a discovered value accepted unchanged:

- canonical Business/Profile value is stored;
- a BusinessProfileField provenance record is stored as `public-observed`, with original source URL/provider/time/confidence and `OwnerConfirmed=true`.

For a discovered value edited by the owner:

- the canonical value uses the owner edit;
- the persisted provenance record classifies the authoritative value as `owner-reported` while retaining the original public observation as discovery evidence.

For a manually entered value with no public observation:

- canonical value is stored as owner-reported;
- no fabricated public provenance is created.

Discovery snapshot/fact records are not end-customer data and must remain account-scoped before Business creation and Business-scoped once consumed.

## API design

### `GET /api/v1/business-categories`

Retain the authenticated taxonomy endpoint. Return canonical category/subcategory keys and labels; Generic Business remains fallback.

### `POST /api/v1/business-discovery`

Input:

```json
{ "url": "https://example-business.com" }
```

Behavior:

1. validate HTTPS URL;
2. reject credentials in URLs, non-default unsafe URI forms, localhost and private/reserved network targets;
3. resolve/connect only to public IP addresses;
4. do not follow redirects automatically;
5. fetch bounded HTML with an 8-second timeout and maximum body size;
6. extract supported HTML/JSON-LD metadata;
7. classify provider and canonical category/subcategory where evidence supports it;
8. persist the authenticated owner's discovery snapshot/facts;
9. return the snapshot.

Errors use stable codes and safe copy: invalid URL, unsafe/private URL, unsupported content, redirected source, source unavailable, timeout and no useful public facts.

### `POST /api/v1/businesses/from-discovery`

Creates the initial Business atomically from a server-owned discovery snapshot plus confirmed/edited fields. It must:

- require BusinessOwner authentication;
- reject snapshots owned by another account;
- enforce one initial Business per owner;
- validate required Business fields and taxonomy keys;
- create/assign the Generic Business Knowledge Pack exactly as current Business creation does;
- persist canonical Business/Profile values;
- persist BusinessProfileField provenance;
- mark the snapshot consumed by the resulting Business;
- write audit records;
- return the normal `BusinessResponse`.

The existing `POST /api/v1/businesses` remains the manual fallback path and remains backward compatible.

## Generic website extraction

Ordinary HTTPS websites use conservative metadata extraction only:

1. schema.org JSON-LD `LocalBusiness`/subtypes when valid;
2. Open Graph metadata;
3. standard title/meta description;
4. directly present structured address, telephone and opening-hours fields.

Category matching uses the canonical taxonomy aliases against structured type/category/name/description text. A match may propose category/subcategory with confidence; otherwise return Generic Business or no proposed subcategory. Wolt/Bolt public pages may continue to propose Restaurant & Cafe because those provider domains are category-specific.

No sales, profitability, customer demographics, repeat rate, table utilization or popularity metric is inferred from page structure.

## SSRF and network safety

Generic URL support is a security boundary.

- HTTPS only.
- URL must not contain user-info credentials.
- Host must not be localhost or a local pseudo-domain.
- Literal IP hosts are validated before use.
- DNS resolution is performed at connection time and connections are allowed only to globally routable/public addresses.
- IPv4 loopback, private, link-local, multicast, unspecified, documentation/test and carrier-grade NAT ranges are rejected.
- IPv6 loopback, link-local, unique-local, multicast, unspecified and IPv4-mapped private addresses are rejected.
- automatic redirects remain disabled; redirect responses return a safe error rather than being followed to a potentially unsafe host.
- response body size is bounded before/while reading.
- content type must be HTML-compatible.

Security policy tests must cover representative private/reserved IPv4 and IPv6 addresses and safe public hosts.

## Mobile flow

### Welcome

Keep the approved warm/deep-green Atlas visual grammar and BrandMark boundary. Replace generic AI-co-pilot-only copy with concise Atlas loop messaging. The primary action remains Get started.

### Discover

- heading: tell Atlas about your business;
- one dominant URL field and `Discover my business` action;
- explain that Atlas reads supported public information and the owner confirms it before use;
- loading state describes only work actually being performed (public page/business information/category/location metadata), not fabricated review/social aggregation;
- `Set up manually instead` remains visible.

### Review and confirm

Show only facts returned by the snapshot or entered by the owner. Every public fact shows provenance and confidence in plain language, not color alone. Owner can edit proposed values before confirmation.

Required canonical fields that discovery could not supply are requested in this screen. Category uses the canonical taxonomy, not unrestricted free text. Public fields are not saved until the owner explicitly confirms the review.

Remove all Starbucks-specific/demo branches, invented ratings, phone numbers, hours, photos and labels. `BrandMark` remains a product placeholder, not evidence about the discovered business.

### Manual fallback

Retain manual Business creation with the same required canonical fields and searchable canonical category taxonomy. Manual values are owner-reported.

## Accessibility and responsive behavior

- approximately 44×44pt minimum interactive targets;
- semantic headings and button labels;
- clear loading/busy state for web and native accessibility APIs;
- validation associated with fields and announced accessibly;
- provenance/confidence never communicated by color alone;
- keyboard-safe scrolling and visible focused fields;
- dynamic text must not hide the primary action or provenance labels;
- phone and tablet layouts must avoid horizontal overflow;
- reduced-motion preference must avoid decorative looping animation when motion is not required for understanding.

## Data flow

```text
Authenticated owner
  → URL
  → safe public HTTP boundary
  → provider-neutral extractor
  → Public Business Snapshot + facts (account-scoped)
  → mobile review/edit/confirm
  → server verifies snapshot ownership
  → canonical Business/Profile
  + BusinessProfileField provenance
  + audit
  → existing Atlas owner journey
```

## Failure/degraded behavior

- invalid/unsafe URL: block before fetch and explain how to correct it;
- redirect: ask for the final public URL;
- network timeout/source unavailable: preserve URL, allow retry/manual fallback;
- partial extraction: show available facts and request only required missing fields;
- no useful facts: manual fallback without pretending discovery succeeded;
- stale/consumed snapshot: fail safely and require rediscovery or manual setup;
- authentication loss: return to sign-in without claiming Business creation;
- Business creation failure: preserve the review draft locally and allow retry.

## Testing strategy

### Domain/policy

- taxonomy keys/aliases/fallback;
- URL safety and IP classification;
- JSON-LD/OG extraction precedence;
- no fabricated facts;
- category inference boundaries;
- owner edit vs accepted-public provenance classification;
- snapshot ownership/consumption rules.

### API/integration

- authenticated discovery creates account-scoped snapshot;
- other owner cannot consume snapshot;
- consumed snapshot cannot be reused;
- manual Business creation remains compatible;
- discovery creation atomically creates Business, Profile/provenance, membership, pack assignment and audit;
- clean PostgreSQL migrations apply sequentially.

### Mobile/model/runtime

- URL validation/loading/retry/manual fallback;
- partial result and missing required fields;
- canonical category selection;
- public provenance/confirmation;
- owner edit conversion to owner-reported authoritative value;
- no Starbucks/demo copy in onboarding surfaces;
- phone/tablet layout, touch targets and accessibility busy/error states;
- successful confirmation stores Business id and reaches the existing authenticated owner journey.

## Definition of Done

VS-16 is complete only when:

- URL-first Business discovery works for ordinary HTTPS websites plus Wolt/Bolt public pages within the conservative extraction contract;
- unsafe/private URL targets are blocked;
- provenance is retained server-side and public values require owner confirmation;
- manual fallback remains functional;
- fabricated demo/Starbucks-specific onboarding data is removed;
- the Welcome/discovery/review flow matches the approved Atlas design grammar and BrandMark boundary;
- deterministic tests, mobile/runtime evidence, API tests, clean migrations, Security, Product Intake and CI pass at the exact implementation SHA;
- PES certification is recorded for that SHA;
- merge occurs only after certification and post-merge `main` CI passes;
- release and production remain unauthorized.
