# VS-36 — Menu & Media Coverage Recovery Design

## Goal
Recover truthful Business media and structured menu coverage from already-authorised public sources without reopening the VS-25 storage model, weakening SSRF/public-source controls, or introducing private provider APIs or production browser automation.

## Observed gap
VS-25 certified JSON-LD and Bolt semantic-HTML extraction against the public page shape available at the time. Current Bolt Food pages are inconsistent for non-JavaScript readers: some public representations expose rich menu sections, item descriptions, prices and remote image references, while a direct non-JavaScript fetch may expose only a JavaScript-required shell. Atlas currently treats an extraction with no media/menu as indistinguishable from a Business that genuinely has no public menu/media.

VS-36 must improve parseable public-document coverage and make the unparseable/renderer-dependent case explicit. It must never fabricate menu items from search snippets or infer that a missing extraction means the Business has no menu.

## Existing authority
- FR-02, FR-03 and FR-05: public Business discovery/profile/context may use relevant public facts with owner confirmation where required.
- DEC-04: ordinary HTTPS business websites plus Wolt/Bolt public pages are authorised discovery sources through the SSRF-safe public snapshot boundary; private connectors remain out of scope.
- DEC-07: retain provenance-rich remote media references and structured factual offerings only; do not copy/rehost third-party image binaries.
- DEC-08 remains unchanged for Google content and is not expanded by this slice.

No new provider/storage decision is required.

## Architecture
Keep the existing `BusinessDiscoveryService -> PublicBusinessExtractor -> PublicBusinessMediaMenuExtractor -> reconciliation -> persistence` path. Add bounded extraction helpers and a provider-neutral coverage classification; do not add a second ingestion subsystem.

The extractor order is:
1. schema.org JSON-LD Business/Menu/Image data;
2. existing provider semantic HTML;
3. bounded embedded public page-state found inside the same fetched HTML document, only when it can be parsed as data already delivered in that response;
4. safe OG-image fallback;
5. truthful coverage diagnostic when menu/media cannot be observed from the fetched public document.

Embedded-state parsing is allowlisted by shape, bounded by the existing document/item/media limits, rejects malformed/unbounded data, and never executes JavaScript.

## Provider-neutral coverage classification
Add an extraction coverage result with stable states such as:
- `structured` — menu/media observed from JSON-LD;
- `semantic-html` — observed from approved provider/public semantic HTML;
- `embedded-public-state` — observed from bounded serialized public state contained in the fetched HTML;
- `media-only` — safe public media observed but no structured offerings;
- `renderer-required` — the document identifies a supported provider/business page but contains only a JavaScript-required shell and no parseable menu/media;
- `none` — no supported media/menu evidence was present.

Coverage metadata is diagnostic/provenance, not a Business fact and not an owner claim. It must not be presented as “no menu exists.”

## Bolt recovery rules
For `food.bolt.eu`:
- preserve the certified VS-25 semantic markup parser;
- add tests for current page-shape variants rather than replacing the old parser;
- accept only public data contained in the fetched HTML response;
- do not mimic Google/Bing crawler identities;
- do not call undocumented/private Bolt APIs;
- do not run a headless browser in production;
- retain existing URL, size, item-count, media-count and HTTPS-only limits.

If the fetched page is renderer-only, Atlas returns the normal Business discovery facts that are still observable and records `renderer-required` menu/media coverage. Discovery must not fail solely because menu/media enrichment is unavailable.

## Reconciliation and persistence
Existing source identity reconciliation remains authoritative. Menu/media from mismatched or ambiguous secondary sources stays excluded.

Existing `BusinessMediaReference` and `BusinessOffering` materialisation remains unchanged. A coverage diagnostic may be retained with discovery provenance only if an existing metadata/provenance extension can carry it without a schema migration; otherwise it remains response/diagnostic-only in VS-36. No migration is justified solely for this diagnostic.

## Owner experience
The Business Hub continues to show only persisted, provenance-backed media/menu intelligence. Empty menu/media states become more truthful when coverage is known to be renderer-dependent: use provider-neutral copy such as “This public source did not expose menu details to Atlas. Add another public business/menu page or try again later.”

Do not add a new tab, dashboard, ordering flow, or restaurant-only owner workflow. The existing full-menu read-only screen and Profile source/edit flows remain the surface.

## Error handling
- Malformed embedded state: ignore that representation and continue existing fallbacks.
- Unsafe/non-HTTPS media URL: reject it.
- Oversized/unbounded state or offering list: stop at existing caps.
- Provider page becomes JS-only: classify renderer-required; do not fail the whole Business discovery if useful identity/profile facts remain.
- No useful Business facts at all: preserve existing `business_source_no_facts` behavior.

## Testing
Use strict TDD:
1. characterization coverage for certified VS-25 JSON-LD and semantic Bolt shape;
2. RED fixtures for at least two current Bolt-style variants (for example Chick'n Bites and McDonald's page families) using sanitized deterministic HTML/embedded-state shapes;
3. RED/green coverage-state tests for renderer-required versus true none;
4. reconciliation tests proving mismatched media/menu remains excluded;
5. persistence/Business Hub tests proving only accepted provenance-backed records render;
6. full mobile/API/preflight, clean PostgreSQL replay, Security baseline and Product Intake.

Live public pages may be used only as supplemental evidence because provider markup is mutable; deterministic fixtures remain certification authority.

## Compatibility
VS-36 starts from merged VS-35 main `ff4ca1ac2245ef8f29f4efc5d04693d99f7ec597`. Today / History / Goals / Profile, Pilot Operations, Opportunity generation, Knowledge Packs, Google enrichment and owner authority remain unchanged.

## Explicit non-goals
- private/undocumented Bolt or Wolt APIs;
- crawler impersonation;
- production headless-browser scraping;
- copied/rehosted third-party image binaries;
- search-engine/cache content as Atlas source evidence;
- POS/menu provider connectors;
- ordering, inventory, price-history or competitor intelligence;
- new database migration unless an existing required persisted contract is proven impossible without one;
- deployment, EAS/OTA, release or production database mutation.
