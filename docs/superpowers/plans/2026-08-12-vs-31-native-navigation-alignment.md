# VS-31 Native Navigation Alignment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the inherited five-tab Atlas mobile shell with the approved four-destination Today / History / Goals / Profile native navigation while preserving Business, Context and Settings capabilities as existing Profile-root/detail flows.

**Architecture:** Keep Expo Router `NativeTabs` from VS-28 and change only the route grouping/entry surfaces needed for information-architecture alignment. History becomes a tab route while Context and Settings become root Stack detail routes reached from the existing Business Hub/Profile root. Domain/API/persistence behavior remains untouched.

**Tech Stack:** Expo SDK 54, React Native, TypeScript, Expo Router NativeTabs, Node test runner, PES governance, Superpowers TDD/systematic debugging.

## Global Constraints

- Start from post-VS-29 `main@689c984040ec72e0cb7cfa314b3efe71d50ee74e`.
- Requirements: FR-03, FR-12, FR-16.
- Design authority: `ATLAS-DESIGN-001@1.2`.
- Decision: DEC-10.
- Preserve VS-28 NativeTabs implementation and native safe-area/accessibility behavior.
- Preserve VS-29 runtime behavior and VS-30 Today’s Focus evidence repair.
- Persistent navigation must contain exactly Today, History, Goals, Profile.
- Business Hub remains Profile root.
- Context and Settings remain reachable as Profile detail routes.
- Public/deep-link paths `/history`, `/context`, `/settings` must remain stable.
- No API/backend/database/migration/auth/recommendation changes.
- No release, production deployment, EAS build/submit/OTA or production database mutation.

---

### Task 0: Activate VS-31 and establish a clean baseline

**Files:**
- Create: `docs/slices/VS-31.md`
- Create: `docs/superpowers/specs/2026-08-12-vs-31-native-navigation-alignment-design.md`
- Create: `docs/superpowers/plans/2026-08-12-vs-31-native-navigation-alignment.md`
- Modify: `delivery/current-slice.json`
- Modify: `delivery/decisions.json`

**Interfaces:**
- Produces the approved PES/runtime boundary for all later tasks.

- [ ] **Step 1: Record DEC-10 and activate the slice**

Set VS-31 to `lifecycle: implementing`, `implementationMode: runtime-enabled`, risk `medium`, requirements `FR-03`, `FR-12`, `FR-16` and allowed paths limited to mobile/tests/delivery/docs.

- [ ] **Step 2: Open a draft runtime PR from the isolated VS-31 branch**

Use `atlas/vs31-native-navigation-alignment` → `main` and make no runtime edits before baseline gates are available.

- [ ] **Step 3: Verify the baseline**

Required exact-head gates:

```text
CI = success
Security baseline = success
Product Intake = success
```

Stop if the activation metadata itself breaks preflight.

---

### Task 1: Write RED navigation-contract tests

**Files:**
- Modify: `tests/mobile/native-tab-shell.test.mjs`
- Create: `tests/mobile/vs31-navigation-alignment.test.mjs`

**Interfaces:**
- Defines the exact persistent-tab and route-placement contract.

- [ ] **Step 1: Replace the temporary five-tab assertion**

Require exactly these tab triggers:

```js
for (const route of ['index', 'history', 'goals', 'profile']) {
  assert.match(source, new RegExp(`name="${route}"`));
}
for (const removed of ['context', 'settings']) {
  assert.doesNotMatch(source, new RegExp(`name="${removed}"`));
}
```

Also assert labels:

```text
Today
History
Goals
Profile
```

Preserve assertions that `NativeTabs` and SF Symbols remain in use.

- [ ] **Step 2: Add source-contract assertions for route placement**

Assert:

```text
apps/mobile/app/(tabs)/history.tsx exists and renders HistoryScreen
apps/mobile/app/history.tsx does not exist
apps/mobile/app/context.tsx exists
apps/mobile/app/settings.tsx exists
apps/mobile/app/(tabs)/context.tsx does not exist
apps/mobile/app/(tabs)/settings.tsx does not exist
```

Assert BusinessHub routes Context to `/context` and Settings to `/settings`.

Assert HistoryScreen contains `hasTabBar` and does not contain the old `router.back()` Back button.

Assert Context/Settings detail sources do not render `hasTabBar` and contain an accessible Back-to-Profile fallback.

- [ ] **Step 3: Commit RED tests only**

Expected preflight result: the new tests fail because the runtime still has the inherited five-tab structure.

---

### Task 2: Align the four native tab triggers and Android fallback icon

**Files:**
- Modify: `apps/mobile/app/(tabs)/_layout.tsx`
- Modify: `apps/mobile/src/components/AtlasIcon.tsx`

**Interfaces:**
- Produces exactly four explicit `NativeTabs.Trigger` entries.

- [ ] **Step 1: Change tab triggers**

Use:

```tsx
<NativeTabs.Trigger name="index">...<Label>Today</Label></NativeTabs.Trigger>
<NativeTabs.Trigger name="history">...<Label>History</Label></NativeTabs.Trigger>
<NativeTabs.Trigger name="goals">...<Label>Goals</Label></NativeTabs.Trigger>
<NativeTabs.Trigger name="profile">...<Label>Profile</Label></NativeTabs.Trigger>
```

Remove Context and Settings triggers completely.

Use SF Symbols:

```text
Today: house / house.fill
History: clock / clock.fill
Goals: flag / flag.fill
Profile: person.crop.circle / person.crop.circle.fill
```

- [ ] **Step 2: Add `history` to AtlasIcon**

Extend the type to:

```ts
export type AtlasIconName = 'home' | 'history' | 'business' | 'goals' | 'context' | 'settings';
```

Add a simple bounded clock/history geometry for Android fallback. Do not redesign existing icons.

- [ ] **Step 3: Run focused tests**

```bash
node --test tests/mobile/native-tab-shell.test.mjs tests/mobile/vs31-navigation-alignment.test.mjs
```

Some route-placement assertions remain RED until Task 3.

---

### Task 3: Move route entries without changing public URLs

**Files:**
- Create: `apps/mobile/app/(tabs)/history.tsx`
- Create: `apps/mobile/app/context.tsx`
- Create: `apps/mobile/app/settings.tsx`
- Delete: `apps/mobile/app/history.tsx`
- Delete: `apps/mobile/app/(tabs)/context.tsx`
- Delete: `apps/mobile/app/(tabs)/settings.tsx`

**Interfaces:**
- `/history` stays `/history` because `(tabs)` is a route group.
- `/context` and `/settings` remain root Stack paths.

- [ ] **Step 1: Move History entry**

The new tab file remains:

```tsx
import { HistoryScreen } from '@/features/history/HistoryScreen';
export default function HistoryRoute() { return <HistoryScreen />; }
```

Delete the old root history entry.

- [ ] **Step 2: Move Context and Settings entry implementations**

Copy the current implementations to root `app/context.tsx` and `app/settings.tsx`, then delete the old tab files. Do not change data behavior in this step.

- [ ] **Step 3: Run the route-placement RED tests**

Expected: tab/route placement passes; screen-semantic assertions may still fail until Task 4.

---

### Task 4: Adapt History, Profile, Context and Settings to their new navigation roles

**Files:**
- Modify: `apps/mobile/src/features/history/HistoryScreen.tsx`
- Modify: `apps/mobile/src/features/business-hub/BusinessHubScreen.tsx`
- Modify: `apps/mobile/app/context.tsx`
- Modify: `apps/mobile/app/settings.tsx`

**Interfaces:**
- Profile root owns links to `/context` and `/settings`.
- Detail routes own Back-to-Profile fallback behavior.

- [ ] **Step 1: Make History a tab root**

Change:

```tsx
<AtlasScreen contentStyle={...} ...>
```

to:

```tsx
<AtlasScreen hasTabBar contentStyle={...} ...>
```

Remove the Back action entirely. Keep Weekly Review.

- [ ] **Step 2: Make Business Hub the explicit Profile root**

Change eyebrow from `BUSINESS` to `PROFILE` while keeping the existing Business Hub content hierarchy.

Change Context navigation:

```ts
router.push('/context')
```

Add one Settings/preferences secondary action:

```ts
router.push('/settings')
```

Do not remove Edit business details, menu, media, context status or freshness content.

- [ ] **Step 3: Add shared detail back behavior in Context and Settings**

Each detail screen uses:

```ts
function backToProfile() {
  if (router.canGoBack()) router.back();
  else router.replace('/(tabs)/profile');
}
```

Render an accessible Back control near the top with minimum 44-point target.

Remove `hasTabBar` from detail `AtlasScreen` calls, including Context state screens.

- [ ] **Step 4: Preserve existing behaviors**

Verify source contracts still contain:

```text
Context: getContext / saveContext / confirmation controls
Settings: Notifications / BusinessMemoryPanel / resetExpoDemoBusiness
Business Hub: Edit business details / menu / media / context status
History: Weekly review / filters / Opportunity routes
```

---

### Task 5: Full verification, review and PES certification

**Files:**
- Modify: `docs/slices/VS-31.md`
- Modify: `delivery/current-slice.json`

**Interfaces:**
- Certification binds exact final runtime SHA.

- [ ] **Step 1: Run exact-head deterministic gates**

Required:

```text
npm run preflight
CI full validation
Security baseline
Product Intake
```

The full mobile suite must pass; API/migration/dashboard checks run through repository CI even though no backend files changed.

- [ ] **Step 2: Review changed-file boundary**

Fail certification if any API/database/infrastructure/release file changed or if Context/Settings capability was lost.

- [ ] **Step 3: Record certification**

Bind certification to the exact 40-character runtime implementation SHA and include test/gate evidence.

- [ ] **Step 4: Validate the governance-only certification head**

Require CI, Security baseline and Product Intake again on the metadata head.

- [ ] **Step 5: Merge under standing Product Owner authorization**

Mark the PR ready and merge with `expected_head_sha` protection. Do not deploy or release.
