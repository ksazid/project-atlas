# VS-28 — Device-Adaptive Native Polish & Motion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the existing Atlas mobile experience device-adaptive and native-feeling across modern iOS and Android, with safe-area-correct geometry, restrained interruptible motion, and selective iOS 26 Liquid Glass on floating chrome while preserving the approved Atlas visual identity and current route semantics.

**Architecture:** Add one pure layout-policy module, one app-wide accessibility preference provider, and three focused presentation primitives (`AtlasScreen`, `AtlasMaterialSurface`, `AtlasPressable`). The existing Expo Router stack and five-tab shell remain structurally intact; screens migrate from fixed top/bottom page padding to the shared safe-area contract. Native stack transitions remain system-owned, Reanimated handles local direct-feedback motion, and Liquid Glass is an optional iOS presentation path with a solid Atlas fallback.

**Tech Stack:** Expo SDK 54, Expo Router 6, React Native 0.81, React 19, React Native Reanimated 4, `react-native-safe-area-context`, `react-native-screens`, `AccessibilityInfo`, optional Expo-compatible `expo-glass-effect`, Node 22 `node:test`, PES/Loop governance.

## Global Constraints

- `ATLAS-DESIGN-001` v1.2 remains the visual authority. Emil Kowalski Apple Design pinned at commit `78761e1b57f97dce65b983d640c70a68f39e8163` is a secondary motion/material reference only.
- Preserve the certified five-tab runtime routes exactly: `index`, `profile`, `goals`, `context`, `settings`. VS-28 does not resolve the four-vs-five destination product decision.
- Before runtime activation, record a fresh typed decision preserving the certified five-tab shell for this slice only and deferring navigation alignment to a separate governed slice.
- Glass is limited to floating/structural chrome: bottom navigation, already-approved sheets/modal overlays, and real floating controls. Ordinary content cards remain solid Atlas surfaces.
- `expo-glass-effect` is the only new runtime dependency permitted, and it must be installed with `npx expo install expo-glass-effect` after Expo compatibility verification.
- Use existing Reanimated 4 for local motion. Do not introduce another animation or gesture library.
- Normal UI motion is critically damped/no-overshoot by default. Bounce is reserved for genuine momentum-driven gestures.
- Reduce Motion suppresses spatial/elastic effects; Reduce Transparency forces a solid Atlas material fallback. These preferences are independent.
- Screen geometry must derive from real safe-area insets and viewport/font-scale inputs. Do not solve iPhone 17 Pro Max alignment with device-name checks or new one-device magic numbers.
- Android uses the same safe-area geometry contract and a stable solid/elevated material fallback in VS-28; do not add experimental blur for parity.
- Do not change API, database, authentication/session, business-domain, provider, recommendation, Goals, Context, Business Hub, or navigation semantics.
- No production release, EAS build/submit, OTA update, production enablement, or production deployment is authorized.
- Runtime implementation must branch from the then-current `main`, not from the documentation branch. If `main` has moved from `48bbafb07494e41cfb351643459ce4c6552de378`, re-read the changed mobile/governance files and reconcile before activating VS-28.

---

## File Structure

### New focused files

- `apps/mobile/src/theme/native-layout.ts` — pure safe-area/tab-bar geometry functions; no React or domain dependencies.
- `apps/mobile/src/lib/accessibility-policy.ts` — pure decisions for full/reduced motion and glass/solid material selection.
- `apps/mobile/src/components/AtlasAccessibilityProvider.tsx` — app-wide Reduce Motion / Reduce Transparency state and subscriptions.
- `apps/mobile/src/components/AtlasScreen.tsx` — shared safe-area-aware scroll/static screen shell.
- `apps/mobile/src/components/AtlasMaterialSurface.tsx` — selective native iOS glass with solid Atlas fallback.
- `apps/mobile/src/components/AtlasPressable.tsx` — immediate, interruptible Reanimated press feedback with reduced-motion behavior.
- `tests/mobile/native-layout-model.test.mjs` — pure geometry contract.
- `tests/mobile/native-material-policy.test.mjs` — material/accessibility policy plus source boundary checks.
- `tests/mobile/native-tab-shell.test.mjs` — tab routes, geometry integration, material background, no fixed 76-point shell.
- `tests/mobile/native-motion.test.mjs` — press feedback and reduced-motion/source contract.
- `tests/mobile/native-screen-shell.test.mjs` — migrated first-party screen coverage and regression checks for fixed page offsets.
- `docs/slices/VS-28.md` — governed scope, acceptance, evidence placeholders populated only with facts available at each lifecycle stage.

### Existing files modified by the implementation

- `apps/mobile/package.json` — add Expo-compatible `expo-glass-effect` only.
- package-manager lockfile generated by the repository's normal install workflow, if present after `npx expo install`.
- `apps/mobile/app/_layout.tsx` — install accessibility provider around the existing native Stack; retain SafeAreaProvider/GestureHandlerRootView.
- `apps/mobile/app/(tabs)/_layout.tsx` — preserve five routes; derive tab geometry and use `AtlasMaterialSurface` for chrome.
- `apps/mobile/src/theme/tokens.ts` — add only reusable native shell/material/motion token values needed by the new primitives.
- Core screens: `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`, `apps/mobile/src/features/business-hub/BusinessHubScreen.tsx`, `apps/mobile/app/(tabs)/goals.tsx`, `apps/mobile/app/(tabs)/context.tsx`, `apps/mobile/app/(tabs)/settings.tsx`, `apps/mobile/app/create-business.tsx`.
- Remaining first-party screens: `apps/mobile/app/welcome.tsx`, `apps/mobile/app/sign-in.tsx`, `apps/mobile/app/progressive-questions.tsx`, `apps/mobile/app/edit-business.tsx`, `apps/mobile/src/features/business-hub/BusinessMenuScreen.tsx`, `apps/mobile/src/features/opportunity-detail/OpportunityDetailScreen.tsx`, `apps/mobile/src/features/execution-kit/ExecutionKitScreen.tsx`, `apps/mobile/src/features/history/HistoryScreen.tsx`, `apps/mobile/src/features/weekly-review/WeeklyReviewScreen.tsx`, `apps/mobile/src/features/notifications/NotificationCenterScreen.tsx`.
- `delivery/decisions.json`, `delivery/current-slice.json` — typed decision and active VS-28 state on the runtime branch only.

---

### Task 1: Establish the governed VS-28 runtime branch and green baseline

**Files:**
- Create on runtime branch: `docs/slices/VS-28.md`
- Modify on runtime branch: `delivery/decisions.json`
- Modify on runtime branch: `delivery/current-slice.json`
- Carry from approved documentation branch: `docs/superpowers/specs/2026-08-12-vs-28-device-adaptive-native-polish-motion-design.md`
- Carry from approved documentation branch: `docs/superpowers/plans/2026-08-12-vs-28-device-adaptive-native-polish-motion.md`

**Interfaces:**
- Consumes: current `main`; certified VS-27 state; `ATLAS-DESIGN-001` v1.2; approved VS-28 spec and this plan.
- Produces: an isolated runtime branch `atlas/vs28-device-adaptive-native-polish`, one approved typed navigation-preservation decision, active VS-28 governance with runtime implementation permission, and a verified green pre-change baseline.

- [ ] **Step 1: Re-check live `main` and concurrent Atlas work**

Run through the connected GitHub surface:

```text
1. Read the exact `main` SHA.
2. Inspect open Atlas PRs and branches for VS-28 or overlapping mobile-shell work.
3. Re-read `delivery/current-slice.json`, `delivery/decisions.json`, `AGENTS.md`, `product/DESIGN.md`.
4. If `main` != 48bbafb07494e41cfb351643459ce4c6552de378, compare the moved range and specifically inspect every changed `apps/mobile/**`, `tests/mobile/**`, `delivery/**`, and `product/DESIGN.md` file before continuing.
```

Expected: no unreviewed concurrent runtime slice owns the same mobile shell files. If there is overlap, stop with `HUMAN_DECISION_REQUIRED` rather than creating a competing active slice.

- [ ] **Step 2: Create the isolated runtime branch from exact current `main`**

```bash
git switch main
git pull --ff-only
git switch -c atlas/vs28-device-adaptive-native-polish
```

When GitHub connector execution is the only available git surface, create `atlas/vs28-device-adaptive-native-polish` directly from the exact current `main` SHA. Do not branch runtime work from `atlas/vs28-device-adaptive-native-polish-design`.

- [ ] **Step 3: Carry only the approved VS-28 spec and plan onto the runtime branch**

The runtime branch must contain these exact approved documents before activation:

```text
docs/superpowers/specs/2026-08-12-vs-28-device-adaptive-native-polish-motion-design.md
docs/superpowers/plans/2026-08-12-vs-28-device-adaptive-native-polish-motion.md
```

Commit:

```bash
git add docs/superpowers/specs/2026-08-12-vs-28-device-adaptive-native-polish-motion-design.md \
        docs/superpowers/plans/2026-08-12-vs-28-device-adaptive-native-polish-motion.md
git commit -m "docs(vs28): carry approved native polish design and plan"
```

- [ ] **Step 4: Transition certified VS-27 to `superseded` without rewriting its certification evidence**

Run the governed transition command on the runtime branch:

```bash
npm run slice:transition -- superseded
```

Expected: the VS-27 record preserves its exact certification SHA/evidence while no longer remaining the active implementation slice.

- [ ] **Step 5: Record the fresh navigation-preservation decision**

Append the next available decision ID (expected `DEC-09`; use the actual next ID if main has added a decision) to `delivery/decisions.json` with this content:

```json
{
  "id": "DEC-09",
  "sliceId": "VS-28",
  "status": "approved",
  "question": "How should VS-28 handle the approved four-destination design baseline while the certified runtime currently ships five PES tabs?",
  "options": [
    "Restructure navigation while performing the native polish",
    "Preserve the certified five-tab shell for VS-28 only and defer navigation alignment to a dedicated governed slice",
    "Silently treat the merged five-tab runtime as a replacement design baseline"
  ],
  "decision": "Preserve the certified five-tab shell for VS-28 only. Keep route names, ordering and destination meaning unchanged, and keep the four-vs-five destination alignment deferred to a separate governed navigation slice.",
  "blocks": [],
  "decidedBy": "ksazid",
  "decidedAt": "<the actual approval timestamp for this execution>",
  "rationale": "The Product Owner approved the VS-28 written design for device-adaptive native polish without a navigation redesign. DEC-02 and DEC-03 already identified the inherited mismatch and deferred structural alignment; VS-28 makes that preservation explicit rather than silently overriding ATLAS-DESIGN-001."
}
```

Use the actual next decision ID and actual execution timestamp; do not fabricate either if main has moved.

- [ ] **Step 6: Create and activate the VS-28 slice record**

Create `docs/slices/VS-28.md` with the approved outcome, in-scope/non-goals, dependency on the merged VS-27 baseline, DEC-09 reference, test/device acceptance requirements, and release boundary from the spec.

Set `delivery/current-slice.json` to a runtime-enabled VS-28 record with:

```json
{
  "schemaVersion": 2,
  "sliceId": "VS-28",
  "title": "VS-28 — Device-Adaptive Native Polish & Motion",
  "status": "active",
  "lifecycle": "implementing",
  "riskLevel": "medium",
  "implementationMode": "runtime-enabled",
  "requirements": ["FR-16"],
  "dependencies": [
    "ATLAS-PRD-001",
    "ATLAS-TRD-001",
    "ATLAS-DESIGN-001@1.2",
    "VS-15@a46b9f28ec1e1d360c153adf8e90c40bbe0caca2",
    "VS-27@<exact current merged main SHA containing PR #49>"
  ],
  "decisionIds": ["DEC-09"]
}
```

Populate the rest from the repository's schema/pattern, with allowed paths limited to `apps/mobile/**`, `tests/mobile/**`, `delivery/**`, and `docs/**`; protect release/infrastructure/payment/upload paths; record scope + implementation as approved by `ksazid`; record policy as `not-required`; leave certification/release/production-enable pending. The implementation approval timestamp must correspond to the user's execution authorization after this plan handoff, not the earlier written-spec approval.

The impact notes must explicitly say this is presentation/accessibility/runtime-shell work only and does not change domain/API/session/navigation semantics.

- [ ] **Step 7: Validate governance before runtime code**

Run:

```bash
npm run governance:validate
npm run preflight
```

Expected: both PASS on the exact activated VS-28 head. If either fails, use `systematic-debugging`; do not begin Task 2 until the baseline is green.

- [ ] **Step 8: Commit the governed activation**

```bash
git add delivery/decisions.json delivery/current-slice.json docs/slices/VS-28.md
git commit -m "chore(vs28): activate native polish slice"
```

---

### Task 2: Add deterministic safe-area geometry and accessibility preference policy

**Files:**
- Create: `apps/mobile/src/theme/native-layout.ts`
- Create: `apps/mobile/src/lib/accessibility-policy.ts`
- Create: `apps/mobile/src/components/AtlasAccessibilityProvider.tsx`
- Create: `apps/mobile/src/components/AtlasScreen.tsx`
- Modify: `apps/mobile/src/theme/tokens.ts`
- Modify: `apps/mobile/app/_layout.tsx`
- Test: `tests/mobile/native-layout-model.test.mjs`
- Test: `tests/mobile/native-material-policy.test.mjs`

**Interfaces:**
- Consumes: `tokens.spacing`, `tokens.touchTarget`, `SafeAreaInsets`, `useWindowDimensions()`, React Native `AccessibilityInfo`.
- Produces:

```ts
export type AtlasTabBarMetricsInput = {
  width: number;
  bottomInset: number;
  fontScale: number;
};

export type AtlasTabBarMetrics = {
  mode: 'floating' | 'edge';
  horizontalInset: number;
  bottomOffset: number;
  frameHeight: number;
  paddingBottom: number;
  borderRadius: number;
  obstructionHeight: number;
};

export type AtlasScreenMetricsInput = {
  width: number;
  topInset: number;
  bottomInset: number;
  fontScale: number;
  hasTabBar: boolean;
};

export type AtlasScreenMetrics = {
  paddingTop: number;
  paddingBottom: number;
  paddingHorizontal: number;
};

export function getAtlasTabBarMetrics(input: AtlasTabBarMetricsInput): AtlasTabBarMetrics;
export function getAtlasScreenMetrics(input: AtlasScreenMetricsInput): AtlasScreenMetrics;

export type AtlasMaterialMode = 'glass' | 'solid';
export type AtlasMotionMode = 'full' | 'reduced';
export function resolveMaterialMode(input: {
  platform: 'ios' | 'android' | 'web' | string;
  glassAvailable: boolean;
  reduceTransparency: boolean;
}): AtlasMaterialMode;
export function resolveMotionMode(reduceMotion: boolean): AtlasMotionMode;

export type AtlasAccessibilityPreferences = {
  reduceMotion: boolean;
  reduceTransparency: boolean;
  ready: boolean;
};
export function useAtlasAccessibility(): AtlasAccessibilityPreferences;

export type AtlasScreenProps = {
  children: React.ReactNode;
  mode?: 'scroll' | 'static';
  hasTabBar?: boolean;
  contentStyle?: StyleProp<ViewStyle>;
  refreshControl?: React.ReactElement;
  showsVerticalScrollIndicator?: boolean;
  keyboardShouldPersistTaps?: ScrollViewProps['keyboardShouldPersistTaps'];
};
export function AtlasScreen(props: AtlasScreenProps): React.ReactElement;
```

- [ ] **Step 1: Write the failing pure geometry tests**

Create `tests/mobile/native-layout-model.test.mjs`:

```js
import assert from 'node:assert/strict';
import test from 'node:test';
import {
  getAtlasScreenMetrics,
  getAtlasTabBarMetrics,
} from '../../apps/mobile/src/theme/native-layout.ts';

test('comfortable modern iPhone geometry follows real insets', () => {
  assert.deepEqual(getAtlasTabBarMetrics({ width: 440, bottomInset: 34, fontScale: 1 }), {
    mode: 'floating',
    horizontalInset: 16,
    bottomOffset: 26,
    frameHeight: 58,
    paddingBottom: 0,
    borderRadius: 24,
    obstructionHeight: 84,
  });
  assert.deepEqual(getAtlasScreenMetrics({ width: 440, topInset: 59, bottomInset: 34, fontScale: 1, hasTabBar: true }), {
    paddingTop: 71,
    paddingBottom: 100,
    paddingHorizontal: 28,
  });
});

test('compact iPhone uses edge material and compact horizontal rhythm', () => {
  assert.deepEqual(getAtlasTabBarMetrics({ width: 320, bottomInset: 34, fontScale: 1 }), {
    mode: 'edge',
    horizontalInset: 0,
    bottomOffset: 0,
    frameHeight: 92,
    paddingBottom: 34,
    borderRadius: 0,
    obstructionHeight: 92,
  });
  assert.deepEqual(getAtlasScreenMetrics({ width: 320, topInset: 47, bottomInset: 34, fontScale: 1, hasTabBar: true }), {
    paddingTop: 55,
    paddingBottom: 108,
    paddingHorizontal: 20,
  });
});

test('Android geometry uses the same semantic contract without iPhone constants', () => {
  assert.deepEqual(getAtlasTabBarMetrics({ width: 412, bottomInset: 24, fontScale: 1 }), {
    mode: 'floating',
    horizontalInset: 12,
    bottomOffset: 16,
    frameHeight: 58,
    paddingBottom: 0,
    borderRadius: 24,
    obstructionHeight: 74,
  });
  assert.deepEqual(getAtlasScreenMetrics({ width: 412, topInset: 24, bottomInset: 24, fontScale: 1, hasTabBar: true }), {
    paddingTop: 36,
    paddingBottom: 90,
    paddingHorizontal: 24,
  });
});

test('large text increases the interactive tab row instead of clipping labels', () => {
  assert.equal(getAtlasTabBarMetrics({ width: 440, bottomInset: 34, fontScale: 1.4 }).frameHeight, 64);
});
```

- [ ] **Step 2: Run the geometry tests and verify RED**

Run:

```bash
node --test tests/mobile/native-layout-model.test.mjs
```

Expected: FAIL because `apps/mobile/src/theme/native-layout.ts` does not exist.

- [ ] **Step 3: Implement the minimal pure geometry module**

Create `apps/mobile/src/theme/native-layout.ts`:

```ts
export type AtlasTabBarMetricsInput = { width: number; bottomInset: number; fontScale: number };
export type AtlasTabBarMetrics = {
  mode: 'floating' | 'edge';
  horizontalInset: number;
  bottomOffset: number;
  frameHeight: number;
  paddingBottom: number;
  borderRadius: number;
  obstructionHeight: number;
};
export type AtlasScreenMetricsInput = {
  width: number;
  topInset: number;
  bottomInset: number;
  fontScale: number;
  hasTabBar: boolean;
};
export type AtlasScreenMetrics = { paddingTop: number; paddingBottom: number; paddingHorizontal: number };

const COMPACT_WIDTH = 390;
const LARGE_TEXT_SCALE = 1.2;

export function getAtlasTabBarMetrics({ width, bottomInset, fontScale }: AtlasTabBarMetricsInput): AtlasTabBarMetrics {
  const rowHeight = fontScale > LARGE_TEXT_SCALE ? 64 : 58;
  if (width < COMPACT_WIDTH) {
    const frameHeight = rowHeight + bottomInset;
    return {
      mode: 'edge', horizontalInset: 0, bottomOffset: 0, frameHeight,
      paddingBottom: bottomInset, borderRadius: 0, obstructionHeight: frameHeight,
    };
  }
  const horizontalInset = width >= 430 ? 16 : 12;
  const bottomOffset = Math.max(8, bottomInset - 8);
  return {
    mode: 'floating', horizontalInset, bottomOffset, frameHeight: rowHeight,
    paddingBottom: 0, borderRadius: 24, obstructionHeight: rowHeight + bottomOffset,
  };
}

export function getAtlasScreenMetrics({ width, topInset, bottomInset, fontScale, hasTabBar }: AtlasScreenMetricsInput): AtlasScreenMetrics {
  const paddingHorizontal = width < 360 ? 20 : width < 430 ? 24 : 28;
  const topGap = width < 360 ? 8 : 12;
  const tab = getAtlasTabBarMetrics({ width, bottomInset, fontScale });
  return {
    paddingTop: topInset + topGap,
    paddingBottom: hasTabBar ? tab.obstructionHeight + 16 : bottomInset + 24,
    paddingHorizontal,
  };
}
```

These constants are viewport/layout breakpoints and Atlas spacing decisions, not device-model identifiers.

- [ ] **Step 4: Run the geometry tests and verify GREEN**

```bash
node --test tests/mobile/native-layout-model.test.mjs
```

Expected: PASS all four tests.

- [ ] **Step 5: Write failing accessibility/material policy tests**

Create `tests/mobile/native-material-policy.test.mjs`:

```js
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import { resolveMaterialMode, resolveMotionMode } from '../../apps/mobile/src/lib/accessibility-policy.ts';

const rootSource = readFileSync(new URL('../../apps/mobile/app/_layout.tsx', import.meta.url), 'utf8');
const providerSource = readFileSync(new URL('../../apps/mobile/src/components/AtlasAccessibilityProvider.tsx', import.meta.url), 'utf8');

test('material policy only allows glass on supported iOS when transparency is allowed', () => {
  assert.equal(resolveMaterialMode({ platform: 'ios', glassAvailable: true, reduceTransparency: false }), 'glass');
  assert.equal(resolveMaterialMode({ platform: 'ios', glassAvailable: true, reduceTransparency: true }), 'solid');
  assert.equal(resolveMaterialMode({ platform: 'android', glassAvailable: true, reduceTransparency: false }), 'solid');
  assert.equal(resolveMaterialMode({ platform: 'ios', glassAvailable: false, reduceTransparency: false }), 'solid');
});

test('motion policy is independent from transparency', () => {
  assert.equal(resolveMotionMode(false), 'full');
  assert.equal(resolveMotionMode(true), 'reduced');
});

test('root owns one app-wide accessibility preference provider', () => {
  assert.match(rootSource, /AtlasAccessibilityProvider/);
  assert.match(providerSource, /isReduceMotionEnabled/);
  assert.match(providerSource, /reduceMotionChanged/);
  assert.match(providerSource, /isReduceTransparencyEnabled/);
  assert.match(providerSource, /reduceTransparencyChanged/);
});
```

- [ ] **Step 6: Run the policy tests and verify RED**

```bash
node --test tests/mobile/native-material-policy.test.mjs
```

Expected: FAIL because `accessibility-policy.ts` and `AtlasAccessibilityProvider.tsx` do not exist and root layout is not wired.

- [ ] **Step 7: Implement the pure accessibility policy**

Create `apps/mobile/src/lib/accessibility-policy.ts`:

```ts
export type AtlasMaterialMode = 'glass' | 'solid';
export type AtlasMotionMode = 'full' | 'reduced';

export function resolveMaterialMode({ platform, glassAvailable, reduceTransparency }: {
  platform: string;
  glassAvailable: boolean;
  reduceTransparency: boolean;
}): AtlasMaterialMode {
  return platform === 'ios' && glassAvailable && !reduceTransparency ? 'glass' : 'solid';
}

export function resolveMotionMode(reduceMotion: boolean): AtlasMotionMode {
  return reduceMotion ? 'reduced' : 'full';
}
```

- [ ] **Step 8: Implement the conservative app-wide accessibility provider**

Create `apps/mobile/src/components/AtlasAccessibilityProvider.tsx` with a context default of:

```ts
{ reduceMotion: true, reduceTransparency: true, ready: false }
```

On mount:

```ts
const [reduceMotion, reduceTransparency] = await Promise.all([
  AccessibilityInfo.isReduceMotionEnabled(),
  Platform.OS === 'ios' ? AccessibilityInfo.isReduceTransparencyEnabled() : Promise.resolve(false),
]);
```

Then subscribe once to `reduceMotionChanged` and, on iOS, `reduceTransparencyChanged`. On query failure keep the conservative initial values rather than blocking the app. Export `useAtlasAccessibility()` and remove both subscriptions on unmount.

- [ ] **Step 9: Add the provider to the existing native root shell**

Modify `apps/mobile/app/_layout.tsx` so the hierarchy remains:

```tsx
<GestureHandlerRootView style={{ flex: 1 }}>
  <SafeAreaProvider>
    <AtlasAccessibilityProvider>
      <Stack screenOptions={{ headerShown: false, animation: 'default', gestureEnabled: true }} />
    </AtlasAccessibilityProvider>
  </SafeAreaProvider>
</GestureHandlerRootView>
```

Do not add JavaScript page transitions. The Stack remains the native navigation owner.

- [ ] **Step 10: Implement the shared `AtlasScreen`**

Create `apps/mobile/src/components/AtlasScreen.tsx` using `useSafeAreaInsets()` and `useWindowDimensions()`. Compute `getAtlasScreenMetrics({ width, topInset: insets.top, bottomInset: insets.bottom, fontScale, hasTabBar })` on every render.

For `mode="scroll"`, render a `ScrollView` whose `contentContainerStyle` combines:

```ts
[
  { flexGrow: 1 },
  contentStyle,
  {
    paddingTop: metrics.paddingTop,
    paddingBottom: metrics.paddingBottom,
    paddingHorizontal: metrics.paddingHorizontal,
  },
]
```

The computed safe-area padding must come after `contentStyle`, so legacy/fixed page padding cannot override the shared contract during migration.

For `mode="static"`, use the same ordering on a `View` with `{ flex: 1 }`.

Forward `refreshControl`, `showsVerticalScrollIndicator`, and `keyboardShouldPersistTaps` only for scroll mode.

- [ ] **Step 11: Add only reusable shell tokens**

Extend `apps/mobile/src/theme/tokens.ts` without changing existing token meanings:

```ts
native: {
  tabRowHeight: 58,
  tabRowHeightLargeText: 64,
  floatingDockRadius: 24,
  screenTopGapCompact: 8,
  screenTopGap: 12,
  screenBottomGap: 16,
  pressScale: 0.985,
}
```

If the implementation can reuse the existing spacing/radius/touchTarget tokens for any value, do so instead of duplicating it.

- [ ] **Step 12: Run targeted and repository validation**

```bash
node --test tests/mobile/native-layout-model.test.mjs tests/mobile/native-material-policy.test.mjs
npm run mobile:typecheck
npm run mobile:lint
```

Expected: all PASS.

- [ ] **Step 13: Commit Task 2**

```bash
git add apps/mobile/src/theme/native-layout.ts \
        apps/mobile/src/lib/accessibility-policy.ts \
        apps/mobile/src/components/AtlasAccessibilityProvider.tsx \
        apps/mobile/src/components/AtlasScreen.tsx \
        apps/mobile/src/theme/tokens.ts \
        apps/mobile/app/_layout.tsx \
        tests/mobile/native-layout-model.test.mjs \
        tests/mobile/native-material-policy.test.mjs
git commit -m "feat(vs28): add device-adaptive native shell policy"
```

---

### Task 3: Add selective iOS material and safe-area-derived tab navigation

**Files:**
- Modify: `apps/mobile/package.json`
- Modify: package-manager lockfile generated by Expo install, if the repository uses one
- Create: `apps/mobile/src/components/AtlasMaterialSurface.tsx`
- Modify: `apps/mobile/app/(tabs)/_layout.tsx`
- Modify: `tests/mobile/native-material-policy.test.mjs`
- Create: `tests/mobile/native-tab-shell.test.mjs`
- Preserve: `tests/mobile/atlas-brand-navigation.test.mjs`

**Interfaces:**
- Consumes: `getAtlasTabBarMetrics`, `useAtlasAccessibility`, `resolveMaterialMode`, Expo `GlassView`, `isLiquidGlassAvailable`, `isGlassEffectAPIAvailable`.
- Produces:

```ts
export type AtlasMaterialSurfaceProps = {
  children?: React.ReactNode;
  kind: 'navigation' | 'sheet' | 'floating';
  style?: StyleProp<ViewStyle>;
};
export function AtlasMaterialSurface(props: AtlasMaterialSurfaceProps): React.ReactElement;
```

The tab shell keeps exactly the five existing `Tabs.Screen` names and local `AtlasIcon` mapping.

- [ ] **Step 1: Write the failing tab-shell contract test**

Create `tests/mobile/native-tab-shell.test.mjs`:

```js
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const source = readFileSync(new URL('../../apps/mobile/app/(tabs)/_layout.tsx', import.meta.url), 'utf8');

test('VS-28 preserves the certified five tab routes', () => {
  for (const route of ['index', 'profile', 'goals', 'context', 'settings']) {
    assert.match(source, new RegExp(`name="${route}"`));
  }
});

test('tab shell derives geometry and uses the bounded material surface', () => {
  assert.match(source, /getAtlasTabBarMetrics/);
  assert.match(source, /useSafeAreaInsets/);
  assert.match(source, /useWindowDimensions/);
  assert.match(source, /AtlasMaterialSurface/);
  assert.match(source, /tabBarBackground/);
  assert.doesNotMatch(source, /height:\s*76\b/);
});
```

Extend `tests/mobile/native-material-policy.test.mjs` with source checks:

```js
const materialSource = readFileSync(new URL('../../apps/mobile/src/components/AtlasMaterialSurface.tsx', import.meta.url), 'utf8');

test('native material is guarded and always has a solid fallback', () => {
  assert.match(materialSource, /isLiquidGlassAvailable/);
  assert.match(materialSource, /isGlassEffectAPIAvailable/);
  assert.match(materialSource, /resolveMaterialMode/);
  assert.match(materialSource, /GlassView/);
  assert.match(materialSource, /tokens\.color\.surface/);
  assert.doesNotMatch(materialSource, /opacity\s*:\s*0\.[0-9]+/);
});
```

- [ ] **Step 2: Run the tab/material tests and verify RED**

```bash
node --test tests/mobile/native-tab-shell.test.mjs tests/mobile/native-material-policy.test.mjs tests/mobile/atlas-brand-navigation.test.mjs
```

Expected: `native-tab-shell` and material-source assertions FAIL; existing brand/navigation test remains GREEN.

- [ ] **Step 3: Verify and install the Expo-compatible glass package**

From the repository root:

```bash
cd apps/mobile
npx expo install expo-glass-effect
cd ../..
npm run mobile:dependencies
```

Expected: Expo selects the SDK-54-compatible package; dependency validation passes. If Expo reports incompatibility, stop the native glass path and implement only the already-approved solid fallback rather than forcing a version.

- [ ] **Step 4: Implement `AtlasMaterialSurface`**

Create `apps/mobile/src/components/AtlasMaterialSurface.tsx`.

Use:

```ts
const glassAvailable = Platform.OS === 'ios'
  && isGlassEffectAPIAvailable()
  && isLiquidGlassAvailable();
const mode = resolveMaterialMode({
  platform: Platform.OS,
  glassAvailable,
  reduceTransparency,
});
```

When `mode === 'glass'`, render `GlassView` with the package's supported regular/clear effect appropriate to structural navigation and a light interactive style only if the API supports that use. Do **not** set sub-1 opacity on the GlassView or a wrapper whose opacity affects it.

Otherwise render a normal `View` with an opaque/near-opaque Atlas surface, border/elevation adequate to separate floating chrome, and the same geometry/touch layout.

`kind` may select small differences in radius/elevation, but must not turn this into a generic glass card wrapper.

- [ ] **Step 5: Replace fixed tab geometry with safe-area-derived geometry**

Modify `apps/mobile/app/(tabs)/_layout.tsx`:

```ts
const insets = useSafeAreaInsets();
const { width, fontScale } = useWindowDimensions();
const metrics = getAtlasTabBarMetrics({ width, bottomInset: insets.bottom, fontScale });
```

Configure the tab bar with absolute/floating geometry derived from `metrics`, not fixed `height: 76`:

```tsx
<Tabs
  screenOptions={{
    headerShown: false,
    tabBarActiveTintColor: tokens.color.green,
    tabBarInactiveTintColor: tokens.color.muted,
    tabBarStyle: {
      position: 'absolute',
      left: metrics.horizontalInset,
      right: metrics.horizontalInset,
      bottom: metrics.bottomOffset,
      height: metrics.frameHeight,
      paddingBottom: metrics.paddingBottom,
      borderTopWidth: 0,
      backgroundColor: 'transparent',
      elevation: 0,
      shadowOpacity: 0,
    },
    tabBarBackground: () => <AtlasMaterialSurface kind="navigation" style={{ flex: 1, borderRadius: metrics.borderRadius }} />,
  }}
>
```

If React Navigation's tab background requires clipping/radius on a wrapper, keep the radius on the material container; do not make ordinary screen content translucent.

Preserve route names, order, titles and `AtlasIcon` mappings exactly.

- [ ] **Step 6: Run exact targeted tests**

```bash
node --test tests/mobile/native-tab-shell.test.mjs tests/mobile/native-material-policy.test.mjs tests/mobile/atlas-brand-navigation.test.mjs
npm run mobile:typecheck
npm run mobile:lint
npm run mobile:dependencies
```

Expected: all PASS.

- [ ] **Step 7: Commit Task 3**

```bash
git add apps/mobile/package.json apps/mobile/src/components/AtlasMaterialSurface.tsx \
        apps/mobile/app/'(tabs)'/_layout.tsx \
        tests/mobile/native-material-policy.test.mjs tests/mobile/native-tab-shell.test.mjs \
        package-lock.json npm-shrinkwrap.json 2>/dev/null || true
git commit -m "feat(vs28): add adaptive material tab shell"
```

Stage only the lockfile that actually exists/changed; do not create an empty lockfile merely to satisfy this example command.

---

### Task 4: Add immediate, interruptible press motion and remove decorative looping motion

**Files:**
- Create: `apps/mobile/src/components/AtlasPressable.tsx`
- Modify: `apps/mobile/app/create-business.tsx`
- Test: `tests/mobile/native-motion.test.mjs`

**Interfaces:**
- Consumes: Reanimated 4, `useAtlasAccessibility()`, `tokens.native.pressScale`, React Native `PressableProps`.
- Produces:

```ts
export type AtlasPressableProps = Omit<PressableProps, 'style'> & {
  style?: StyleProp<ViewStyle>;
  pressedScale?: number;
  pressedOpacity?: number;
};
export function AtlasPressable(props: AtlasPressableProps): React.ReactElement;
```

Default `pressedScale` is `0.985`; default `pressedOpacity` is `0.92`.

- [ ] **Step 1: Write the failing motion contract test**

Create `tests/mobile/native-motion.test.mjs`:

```js
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const pressable = readFileSync(new URL('../../apps/mobile/src/components/AtlasPressable.tsx', import.meta.url), 'utf8');
const createBusiness = readFileSync(new URL('../../apps/mobile/app/create-business.tsx', import.meta.url), 'utf8');

test('AtlasPressable responds on touch-down with Reanimated and returns with a no-overshoot spring', () => {
  assert.match(pressable, /onPressIn/);
  assert.match(pressable, /withTiming/);
  assert.match(pressable, /withSpring/);
  assert.match(pressable, /overshootClamping:\s*true/);
  assert.match(pressable, /useAtlasAccessibility/);
});

test('reduced motion suppresses scale while preserving immediate feedback', () => {
  assert.match(pressable, /reduceMotion/);
  assert.match(pressable, /pressedOpacity/);
});

test('business discovery no longer runs decorative looping pulse motion', () => {
  assert.doesNotMatch(createBusiness, /Animated\.loop/);
});
```

- [ ] **Step 2: Run the motion test and verify RED**

```bash
node --test tests/mobile/native-motion.test.mjs
```

Expected: FAIL because `AtlasPressable.tsx` does not exist and `create-business.tsx` still contains `Animated.loop`.

- [ ] **Step 3: Implement `AtlasPressable`**

Use `Animated.createAnimatedComponent(Pressable)` from Reanimated, one shared `scale` and `opacity` value, and an animated transform/opacity style.

On touch-down in full-motion mode:

```ts
scale.value = withTiming(pressedScale, { duration: 70 });
opacity.value = withTiming(pressedOpacity, { duration: 70 });
```

On release/cancel:

```ts
scale.value = withSpring(1, {
  stiffness: 300,
  damping: 35,
  mass: 1,
  overshootClamping: true,
});
opacity.value = withTiming(1, { duration: 100 });
```

When `reduceMotion` is true, leave scale at `1` and use the same immediate opacity feedback. Invoke any caller-provided `onPressIn` / `onPressOut` handlers after updating the presentation value; preserve every semantic/accessibility prop from `PressableProps`.

Do not disable the component merely because its feedback animation is still settling; new touches retarget from the current shared value.

- [ ] **Step 4: Remove the decorative discovery pulse loop**

In `apps/mobile/app/create-business.tsx`, remove React Native `Animated`, the `pulse` ref, and the effect that runs `Animated.loop` while `busy`.

Keep the existing `ActivityIndicator`/busy semantics and any static progress copy. Do not replace the loop with another decorative loop.

- [ ] **Step 5: Run targeted motion tests and mobile compile checks**

```bash
node --test tests/mobile/native-motion.test.mjs
npm run mobile:typecheck
npm run mobile:lint
```

Expected: all PASS.

- [ ] **Step 6: Commit Task 4**

```bash
git add apps/mobile/src/components/AtlasPressable.tsx apps/mobile/app/create-business.tsx tests/mobile/native-motion.test.mjs
git commit -m "feat(vs28): add restrained native press motion"
```

---

### Task 5: Migrate every first-party Atlas screen to the shared safe-area shell

**Files:**
- Modify: `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`
- Modify: `apps/mobile/src/features/business-hub/BusinessHubScreen.tsx`
- Modify: `apps/mobile/app/(tabs)/goals.tsx`
- Modify: `apps/mobile/app/(tabs)/context.tsx`
- Modify: `apps/mobile/app/(tabs)/settings.tsx`
- Modify: `apps/mobile/app/create-business.tsx`
- Modify: `apps/mobile/app/welcome.tsx`
- Modify: `apps/mobile/app/sign-in.tsx`
- Modify: `apps/mobile/app/progressive-questions.tsx`
- Modify: `apps/mobile/app/edit-business.tsx`
- Modify: `apps/mobile/src/features/business-hub/BusinessMenuScreen.tsx`
- Modify: `apps/mobile/src/features/opportunity-detail/OpportunityDetailScreen.tsx`
- Modify: `apps/mobile/src/features/execution-kit/ExecutionKitScreen.tsx`
- Modify: `apps/mobile/src/features/history/HistoryScreen.tsx`
- Modify: `apps/mobile/src/features/weekly-review/WeeklyReviewScreen.tsx`
- Modify: `apps/mobile/src/features/notifications/NotificationCenterScreen.tsx`
- Test: `tests/mobile/native-screen-shell.test.mjs`
- Modify as needed: existing source-contract tests whose only expectation was the replaced fixed page-padding implementation; do not weaken route/domain/accessibility assertions.

**Interfaces:**
- Consumes: `AtlasScreen`, `AtlasPressable`, existing feature state/data models, existing `RefreshControl` and keyboard behavior.
- Produces: every current first-party user-facing screen uses the same safe-area-derived page geometry; core controls use the shared immediate press feedback where practical; feature/domain behavior and route semantics remain unchanged.

- [ ] **Step 1: Write the failing first-party screen-shell regression test**

Create `tests/mobile/native-screen-shell.test.mjs`:

```js
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const files = [
  '../../apps/mobile/src/features/today-focus/TodayFocusScreen.tsx',
  '../../apps/mobile/src/features/business-hub/BusinessHubScreen.tsx',
  '../../apps/mobile/app/(tabs)/goals.tsx',
  '../../apps/mobile/app/(tabs)/context.tsx',
  '../../apps/mobile/app/(tabs)/settings.tsx',
  '../../apps/mobile/app/create-business.tsx',
  '../../apps/mobile/app/welcome.tsx',
  '../../apps/mobile/app/sign-in.tsx',
  '../../apps/mobile/app/progressive-questions.tsx',
  '../../apps/mobile/app/edit-business.tsx',
  '../../apps/mobile/src/features/business-hub/BusinessMenuScreen.tsx',
  '../../apps/mobile/src/features/opportunity-detail/OpportunityDetailScreen.tsx',
  '../../apps/mobile/src/features/execution-kit/ExecutionKitScreen.tsx',
  '../../apps/mobile/src/features/history/HistoryScreen.tsx',
  '../../apps/mobile/src/features/weekly-review/WeeklyReviewScreen.tsx',
  '../../apps/mobile/src/features/notifications/NotificationCenterScreen.tsx',
];

const sources = files.map(path => [path, readFileSync(new URL(path, import.meta.url), 'utf8')]);

test('all current first-party screens use the shared safe-area shell', () => {
  for (const [path, source] of sources) {
    assert.match(source, /AtlasScreen/, `${path} must use AtlasScreen`);
  }
});

test('migrated screens do not retain known one-device page offsets', () => {
  for (const [path, source] of sources) {
    assert.doesNotMatch(source, /paddingTop:\s*(54|57|58)\b/, `${path} still has a legacy fixed page top offset`);
  }
});
```

- [ ] **Step 2: Run the shell regression test and verify RED**

```bash
node --test tests/mobile/native-screen-shell.test.mjs
```

Expected: FAIL on the current screens because they do not yet all use `AtlasScreen`, and the known fixed offsets remain in Today/Business/Goals/Context/Create Business.

- [ ] **Step 3: Migrate the five persistent tab destinations first**

Migrate:

```text
Today       apps/mobile/src/features/today-focus/TodayFocusScreen.tsx
Business    apps/mobile/src/features/business-hub/BusinessHubScreen.tsx
Goals       apps/mobile/app/(tabs)/goals.tsx
Context     apps/mobile/app/(tabs)/context.tsx
Settings    apps/mobile/app/(tabs)/settings.tsx
```

For tabbed screens use:

```tsx
<AtlasScreen
  hasTabBar
  mode="scroll"
  contentStyle={styles.contentContainer}
  refreshControl={existingRefreshControlWhenPresent}
  showsVerticalScrollIndicator={false}
>
  ...existing feature content unchanged...
</AtlasScreen>
```

Move only non-safe-area layout rules into `contentStyle`, such as `alignItems`, `gap`, `maxWidth`, or page background. Remove page-level fixed `paddingTop`, fixed `paddingBottom`, and fixed outer horizontal padding now owned by `AtlasScreen`.

For loading/error state wrappers that are static, use `<AtlasScreen hasTabBar mode="static" contentStyle={...}>` so they also clear the system chrome and tab obstruction.

Do not change copy, feature state transitions, API calls, routes, or data semantics.

- [ ] **Step 4: Convert eligible core pressables to `AtlasPressable` without changing semantics**

On the five persistent destinations, replace the ad-hoc `pressed: { opacity, transform }` presentation on primary navigation/action buttons with `AtlasPressable` where the component is a normal button/card activation.

Preserve:

```text
accessibilityRole / accessibilityLabel / accessibilityState
onPress / disabled
minimum 44-point target
existing colors/radii/layout
```

Do not convert text inputs, pull-to-refresh gestures, or controls whose native gesture behavior already owns continuous motion.

- [ ] **Step 5: Migrate onboarding/setup screens**

Migrate:

```text
apps/mobile/app/welcome.tsx
apps/mobile/app/sign-in.tsx
apps/mobile/app/create-business.tsx
apps/mobile/app/progressive-questions.tsx
apps/mobile/app/edit-business.tsx
```

These screens are not behind the persistent tab bar, so use `hasTabBar={false}` (or omit it if false is the default). Preserve existing keyboard/scroller behavior; pass `keyboardShouldPersistTaps` through `AtlasScreen` when the existing screen requires it.

Specifically in `create-business.tsx`, remove the legacy `container` page padding `paddingHorizontal: 26`, `paddingTop: 57`, `paddingBottom: 30`; retain its internal component/card spacing and staged discovery behavior.

- [ ] **Step 6: Migrate detail/support screens**

Migrate:

```text
apps/mobile/src/features/business-hub/BusinessMenuScreen.tsx
apps/mobile/src/features/opportunity-detail/OpportunityDetailScreen.tsx
apps/mobile/src/features/execution-kit/ExecutionKitScreen.tsx
apps/mobile/src/features/history/HistoryScreen.tsx
apps/mobile/src/features/weekly-review/WeeklyReviewScreen.tsx
apps/mobile/src/features/notifications/NotificationCenterScreen.tsx
```

Use `hasTabBar={false}` because these are pushed/detail routes outside the persistent tab shell. Preserve native back behavior and existing route wrappers.

- [ ] **Step 7: Verify the shell regression test is GREEN**

```bash
node --test tests/mobile/native-screen-shell.test.mjs
```

Expected: PASS.

- [ ] **Step 8: Run the complete mobile test suite and fix only real regressions**

```bash
npm run mobile:test
npm run mobile:typecheck
npm run mobile:lint
npm run mobile:dependencies
```

Expected: all PASS. If an existing source-contract test fails only because it asserted a legacy fixed padding implementation, update that assertion to the new semantic safe-area contract. Do not remove or weaken route, behavior, accessibility, owner-authority, or domain assertions.

- [ ] **Step 9: Run repository preflight**

```bash
npm run governance:validate
npm run preflight
```

Expected: PASS on the exact Task-5 head.

- [ ] **Step 10: Commit Task 5**

```bash
git add apps/mobile/app apps/mobile/src/features tests/mobile/native-screen-shell.test.mjs tests/mobile
git commit -m "feat(vs28): apply native shell across Atlas mobile"
```

Review the staged diff before commit and exclude unrelated generated files. `tests/mobile` is staged because existing source-contract tests may need narrow semantic updates; no unrelated test weakening is allowed.

---

### Task 6: Validate native transitions, accessibility fallbacks, device acceptance, and certification readiness

**Files:**
- Modify only if a verified defect is found: `apps/mobile/app/_layout.tsx`, `apps/mobile/src/components/AtlasAccessibilityProvider.tsx`, `apps/mobile/src/components/AtlasMaterialSurface.tsx`, `apps/mobile/src/components/AtlasPressable.tsx`, migrated screen files.
- Modify: `docs/slices/VS-28.md`
- Modify: `delivery/current-slice.json`
- Evidence paths according to the repository's existing convention under `docs/evidence/VS-28/` if governance requires files rather than inline evidence entries.

**Interfaces:**
- Consumes: completed Tasks 1–5, Expo Go runtime, iOS 26-class iPhone 17 Pro Max acceptance, compact iPhone profile, representative Android profile, accessibility settings.
- Produces: exact-head deterministic evidence, device/accessibility evidence, lifecycle transition to certification only when all required gates are green, and a human merge-ready handoff. It does not produce a release/deployment authorization.

- [ ] **Step 1: Prove native stack ownership and absence of peer-tab page slides**

Inspect `apps/mobile/app/_layout.tsx` and `apps/mobile/app/(tabs)/_layout.tsx` and run:

```bash
node --test tests/mobile/native-tab-shell.test.mjs tests/mobile/atlas-brand-navigation.test.mjs
npm run mobile:typecheck
```

Expected:

```text
Root detail navigation remains Expo Router Stack / react-native-screens owned.
No custom JavaScript page transition library exists.
Five peer tabs remain direct tab switches; there is no horizontal page-slide implementation between them.
```

- [ ] **Step 2: Prove accessibility fallback logic deterministically**

Run:

```bash
node --test tests/mobile/native-material-policy.test.mjs tests/mobile/native-motion.test.mjs tests/mobile/native-layout-model.test.mjs
```

Expected: PASS, including:

```text
Reduce Transparency + iOS glass availability => solid
Android + glass availability => solid
Reduce Motion => reduced
full motion => critically damped press return
```

- [ ] **Step 3: Run the full exact-head deterministic gate set before device testing**

```bash
npm run mobile:validate
npm run governance:validate
npm run preflight
```

Expected: PASS. Record the exact 40-character commit SHA being tested.

- [ ] **Step 4: Start the real Expo Go test path without an EAS/prod build**

From the mobile workspace:

```bash
npm run mobile:start
```

Use the existing approved development/test API/session path. Do not trigger EAS build, submit, OTA update, production API deploy, or production enablement.

- [ ] **Step 5: Run the iPhone 17 Pro Max / iOS 26-class acceptance checklist**

On the real device, verify each item and record pass/fail evidence:

```text
[ ] No header, logo, back button or first content collides with the Dynamic Island/status region.
[ ] Today, Business, Goals, Context and Settings share a coherent top rhythm.
[ ] Create Business, sign-in/onboarding and pushed detail screens use the same safe top contract.
[ ] Bottom navigation clears the home indicator and remains reachable with all five labels/icons balanced.
[ ] Normal cards remain solid Atlas surfaces; glass is limited to the floating navigation/approved chrome.
[ ] Press feedback begins on touch-down and feels restrained; controls remain tappable while motion settles.
[ ] Push/back transitions feel native and reversible; peer tabs do not slide whole pages.
[ ] Long scroll content reaches its final actionable control without hiding beneath the dock/home indicator.
[ ] Atlas still uses the approved warm-neutral/green identity and Compass Orbit brand.
```

- [ ] **Step 6: Validate Reduce Motion and Reduce Transparency separately on iOS**

With **Reduce Motion ON**:

```text
[ ] Press/state feedback remains visible.
[ ] Scale/spatial/elastic decorative movement is suppressed.
[ ] Navigation remains immediate and usable.
```

With **Reduce Transparency ON**:

```text
[ ] Bottom navigation uses the solid Atlas fallback.
[ ] Geometry/touch targets do not change.
[ ] Text/icons retain sufficient contrast.
```

Turn each setting back off after its independent check and verify the full presentation returns without restarting the app when React Native exposes the change event.

- [ ] **Step 7: Validate compact iPhone, Android, and Dynamic Type**

Use a compact iPhone simulator/device profile and a representative Android phone profile:

```text
[ ] Compact width selects edge-style navigation when needed for five labels/44-point targets.
[ ] Android cutout/status/navigation insets are respected.
[ ] Android uses solid/elevated Atlas chrome; lack of Liquid Glass does not degrade usability.
```

Increase text size / font scale:

```text
[ ] Tab row grows to the large-text metric instead of clipping labels.
[ ] Critical headers/actions wrap or expand without horizontal overflow.
[ ] Primary actions remain reachable and at least ~44 points high.
```

- [ ] **Step 8: Fix verified runtime defects using TDD/regression tests**

For every defect found in Steps 5–7:

```text
1. Add the smallest deterministic test reproducing the policy/geometry/source regression when possible.
2. Run it RED.
3. Implement the minimum fix.
4. Run the targeted test GREEN.
5. Re-run the affected device acceptance item.
```

Do not make speculative aesthetic changes outside the approved spec during this step.

- [ ] **Step 9: Re-run all exact-head gates after the final device fix**

```bash
npm run mobile:validate
npm run governance:validate
npm run preflight
```

Then require CI, Security baseline, and Product Intake to pass on that same exact head SHA.

Expected: every required gate GREEN on one exact SHA.

- [ ] **Step 10: Transition through testing/certification using repository governance**

Use only permitted lifecycle transitions, for example:

```bash
npm run slice:transition -- testing
npm run slice:transition -- certification
```

Populate `delivery/current-slice.json` certification evidence with factual references to:

```text
exact implementation SHA
CI run
Security baseline run
Product Intake run
mobile deterministic gates
iPhone 17 Pro Max Expo Go acceptance
compact iPhone acceptance
Android acceptance
Reduce Motion acceptance
Reduce Transparency acceptance
Dynamic Type acceptance
```

Certification approval must bind the exact 40-character SHA. Do not mark release or production-enable approved.

- [ ] **Step 11: Run verification-before-completion and branch-finishing workflow**

Invoke the required Superpowers skills:

```text
superpowers:verification-before-completion
superpowers:finishing-a-development-branch
```

Confirm there are no unresolved blockers/review threads, the PR head equals the certified SHA, and the PR is mergeable against current `main`.

- [ ] **Step 12: Stop at the human merge gate**

Report only:

```text
PR number
exact certified head SHA
CI / Security / Product verdicts
device acceptance verdict
mergeability/base freshness
release/deployment status = NOT AUTHORIZED
```

Do not merge automatically. Do not release, deploy, submit, or publish an OTA update.
