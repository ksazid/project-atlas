# VS-34 Today Clarity & Navigation Consistency Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Today read as one obvious daily task with concise evidence and actions, while replacing the inconsistent custom black Profile back pill with the native/light Atlas back treatment on Profile-owned pushed screens.

**Architecture:** Keep Today’s server contract and decision semantics unchanged. Modify only the mobile presentation layer: the ready-state composition in `TodayFocusScreen.tsx` and the pushed-screen header options for Settings, Context and Feedback. Use existing Expo Router/React Navigation behavior, existing Atlas tokens, and existing Opportunity Detail progressive disclosure; introduce no new dependency, API state or route.

**Tech Stack:** React Native, Expo Router / native stack, TypeScript, Node `node:test`, PES governance, GitHub Actions.

## Global Constraints

- Today remains one evidence-qualified Opportunity, not a dashboard or feed.
- Serif is reserved for page/place headings; recommendation/task copy uses strong sans-serif typography.
- Ready state shows `1 thing worth doing today`, quiet freshness, `BEST MOVE`, one task title, one concise reason, one compact evidence row, `I’ll do this`, `Why this?`, and an overflow action for `Later` / `Not relevant`.
- Do not synthesize percentages or business metrics; use only `expectedImpact`, `effort` and `confidence` already returned by the server.
- `Later` continues to call existing `skip` semantics; `Not relevant` continues to call existing `not-relevant` semantics.
- Opportunity Detail remains the source for Why Now, evidence, assumptions, limitations, expiry and Knowledge Pack metadata.
- Native pull-to-refresh and safe stale-refresh labeling remain unchanged.
- Profile-owned pushed screens must not use the custom black Profile pill; use native back title/tint when a back stack exists and a minimal Atlas-green fallback only when no back stack exists.
- Preserve Today / History / Goals / Profile route topology.
- Preserve Dynamic Type, accessible labels/roles, minimum touch targets and current Reduce Motion/press behavior.
- No API, database, migration, connector, Knowledge Pack, menu/media, infrastructure, release workflow, EAS/OTA or deployment change.
- Pilot Operations stays paused and is reserved to resume later as VS-35.

---

### Task 1: Activate governed VS-34 slice and establish clean baseline

**Files:**
- Create: `docs/slices/VS-34.md`
- Modify: `delivery/current-slice.json`
- Existing design: `docs/superpowers/specs/2026-08-12-vs-34-today-clarity-navigation-design.md`
- Existing plan: `docs/superpowers/plans/2026-08-12-vs-34-today-clarity-navigation.md`

**Interfaces:**
- Consumes: merged VS-33 `main@cbf9c21ba440a6691e1344efcd598e5a0193745e` and approved VS-34 written spec.
- Produces: active runtime-enabled VS-34 governance boundary allowing Today, Settings/Context/Feedback route-shell changes, focused mobile tests and documentation only.

- [ ] **Step 1: Create the VS-34 slice document**

Record requirements FR-07 / FR-10 / FR-16, approved scope, cross-industry rule, explicit non-goals, no-deployment boundary and TDD/certification placeholders that are descriptive rather than `TBD` markers.

- [ ] **Step 2: Replace `delivery/current-slice.json` with VS-34 activation metadata**

Use:

```json
{
  "schemaVersion": 2,
  "sliceId": "VS-34",
  "title": "VS-34 — Today Clarity & Navigation Consistency",
  "status": "active",
  "lifecycle": "implementation",
  "riskLevel": "medium",
  "implementationMode": "runtime-enabled",
  "requirements": ["FR-07", "FR-10", "FR-16"],
  "dependencies": [
    "ATLAS-PRD-001",
    "ATLAS-TRD-001",
    "ATLAS-DESIGN-001@1.2",
    "VS-15@a46b9f28ec1e1d360c153adf8e90c40bbe0caca2",
    "VS-31@9cd153aae1f0e0962548ecf2cfb5f112350a5a73",
    "VS-32@66e2b7979d68b390d74b395f650b0b6d215e71a8",
    "VS-33@cbf9c21ba440a6691e1344efcd598e5a0193745e"
  ]
}
```

Complete all existing required ownership, approval, progress, release and link fields following the schema used by VS-33. Allowed runtime paths must be limited to:

```text
apps/mobile/src/features/today-focus/**
apps/mobile/app/settings.tsx
apps/mobile/app/context.tsx
apps/mobile/app/feedback.tsx
tests/mobile/**
delivery/**
docs/**
```

Keep API, DB, infrastructure, release and unrelated app routes protected.

- [ ] **Step 3: Commit governance activation**

```bash
git add delivery/current-slice.json docs/slices/VS-34.md docs/superpowers/specs/2026-08-12-vs-34-today-clarity-navigation-design.md docs/superpowers/plans/2026-08-12-vs-34-today-clarity-navigation.md
git commit -m "chore(vs34): activate Today clarity polish"
```

- [ ] **Step 4: Open draft PR against `main` and run exact-head baseline gates**

Expected gates before runtime edits:

```text
CI: success
Security baseline: success
Product Intake: success
```

Stop before RED if any baseline gate is not green.

---

### Task 2: TDD RED — lock the Today clarity contract

**Files:**
- Create: `tests/mobile/vs34-today-clarity.test.mjs`
- Existing regression: `tests/mobile/vs33-today-experience.test.mjs`
- Production target later: `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`

**Interfaces:**
- Consumes: existing `TodayFocusScreen.tsx` source text and current Opportunity fields.
- Produces: failing acceptance contract for the new ready-state hierarchy without changing production code.

- [ ] **Step 1: Write the failing Today hierarchy test**

Use source-structure assertions that require:

```js
assert.match(ready, />1 thing worth doing today</);
assert.match(ready, />BEST MOVE</);
assert.match(ready, />I’ll do this</);
assert.match(ready, />Why this\?</);
assert.match(ready, /accessibilityLabel="More actions"/);
assert.match(ready, />Later</);
assert.match(ready, />Not relevant</);
assert.doesNotMatch(ready, /Want the reasoning\?/);
assert.doesNotMatch(ready, /<Metric\b/);
```

Also assert the title style does not contain `fontFamily: 'Georgia'` while `pageTitle` still does, and assert one compact evidence expression references `opportunity.expectedImpact`, `opportunity.effort`, and `opportunity.confidence` separated by middle dots.

- [ ] **Step 2: Run only the new test and verify RED**

```bash
node --test tests/mobile/vs34-today-clarity.test.mjs
```

Expected: FAIL because the old ready state still says `Here’s what matters today.`, `Best move`, `Apply`, `Why?`, renders three `Metric` cards and the duplicate `Want the reasoning?` card.

- [ ] **Step 3: Commit RED only**

```bash
git add tests/mobile/vs34-today-clarity.test.mjs
git commit -m "test(vs34): define Today clarity contract"
```

---

### Task 3: TDD GREEN — implement the single-task Today surface

**Files:**
- Modify: `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`
- Test: `tests/mobile/vs34-today-clarity.test.mjs`
- Regression: `tests/mobile/vs33-today-experience.test.mjs`
- Regression: `tests/mobile/today-focus-runtime.test.mjs`
- Regression: `tests/mobile/today-focus-states.test.mjs`

**Interfaces:**
- Consumes: `TodayFocus`, `OpportunityDecision`, `decideOpportunity`, existing `router.push`, existing refresh behavior.
- Produces: the same Today behavioral contract with a clearer ready-state UI and a local-only overflow reveal state.

- [ ] **Step 1: Add local overflow state**

Add:

```ts
const [showMoreActions, setShowMoreActions] = useState(false);
```

The overflow button toggles this state. No persistence or backend state is introduced.

- [ ] **Step 2: Replace ready-state orientation copy and semantic label**

Render:

```tsx
<Text accessibilityRole="header" style={styles.pageTitle}>Today</Text>
<Text style={styles.pageLead}>1 thing worth doing today</Text>
<FreshnessLabel label={freshnessLabel} />
...
<Text style={styles.bestMoveLabel}>BEST MOVE</Text>
```

Keep the existing BrandMark, History shortcut, pull-to-refresh and freshness behavior.

- [ ] **Step 3: Make the recommendation title action typography**

Keep `pageTitle` on Georgia but change `bestMoveTitle` to strong platform sans-serif by removing the `fontFamily: 'Georgia'` declaration and retaining a clear bold weight/size.

- [ ] **Step 4: Replace the three metric cards with one evidence row**

Replace the `Metric` component and `metricsRow` with:

```tsx
<Text style={styles.evidenceSummary}>
  {opportunity.expectedImpact} impact · {opportunity.effort} effort · {opportunity.confidence} confidence
</Text>
```

Remove `MetricTone`, `Metric`, metric-card styles and unused soft lavender color if no longer needed.

- [ ] **Step 5: Replace visible actions with primary, secondary and overflow**

Render one row containing:

```tsx
<Pressable accessibilityLabel="Apply best move" ...>
  <Text style={styles.applyText}>I’ll do this</Text>
</Pressable>
<Pressable accessibilityLabel="Why this move" ...>
  <Text style={styles.whyText}>Why this?</Text>
</Pressable>
<Pressable accessibilityLabel="More actions" accessibilityState={{ expanded: showMoreActions }} ...>
  <Text style={styles.moreText}>•••</Text>
</Pressable>
```

When expanded, render the existing `Later` → `decide('skip')` and `Not relevant` → `decide('not-relevant')` controls. Keep both within two taps.

- [ ] **Step 6: Remove duplicate reasoning card**

Delete the entire `Want the reasoning?` / `Open details →` support card from ready state. `Why this?` remains the single progressive-disclosure path to Opportunity Detail.

- [ ] **Step 7: Run focused Today tests and verify GREEN**

```bash
node --test tests/mobile/vs34-today-clarity.test.mjs tests/mobile/vs33-today-experience.test.mjs tests/mobile/today-focus-states.test.mjs
```

Update only obsolete VS-33 presentation assertions (`Apply`/`Why?`) so they continue to assert the same decision semantics under the approved VS-34 copy. Do not weaken refresh/safety assertions.

Expected: PASS.

- [ ] **Step 8: Run authentic Today runtime test**

```bash
node --test tests/mobile/today-focus-runtime.test.mjs
```

Expected: PASS through Today → Opportunity Detail with the existing storage-origin hardening.

- [ ] **Step 9: Commit Today GREEN**

```bash
git add apps/mobile/src/features/today-focus/TodayFocusScreen.tsx tests/mobile/vs34-today-clarity.test.mjs tests/mobile/vs33-today-experience.test.mjs tests/mobile/today-focus-runtime.test.mjs tests/mobile/today-focus-states.test.mjs
git commit -m "feat(vs34): clarify Today action hierarchy"
```

---

### Task 4: TDD RED/GREEN — replace custom Profile header pill with native/light back treatment

**Files:**
- Create: `tests/mobile/vs34-profile-back-navigation.test.mjs`
- Modify: `apps/mobile/app/settings.tsx`
- Modify: `apps/mobile/app/context.tsx`
- Modify: `apps/mobile/app/feedback.tsx`
- Regression: `tests/mobile/vs31-navigation-alignment.test.mjs`

**Interfaces:**
- Consumes: Expo Router `Stack`, `useRouter`, Atlas `tokens` and existing fallback route `/(tabs)/profile`.
- Produces: native Profile back title/tint when the native stack can go back; minimal transparent Atlas-green fallback control when it cannot.

- [ ] **Step 1: Write failing navigation test**

For Settings, Context and Feedback assert:

```js
assert.match(source, /headerBackTitle:\s*'Profile'/);
assert.match(source, /headerTintColor:\s*tokens\.color\.green/);
assert.doesNotMatch(source, /headerLeft:\s*\(\)\s*=>\s*\(\s*<Pressable[\s\S]*>\s*<Text[\s\S]*>Profile<\/Text>/);
assert.match(source, /router\.replace\('\/(?:\(tabs\)\/)?profile'\)/);
```

Also require an explicit fallback branch keyed from `router.canGoBack()` so deep-linked pushed screens retain a Profile escape route.

- [ ] **Step 2: Run navigation test and verify RED**

```bash
node --test tests/mobile/vs34-profile-back-navigation.test.mjs
```

Expected: FAIL because all three routes currently implement Profile as the custom `headerLeft` Pressable/Text control.

- [ ] **Step 3: Implement native back options with minimal fallback**

Use this shape in each route:

```tsx
const canGoBack = router.canGoBack();

<Stack.Screen
  options={{
    headerShown: true,
    headerShadowVisible: false,
    headerStyle: { backgroundColor: tokens.color.surface },
    headerTitle: 'Settings',
    headerTintColor: tokens.color.green,
    headerBackTitle: 'Profile',
    headerBackButtonDisplayMode: 'default',
    headerLeft: canGoBack
      ? undefined
      : () => (
          <Pressable
            accessibilityLabel="Back to Profile"
            accessibilityRole="button"
            onPress={() => router.replace('/(tabs)/profile')}
            style={{ minHeight: 44, minWidth: 44, justifyContent: 'center' }}
          >
            <Text style={{ color: tokens.color.green, fontSize: 24, fontWeight: '700' }}>‹</Text>
          </Pressable>
        ),
  }}
/>
```

Use route-specific header titles. The fallback shows only a green chevron, never a black/text pill. If TypeScript proves `headerBackButtonDisplayMode` unavailable in the installed native-stack version, remove only that option and retain `headerBackTitle` + tint + conditional fallback; do not reintroduce the custom Profile text pill.

- [ ] **Step 4: Update VS-31 regression contract**

Change its pushed-route expectation from requiring the old custom `Back to Profile` header control on every normal navigation path to requiring native back title/tint plus the preserved no-stack fallback.

- [ ] **Step 5: Run focused navigation tests and typecheck**

```bash
node --test tests/mobile/vs34-profile-back-navigation.test.mjs tests/mobile/vs31-navigation-alignment.test.mjs
npm run mobile:typecheck
```

Expected: PASS. If typecheck rejects a native-stack option, apply the documented minimal fallback from Step 3 and rerun.

- [ ] **Step 6: Commit navigation GREEN**

```bash
git add apps/mobile/app/settings.tsx apps/mobile/app/context.tsx apps/mobile/app/feedback.tsx tests/mobile/vs34-profile-back-navigation.test.mjs tests/mobile/vs31-navigation-alignment.test.mjs
git commit -m "fix(vs34): align Profile back navigation"
```

---

### Task 5: Full deterministic verification, review and certification

**Files:**
- Modify: `docs/slices/VS-34.md`
- Modify: `delivery/current-slice.json`
- Review all PR changed files before certification.

**Interfaces:**
- Consumes: all GREEN runtime commits from Tasks 3–4.
- Produces: frozen exact runtime SHA with PES certification evidence; governance-only certification commit after runtime freeze.

- [ ] **Step 1: Run local deterministic mobile suite**

```bash
npm run mobile:test
npm run mobile:typecheck
npm run mobile:lint
npm run preflight
```

Expected: all pass with pristine VS-34 output.

- [ ] **Step 2: Push/freeze runtime candidate and require exact-head GitHub gates**

Required:

```text
CI: success
Security baseline: success
Product Intake: success
```

CI evidence must include mobile tests, Expo SDK/platform validation, TypeScript, lint, authentic Expo Web runtime, clean PostgreSQL migration replay, API regression tests, Release build and dashboard artifact as defined by repository CI.

- [ ] **Step 3: Changed-file review**

Confirm runtime changes are limited to:

```text
apps/mobile/src/features/today-focus/TodayFocusScreen.tsx
apps/mobile/app/settings.tsx
apps/mobile/app/context.tsx
apps/mobile/app/feedback.tsx
tests/mobile/**
```

plus governance/docs. Reject any API, migration, infrastructure, release workflow, menu/media or Pilot Operations change.

- [ ] **Step 4: Record certification on the exact runtime SHA**

Update `delivery/current-slice.json`:

```json
"lifecycle": "certified",
"progress": {
  "discovery": 100,
  "decisions": 100,
  "implementation": 100,
  "testing": 100,
  "certification": 100,
  "release": 0,
  "validation": 0
}
```

Add certification approval bound to the exact runtime SHA and evidence for RED, GREEN, full gates and changed-file review. Keep release and production-enable pending/not-authorized.

Update `docs/slices/VS-34.md` with the same exact evidence.

- [ ] **Step 5: Commit governance-only certification metadata**

```bash
git add delivery/current-slice.json docs/slices/VS-34.md
git commit -m "chore(vs34): certify Today clarity polish"
```

- [ ] **Step 6: Run post-certification exact-head gates**

Require CI / Security baseline / Product Intake success on the governance head too. Runtime certification remains bound only to the frozen runtime SHA.

- [ ] **Step 7: Mark PR ready for review; do not merge or deploy**

No production deployment, Expo harness update, EAS build/submit/OTA, API deployment or DB mutation is authorized by this plan.
