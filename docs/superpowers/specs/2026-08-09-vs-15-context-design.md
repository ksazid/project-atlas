# VS-15 Context Visual Migration Design

## Status and authority

This design is a bounded implementation interpretation of the Product Owner's approved Atlas visual-migration sequence. It implements FR-05 Business Context only and introduces no new product, architecture, security, release, or production decision.

Authority remains, in order: `product/PRD.md`, `product/TRD.md`, `product/DESIGN.md`, `docs/slices/VS-02.md`, `delivery/governance.json`, `delivery/decisions.json`, the completed VS-13/VS-14 migration patterns, and this written specification. The approved warm-neutral/deep-green Atlas visual grammar is reused. Atlas remains the product identity and the temporary prototype mark may be rendered only by `BrandMark`.

## Outcome

Migrate the existing authenticated Business Context route to the approved Atlas visual language while preserving the current Expo Router route, Business Context API contract, authentication, Business isolation, persistence behavior, and navigation model. Owners can add only the context they choose, understand provenance, confirm public context before saving, recover from loading failures without losing edits, and save with clear accessible feedback.

## Scope

- Keep `apps/mobile/app/(tabs)/context.tsx` as the Expo Router route.
- Keep `getContext`, `saveContext`, `loadSession`, `BusinessContextEntry`, and the existing authenticated Business API contract unchanged.
- Preserve the four existing editable keys: `customers`, `busyPeriods`, `constraints`, and `currentPriorities`.
- Preserve any additional API-returned context entries in local state and subsequent saves even when VS-15 does not add a UI editor for those unknown keys.
- Preserve existing persistence semantics: only non-empty entries are sent through the existing `saveContext` boundary; VS-15 does not add delete semantics or change API behavior.
- Preserve `source` and `ownerConfirmed` provenance supplied by the API. A loaded public entry remains public unless the API returns otherwise.
- Require an explicit owner confirmation control for any visible public entry whose `ownerConfirmed` value is false before Save can proceed.
- Extract a small pure Context presentation/model module for field metadata, immutable entry updates, draft-safe reload behavior, confirmation rules, validation, operation serialization, and accessible state labels.
- Reuse shared `tokens` and `BrandMark`; do not create a second theme or direct temporary-logo reference.
- Add deterministic mobile tests for the production-consumed pure model.
- Record runtime visual evidence only when an authentic runnable Expo channel is available. Do not substitute fabricated screenshots or source-text assertions for runtime evidence.

## Approaches considered

### 1. Bounded screen migration plus pure Context model — selected

Retain the existing route and API ownership, extract only deterministic Context behavior, and rebuild the screen presentation around the established VS-13/VS-14 Atlas patterns. This keeps the change reviewable, enables TDD for provenance/draft/concurrency rules, and avoids changing the original VS-02 architecture.

### 2. Shared cross-screen form framework — rejected for this slice

A general form/card abstraction could reduce repeated styles later, but it would couple VS-15 to Profile, Goals, and future settings work. The existing repetition is not sufficient reason to enlarge this governed visual slice.

### 3. Context API or persistence redesign — rejected

Adding bulk replace, delete, optimistic-concurrency fields, a new server-state library, or a new context schema would exceed the visual-migration instruction and FR-05 implementation boundary. Existing API/persistence behavior remains authoritative.

## Context field model

The four current Context fields remain optional and retain their exact API keys. Presentation copy may make those keys easier to understand without changing their stored names.

1. `customers`
   - Label: `Customers`
   - Prompt: `Who do you serve most often?`
   - Helper: describe customer groups at a business level; do not enter end-customer personal data.
2. `busyPeriods`
   - Label: `Busy periods`
   - Prompt: `When does demand or workload usually peak?`
   - Helper: days, seasons, events, or operating patterns are sufficient.
3. `constraints`
   - Label: `Constraints`
   - Prompt: `What limits your choices right now?`
   - Helper: examples may include time, staffing, capacity, cash, or operating limits; the copy must not imply any value is required.
4. `currentPriorities`
   - Label: `Current priorities`
   - Prompt: `What deserves attention beyond your saved goals?`
   - Helper: capture short-term priorities only when they materially improve guidance.

No field is required. No fabricated Business value or category-specific assumption is introduced.

## Screen composition

The screen uses the shared warm-neutral canvas, centered content up to 680 points wide, generous vertical rhythm, restrained white cards, deep-green hierarchy, and one primary Save action.

1. Header: `BrandMark`, `BUSINESS CONTEXT` eyebrow, benefit-led heading `Help Atlas understand how your business works.`, and concise explanation that context is optional and should be limited to what improves guidance.
2. Data-minimisation guidance card: explain that owners can leave fields blank and must avoid end-customer personal data. This is product guidance, not fabricated Business information.
3. Context cards: one white card per existing editable key. Each card contains a readable title, prompt/helper copy, multiline input, and provenance treatment when applicable.
4. Provenance treatment: owner-provided entries show a compact `OWNER PROVIDED` text label. Public entries show a `PUBLIC SOURCE` label and a confirmation control. Public unconfirmed entries are not eligible for Save until explicitly confirmed.
5. Feedback region: polite validation, load, and save feedback that does not expose stack/provider details.
6. Primary action: full-width green `Save context` action with explicit visible and assistive saving state.

The screen does not add analytics, recommendations, connected data sources, industry-specific fields, context scoring, deletion, autonomous enrichment, or new navigation.

## Behavior and data flow

### Initial load

The screen begins in `loading`, restores the secure session, and does not present editable content as if it had loaded successfully. If the session has no usable Business ID, the screen enters a recoverable `missing` state using the same centralized session-entry route used by VS-13/VS-14.

With a Business ID, the screen calls the unchanged `getContext` boundary.

- API entries are merged with the four editable field definitions without mutating the API response.
- Existing values, provenance, and confirmation state are retained exactly.
- Editable keys not returned by the API are represented as empty owner-provided, owner-confirmed drafts.
- API-returned unknown keys remain in local state so later saves do not drop them.
- An empty API response is a valid ready state; the four optional fields remain blank.

### Editing

Editing a visible entry changes only its `value`. Existing `source` and `ownerConfirmed` metadata remain unchanged, preventing a UI edit from silently rewriting provenance. New empty defaults use `source: 'owner'` and `ownerConfirmed: true`.

Whitespace-only values are treated as blank for the existing save boundary. VS-15 does not pretend blanking an already-persisted entry deletes it because no delete endpoint exists in the approved API.

### Owner confirmation

A visible entry with `source: 'public'` exposes an accessible checkbox-style confirmation. Toggling confirmation changes only `ownerConfirmed` for that entry.

Save is blocked when any non-empty visible or preserved API entry has `source: 'public'` and `ownerConfirmed: false`. The validation message identifies the affected field when it is one of the four editable keys, otherwise it uses a generic public-context message. This mirrors the existing API rule rather than bypassing it.

### Validation

Context values remain optional. Client validation is intentionally limited to rules already implied by the API/product policy:

- whitespace-only values are omitted from the save payload;
- public non-empty entries must be owner-confirmed;
- there is no invented client-only character limit because the current API/domain model defines none;
- validation does not inspect or classify owner prose beyond the explicit privacy guidance.

### Retry and draft preservation

A failed initial load enters a recoverable error state with `Try again`.

After ready state has been reached, a manual Retry/refresh must not overwrite unsaved owner edits. The pure model tracks whether a draft exists. A successful or failed manual reload while a draft exists preserves the draft and reports that the owner’s unsaved context is still present. When no draft exists, a successful reload replaces the local representation with the latest API response.

### Saving

Save operations are serialized with refresh/retry operations so they cannot overlap. Save:

1. validates public confirmation;
2. restores the secure session;
3. calls the unchanged `saveContext(accessToken, businessId, nonEmptyEntries)` boundary;
4. replaces local state with the confirmed API response merged back into the editable field model;
5. clears the draft marker only after server confirmation;
6. announces `Context saved.` only after the server returns successfully.

A missing session enters the recoverable `missing` state. A save failure preserves the draft and reports `Could not save context. Your changes are still here.`.

## State model

- `loading`: honest progress state; no editable form yet.
- `missing`: no usable Business session; clear next action through centralized routing.
- `error`: initial Context load failed; safe Retry is available.
- `ready`: editable optional Context fields.
- `refreshing`: Retry/refresh is active; Save and repeated Retry are disabled.
- `saving`: Save is disabled against overlap, spinner plus visible `Saving…`, native accessibility busy state, and web `aria-busy` state.
- `validation`: public confirmation is missing; draft remains editable and error is announced.
- `save-error`: draft is preserved with stable retryable copy.
- `success`: API-confirmed values are shown and `Context saved.` is announced politely.

## Accessibility and responsive behavior

- Logical order: header, privacy/minimisation guidance, context cards, validation/feedback, Save.
- Semantic screen heading and descriptive input labels/hints.
- All Pressable targets use at least the shared 44-point touch target.
- Provenance and confirmation are expressed in text, not colour alone.
- Public confirmation uses checkbox semantics, `aria-checked`, and native `accessibilityState`.
- Loading, retry, validation, save failure, and save success use accessible live feedback where appropriate.
- Keyboard-safe scrolling uses automatic keyboard insets, drag dismissal, and handled taps.
- Multiline inputs flex without fixed text widths and remain usable on small phones.
- Content is contained to 680 points on representative tablets.
- Motion is limited to native press feedback and activity indicators; no new animation dependency is introduced.

## Visual continuity

- Use `tokens.color.canvas`, `surface`, `ceramic`, `mint`, `green`, `greenDeep`, `ink`, `muted`, `border`, and semantic error colours.
- Use established VS-13/VS-14 geometry: 18-point cards, restrained border/shadow, 44-point minimum controls, and strong green CTA.
- `BrandMark` remains the only prototype-mark boundary. No Context-level asset URI, Starbucks label, copied retail artwork, or direct temporary-logo reference is permitted.
- Do not reintroduce generic dark Expo styling or the previous generic light-green system.

## Testing and verification

TDD covers production-consumed pure behavior:

- editable field definitions preserve the exact four existing keys;
- API entries merge into editable defaults without mutating input;
- unknown API entries survive merges and save payload construction;
- edits preserve provenance metadata;
- empty values remain optional and are excluded from the save payload;
- public unconfirmed values block saving and become valid only after explicit confirmation;
- operation coordination serializes Retry and Save and rejects stale completion tickets;
- a draft survives successful and failed manual reloads;
- loading/missing/error/retry/save presentations expose correct visible and accessibility state.

Repository verification includes the complete mobile test suite, TypeScript check, lint, PES planning/governance/slice checks, dashboard/platform checks, preflight, Security baseline, Product Intake, and independent review where available.

Authentic runtime visual checks should cover at minimum ready, public-unconfirmed, validation, saving, save failure/success, narrow-phone containment, representative tablet containment, and keyboard reachability. If this execution environment cannot run Expo, the evidence must be recorded as unavailable rather than represented as passed, and certification must respect the repository’s exact-head evidence requirements.

## Governance and non-goals

- Requirement: FR-05 only.
- Risk: medium visual/customer-flow change over an already implemented authenticated API.
- Runtime implementation is allowed only under the Product Owner’s standing approved visual-migration sequence and recorded VS-15 scope/implementation approvals.
- Preserve the existing five-tab navigation for this bounded migration, consistent with DEC-02; navigation alignment remains a separate governed slice.
- No API/domain/migration/auth/navigation/global-state/dependency change.
- No connected-source implementation, Context ingestion pipeline, AI behavior change, database migration, or new collection of personal data.
- No deployment, EAS build, OTA update, release, or production enablement.
- Certification and merge require exact-head CI, Security baseline, Product Intake, governance, required runtime evidence, and exact-SHA human approval according to repository governance.