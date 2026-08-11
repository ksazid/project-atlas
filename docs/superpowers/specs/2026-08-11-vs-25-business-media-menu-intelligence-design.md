# VS-25 Business Media & Menu Intelligence — Design

## Context

VS-21/22 public discovery already establishes provider-neutral URL ingestion, SSRF-safe fetching, multi-source reconciliation, source/evidence provenance, owner review and Business creation. VS-25 extends that existing boundary; it does not create a second scraping subsystem.

The Product Owner explicitly asked Atlas to retain restaurant images when available and approved the sequence to add business media capture followed by structured menu/menu-price ingestion.

## Design decision

Use two generic core concepts:

- **Business Media Reference** — a provenance-rich reference to remotely hosted public media. Public-source records retain URLs and metadata only; Atlas does not copy the binary.
- **Business Offering** — a provenance-rich commercial item. `menu-item` is the first offering kind, but the entity remains suitable for later product/service catalogue facts without restaurant-specific core tables.

Discovery has pre-confirmation snapshot children for both concepts. Once the owner confirms and creates the Business, accepted discovery records are materialised to Business-owned records while retaining their public/unconfirmed evidence status.

## Public extraction

### Media

Prefer structured `image` values from the selected business JSON-LD. Support string, array and ImageObject-style URL/contentUrl values. Use `og:image` only as a conservative business-image fallback.

For an already-supported marketplace whose public server-rendered page exposes stable semantic item markup, Atlas may additionally retain the item image URL as `menu-item-image`. The adapter must operate only on the same public HTML already accepted by the discovery boundary; it must not call private APIs, replay browser credentials, or bypass provider controls.

Retain only absolute HTTPS URLs that satisfy the same public URL/address rules as discovery. Canonicalise and deduplicate. Do not send GET requests for the image content in this slice.

### Menu / offerings

Prefer schema.org `Menu`, `MenuSection` and `MenuItem` objects from public JSON-LD when present. Extract:

- offering kind (`menu-item`);
- section;
- name;
- description when present;
- price when parseable and non-negative;
- ISO-style currency when present;
- source/provider/page URL;
- observed time;
- confidence/evidence class;
- deterministic source order.

If an already-supported marketplace exposes the same information directly in stable, public, server-rendered semantic HTML, a bounded provider adapter may extract those fields as a fallback. Provider adapters must be fail-closed: missing or changed semantic markers produce no offerings rather than guessed data. They remain subject to the same field limits, URL validation, provenance rules and collection caps as JSON-LD extraction.

For Bolt Food, the observed public SSR contract uses provider semantic markers for category headings, dish records, descriptions and prices; dish image alt text supplies the public item name. This is treated as a public-HTML fallback, not as a private provider integration.

If structured item data is absent but a public `menu` or `hasMenu` URL exists, retain that URL as a `menuUrl` public fact so Atlas knows where the public menu was observed without fabricating item data.

## Multi-source behaviour

Existing identity reconciliation remains authoritative. Only the anchor and secondary sources classified `strong` may contribute media or offerings. `mismatch`, `ambiguous` and unavailable sources remain recorded as source diagnostics but their media/menu data are excluded.

Media merge key: canonical remote URL.

Offering merge key: normalised `(kind, section, name, price, currency)`. Earlier source priority wins; equivalent later records are corroborating provenance rather than duplicate Business items.

## Persistence

Discovery layer:

- `BusinessDiscoveryMediaReferences`
- `BusinessDiscoveryOfferings`

Business layer after confirmation:

- `BusinessMediaReferences`
- `BusinessOfferings`

Every Business-layer row carries `BusinessId`. Discovery rows carry `SnapshotId` and source metadata until confirmation. Cascades follow existing discovery snapshot ownership. Business materialisation occurs inside the existing discovery-confirmation transaction.

Public-derived Business rows set `OwnerConfirmed=false` unless a future UI explicitly asks the owner to confirm that specific media/offering. This avoids incorrectly treating the existing profile confirmation as confirmation of an unseen menu catalogue.

## Limits and safety

- Max 24 media references per accepted source.
- Max 250 structured/public-semantic offerings per accepted source.
- Max 2,000 chars for remote/source URLs.
- Bounded text fields and deterministic truncation rejection rather than silent semantic truncation.
- No credentials/userinfo, non-HTTPS URLs or private-network literals.
- No binary image storage, OCR, PDF parsing or private provider APIs.
- No browser automation in the production discovery path.
- Existing discovery request timeout and HTML size cap remain unchanged.

## API contract

The discovery response may expose `media` and `offerings` collections so the mobile/client can use them later without a breaking endpoint replacement. Existing `facts` remain unchanged.

Business creation does not require the owner to edit or confirm every media/offering record. The server materialises only accepted snapshot records and preserves provenance.

## Testing

TDD coverage must include:

- image string/array/ImageObject extraction;
- OG image fallback;
- blocked/non-HTTPS media references rejected;
- menu section/item extraction and price/currency parsing;
- supported-marketplace public semantic HTML fallback and fail-closed behaviour;
- menu URL fallback without fabricated item data;
- per-source collection limits and dedupe;
- mismatch/ambiguous secondary source exclusion;
- discovery snapshot persistence;
- Business materialisation and isolation;
- migrations on PostgreSQL;
- existing discovery/onboarding regressions.

## Non-goals

No UI redesign, image gallery, owner upload, menu editor, catalogue sync, private provider integration, production browser automation, media CDN or production rollout in VS-25.