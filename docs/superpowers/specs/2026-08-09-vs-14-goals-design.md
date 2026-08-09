# VS-14 Goals Visual Migration Design

## Status and authority

This design is a bounded implementation interpretation of the Product Owner's approved five-slice Atlas visual migration sequence. It implements the approved FR-04 Goals surface and introduces no new product, architecture, security, release, or production decision.

Authority remains, in order: `product/PRD.md`, `product/TRD.md`, `product/DESIGN.md`, `docs/slices/VS-02.md`, PES governance, and the approved Atlas visual-language references. Starbucks getdesign supplies visual grammar only. Atlas remains the product identity and every temporary mark remains behind `BrandMark`.

## Outcome

Migrate the existing authenticated Goals route to the approved Atlas visual language while preserving the current API and navigation boundaries. Owners can understand priority order, reorder existing goals, add a valid custom goal, and save with clear recoverable feedback.

## Scope

- Keep `apps/mobile/app/(tabs)/goals.tsx` as the Expo Router route.
- Keep `getGoals`, `saveGoals`, `loadSession`, `BusinessGoal`, and the existing authenticated Business API contract unchanged.
- Preserve the existing three approved generic starter goals when the Business has no saved goals.
- Preserve loaded server goals exactly, except for deterministic priority normalization after an owner reorder or add.
- Extract pure goal-model behavior for ordering, duplicate/limit validation, starter goals, and accessible presentation state.
- Use the existing shared Atlas tokens and `BrandMark`; do not create another visual system.
- Add authentic Expo Web visual and interaction evidence at narrow phone and representative tablet widths.

## Approaches considered

### 1. Bounded screen migration plus pure model — selected

Retain the existing route and local state ownership, add a small pure model for deterministic behavior, and rebuild only the Goals presentation. This gives TDD coverage without changing API, auth, navigation, or global state.

### 2. Shared form/card system extraction — rejected for this slice

Extracting a broad design-component library could reduce future repetition, but it would couple the Goals migration to unrelated screens and create avoidable review surface. Shared extraction can occur only when repeated implemented patterns justify it.

### 3. Query/state architecture rewrite — rejected

Moving Goals to a new server-state or global-state architecture is outside this visual migration and conflicts with the instruction to preserve PES Mobile architecture.

## Screen composition

The screen uses a warm neutral canvas, centered content up to 680 points wide, generous vertical rhythm, restrained white cards, deep-green hierarchy, and one primary Save action.

1. Header: `BrandMark`, `BUSINESS GOALS` eyebrow, benefit-led heading `Choose what Atlas should optimize for.`, and concise explanation that rank 1 is the strongest signal.
2. Priority guidance card: explain that Atlas uses the order to evaluate future Opportunities. This card describes behavior only and contains no fabricated business fact.
3. Goal list: one white card per actual goal. Each card shows a non-colour priority label, goal title, human-readable type, and a `CUSTOM` badge only when `isCustom` is true. Move-up and move-down controls remain the existing edit mechanism.
4. Add-custom card: labelled input, short helper text, inline validation, and a secondary Add action. The input/action row wraps on narrow widths.
5. Feedback region: polite success/error/validation feedback that never exposes provider or stack details.
6. Primary action: full-width green Save Goals action with explicit visible and assistive saving state.

The screen does not add removal, deactivation, drag-and-drop, goal analytics, recommendation counts, autonomous changes, or new generic goal selection behavior.

## Behavior and data flow

### Initial load

The screen begins in `loading` and restores the secure session. A missing session/business enters a recoverable `missing` state using the existing centralized session-routing policy. With a Business ID, it calls the unchanged `getGoals` boundary.

- A non-empty server list becomes the editable list, ordered by priority.
- An empty server list becomes the three approved starter goals and is labelled as a starting point, not as previously saved owner data.
- A load failure keeps the starter goals available, labels them as unsaved defaults, and shows recoverable feedback with Retry. It does not claim remote data loaded.

### Reordering

Move controls create a new array, swap only within bounds, and normalize every priority to contiguous one-based values. Boundary controls are disabled and expose disabled state; they do not silently perform a no-op as if successful.

### Adding a custom goal

The owner-provided title is trimmed. Add is rejected when the title is empty, case-insensitively duplicates an existing goal title, or would exceed the existing API maximum of ten goals. Valid additions use type `custom`, `isCustom: true`, and the next contiguous priority. No generic business data is invented.

### Saving

Save prevents overlap, restores the secure session, calls the unchanged `saveGoals(accessToken, businessId, goals)`, and replaces local state with the returned server representation. A missing session follows the recoverable session route. A failure preserves the draft and shows stable Atlas copy. Success is announced only after server confirmation.

## State model

- `loading`: honest progress state; no editable list yet.
- `missing`: no usable Business session; clear next action through centralized routing.
- `ready`: editable goals, including labelled starter defaults when appropriate.
- `load-error`: starter defaults remain available with a Retry action and honest unsaved-default notice.
- `saving`: Save disabled against overlap, spinner plus visible `Saving…`, dynamic accessibility label, native busy state, and web busy state.
- `save-error`: draft preserved and stable retryable feedback.
- `success`: returned server goals displayed and polite `Goals saved.` announcement.

## Accessibility and responsive behavior

- Logical order: header, guidance, ordered goals, custom goal, feedback, Save.
- Semantic screen heading and descriptive labels.
- Every interactive target is at least 44 points.
- Move buttons have goal-specific labels, hints, and disabled state at boundaries.
- Priority and custom status are expressed in text, not colour alone.
- Input validation is plain-language and announced politely.
- Keyboard-safe scrolling uses automatic insets, drag dismissal, and persistent handled taps.
- Cards, controls, and text flex without fixed text widths; the custom-goal row wraps at narrow phone widths.
- Tablet content stays contained at 680 points.
- Motion is limited to native press feedback; no new animation dependency or decorative motion.

## Visual continuity

- Use `tokens.color.canvas`, `surface`, `mint`, `green`, `greenDeep`, `ink`, `muted`, `border`, and semantic error colors.
- Use the established Profile geometry: 18-point cards, restrained border/shadow, 44-point controls, and strong green CTA.
- `BrandMark` is the only prototype-mark boundary. No Goals-level asset URI, Starbucks label, retail imagery, or copied brand layout is permitted.
- Do not reintroduce generic grey/dark Expo styling or the previous generic light-green system.

## Testing and verification

TDD covers real pure behavior, not source-text checks:

- starter goals are ordered and contiguous;
- server goals sort without mutating the input;
- valid moves swap and normalize priorities;
- boundary moves preserve state;
- custom titles trim, reject empty/duplicates, enforce the ten-goal maximum, and append valid goals;
- loading/missing/error/save presentations expose the correct labels and accessibility state.

Verification also includes mobile typecheck, lint, the complete mobile suite, PES planning/governance/slice checks, preflight, independent code review, and authentic Expo Web interaction checks for loading/error/ready, reorder, duplicate validation, saving/success, 44-point controls, 390×844, and 768×1024. Native iOS keyboard/dynamic-type behavior must be recorded honestly if no native channel exists.

## Governance and non-goals

- Requirement: FR-04 only.
- Risk: medium visual/customer-flow change over an already implemented authenticated API.
- Runtime implementation is allowed only after VS-14 scope and implementation approvals are recorded.
- No API/domain/migration/auth/navigation/global-state change.
- No deployment, EAS build, OTA update, release, or production enablement.
- Certification and merge require exact-head local and remote CI, Security baseline, Product Intake, governance, and human-approved merge gates.
