# VS-28 — Device-Adaptive Native Polish & Motion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the current Atlas mobile experience device-adaptive and native-feeling on modern iOS and Android, with safe-area-correct geometry, restrained interruptible motion, and selective iOS 26 Liquid Glass on floating chrome while preserving Atlas product behavior, brand identity, and route semantics.

**Architecture:** Introduce one pure layout-policy module, one app-wide accessibility preference provider, and three small presentation primitives: `AtlasScreen`, `AtlasMaterialSurface`, and `AtlasPressable`. Keep Expo Router/native screens responsible for push/pop navigation. Migrate current first-party screens away from fixed page-edge padding into the shared safe-area contract. Use Reanimated 4 only for local interaction/state feedback. Treat Liquid Glass as an optional iOS presentation path; the solid Atlas material is always a complete fallback.

**Tech Stack:** Expo SDK 54, Expo Router 6, React Native 0.81, React 19, React Native Reanimated 4, `react-native-safe-area-context`, `react-native-screens`, React Native `AccessibilityInfo`, Expo-compatible `expo-glass-effect`, Node 22 `node:test`, PES/Loop governance.

## Global Constraints

- `ATLAS-DESIGN-001` v1.2 is the visual authority.
- Emil Kowalski Apple Design pinned at `78761e1b57f97dce65b983d640c70a68f39e8163` is a secondary interaction/motion/material reference only.
- Preserve the certified runtime tab routes and order exactly: `index`, `profile`, `goals`, `context`, `settings`.
- VS-28 does not resolve the approved-design four-destination versus inherited-runtime five-tab mismatch. The typed VS-28 decision preserves five tabs only for this bounded polish slice and leaves structural navigation alignment to a separate governed slice.
- Glass is restricted to floating/structural chrome: bottom navigation, existing/approved sheets or modal overlays, and a real floating control if one exists. Ordinary Today, Business, Goals, Context, form, menu, evidence, and content cards stay solid Atlas surfaces.
- `expo-glass-effect` is the only new runtime dependency allowed by this plan. Install it through `npx expo install expo-glass-effect`; never force an incompatible version.
- Keep existing Reanimated 4 and Gesture Handler. Do not add another animation or gesture library.
- Default interaction motion is critically damped/no-overshoot. Bounce is reserved for genuine momentum-driven gestures.
- Reduce Motion and Reduce Transparency are independent. Reduced motion removes spatial/elastic effects; reduced transparency forces solid material.
- Safe-area geometry comes from actual insets, viewport width, and font scale. Never branch on an iPhone model name or add a one-device notch constant.
- Android uses the same safe-area geometry contract and a stable solid/elevated Atlas material. Do not add experimental Android blur just for parity.
- Do not alter APIs, persistence, authentication/session contracts, provider behavior, Goals logic, Context logic, Business Hub data behavior, Today Focus eligibility, or product navigation semantics.
- No production release, EAS build/submit, OTA update, production enablement, or production deployment is authorized.
- Runtime implementation must branch from then-current `main`, not from `atlas/vs28-device-adaptive-native-polish-design`.
- If `main` moves from the approved planning baseline `48bbafb07494e41cfb351643459ce4c6552de378`, inspect all changed `apps/mobile/**`, `tests/mobile/**`, `delivery/**`, and `product/DESIGN.md` paths before activating VS-28.

## Planned File Structure

### New files

- `apps/mobile/src/theme/native-layout.ts` — pure device/safe-area geometry.
- `apps/mobile/src/lib/accessibility-policy.ts` — pure motion/material policy.
- `apps/mobile/src/components/AtlasAccessibilityProvider.tsx` — Reduce Motion / Reduce Transparency state and subscriptions.
- `apps/mobile/src/components/AtlasScreen.tsx` — safe-area-aware scroll/static screen shell.
- `apps/mobile/src/components/AtlasMaterialSurface.tsx` — selective iOS Liquid Glass and solid fallback.
- `apps/mobile/src/components/AtlasPressable.tsx` — immediate interruptible press feedback.
- `tests/mobile/native-layout-model.test.mjs` — geometry tests.
- `tests/mobile/native-material-policy.test.mjs` — accessibility/material tests.
- `tests/mobile/native-tab-shell.test.mjs` — tab route/geometry/material contract.
- `tests/mobile/native-motion.test.mjs` — motion/press contract.
- `tests/mobile/native-screen-shell.test.mjs` — whole-app safe-area migration contract.
- `docs/slices/VS-28.md` — governed slice scope and factual evidence record.

### Existing files expected to change

- `apps/mobile/package.json` and the repository lockfile actually used by the install workflow.
- `apps/mobile/app/_layout.tsx`.
- `apps/mobile/app/(tabs)/_layout.tsx`.
- `apps/mobile/src/theme/tokens.ts`.
- `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`.
- `apps/mobile/src/features/business-hub/BusinessHubScreen.tsx`.
- `apps/mobile/app/(tabs)/goals.tsx`.
- `apps/mobile/app/(tabs)/context.tsx`.
- `apps/mobile/app/(tabs)/settings.tsx`.
- `apps/mobile/app/create-business.tsx`.
- `apps/mobile/app/welcome.tsx`.
- `apps/mobile/app/sign-in.tsx`.
- `apps/mobile/app/progressive-questions.tsx`.
- `apps/mobile/app/edit-business.tsx`.
- `apps/mobile/src/features/business-hub/BusinessMenuScreen.tsx`.
- `apps/mobile/src/features/opportunity-detail/OpportunityDetailScreen.tsx`.
- `apps/mobile/src/features/execution-kit/ExecutionKitScreen.tsx`.
- `apps/mobile/src/features/history/HistoryScreen.tsx`.
- `apps/mobile/src/features/weekly-review/WeeklyReviewScreen.tsx`.
- `apps/mobile/src/features/notifications/NotificationCenterScreen.tsx`.
- `delivery/decisions.json` and `delivery/current-slice.json` on the runtime branch.

---

## Task 1 — Establish the governed VS-28 runtime branch and green baseline

**Files**

- Create: `docs/slices/VS-28.md`
- Modify: `delivery/decisions.json`
- Modify: `delivery/current-slice.json`
- Carry unchanged from planning branch:
  - `docs/superpowers/specs/2026-08-12-vs-28-device-adaptive-native-polish-motion-design.md`
  - `docs/superpowers/plans/2026-08-12-vs-28-device-adaptive-native-polish-motion.md`

**Interfaces**

- Consumes: current `main`, certified VS-27, `ATLAS-DESIGN-001`, approved VS-28 spec, this plan.
- Produces: isolated runtime branch `atlas/vs28-device-adaptive-native-polish`, approved typed navigation-preservation decision, active runtime-enabled VS-28 record, green pre-change baseline.

- [ ] **Step 1: Re-check `main` and concurrent work**

Through the connected GitHub surface:

```text
Read the exact main SHA.
Read delivery/current-slice.json and delivery/decisions.json from main.
Inspect open Atlas PRs and VS-28-like branches.
Inspect overlapping mobile-shell PR file lists if any exist.
Re-read AGENTS.md and product/DESIGN.md.
```

Expected: no unreviewed concurrent active slice owns the same mobile shell files. If there is overlapping active runtime work, stop with `HUMAN_DECISION_REQUIRED`.

If `main` is not `48bbafb07494e41cfb351643459ce4c6552de378`, compare the moved range and review every changed mobile/governance/design file before continuing.

- [ ] **Step 2: Create the runtime branch from exact current `main`**

Local-git path:

```bash
git switch main
git pull --ff-only
git switch -c atlas/vs28-device-adaptive-native-polish
```

Connector-only path: create `atlas/vs28-device-adaptive-native-polish` directly from the exact current `main` SHA.

Do not branch runtime work from the design branch.

- [ ] **Step 3: Carry only the approved VS-28 spec and plan**

The runtime branch must contain exactly the approved documents at these paths before activation:

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

- [ ] **Step 4: Supersede certified VS-27 without rewriting its evidence**

```bash
npm run slice:transition -- superseded
```

Expected: the VS-27 certification SHA/evidence stays intact and its lifecycle advances only through the permitted transition.

- [ ] **Step 5: Record the typed navigation-preservation decision**

Current `main` ends at DEC-08, so the expected ID is `DEC-09`. If Step 1 finds a newer decision on main, use the next available ID and update every VS-28 reference consistently.

The approved decision content is:

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
  "decidedAt": "2026-08-12T01:01:00+02:00",
  "rationale": "The Product Owner approved the VS-28 written design for device-adaptive native polish without a navigation redesign. DEC-02 and DEC-03 already identified the inherited mismatch and deferred structural alignment; VS-28 makes that preservation explicit rather than silently overriding ATLAS-DESIGN-001."
}
```

The timestamp above is the written-spec approval time supplied for this conversation. Do not replace it with a fabricated later time.

- [ ] **Step 6: Create `docs/slices/VS-28.md`**

Write the approved outcome and acceptance contract from the spec. Include:

```text
Authority: ATLAS-PRD-001, ATLAS-TRD-001, ATLAS-DESIGN-001@1.2, VS-27 merge 48bbafb07494e41cfb351643459ce4c6552de378, DEC-09.
In scope: safe areas, native shell geometry, selective glass, restrained motion, reduced-motion/transparency behavior, whole-app first-party screen migration, device acceptance.
Out of scope: navigation restructuring, domain/API/data behavior, ordinary-card glass, experimental Android blur, production release/deployment.
Acceptance: deterministic gates plus iPhone 17 Pro Max, compact iPhone, Android, Reduce Motion, Reduce Transparency, Dynamic Type checks.
Evidence: begin empty/pending and add only evidence that has actually run.
```

- [ ] **Step 7: Activate VS-28 with typed approvals only after the execution handoff is accepted**

Use the repository's existing `schemaVersion: 2` shape. The fixed core values are:

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
    "VS-27@48bbafb07494e41cfb351643459ce4c6552de378"
  ],
  "decisionIds": ["DEC-09"]
}
```

If Step 1 found a newer decision ID, substitute that real ID consistently. The VS-27 dependency remains its merge commit above even when unrelated commits later advance main.

Use allowed paths:

```text
apps/mobile/**
tests/mobile/**
delivery/**
docs/**
```

Preserve the repository's protected release/infrastructure/payment/upload paths. Record scope approval by `ksazid`, policy as `not-required`, and implementation approval only after the user selects an execution approach for this plan. Use the real timestamp of that execution authorization. Keep certification/release/production-enable pending.

Impact notes must say this is presentation/accessibility/runtime-shell work only; no domain/API/session/navigation semantics change.

- [ ] **Step 8: Prove the activated baseline is green before runtime code**

```bash
npm run governance:validate
npm run preflight
```

Expected: PASS. If either fails, invoke `superpowers:systematic-debugging` and fix only the baseline/governance defect before Task 2.

- [ ] **Step 9: Commit Task 1**

```bash
git add delivery/decisions.json delivery/current-slice.json docs/slices/VS-28.md
git commit -m "chore(vs28): activate native polish slice"
```

---

## Task 2 — Add deterministic safe-area geometry and accessibility preference policy

**Files**

- Create: `apps/mobile/src/theme/native-layout.ts`
- Create: `apps/mobile/src/lib/accessibility-policy.ts`
- Create: `apps/mobile/src/components/AtlasAccessibilityProvider.tsx`
- Create: `apps/mobile/src/components/AtlasScreen.tsx`
- Modify: `apps/mobile/src/theme/tokens.ts`
- Modify: `apps/mobile/app/_layout.tsx`
- Test: `tests/mobile/native-layout-model.test.mjs`
- Test: `tests/mobile/native-material-policy.test.mjs`

**Interfaces**

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
  platform: string;
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

- [ ] **Step 1: Write the failing geometry tests**

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

test('compact iPhone geometry uses edge chrome and compact horizontal rhythm', () => {
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

test('Android uses the same semantic inset contract', () => {
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

test('large text grows the tab row instead of clipping labels', () => {
  assert.equal(getAtlasTabBarMetrics({ width: 440, bottomInset: 34, fontScale: 1.4 }).frameHeight, 64);
});
```

- [ ] **Step 2: Run RED**

```bash
node --test tests/mobile/native-layout-model.test.mjs
```

Expected: FAIL because `native-layout.ts` does not exist.

- [ ] **Step 3: Implement the minimum geometry module**

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
      mode: 'edge',
      horizontalInset: 0,
      bottomOffset: 0,
      frameHeight,
      paddingBottom: bottomInset,
      borderRadius: 0,
      obstructionHeight: frameHeight,
    };
  }
  const horizontalInset = width >= 430 ? 16 : 12;
  const bottomOffset = Math.max(8, bottomInset - 8);
  return {
    mode: 'floating',
    horizontalInset,
    bottomOffset,
    frameHeight: rowHeight,
    paddingBottom: 0,
    borderRadius: 24,
    obstructionHeight: rowHeight + bottomOffset,
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

These are viewport/layout breakpoints and Atlas spacing values, not device-model constants.

- [ ] **Step 4: Run GREEN**

```bash
node --test tests/mobile/native-layout-model.test.mjs
```

Expected: PASS all four tests.

- [ ] **Step 5: Write the failing accessibility/material policy tests**

Create `tests/mobile/native-material-policy.test.mjs`:

```js
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import { resolveMaterialMode, resolveMotionMode } from '../../apps/mobile/src/lib/accessibility-policy.ts';

const rootSource = readFileSync(new URL('../../apps/mobile/app/_layout.tsx', import.meta.url), 'utf8');
const providerSource = readFileSync(new URL('../../apps/mobile/src/components/AtlasAccessibilityProvider.tsx', import.meta.url), 'utf8');

test('glass is allowed only on supported iOS when transparency is allowed', () => {
  assert.equal(resolveMaterialMode({ platform: 'ios', glassAvailable: true, reduceTransparency: false }), 'glass');
  assert.equal(resolveMaterialMode({ platform: 'ios', glassAvailable: true, reduceTransparency: true }), 'solid');
  assert.equal(resolveMaterialMode({ platform: 'android', glassAvailable: true, reduceTransparency: false }), 'solid');
  assert.equal(resolveMaterialMode({ platform: 'ios', glassAvailable: false, reduceTransparency: false }), 'solid');
});

test('motion preference is independent from transparency', () => {
  assert.equal(resolveMotionMode(false), 'full');
  assert.equal(resolveMotionMode(true), 'reduced');
});

test('root owns one accessibility preference provider', () => {
  assert.match(rootSource, /AtlasAccessibilityProvider/);
  assert.match(providerSource, /isReduceMotionEnabled/);
  assert.match(providerSource, /reduceMotionChanged/);
  assert.match(providerSource, /isReduceTransparencyEnabled/);
  assert.match(providerSource, /reduceTransparencyChanged/);
});
```

- [ ] **Step 6: Run RED**

```bash
node --test tests/mobile/native-material-policy.test.mjs
```

Expected: FAIL because the policy/provider do not exist and root is not wired.

- [ ] **Step 7: Implement the pure policy**

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

- [ ] **Step 8: Implement `AtlasAccessibilityProvider` conservatively**

Initial state:

```ts
{ reduceMotion: true, reduceTransparency: true, ready: false }
```

On mount, query:

```ts
const [motion, transparency] = await Promise.all([
  AccessibilityInfo.isReduceMotionEnabled(),
  Platform.OS === 'ios' ? AccessibilityInfo.isReduceTransparencyEnabled() : Promise.resolve(false),
]);
```

Then subscribe once to `reduceMotionChanged`; on iOS also subscribe to `reduceTransparencyChanged`. Remove both subscriptions on unmount. If initial queries reject, keep the conservative state and mark the provider usable rather than blocking navigation.

- [ ] **Step 9: Wire the provider into the native root**

Modify `apps/mobile/app/_layout.tsx` to preserve the existing shell and add the provider:

```tsx
<GestureHandlerRootView style={{ flex: 1 }}>
  <SafeAreaProvider>
    <AtlasAccessibilityProvider>
      <Stack screenOptions={{ headerShown: false, animation: 'default', gestureEnabled: true }} />
    </AtlasAccessibilityProvider>
  </SafeAreaProvider>
</GestureHandlerRootView>
```

Do not implement bespoke JavaScript page transitions.

- [ ] **Step 10: Implement `AtlasScreen`**

Use `useSafeAreaInsets()` and `useWindowDimensions()`. Compute `getAtlasScreenMetrics()` every render.

For `mode="scroll"`, render `ScrollView` with this style ordering:

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

Computed safe-area padding must come after caller content styles so a legacy fixed page-edge padding cannot override the shell. Forward `refreshControl`, `showsVerticalScrollIndicator`, and `keyboardShouldPersistTaps` in scroll mode.

For `mode="static"`, render `View` with `{ flex: 1 }` and the same safe-area ordering.

- [ ] **Step 11: Add only reusable native tokens**

Extend `apps/mobile/src/theme/tokens.ts` with a `native` section for values genuinely shared by the primitives:

```ts
native: {
  tabRowHeight: 58,
  tabRowHeightLargeText: 64,
  floatingDockRadius: 24,
  pressScale: 0.985,
}
```

Reuse existing spacing/radius/touch-target tokens instead of duplicating equivalent values.

- [ ] **Step 12: Run Task-2 gates**

```bash
node --test tests/mobile/native-layout-model.test.mjs tests/mobile/native-material-policy.test.mjs
npm run mobile:typecheck
npm run mobile:lint
```

Expected: PASS.

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

## Task 3 — Add selective iOS material and safe-area-derived tab chrome

**Files**

- Modify: `apps/mobile/package.json`
- Modify: the package-manager lockfile that actually changes during Expo install.
- Create: `apps/mobile/src/components/AtlasMaterialSurface.tsx`
- Modify: `apps/mobile/app/(tabs)/_layout.tsx`
- Modify: `tests/mobile/native-material-policy.test.mjs`
- Create: `tests/mobile/native-tab-shell.test.mjs`
- Preserve: `tests/mobile/atlas-brand-navigation.test.mjs`

**Interfaces**

```ts
export type AtlasMaterialSurfaceProps = {
  children?: React.ReactNode;
  kind: 'navigation' | 'sheet' | 'floating';
  style?: StyleProp<ViewStyle>;
};

export function AtlasMaterialSurface(props: AtlasMaterialSurfaceProps): React.ReactElement;
```

Consumes `getAtlasTabBarMetrics`, `useAtlasAccessibility`, `resolveMaterialMode`, and Expo GlassEffect APIs. Produces a complete glass-or-solid floating surface without changing tab routes.

- [ ] **Step 1: Write the failing tab-shell test**

Create `tests/mobile/native-tab-shell.test.mjs`:

```js
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const source = readFileSync(new URL('../../apps/mobile/app/(tabs)/_layout.tsx', import.meta.url), 'utf8');

test('VS-28 preserves all five certified routes', () => {
  for (const route of ['index', 'profile', 'goals', 'context', 'settings']) {
    assert.match(source, new RegExp(`name="${route}"`));
  }
});

test('tab shell derives safe-area geometry and uses bounded material chrome', () => {
  assert.match(source, /getAtlasTabBarMetrics/);
  assert.match(source, /useSafeAreaInsets/);
  assert.match(source, /useWindowDimensions/);
  assert.match(source, /AtlasMaterialSurface/);
  assert.match(source, /tabBarBackground/);
  assert.doesNotMatch(source, /height:\s*76\b/);
});
```

Add this source test to `native-material-policy.test.mjs`:

```js
const materialSource = readFileSync(new URL('../../apps/mobile/src/components/AtlasMaterialSurface.tsx', import.meta.url), 'utf8');

test('material surface guards Liquid Glass and includes a solid fallback', () => {
  assert.match(materialSource, /isLiquidGlassAvailable/);
  assert.match(materialSource, /isGlassEffectAPIAvailable/);
  assert.match(materialSource, /resolveMaterialMode/);
  assert.match(materialSource, /GlassView/);
  assert.match(materialSource, /tokens\.color\.surface/);
  assert.doesNotMatch(materialSource, /opacity\s*:\s*0\.[0-9]+/);
});
```

- [ ] **Step 2: Run RED while preserving the existing navigation test**

```bash
node --test tests/mobile/native-tab-shell.test.mjs tests/mobile/native-material-policy.test.mjs tests/mobile/atlas-brand-navigation.test.mjs
```

Expected: new tests FAIL; existing VS-27 route/icon test stays GREEN.

- [ ] **Step 3: Verify and install GlassEffect through Expo**

```bash
cd apps/mobile
npx expo install expo-glass-effect
cd ../..
npm run mobile:dependencies
```

Expected: Expo installs its SDK-54-compatible version. If Expo rejects compatibility, do not force a package version; keep the solid Atlas fallback and record the native-glass path as blocked for that runtime.

- [ ] **Step 4: Implement `AtlasMaterialSurface`**

Compute native-glass eligibility:

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

When mode is `glass`, render `GlassView` using supported `regular` or `clear` style appropriate for structural navigation. Never set sub-1 opacity on `GlassView` or a parent whose opacity affects it. Use the documented glass animation API only if material arrival itself must animate.

Otherwise render a normal `View` using Atlas surface/border/elevation tokens. The solid path must preserve identical geometry and touch layout.

`kind` may select bounded radius/elevation differences for navigation/sheet/floating chrome. It must never make the component a general content-card wrapper.

- [ ] **Step 5: Replace the fixed 76-point tab bar**

Modify `apps/mobile/app/(tabs)/_layout.tsx`:

```ts
const insets = useSafeAreaInsets();
const { width, fontScale } = useWindowDimensions();
const metrics = getAtlasTabBarMetrics({ width, bottomInset: insets.bottom, fontScale });
```

Configure presentation from `metrics`:

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
    tabBarBackground: () => (
      <AtlasMaterialSurface
        kind="navigation"
        style={{ flex: 1, borderRadius: metrics.borderRadius }}
      />
    ),
  }}
>
```

Preserve all five `Tabs.Screen` names, order, titles, and `AtlasIcon` mappings exactly.

- [ ] **Step 6: Run GREEN and compile gates**

```bash
node --test tests/mobile/native-tab-shell.test.mjs tests/mobile/native-material-policy.test.mjs tests/mobile/atlas-brand-navigation.test.mjs
npm run mobile:typecheck
npm run mobile:lint
npm run mobile:dependencies
```

Expected: PASS.

- [ ] **Step 7: Commit Task 3**

Stage `apps/mobile/package.json`, the lockfile that actually changed, `AtlasMaterialSurface.tsx`, tab layout, and targeted tests. Review the staged diff, then:

```bash
git commit -m "feat(vs28): add adaptive material tab shell"
```

---

## Task 4 — Add immediate interruptible press feedback and remove decorative looping motion

**Files**

- Create: `apps/mobile/src/components/AtlasPressable.tsx`
- Modify: `apps/mobile/app/create-business.tsx`
- Test: `tests/mobile/native-motion.test.mjs`

**Interfaces**

```ts
export type AtlasPressableProps = Omit<PressableProps, 'style'> & {
  style?: StyleProp<ViewStyle>;
  pressedScale?: number;
  pressedOpacity?: number;
};

export function AtlasPressable(props: AtlasPressableProps): React.ReactElement;
```

Default `pressedScale = 0.985`; default `pressedOpacity = 0.92`. Preserve all Pressable semantics and accessibility props.

- [ ] **Step 1: Write the failing motion contract test**

Create `tests/mobile/native-motion.test.mjs`:

```js
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const pressable = readFileSync(new URL('../../apps/mobile/src/components/AtlasPressable.tsx', import.meta.url), 'utf8');
const createBusiness = readFileSync(new URL('../../apps/mobile/app/create-business.tsx', import.meta.url), 'utf8');

test('AtlasPressable responds on touch-down and settles with a no-overshoot spring', () => {
  assert.match(pressable, /onPressIn/);
  assert.match(pressable, /withTiming/);
  assert.match(pressable, /withSpring/);
  assert.match(pressable, /overshootClamping:\s*true/);
  assert.match(pressable, /useAtlasAccessibility/);
});

test('reduced motion suppresses scale while retaining immediate feedback', () => {
  assert.match(pressable, /reduceMotion/);
  assert.match(pressable, /pressedOpacity/);
});

test('business discovery has no decorative looping pulse', () => {
  assert.doesNotMatch(createBusiness, /Animated\.loop/);
});
```

- [ ] **Step 2: Run RED**

```bash
node --test tests/mobile/native-motion.test.mjs
```

Expected: FAIL because `AtlasPressable.tsx` is absent and Create Business still contains `Animated.loop`.

- [ ] **Step 3: Implement `AtlasPressable` with Reanimated 4**

Use Reanimated's animated Pressable wrapper plus shared `scale` and `opacity` values.

Full-motion touch-down:

```ts
scale.value = withTiming(pressedScale, { duration: 70 });
opacity.value = withTiming(pressedOpacity, { duration: 70 });
```

Release/cancel:

```ts
scale.value = withSpring(1, {
  stiffness: 300,
  damping: 35,
  mass: 1,
  overshootClamping: true,
});
opacity.value = withTiming(1, { duration: 100 });
```

Reduced-motion touch-down keeps scale at `1` while applying immediate opacity feedback. New input must retarget current shared values; never disable input merely because the spring is settling.

Invoke caller-provided `onPressIn` and `onPressOut` after updating feedback. Spread semantic/accessibility props without hiding them.

- [ ] **Step 4: Remove the Create Business decorative pulse loop**

Delete React Native `Animated` from the import, the `pulse` ref, and the effect that starts/stops `Animated.loop` while discovery is busy. Keep the existing `ActivityIndicator`, busy copy, state semantics, and reduced-motion behavior. Do not replace it with another loop.

- [ ] **Step 5: Run GREEN and compile checks**

```bash
node --test tests/mobile/native-motion.test.mjs
npm run mobile:typecheck
npm run mobile:lint
```

Expected: PASS.

- [ ] **Step 6: Commit Task 4**

```bash
git add apps/mobile/src/components/AtlasPressable.tsx apps/mobile/app/create-business.tsx tests/mobile/native-motion.test.mjs
git commit -m "feat(vs28): add restrained native press motion"
```

---

## Task 5 — Migrate every current first-party screen to the shared safe-area shell

**Files**

Core persistent destinations:

- `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`
- `apps/mobile/src/features/business-hub/BusinessHubScreen.tsx`
- `apps/mobile/app/(tabs)/goals.tsx`
- `apps/mobile/app/(tabs)/context.tsx`
- `apps/mobile/app/(tabs)/settings.tsx`

Setup/onboarding:

- `apps/mobile/app/create-business.tsx`
- `apps/mobile/app/welcome.tsx`
- `apps/mobile/app/sign-in.tsx`
- `apps/mobile/app/progressive-questions.tsx`
- `apps/mobile/app/edit-business.tsx`

Detail/support:

- `apps/mobile/src/features/business-hub/BusinessMenuScreen.tsx`
- `apps/mobile/src/features/opportunity-detail/OpportunityDetailScreen.tsx`
- `apps/mobile/src/features/execution-kit/ExecutionKitScreen.tsx`
- `apps/mobile/src/features/history/HistoryScreen.tsx`
- `apps/mobile/src/features/weekly-review/WeeklyReviewScreen.tsx`
- `apps/mobile/src/features/notifications/NotificationCenterScreen.tsx`

Test:

- Create: `tests/mobile/native-screen-shell.test.mjs`
- Narrowly modify existing source-contract tests only when they assert the replaced fixed-padding implementation. Never weaken route/domain/accessibility assertions.

**Interfaces**

- Consumes: `AtlasScreen`, `AtlasPressable`, existing feature state/data/API behavior.
- Produces: one safe-area geometry contract across every current first-party user-facing screen, with preserved feature and route semantics.

- [ ] **Step 1: Write the failing whole-app shell regression test**

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

test('every current first-party screen uses AtlasScreen', () => {
  for (const [path, source] of sources) {
    assert.match(source, /AtlasScreen/, `${path} must use AtlasScreen`);
  }
});

test('migrated screens do not retain known one-device page offsets', () => {
  for (const [path, source] of sources) {
    assert.doesNotMatch(source, /paddingTop:\s*(54|57|58)\b/, `${path} retains legacy page top padding`);
  }
});
```

- [ ] **Step 2: Run RED**

```bash
node --test tests/mobile/native-screen-shell.test.mjs
```

Expected: FAIL on existing screens and fixed offsets.

- [ ] **Step 3: Migrate the five persistent destinations first**

Use `AtlasScreen hasTabBar` for Today, Business, Goals, Context, Settings. Preserve each screen's inner content composition, max width, refresh control, feature state, data loading, copy, and routes.

Typical ready-state structure:

```tsx
<AtlasScreen
  hasTabBar
  mode="scroll"
  contentStyle={styles.contentContainer}
  refreshControl={refreshControl}
  showsVerticalScrollIndicator={false}
>
  {children}
</AtlasScreen>
```

Static loading/error/missing states use:

```tsx
<AtlasScreen hasTabBar mode="static" contentStyle={styles.stateContent}>
  {children}
</AtlasScreen>
```

Remove only outer page-edge `paddingTop`, `paddingBottom`, and `paddingHorizontal` now owned by `AtlasScreen`. Retain local card/section spacing.

- [ ] **Step 4: Apply `AtlasPressable` to eligible core actions**

On the five persistent destinations, replace ad-hoc `pressed: { opacity, transform }` feedback for ordinary action/navigation buttons with `AtlasPressable` where practical.

Preserve:

```text
accessibilityRole
accessibilityLabel
accessibilityState
onPress
disabled
existing minHeight >= 44
the approved Atlas color/radius/layout
```

Do not wrap text inputs, pull-to-refresh, or native/continuous gesture controls.

- [ ] **Step 5: Migrate setup/onboarding screens**

Use `AtlasScreen` without `hasTabBar` for Welcome, Sign In, Create Business, Progressive Questions, Edit Business. Forward `keyboardShouldPersistTaps` where the existing screen requires it.

Create Business specifically removes the old outer page constants:

```text
paddingHorizontal: 26
paddingTop: 57
paddingBottom: 30
```

Do not modify its discovery, owner confirmation, location, enrichment, Goals-first routing, or error semantics.

- [ ] **Step 6: Migrate detail/support screens**

Use non-tabbed `AtlasScreen` for Business Menu, Opportunity Detail, Execution Kit, History, Weekly Review, and Notification Center. Preserve existing back routing and wrapper routes.

- [ ] **Step 7: Run shell GREEN**

```bash
node --test tests/mobile/native-screen-shell.test.mjs
```

Expected: PASS.

- [ ] **Step 8: Run the entire mobile validation surface**

```bash
npm run mobile:test
npm run mobile:typecheck
npm run mobile:lint
npm run mobile:dependencies
```

Expected: PASS. If a pre-existing source-contract test fails only because it encoded fixed page padding, update it to assert `AtlasScreen`/safe-area semantics. Do not delete or relax behavioral, route, owner-authority, security, or accessibility assertions.

- [ ] **Step 9: Run repository gates**

```bash
npm run governance:validate
npm run preflight
```

Expected: PASS on exact Task-5 head.

- [ ] **Step 10: Commit Task 5**

Review the staged diff and exclude unrelated generated files, then:

```bash
git add apps/mobile/app apps/mobile/src/features tests/mobile
git commit -m "feat(vs28): apply native shell across Atlas mobile"
```

---

## Task 6 — Validate native transitions, accessibility fallbacks, devices, and certification readiness

**Files**

- Modify only when a verified defect requires it: root/tab layout, `AtlasAccessibilityProvider`, `AtlasMaterialSurface`, `AtlasPressable`, `AtlasScreen`, or migrated screen files.
- Modify: `docs/slices/VS-28.md` with factual evidence.
- Modify: `delivery/current-slice.json` through governed lifecycle/certification updates.
- Add `docs/evidence/VS-28/**` only if the repository's evidence convention requires files for captured acceptance evidence.

**Interfaces**

- Consumes: completed Tasks 1–5, Expo Go test runtime, iPhone 17 Pro Max/iOS 26-class device, compact iPhone profile, representative Android profile, OS accessibility preferences.
- Produces: exact-head deterministic and device evidence, certification-ready SHA, human merge handoff. Produces no release/deployment authorization.

- [ ] **Step 1: Prove native transition ownership and direct peer-tab switching**

```bash
node --test tests/mobile/native-tab-shell.test.mjs tests/mobile/atlas-brand-navigation.test.mjs
npm run mobile:typecheck
```

Verify from source:

```text
Root detail navigation is still Expo Router Stack / native screens owned.
No second JavaScript page-transition framework exists.
Peer tabs switch directly; VS-28 adds no horizontal whole-page slide between tabs.
Push and back remain symmetric/system-owned where the platform supports them.
```

- [ ] **Step 2: Prove accessibility fallbacks deterministically**

```bash
node --test tests/mobile/native-material-policy.test.mjs tests/mobile/native-motion.test.mjs tests/mobile/native-layout-model.test.mjs
```

Expected: PASS for glass/solid policy, motion policy, geometry, and no-overshoot press feedback.

- [ ] **Step 3: Run the full pre-device exact-head gates**

```bash
npm run mobile:validate
npm run governance:validate
npm run preflight
```

Record the exact 40-character head SHA. Device evidence later must correspond to code descended from this head; any device-found fix requires a new exact-head gate run.

- [ ] **Step 4: Start the existing Expo Go test path**

```bash
npm run mobile:start
```

Use the already-approved development/test API and session path. Do not trigger EAS build, EAS submit, OTA update, production API deploy, or production enablement.

- [ ] **Step 5: iPhone 17 Pro Max / iOS 26-class acceptance**

Record pass/fail for every item:

```text
[ ] No header, logo, back control, or first content collides with Dynamic Island/status safe area.
[ ] Today, Business, Goals, Context, and Settings share coherent top rhythm.
[ ] Create Business and pushed/detail screens use the same safe top contract.
[ ] Bottom dock clears the home indicator and all five labels/icons remain balanced/reachable.
[ ] Ordinary cards remain solid Atlas surfaces.
[ ] Glass is limited to approved floating/structural chrome.
[ ] Touch feedback begins immediately and remains restrained.
[ ] Controls can be re-tapped while motion is settling.
[ ] Push/back transitions feel native/reversible; peer tabs do not slide whole pages.
[ ] Final scroll actions remain reachable above the dock/home indicator.
[ ] Atlas retains the approved warm-neutral/green identity and Compass Orbit brand.
```

- [ ] **Step 6: Test Reduce Motion independently**

With iOS Reduce Motion enabled:

```text
[ ] Press/state feedback remains visible.
[ ] Scale/spatial/elastic decorative movement is suppressed.
[ ] Navigation remains immediate and usable.
```

Disable it again and confirm the full-motion path returns through the change subscription without requiring an app restart when React Native exposes the event.

- [ ] **Step 7: Test Reduce Transparency independently**

With iOS Reduce Transparency enabled:

```text
[ ] Bottom navigation uses the solid Atlas fallback.
[ ] Geometry and touch targets stay unchanged.
[ ] Text/icons retain clear contrast.
```

Disable it again and confirm the eligible glass path returns through the change subscription when supported.

- [ ] **Step 8: Test compact iPhone, Android, and Dynamic Type**

Compact iPhone:

```text
[ ] Navigation selects the edge presentation when needed for five labels and >=44-point targets.
[ ] Content does not inherit excessive large-phone whitespace.
```

Representative Android:

```text
[ ] Status/cutout/navigation insets are respected.
[ ] Solid/elevated Atlas chrome is complete without Liquid Glass.
[ ] No iOS-only API crash occurs.
```

Increased text size/font scale:

```text
[ ] Tab row grows to the large-text metric instead of clipping labels.
[ ] Critical headings/actions wrap or expand without horizontal overflow.
[ ] Primary actions remain reachable and >=44 points high.
```

- [ ] **Step 9: Fix each verified device defect through a regression loop**

For each defect:

```text
Add the smallest deterministic regression test when the issue can be represented in geometry/policy/source.
Run the regression RED.
Implement the minimum fix.
Run the targeted test GREEN.
Repeat the affected device acceptance item.
```

Do not add speculative aesthetic changes outside the approved VS-28 spec.

- [ ] **Step 10: Re-run all exact-head gates after the last device fix**

```bash
npm run mobile:validate
npm run governance:validate
npm run preflight
```

Require CI, Security baseline, and Product Intake to pass on the same exact head SHA.

- [ ] **Step 11: Transition through testing and certification only through PES**

Use only repository-permitted transitions:

```bash
npm run slice:transition -- testing
npm run slice:transition -- certification
```

Populate certification evidence only with completed facts:

```text
exact implementation SHA
CI run and verdict
Security baseline run and verdict
Product Intake run and verdict
mobile deterministic gates
iPhone 17 Pro Max Expo Go acceptance
compact iPhone acceptance
Android acceptance
Reduce Motion acceptance
Reduce Transparency acceptance
Dynamic Type acceptance
```

Certification approval must bind the exact 40-character implementation SHA. Release and production-enable remain pending/not authorized.

- [ ] **Step 12: Run completion/branch review skills**

Invoke:

```text
superpowers:verification-before-completion
superpowers:finishing-a-development-branch
```

Confirm:

```text
no unresolved blocker
no unresolved required review thread
PR head equals certified SHA
PR base is current enough to merge
CI/Security/Product green on certified head
no release/deployment authorization was added
```

- [ ] **Step 13: Stop at the human merge gate**

Final readiness handoff contains only factual status:

```text
PR number
exact certified head SHA
CI verdict
Security verdict
Product Intake verdict
device/accessibility acceptance verdict
mergeability/base freshness
release/deployment = NOT AUTHORIZED
```

Do not merge automatically. Do not release, deploy, submit, or publish an OTA update.
