# VS-27 Business Hub & Atlas Brand Experience — Design

## Context

VS-25 introduced provenance-rich Business Media References and Business Offerings, including public restaurant image references and structured menu items. The current Business tab remains an edit-first profile form, the bottom navigation uses typographic placeholder symbols, and the centralized `BrandMark` still loads the Starbucks logo from Wikimedia.

The Product Owner approved replacing the Business tab with a read-first business hub, keeping menu intelligence compact on the hub, providing a dedicated full-menu view, and replacing the prototype Starbucks mark with an Atlas-owned mark that borrows premium circular visual confidence without copying Starbucks artwork.

VS-26 is concurrently active in PR #47 (`atlas/vs26-google-place-intelligence`). VS-27 therefore remains specification-only until VS-26 is certified/merged and the implementation branch is rebased from the resulting `main`.

## Product outcome

The Business tab should make the owner immediately feel that Atlas understands the real business rather than presenting a settings form. It must answer, at a glance:

- Which business and operating location is Atlas working with?
- What public/owner-confirmed facts does Atlas know?
- What visual identity and business imagery has Atlas observed?
- What menu/catalogue intelligence does Atlas have?
- Where did those facts come from, and how fresh are they?
- What can the owner review or edit next?

The Business tab is not a food-ordering experience. Menu and media exist as business intelligence inputs.

## Approved visual direction

Use the existing Atlas/Starbucks-derived visual grammar already present in the approved screens:

- deep forest editorial headings;
- Starbucks-like green hierarchy without Starbucks branding;
- white primary canvas with warm/soft secondary surfaces;
- large editorial typography, generous spacing and restrained borders;
- rounded cards with subtle depth rather than dense dashboard chrome;
- one dominant action per section;
- readable, owner-oriented copy instead of technical provider language.

Do not copy Starbucks logos, siren artwork, wordmarks, cup artwork, trade dress, or branded illustrations.

## Atlas brandmark

Replace the remote Starbucks prototype image with an Atlas-owned vector/component mark.

### Form

Use a compact circular **Atlas Compass Orbit**:

- outer circular ring expressing completeness/continuity;
- four restrained directional ticks or orbit breaks suggesting navigation/intelligence;
- a small central point/node representing the business as the source of truth;
- optional subtle asymmetric arc to imply motion/agent activity;
- no letters inside the mark at small sizes;
- no siren, crown, stars, mermaid, Starbucks geometry or copied linework.

### Behaviour

- implemented locally with React Native primitives/SVG-compatible vector shapes; no remote brand asset dependency;
- supports `size`, decorative/accessibility semantics and light-background usage;
- legible at bottom-nav scale and hero scale;
- uses Atlas green/forest tokens only;
- centralized through the existing `BrandMark` API so later replacement remains one-file work.

The Business tab may use a larger mark only where no real business hero image is available. Real business imagery takes precedence over decorative branding.

## Business Hub information architecture

The existing `profile` tab remains the route boundary to avoid changing PES mobile navigation structure. Its content changes from edit-first form to a read-first hub.

### 1. Hero and business identity

At the top of the Business tab:

- hero image from accepted Business Media References when available;
- graceful Atlas-branded fallback when no suitable image exists;
- business name;
- category/subcategory;
- operating location;
- compact provenance/confidence indicator;
- optional short public/owner description when useful.

The hero must avoid showing broken remote images as dominant content. Failed media references fall back without blocking the page.

### 2. Business snapshot

A compact owner-readable facts card showing the highest-value operating facts already known, such as:

- location;
- opening hours;
- phone/website when available;
- primary category;
- source/freshness summary.

This is read-only on the hub. `Edit business details` is a secondary action that opens the existing editable profile experience on a dedicated route/screen rather than keeping every field inline.

### 3. Business photos

Show a restrained gallery preview of up to 3–6 accepted image references:

- one dominant image plus smaller supporting images when layout permits;
- preserve remote-source provenance;
- no binary copying/rehosting for public third-party images;
- no requirement for every image to be owner-confirmed before read-only display, but public/unconfirmed status remains transparent where relevant.

A future owner media manager is out of scope.

### 4. Menu intelligence preview

The Business hub shows **summary intelligence, not the full menu**.

Display only useful aggregate/preview information, for example:

- number of menu sections/categories;
- number of observed items;
- observed price range when prices exist;
- 3–5 representative/high-signal sections or items;
- public source and observed/freshness metadata;
- `View full menu` action.

Do not present Atlas as an ordering surface and do not add cart/order controls.

### 5. Dedicated Menu screen

`View full menu` opens a dedicated Business Menu route within the existing Expo Router stack.

The screen groups Business Offerings by section and may show:

- section name;
- item name;
- description;
- observed price/currency;
- associated image when a trustworthy item/media association exists;
- provenance/source/freshness in a secondary detail treatment.

The menu is read-only in VS-27. Owner editing, catalogue sync, availability toggles and profitability editing remain future work.

### 6. Business intelligence/context status

Show a small status section that communicates whether Atlas has enough information to guide the business, using owner-friendly states rather than a technical completeness percentage.

Examples:

- `Atlas has a strong operating picture`;
- `A few details would improve recommendations`;
- `Review business context`.

This section links to the existing Context tab and does not duplicate the Context editor.

### 7. Secondary actions

Use restrained actions beneath the read-first content:

- `Edit business details`;
- `Review business context`;
- `View full menu` where menu data exists;
- source/freshness detail when the owner wants to inspect provenance.

The Business hub itself should not become an action grid.

## Home visual review

VS-27 may improve Home through shared visual primitives only unless a post-VS-26 rebase shows a safe isolated Home change.

Allowed:

- new Atlas brandmark through the centralized `BrandMark` component;
- shared spacing/iconography consistency;
- bottom-nav visual consistency.

Not allowed without a fresh conflict review:

- changing Today Focus readiness/business logic;
- changing recommendation state transitions;
- changing VS-26 onboarding/goal-first routing.

This prevents collision with concurrent/stale work that touches Today Focus or onboarding.

## Bottom navigation

Keep the existing five-tab PES structure and route names:

1. Home
2. Business
3. Goals
4. Context
5. Settings

Replace typographic placeholder glyphs (`⌂`, `◎`, `↗`, `◌`, `⚙`) with one coherent locally owned icon family or vector treatment.

Requirements:

- consistent stroke/weight and optical size;
- active green, muted inactive state;
- no emoji-style Settings gear;
- accessible labels remain owned by tab configuration;
- minimum touch targets remain unchanged or improve;
- no navigation architecture change.

The Business icon may echo the circular Atlas Compass Orbit without turning the entire tab bar into brandmarks.

## Read model / API boundary

VS-27 should not force the mobile client to assemble a business hub from many unrelated requests. Add one account-isolated read contract for the Business Hub after VS-26 is merged and its final API shape is known.

Recommended shape:

`GET /api/v1/businesses/{businessId}/hub`

The response should be a read model composed from already-owned Atlas data, for example:

- business/profile identity fields;
- selected operating location/canonical market fields;
- accepted Business Media References;
- menu summary plus a small bounded preview of Business Offerings;
- provenance/freshness summaries;
- context/readiness summary suitable for owner presentation.

A separate bounded endpoint should return the complete persisted menu catalogue for the dedicated screen, e.g.:

`GET /api/v1/businesses/{businessId}/offerings?kind=menu-item`

The implementation must preserve existing business/account isolation and fail-safe not-found behaviour.

VS-27 does not persist new third-party provider facts simply to render the hub. It reads Atlas-owned persisted records created by prior governed discovery/enrichment flows.

## Client components

Create small, independently testable presentation units rather than growing `profile.tsx` further:

- `BusinessHubScreen` — orchestration/state boundary;
- `BusinessHero` — image/fallback + identity;
- `BusinessSnapshotCard` — operating facts;
- `BusinessMediaPreview` — bounded gallery;
- `MenuIntelligenceCard` — menu summary/preview;
- `BusinessContextStatus` — contextual readiness link;
- `BusinessMenuScreen` — grouped read-only offerings;
- `AtlasIcon`/local icon primitives for coherent tab icons;
- centralized `BrandMark` implementation for the Atlas Compass Orbit.

The existing editable Profile form should move behind a dedicated edit route while preserving its model and save semantics unless the post-VS-26 rebase requires a small compatibility adjustment.

## Loading, empty and error states

Business hub state handling must remain truthful and recoverable:

- loading: branded skeleton/progress without fake facts;
- no business/session: existing guarded continuation path;
- profile present but no media: show branded fallback, not an empty gallery shell;
- no menu: omit the menu preview or show a concise `No menu observed yet` state without treating it as an application error;
- partial media failure: hide failed references and retain usable images;
- API failure: keep retry action and do not expose raw provider/server errors;
- stale/public facts: show source/freshness copy rather than pretending owner confirmation.

## Accessibility and responsive behaviour

- preserve semantic headers and accessible button labels;
- business image alt text should use stored alt text when meaningful, otherwise decorative semantics for redundant imagery;
- tab icons remain accompanied by text labels;
- minimum 44pt interactive targets;
- support reduced motion;
- layouts must remain usable on narrow Expo Go phone screens and larger web/tablet widths;
- no horizontal menu tables on mobile; grouped cards/lists only.

## Conflict and sequencing policy

VS-27 implementation must not start from the current design branch.

Before runtime implementation:

1. PR #47 / VS-26 must be certified/merged or explicitly superseded.
2. Re-read `AGENTS.md`, product docs, `delivery/current-slice.json`, decisions and applicable skills.
3. Rebase/create the VS-27 implementation branch from the then-current `main`.
4. Compare VS-26 changed files against the planned VS-27 files.
5. Reconfirm the API/read-model boundary if VS-26 introduced a new Business place/intelligence contract.
6. Activate VS-27 in PES only after the current active slice is clear.

Known current VS-26 changed surface includes governance/spec files and `BusinessPlaceEnrichmentTests.cs`; its approved plan is expected to expand into business creation/onboarding and therefore must complete first.

## Testing strategy

Use Superpowers TDD inside PES/Loop.

Server tests:

- account-isolated hub read model;
- media ordering/bounds and broken/unusable reference filtering rules;
- menu summary counts, price range and representative preview;
- full menu offering grouping/read contract;
- provenance/freshness mapping;
- no new persistence required solely for UI composition.

Mobile model/component tests:

- hero image vs Atlas fallback;
- menu summary vs no-menu state;
- no fake facts during loading/partial states;
- secondary action routing;
- locally owned brandmark and no Starbucks remote asset/reference;
- bottom nav keeps the five existing route names and accessible labels;
- dedicated menu presentation groups sections correctly;
- existing Profile edit/save behaviour remains intact behind the edit route.

Runtime acceptance:

- authentic Expo Web/Expo Go rendering at narrow mobile width;
- Hasan's Turkish Kebab House test data visibly resolves to real business identity, available imagery and compact menu intelligence when persisted records exist;
- visual consistency across Home, Business, Goals and Context after shared brand/nav changes;
- no production deployment as part of slice implementation/certification.

## Non-goals

- no ordering/cart/checkout flow;
- no menu editing or POS/catalogue sync;
- no owner media uploader/manager;
- no copying or rehosting third-party image binaries;
- no redesign of Goals/Context business logic;
- no Today Focus logic changes unless separately governed after conflict review;
- no PES navigation/framework restructuring;
- no production release or enablement.
