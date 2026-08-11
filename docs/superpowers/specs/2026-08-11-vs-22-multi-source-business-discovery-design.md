# VS-22 — Multi-Source Business Discovery & Fact Reconciliation

Date: 2026-08-11
Status: Proposed design — implementation blocked pending Product Owner spec approval
Depends on: ATLAS-PRD-001, ATLAS-TRD-001, VS-21 working branch

## Outcome

Allow an owner to discover one Business from up to three public URLs while preserving a simple mobile flow, strict URL safety, owner-defined source priority, source-level provenance, duplicate suppression, safe fallback and truthful conflict handling.

The first URL is the primary source. Up to two additional URLs are optional. Atlas never requires a secondary source.

## Product rules

1. The first visible URL is the Primary source.
2. Additional sources are optional and are created only when the owner taps the in-field `+` control.
3. Maximum sources in VS-22: three total (one primary + two optional secondary sources).
4. The owner controls priority by field order. Atlas does not silently reorder sources based on provider type.
5. A secondary source may fill a field only when every higher-priority source lacks a usable value for that field.
6. A matching secondary value is retained only as corroborating evidence; it does not create a duplicate displayed fact.
7. A conflicting secondary value never silently overwrites the selected higher-priority value. Material conflicts are surfaced for review and retained as provenance.
8. A valid but unrelated secondary business must not contaminate the Business snapshot. If Atlas cannot confidently associate a source with the same Business, that source is excluded from reconciliation and surfaced as needing review/removal.
9. Customer-facing UI remains provider-neutral. Internal provenance retains the actual provider, canonical source URL, observation time and confidence.
10. The owner must still confirm publicly sourced business data before Business creation.

## Mobile interaction

### Initial state

Only one URL row is shown.

- Left `+`: adds another optional public URL row while fewer than three rows exist.
- Primary URL input: required only when the owner chooses discovery; manual setup remains available.
- Right `×`: appears whenever the row contains a value and clears it in one tap.

### Additional rows

Tapping `+` adds one row immediately below the previous rows.

- Additional rows are optional.
- Each additional row has its own `×` action. On an additional row, `×` removes the row entirely.
- Removing the second row shifts the third row upward while preserving source priority.
- At three rows, the add action is unavailable.
- Empty optional rows do not block discovery.

### Accessibility

Every action has an explicit accessible name and touch target:

- `Add another business page URL`
- `Clear primary business page URL`
- `Remove additional business page URL`

Errors are associated with the exact row and announced accessibly. Reduced-motion behaviour from the existing discovery screen remains unchanged.

## Immediate URL sanitisation

Every row uses the same client sanitiser. The API applies the same policy authoritatively before any network request.

When pasted text contains exactly one HTTPS URL, Atlas extracts that URL and discards surrounding share text. If pasted text contains multiple URLs or an ambiguous URL payload, Atlas rejects it rather than guessing.

Once a complete absolute URL is recognisable, Atlas immediately rewrites the field to the canonical safe form. While the user is still typing an incomplete URL, Atlas does not destructively rewrite partial input.

Sanitisation includes:

- trim whitespace and control characters;
- require HTTPS;
- remove URL fragments;
- reject credentials/user-info;
- reject non-standard ports;
- lowercase/canonicalise host representation;
- enforce maximum canonical URL length;
- remove known tracking parameters such as `utm_*`, `gclid`, `fbclid`, `msclkid`, share/referral analytics parameters and Google `g_st`;
- provider-specific query allowlisting where the provider path already identifies the Business;
- preserve unknown non-tracking query parameters only for ordinary websites where they may be required to identify the page;
- canonicalise trailing separators where safe;
- detect canonical duplicates across all three rows before discovery.

A URL that becomes empty, generic or ambiguous after sanitisation is rejected.

Example:

Input:
`Antalya Kebab St. Julian's - Bolt Food  https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians?utm_source=share_provider&utm_medium=product&utm_content=menu_header`

Canonical field value:
`https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians`

Google share input:
`https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86?g_st=ic`

Canonical field value before controlled Google resolution:
`https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86`

## Source-specific validation

### Ordinary Business website

Accepted when it is a public HTTPS page and passes the network policy. Atlas does not require a particular path structure. Known tracking parameters are removed; non-tracking query parameters may remain because they can be functionally significant.

### Bolt Food

`food.bolt.eu` is accepted only when the URL identifies a specific Business/restaurant page. Generic marketplace home, city, category, search and login/account URLs are rejected.

Tracking/share parameters are removed because the Business identity is carried by the canonical path.

### Wolt

Wolt is accepted only when the URL identifies a specific venue/restaurant page. Generic Wolt home, discovery, search, city/category and account routes are rejected.

Tracking/share parameters are removed when the venue path already identifies the Business.

### Google Maps / Google Business location

Accepted Google source forms include:

- `maps.app.goo.gl/<token>` specific place share links;
- Google Maps place URLs that identify one establishment;
- canonical Google place identifiers resolved by the existing Google Places adapter.

Generic Google Search URLs such as `google.com/search?q=...`, map-area links, broad directions links without one resolved establishment, and search-result pages are rejected as authoritative Business sources.

Atlas does not scrape Google Maps HTML for Business facts. Google source URLs are resolved into the existing Google Places provider boundary.

## Google short-link safety

Google short links are the only source type permitted to use controlled redirects in VS-22.

The resolver:

1. validates the initial `maps.app.goo.gl` URL;
2. disables automatic redirects;
3. follows at most a small fixed redirect count;
4. revalidates every redirect target;
5. allows only approved Google Maps/Google hosts in the redirect chain;
6. resolves DNS and rejects private, loopback, link-local, reserved or otherwise non-public addresses at every hop;
7. requires HTTPS and standard port at every hop;
8. rejects cross-provider redirects;
9. extracts/resolves the specific place and then calls the approved Google Places adapter.

No arbitrary redirect following is added for ordinary websites, Bolt or Wolt.

## Network and SSRF security boundary

Server-side validation is authoritative regardless of mobile sanitisation.

Before fetching any non-Google webpage Atlas must enforce:

- HTTPS only;
- no credentials;
- standard HTTPS port only;
- no localhost/internal/test/local hostnames;
- no private, loopback, link-local, carrier-grade NAT, documentation, multicast or reserved addresses;
- DNS resolution before connect and public-address enforcement on the actual connected address;
- no proxy routing;
- no automatic redirects;
- bounded connection/request timeout;
- HTML content type requirement;
- bounded post-decompression read budget;
- bounded fact lengths and structured-output validation;
- regex/parser timeouts;
- no script execution, browser automation or remote code execution;
- no cookies/authentication to third-party Business sources;
- provider secrets remain server-side.

IP-literal Business URLs are rejected in VS-22 even when the literal is public because they are unnecessary for the target customer workflow and expand the attack surface.

## Large-page handling

A valid public restaurant page must not fail merely because its total response is larger than Atlas needs.

Atlas reads only a strict safe prefix budget and stops consuming the response when that budget is reached. It never downloads an unbounded page into memory. Extraction operates on the bounded content already read.

If useful metadata exists inside the safe prefix, discovery may succeed even when the remote response is larger. If useful facts are not found inside the bounded content, the source degrades to `no useful facts` and later sources may still provide the missing data.

This replaces the current behaviour that rejects an otherwise useful Wolt page solely for exceeding the read cap.

## API contract

Keep backward compatibility with the existing primary `url` field and add optional secondaries:

```json
{
  "url": "https://primary.example/business",
  "additionalUrls": [
    "https://secondary.example/business",
    "https://maps.app.goo.gl/example"
  ]
}
```

Rules:

- `url` is the owner-selected primary source;
- `additionalUrls` contains zero to two values;
- blank optional values are discarded;
- all values are server-sanitised and revalidated;
- canonical duplicates are rejected/ignored deterministically;
- source order is retained.

The discovery response remains centred on one reconciled Business snapshot and also returns provider-neutral source status/warnings required by the mobile review flow.

## Source processing and fallback

Atlas attempts sources in owner-defined priority order, but a network failure in one source does not automatically fail the whole discovery.

- If primary returns usable facts, those facts establish first priority.
- If primary is reachable but a field is absent, secondary 1 may fill that field, then secondary 2.
- If primary is unavailable or yields no usable facts, secondary 1 may establish the first usable Business identity and facts; secondary 2 may then fill gaps.
- If at least one validated source yields useful facts, discovery can continue with warnings for failed optional sources.
- If no source yields useful facts, the request fails with a stable actionable error and manual setup remains available.

An invalid or unsafe URL is a validation error, not a degradable network warning. Atlas never fetches it.

## Business identity association

Before a secondary source contributes facts, Atlas checks that it refers to the same Business identity as the current anchor.

Evidence may include normalised Business name, marketplace merchant identity, resolved Google Place/display name and location/address signals when available.

- Strong match: source may contribute according to precedence.
- Ambiguous match: source facts are retained as unmerged evidence and the owner is warned.
- Clear mismatch: source is excluded from reconciliation.

If the primary yields no identity, the first successful secondary becomes the temporary identity anchor for that discovery snapshot.

Atlas must prefer false-negative enrichment over silently mixing two different Businesses.

## Field-level reconciliation

For each supported fact key:

1. take the first usable value in source priority order;
2. normalise later values for comparison;
3. if later value is equivalent, retain it as corroborating evidence only;
4. if later value differs, retain it as conflicting evidence and keep the higher-priority selected value;
5. never fabricate a merged value;
6. never allow a lower-priority source to overwrite a selected higher-priority fact automatically.

Location, country, timezone and currency continue through the VS-21 canonical location-resolution flow. Arbitrary marketplace text cannot override canonical market metadata.

## Provenance persistence

The existing `BusinessDiscoveryFact` remains the reconciled selected fact used by the confirmation/create-Business flow.

VS-22 adds source/evidence persistence so selected, corroborating, conflicting and excluded observations remain auditable without duplicating the owner-facing fact list.

Minimum persisted source/evidence metadata:

- discovery snapshot ID;
- source order and primary/secondary role;
- actual provider identifier;
- canonical source URL;
- observation time;
- per-source status;
- fact key/value;
- confidence/evidence class;
- selected/corroborating/conflict/excluded state;
- association decision where relevant.

A forward-only EF Core migration is required. Existing single-source discovery records remain readable.

## Confirmation UI

Provider names and marketplace hostnames remain hidden from owner-facing confirmation copy.

The page may say, for example:

- `9 public facts ready for review`
- `3 public sources checked`
- `1 detail needs review` when there is a material source conflict

The exact internal provider/source provenance remains available for audit/trust records.

`Confirm and continue` must successfully consume a valid multi-source snapshot without a server 5xx. The existing confirmation regression observed during VS-21 testing is a certification blocker for VS-22 even if its root cause predates this slice.

## Error model

Stable cases include:

- invalid/unsafe URL;
- duplicate source;
- unsupported generic provider route;
- Google URL does not identify one Business;
- source unavailable;
- source timeout;
- no useful facts;
- source identity mismatch/ambiguous association;
- all sources unavailable/no useful facts.

Unsafe inputs fail before any external request. Optional network failures degrade when another source succeeds.

## Testing strategy

### URL policy

Table-driven tests cover:

- share-text extraction with one HTTPS URL;
- rejection of multiple embedded URLs;
- tracking/query/fragment stripping;
- malformed URL handling;
- credentials and non-443 ports;
- IP literals;
- private/reserved DNS/address classes;
- canonical duplicate detection;
- Bolt specific-business route acceptance and generic-route rejection;
- Wolt venue acceptance and generic-route rejection;
- Google Maps share/place acceptance;
- generic Google Search rejection;
- Google controlled redirect host/IP/redirect-count enforcement.

### Reconciliation

Tests cover:

- primary wins duplicate value;
- secondary fills missing primary fact;
- third source fills when first two lack a fact;
- duplicate values become corroboration, not duplicate facts;
- conflicts retain higher-priority selected value;
- unrelated secondary source cannot contribute;
- primary failure falls back safely;
- optional secondary failure does not fail useful discovery;
- provenance for every candidate is retained.

### Mobile

Tests cover:

- one row initially;
- `+` adds rows up to three;
- optional rows are not required;
- every populated row has one-tap `×` clear/remove;
- pasted share text becomes the canonical URL in the visible input;
- invalid row shows an accessible inline error;
- duplicate source is blocked;
- provider-neutral confirmation copy;
- reduced motion and touch/accessibility semantics.

### Regression/integration

- large Wolt-style response is safely bounded rather than rejected solely for total size;
- Bolt/Wolt provider branding does not leak into confirmation UI;
- valid discovery can complete `Confirm and continue` without 5xx;
- migrations are forward-only and registry/model snapshot checks pass;
- CI, Security baseline, Product Intake/governance and exact-head certification pass.

## Out of scope

- more than three URLs;
- crawling a whole website/domain;
- authenticated/private source access;
- social-network scraping;
- arbitrary redirect following;
- browser automation for third-party pages;
- review-content ingestion as an authoritative owner fact;
- autonomous resolution of material conflicts without owner review;
- production deployment as part of this slice.

## Branch and release model

Implementation branch: `atlas/vs22-multi-source-discovery`.

VS-22 does not develop on `atlas/preview-deployment` or `atlas/test-deployment`. After implementation, deterministic tests, review, governance/security/product gates and exact-SHA certification, the Product Owner may approve merge to `main`.

Deployment remains a separate explicit action after merge/release approval. No deployment branch is modified merely because VS-22 becomes merge-ready.

## Acceptance

VS-22 is acceptable when an owner can paste one primary URL and optionally add up to two more, each URL is immediately canonicalised when complete and authoritatively revalidated server-side, unsafe/generic/duplicate inputs are blocked before fetching, Google place links use a controlled Places resolution path, secondary sources fill only missing facts, duplicates do not duplicate facts, conflicts/mismatches do not silently contaminate the Business, provenance remains auditable, confirmation succeeds, and all exact-head gates pass without production deployment.