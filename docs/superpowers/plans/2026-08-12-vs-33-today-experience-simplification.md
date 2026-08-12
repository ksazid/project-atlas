# VS-33 Today Experience Simplification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Simplify Atlas Today into a calm, concise, refreshable decision screen with one clear Best move and one- or two-tap owner actions, without adding new BI data, backend states, or industry-specific assumptions.

**Architecture:** Keep all runtime work inside the existing mobile Today feature boundary. Reuse the existing `TodayFocus` API contract and Action Decision API; introduce at most one small presentation helper for deterministic copy/freshness mapping if that reduces complexity. Preserve Expo Router navigation and `AtlasScreen` unchanged unless an existing shell defect is proven.

**Tech Stack:** React Native + Expo SDK 54, Expo Router, TypeScript, Node `node:test` mobile acceptance tests, existing Atlas API client and Action Decision contract.

## Global Constraints

- Preserve ATLAS-DESIGN-001 v1.2: Today is a decision screen, not a dashboard.
- Preserve one primary Today Opportunity and one primary Apply action.
- Preserve VS-30 factual-evidence eligibility and existing server state authority.
- Preserve Today / History / Goals / Profile persistent tabs exactly.
- No API, database, migration, connector, metric, competitor, menu, revenue, order, benchmark, forecasting, or Ask Atlas implementation.
- No new Action Decision state. `Later` maps to existing `skip` semantics with the governed reason `Not the right time`.
- No automatic Apply, Skip, Not Relevant, or Goal mutation.
- Pull-to-refresh must preserve safe current content during manual refresh where practical.
- Supporting colours remain restrained semantic accents; Atlas green remains dominant and colour is never the sole status signal.
- Minimum interactive targets remain approximately 44×44 points with screen-reader labels and Dynamic Type-compatible layout.
- No production release, deployment, EAS build/submit/OTA, production enablement, or production database mutation.

---

### Task 1: Activate VS-33 governance and establish a green baseline

**Files:**
- Create: `docs/slices/VS-33.md`
- Modify: `delivery/current-slice.json`
- Existing: `docs/superpowers/specs/2026-08-12-vs-33-today-experience-simplification-design.md`

**Interfaces:**
- Consumes: Approved FR-07, FR-10, FR-16 and ATLAS-DESIGN-001 v1.2.
- Produces: A runtime-enabled VS-33 slice allowing only `apps/mobile/**`, `tests/mobile/**`, `delivery/**`, and `docs/**`.

- [ ] **Step 1: Create the slice specification**

Record scope as mobile-only Today simplification, no backend/migration changes, one primary Best move, pull-to-refresh, concise recovery states, semantic supporting tints, and no new BI features.

- [ ] **Step 2: Activate `delivery/current-slice.json`**

Use `sliceId: VS-33`, `lifecycle: implementing`, `implementationMode: runtime-enabled`, requirements `FR-07`, `FR-10`, `FR-16`, and dependency on merged VS-32 main SHA `66e2b7979d68b390d74b395f650b0b6d215e71a8`.

- [ ] **Step 3: Open a draft PR and run exact-head baseline gates**

Expected checks before runtime edits:

```text
CI: success
Security baseline: success
Product Intake: success
```

If baseline is not green, stop and diagnose before runtime implementation.

---

### Task 2: Write the RED Today interaction/presentation contract

**Files:**
- Create: `tests/mobile/vs33-today-experience.test.mjs`
- Modify only if required for runtime expectation alignment: `tests/mobile/today-focus-runtime.test.mjs`

**Interfaces:**
- Consumes: Current `TodayFocusScreen.tsx` and current authentic Expo Web runtime fixture.
- Produces: Failing tests for the new owner-visible behavior before production code changes.

- [ ] **Step 1: Add failing structural/interaction tests**

Use current repository-style mobile tests to assert observable Today contracts. The test must fail on the current screen because the old heavy composition is still present.

```js
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const screen = readFileSync('apps/mobile/src/features/today-focus/TodayFocusScreen.tsx', 'utf8');

test('VS-33 Today presents one concise Best move instead of the heavy recommendation report', () => {
  assert.match(screen, />Best move</);
  assert.match(screen, />Apply</);
  assert.match(screen, />Why\?</);
  assert.match(screen, />Later</);
  assert.match(screen, />Not relevant</);
  assert.doesNotMatch(screen, /RECOMMENDED MOVE/);
  assert.doesNotMatch(screen, /One action\. Clear reason\. Measurable outcome\./);
});

test('VS-33 keeps pull-to-refresh and exposes successful freshness copy', () => {
  assert.match(screen, /RefreshControl/);
  assert.match(screen, /Updated just now|Updated/);
});

test('VS-33 keeps detailed evidence progressive instead of rendering it as competing Today cards', () => {
  const ready = screen.slice(screen.indexOf('const opportunity = focus.opportunity'));
  assert.doesNotMatch(ready, />Why now</);
  assert.doesNotMatch(ready, />Evidence</);
  assert.match(ready, /router\.push\(`\/opportunities\/\$\{opportunity\.id\}`\)/);
});
```

- [ ] **Step 2: Add failing recovery-state tests**

```js
test('VS-33 recovery states use concise owner language', () => {
  assert.match(screen, /Choose a goal to get your first Best move|Choose a goal/);
  assert.match(screen, /Nothing strong enough to recommend yet/);
  assert.match(screen, /Today couldn[’']t refresh safely/);
});
```

- [ ] **Step 3: Verify RED on the draft PR head**

Run in CI:

```bash
node --test tests/mobile/vs33-today-experience.test.mjs
```

Expected: the new VS-33 tests fail for the intended missing Best move / compact state behavior while pre-existing tests remain green.

- [ ] **Step 4: Commit the RED tests**

Commit message:

```text
test(vs33): define simplified Today experience
```

---

### Task 3: Implement concise Ready-state Best move and quick actions

**Files:**
- Modify: `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`
- Test: `tests/mobile/vs33-today-experience.test.mjs`
- Test: `tests/mobile/today-focus-runtime.test.mjs`

**Interfaces:**
- Consumes: `TodayFocus` ready state, `decideOpportunity(...)`, `router.push(...)`, existing `deciding` duplicate-submit guard.
- Produces: Best move UI with Apply, Why?, Later, Not relevant and compact Impact/Effort/Confidence.

- [ ] **Step 1: Replace the old Ready-state hierarchy**

Render:

```tsx
<Text style={styles.pageTitle}>Today</Text>
<Text style={styles.pageLead}>Here’s what matters today.</Text>
<Text style={styles.freshness}>{freshnessLabel}</Text>

<View style={styles.bestMoveCard}>
  <Text style={styles.cardEyebrow}>Best move</Text>
  <Text accessibilityRole="header" style={styles.bestMoveTitle}>{opportunity.title}</Text>
  <Text style={styles.bestMoveReason}>{opportunity.whyItMatters}</Text>
  <View style={styles.metricsRow}>...</View>
  <View style={styles.actionRow}>
    <Pressable accessibilityRole="button" accessibilityLabel="Apply best move" ...>
      <Text>Apply</Text>
    </Pressable>
    <Pressable accessibilityRole="button" accessibilityLabel="Why this move" onPress={() => router.push(`/opportunities/${opportunity.id}`)} ...>
      <Text>Why?</Text>
    </Pressable>
  </View>
  <View style={styles.quietActionRow}>
    <Pressable accessibilityRole="button" onPress={() => void decide('skip')} ...><Text>Later</Text></Pressable>
    <Pressable accessibilityRole="button" onPress={() => void decide('not-relevant')} ...><Text>Not relevant</Text></Pressable>
  </View>
</View>
```

Do not render `Why now`, `Evidence`, interpretation, expiry, Knowledge Pack key/version, or the old hero card on Today; those remain on Opportunity Detail.

- [ ] **Step 2: Keep exact Action Decision semantics**

The current mutation mapping remains:

```ts
const reason = decision === 'apply'
  ? undefined
  : decision === 'skip'
    ? 'Not the right time'
    : 'Does not fit my business';
```

`Later` calls `decide('skip')`; `Not relevant` calls `decide('not-relevant')`.

- [ ] **Step 3: Add restrained semantic visual hierarchy**

Use the locked visual language: warm/white canvas, green primary Apply button, soft mint Best move treatment, and at most one or two light secondary tints for quiet supporting blocks. Do not add fabricated data or a KPI grid.

- [ ] **Step 4: Run the focused tests**

```bash
node --test tests/mobile/vs33-today-experience.test.mjs
node --test tests/mobile/today-focus-design-baseline.test.mjs tests/mobile/today-focus-states.test.mjs
```

Expected: PASS.

- [ ] **Step 5: Commit Ready-state GREEN**

```text
feat(vs33): simplify Today best move
```

---

### Task 4: Make refresh/freshness behavior explicit and keep safe content visible

**Files:**
- Modify: `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`
- Test: `tests/mobile/vs33-today-experience.test.mjs`
- Test: `tests/mobile/today-focus-runtime.test.mjs`

**Interfaces:**
- Consumes: existing `load(manual)` flow and `RefreshControl`.
- Produces: successful refresh timestamp/freshness label without blanking already-safe Ready content during manual refresh.

- [ ] **Step 1: Add successful-load freshness state**

Add:

```ts
const [lastUpdatedAt, setLastUpdatedAt] = useState<Date | null>(null);
```

On successful `getTodayFocus(...)`, after `setFocus(value)`:

```ts
setLastUpdatedAt(new Date());
```

Do not update the timestamp in `catch`.

- [ ] **Step 2: Preserve current content on manual refresh**

Keep `state` and `focus` unchanged when `manual === true`; only set `refreshing`. Initial retry can still transition through the loading state when no safe content exists.

- [ ] **Step 3: Render concise freshness**

For a successful load render a compact label such as:

```tsx
<Text accessibilityLabel="Today data updated just now" style={styles.freshness}>Updated just now</Text>
```

Avoid fake minute precision in this slice; a simple success freshness label is sufficient.

- [ ] **Step 4: Extend authentic runtime expectations**

Update `tests/mobile/today-focus-runtime.test.mjs` so the ready state waits for `Best move`, `Apply`, `Why?`, and `Updated just now`; retain the Opportunity Detail navigation check using `Why?`.

- [ ] **Step 5: Run focused tests**

```bash
node --test tests/mobile/vs33-today-experience.test.mjs tests/mobile/today-focus-runtime.test.mjs
```

Expected: PASS (runtime subtest executes in GitHub Actions).

- [ ] **Step 6: Commit refresh GREEN**

```text
feat(vs33): clarify Today refresh state
```

---

### Task 5: Simplify loading, empty, no-focus, insufficient-context and degraded states

**Files:**
- Modify: `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`
- Modify only where expected copy changes are part of existing tests: `tests/mobile/today-focus-states.test.mjs`
- Test: `tests/mobile/vs33-today-experience.test.mjs`

**Interfaces:**
- Consumes: server states `insufficient-context`, `no-focus`, `degraded`, unknown/empty, and `todayFocusRecoveryAction(...)`.
- Produces: concise truthful states with one dominant recovery action and at most one quiet secondary route.

- [ ] **Step 1: Simplify initial loading**

Replace the large “Atlas is thinking” composition with compact loading copy such as:

```tsx
<Text accessibilityLiveRegion="polite" style={styles.stateTitle}>Refreshing Today…</Text>
```

- [ ] **Step 2: Simplify insufficient-context state**

Keep the server message authoritative, but use plain header copy. If `focus.code` resolves to Goals, the primary button stays the existing recovery action and Today must not imply starter goals are active.

- [ ] **Step 3: Simplify no-focus state**

Use:

```text
Nothing strong enough to recommend yet
```

with one primary context/profile recovery action and one quiet History route. Preserve the no-filler explanation in concise form.

- [ ] **Step 4: Simplify degraded state**

Use:

```text
Today couldn’t refresh safely
```

with Retry as the primary action and a quiet Profile/context route.

- [ ] **Step 5: Add refresh controls to safe non-ready scrollable states**

Where `AtlasScreen` supports a refresh control, provide the same native `RefreshControl` for insufficient-context, no-focus, and degraded states. Do not change `AtlasScreen` itself.

- [ ] **Step 6: Run state tests**

```bash
node --test tests/mobile/vs33-today-experience.test.mjs tests/mobile/today-focus-states.test.mjs tests/mobile/today-focus-recovery.test.mjs
```

Expected: PASS.

- [ ] **Step 7: Commit state GREEN**

```text
feat(vs33): simplify Today recovery states
```

---

### Task 6: Full verification, accessibility regression and certification candidate

**Files:**
- Modify if test alignment is necessary: `tests/mobile/today-focus-design-baseline.test.mjs`
- Modify governance evidence after runtime freeze: `docs/slices/VS-33.md`, `delivery/current-slice.json`

**Interfaces:**
- Consumes: completed VS-33 runtime implementation.
- Produces: frozen implementation SHA with deterministic and exact-head CI/Security/Product Intake evidence.

- [ ] **Step 1: Run the entire mobile suite**

```bash
npm run mobile:test
```

Expected: all mobile tests PASS.

- [ ] **Step 2: Run mobile validation and preflight**

```bash
npm run mobile:validate
npm run preflight
```

Expected: dependency validation, TypeScript, Expo lint, mobile tests, governance, dashboard, platform validation all PASS.

- [ ] **Step 3: Confirm architecture boundary**

Changed runtime files must remain under:

```text
apps/mobile/src/features/today-focus/**
tests/mobile/**
```

plus approved `docs/**` and `delivery/**`. No API, migration, infrastructure, release workflow, or navigation-shell change.

- [ ] **Step 4: Verify exact-head CI gates**

Required:

```text
CI: success
Security baseline: success
Product Intake: success
```

Record mobile test count and authentic Expo Web runtime result.

- [ ] **Step 5: Freeze and certify exact runtime SHA**

Update `delivery/current-slice.json` and `docs/slices/VS-33.md` with exact certification evidence. Do not merge or deploy without the governed human merge/release boundary.
