# VS-33 — Today Experience Simplification

Status: Product direction approved in chat; written-spec review pending before runtime implementation.

## Goal

Make **Today** feel like a calm daily assistant rather than a dense recommendation report, while preserving Atlas's action-first product model and one primary Today Opportunity.

The owner should understand what matters, act, defer, dismiss, or inspect detail with at most one or two taps for common actions.

## Governing requirements

- PRD: FR-07 Today’s Focus, FR-10 Action decisions, FR-16 Empty and degraded states.
- Design: ATLAS-DESIGN-001 v1.2.
- Preserve the approved four-tab navigation: Today / History / Goals / Profile.
- Preserve evidence-first recommendation eligibility and the VS-30 factual-evidence guard.
- Preserve owner authority: no automatic Apply, Skip, Not Relevant, or Goal mutation.

## Approved experience

### 1. Header and refresh

Today opens with a lightweight heading such as **Today** and a concise one-line summary such as **Here’s what matters today.**

Pull-to-refresh remains native and becomes a first-class interaction. A compact freshness label such as **Updated just now** or an equivalent relative time is shown after a successful load. Refresh must preserve safe cached content while loading where practical instead of blanking the page.

### 2. Best Move is primary

Rename the heavy in-page “Today’s Focus / Recommended Move” presentation to plain owner language: **Best move**.

The Best Move card contains only:

- Opportunity title;
- one concise reason/why-it-matters line;
- compact Impact / Effort / Confidence indicators;
- primary action **Apply**;
- secondary action **Why?** or equivalent route to Opportunity Detail.

Detailed Why Now, Evidence, interpretation, expiry, pack/version metadata, assumptions, and limitations remain available through progressive disclosure / Opportunity Detail. They do not compete with the primary action on Today.

### 3. Lightweight supporting cards

Today may show a small number of secondary cards only when backed by real Atlas data already available in the current product. The initial VS-33 scope is intentionally conservative and must not invent BI metrics or connector data.

Permitted supporting modules in VS-33:

- **Goal** — current priority goal / goal-state cue if available;
- **Recent result / History** — route into recent business history where existing data supports it;
- **Atlas noticed** — only if it is a factual, already-supported explanation from the current Opportunity; otherwise omit;
- **Next step** — recovery card in empty/insufficient-context states.

Future Business Pulse, What Changed, anomaly, channel, menu, market, profitability, connector, forecasting, and Ask Atlas cards are explicitly out of scope for VS-33.

### 4. Quick actions

Common actions stay one or two taps:

- Apply;
- Why? / View details;
- Later (maps to the existing Skip decision with current governed reason semantics unless a distinct product state is approved later);
- Not relevant.

No new backend Action Decision state is introduced in this slice.

### 5. Visual treatment

Use the locked Atlas visual language:

- warm neutral canvas;
- Atlas green for the primary action and identity;
- white elevated primary card;
- restrained semantic supporting tints, for example soft mint/green, soft blue, soft amber, or soft lavender only where the meaning remains clear without colour;
- no one-off decorative palette and no colour-only status semantics;
- generous whitespace;
- modern native press feedback;
- approximately 44×44 point minimum targets;
- Dynamic Type and screen-reader-friendly labels.

Supporting colours are secondary accents; green remains dominant. The screen must not become a KPI wall or dashboard.

### 6. Empty and degraded states

Rewrite Today empty states into concise, plain-language recovery states.

Examples:

- no persisted goals: **Choose a goal to get your first Best move**;
- insufficient factual context: **Atlas needs a little more context before it can suggest a useful move**;
- no evidence-qualified candidate: **Nothing strong enough to recommend yet**;
- degraded/service failure: **Today couldn’t refresh safely** with Retry.

Each state shows one primary recovery action and at most one quiet secondary route.

Starter goals must never be presented as already active. Today continues to rely only on persisted goals.

### 7. Loading and interaction behavior

- Prefer preserving existing safe content during manual refresh.
- Initial load can use a compact loading state/skeleton rather than a large “Atlas is thinking” composition.
- Pull-to-refresh is available on Ready, No Focus, Insufficient Context, and Degraded states where technically safe.
- Meaningful press feedback is retained through the existing native pressable/polish system.
- No decorative motion is required.

## Architecture

Keep the implementation inside the existing Today feature boundary.

Primary files are expected to be:

- `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`;
- a small Today presentation/model helper if needed;
- `tests/mobile/**` Today-focused acceptance/regression tests;
- governed `docs/**` and `delivery/**` files.

Avoid API/database changes unless a verified requirement cannot be satisfied using the existing `TodayFocus` contract. Current inspection indicates the simplification can be mobile-only.

Do not restructure Expo Router or `AtlasScreen` unless a verified shared-shell defect blocks the design.

## Error handling

- Existing safe server states remain authoritative.
- Refresh failure must not claim stale data is current.
- Mutations remain server-confirmed before success presentation.
- Apply/Later/Not Relevant must preserve duplicate-submit protection through the existing `deciding` guard or equivalent.
- No provider or stack details are exposed to the owner.

## Testing

Use TDD for interaction/presentation contracts where deterministic tests are practical.

Minimum regression coverage:

1. Today shows **Best move** and does not expose the old heavy “Recommended Move / One action. Clear reason. Measurable outcome.” composition.
2. Apply remains the single primary action.
3. Why? routes to Opportunity Detail.
4. Later maps to the existing Skip decision semantics; Not relevant remains available without adding a backend state.
5. Pull-to-refresh is present and freshness state is updated only after successful load.
6. Manual refresh preserves existing safe content while loading where practical.
7. no-goal / insufficient-context / no-focus / degraded states remain truthful and concise with a clear recovery action.
8. No fabricated metric, connector, competitor, menu, revenue, order, or benchmark data appears.
9. Today remains accessible under Dynamic Type and screen-reader interaction expectations.
10. Existing four-tab navigation remains unchanged.

## Non-goals

VS-33 does not implement:

- Business Pulse metrics;
- What Changed analytics;
- anomaly detection;
- menu/channel intelligence;
- external connectors;
- competitor intelligence;
- profitability;
- forecasts;
- Ask Atlas;
- a new Opportunity ranking queue;
- new Action Decision states;
- API/database migrations;
- production release/deployment.

Those capabilities can be introduced in later dependency-backed slices after Today is complete.

## Success criteria

VS-33 passes when Today feels lighter and clearer, keeps one unmistakable Best Move, supports native pull-to-refresh, puts common decisions within one or two taps, uses restrained semantic card accents, keeps deeper evidence progressive, and remains truthful when Atlas has nothing strong enough to recommend.
