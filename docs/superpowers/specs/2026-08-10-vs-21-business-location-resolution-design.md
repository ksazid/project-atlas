# VS-21 Business Location Resolution — Design Specification

## Problem

Live Expo Go validation showed that URL-first discovery can return provider-generic metadata (`Bolt Food`) instead of the venue, and then force the owner to type country, timezone and currency manually. This creates two trust failures: wrong Business identity and non-canonical location metadata.

## Desired owner journey

1. Owner pastes a public business/marketplace URL.
2. Atlas extracts merchant-specific identity where the source exposes it.
3. Atlas attempts to resolve an operating location.
4. If exactly one strong candidate exists, Atlas preselects it and asks the owner to confirm or change.
5. If multiple candidates exist, Atlas asks `Which location are you setting up?` and shows concise branch cards.
6. If no reliable candidate exists, Atlas offers `Find your business location` and searches through the configured location provider.
7. Owner selects one location.
8. Atlas derives and stores canonical country, timezone and currency automatically.
9. Owner reviews the combined Business + selected Location facts and continues.

## UX rules

- One owner-facing location choice replaces country/timezone/currency text fields.
- Friendly display labels are shown; technical ISO/IANA values remain internal.
- A branch-specific Wolt/Bolt URL may preselect its matching location when evidence is strong.
- Owner can always change a preselected location before confirmation.
- Multiple branches do not require bulk setup during first use.
- Public/provider facts remain labelled and owner-confirmed.
- Unknown state explains the next useful action; it never asks for technical codes.
- Existing Atlas visual grammar, spacing, cards, buttons and accessibility rules remain authoritative.

## Technical design

### Public discovery

Marketplace extraction remains conservative. Provider-specific extraction may add merchant identity/location hints, but all outputs normalize to `PublicBusinessSnapshot` facts with provenance.

### Location resolution boundary

Businesses owns a provider-neutral location port. Conceptual contract:

- Search(query, market hints) -> candidate locations
- Resolve(place reference) -> canonical selected location

Canonical location includes provider reference, display name, formatted address, latitude, longitude, country code/name, timezone and currency.

Google Places/Place Details + Time Zone is the first adapter for the test/pilot path. Provider credentials are server-side configuration. No provider SDK is called by domain code.

### Deterministic metadata

Country-to-currency mapping is Atlas-owned deterministic data. Timezone is resolved from coordinates/provider result and validated as an IANA identifier before persistence.

### Validation

Server remains authoritative. Mobile performs equivalent local state checks for interaction quality but does not replace server validation. Problem Details field errors are surfaced to the owner.

## Security

- Preserve existing public-URL SSRF policy.
- Do not expose Google server keys to mobile.
- Bound search query lengths and result count.
- Do not log provider credentials or full sensitive request headers.
- Only public business/location information is queried in this slice.

## Data model direction

The product semantics distinguish Business from operating Location. This remediation avoids a broad schema rewrite unless required for correctness; selected canonical location data can continue through existing Business/Profile fields while the separate Location entity is introduced only with a governed migration if acceptance tests prove it necessary.
