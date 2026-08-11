# VS-28 — Device-Adaptive Native Polish & Motion

Status: Product design approved; written-spec review pending
Date: 2026-08-12
Branch baseline: `main@48bbafb07494e41cfb351643459ce4c6552de378`
Depends on: VS-27 merged and certified

## 1. Purpose

VS-28 is a bounded mobile experience-polish slice. It fixes device-adaptive layout problems visible on modern iPhones, especially the iPhone 17 Pro Max, and adds restrained native-feeling motion and selective material depth without redesigning Atlas.

The result should feel like a current premium native application rather than a fixed-spacing prototype: headers must sit naturally below the Dynamic Island/status region, bottom navigation must respect the home indicator, transitions must respond immediately, and floating chrome may use glass only where it communicates hierarchy.

## 2. Authority and source order

VS-28 remains subordinate to Atlas governance and product design authority in this order:

1. `AGENTS.md` and PES governance.
2. Approved PRD/TRD and recorded product/security decisions.
3. `ATLAS-DESIGN-001` v1.2 (`product/DESIGN.md`) as the visual authority.
4. Merged VS-27 product/navigation/brand contracts.
5. Emil Kowalski Apple Design skill pinned to commit `78761e1b57f97dce65b983d640c70a68f39e8163` as a secondary interaction-engineering reference only.
6. Expo/React Native/Reanimated platform documentation for implementation mechanics.

The Emil Apple Design skill may guide response, direct manipulation, interruptibility, spring behavior, spatial consistency, materials, reduced motion, reduced transparency, and typography detail. It must not replace Atlas colors, information architecture, copy style, brand identity, or product hierarchy.

### 2.1 Known navigation-baseline mismatch

`ATLAS-DESIGN-001` still describes four persistent destinations, while the certified runtime inherited and now ships the five-tab shell. DEC-02 and DEC-03 explicitly recognized this mismatch in earlier slices, preserved the existing five-tab model for bounded work, and deferred navigation alignment to a separate governed navigation slice.

VS-28 is not that navigation-alignment slice. It therefore must not silently redefine the design baseline or restructure navigation as part of visual polish.

Before VS-28 runtime activation, PES must record a fresh typed VS-28 decision that:

- preserves the certified five-tab shell for this slice only;
- keeps route names, destination meaning, and ordering unchanged;
- records that the four-vs-five destination alignment remains deferred to a dedicated governed navigation slice;
- blocks VS-28 runtime work if that decision is not approved.

This makes the inherited conflict explicit rather than treating the merged runtime as an implicit override of the approved design baseline.

## 3. Approved design choice

The Product Owner approved **Approach A: selective glass**.

Glass/material treatment is limited to floating or structural chrome where translucency communicates hierarchy:

- bottom navigation;
- sheets and modal overlays where they already exist or are required by the approved flow;
- selected floating controls when a real floating control exists.

Normal content cards remain Atlas solid warm-neutral surfaces. Today, Business Hub, Goals, Context, forms, evidence, menu content, and ordinary cards must not become glass panels.

No broad “Liquid Glass redesign” is permitted.

## 4. In scope

### 4.1 Safe-area and device-adaptive shell

Replace screen-level magic-number spacing with a shared safe-area-aware layout contract.

The app already provides `SafeAreaProvider`; VS-28 must consume real insets rather than hard-code offsets for a particular simulator/device.

Required behavior:

- top content begins below the actual safe-area/status/Dynamic Island inset plus Atlas spacing tokens;
- bottom content clears the home indicator and the visual tab bar;
- keyboard and scrollable content continue to reach the last actionable control;
- orientation/viewport changes do not leave stale offsets;
- Android cutouts/navigation insets use the same semantic contract;
- compact devices do not inherit large-phone whitespace;
- no screen may compensate with isolated `paddingTop`/`paddingBottom` magic numbers when the shared shell can express the requirement.

The implementation should prefer a small shared screen/layout primitive or pure layout helper rather than repeating inset arithmetic across routes.

### 4.2 Bottom navigation geometry

Subject to the typed decision in section 2.1, preserve the certified five-tab navigation and route names. This slice changes presentation and geometry only.

The current fixed-height white bar is replaced with safe-area-derived chrome:

- navigation content height is tokenized rather than tied to one device;
- the home-indicator inset is outside/under the interactive row as appropriate, never overlapped;
- touch targets remain approximately 44×44 points or larger;
- labels remain readable and do not clip under Dynamic Type;
- on comfortable phone widths, the material may read as an inset floating dock;
- on compact widths, it may become a wider/edge-aligned material surface when required for label/touch-target integrity;
- switching tabs remains immediate; do not animate whole pages horizontally between peer tabs.

The five destinations and their meaning do not change in VS-28.

### 4.3 Top/header rhythm

Audit first-party mobile screens for top alignment and vertical rhythm. Headers must be positioned from the safe-area contract, not from assumptions about a notch height.

The polish pass may normalize:

- safe top gap;
- title/eyebrow spacing;
- first-card spacing;
- scroll-content top/bottom insets;
- consistent header alignment across Today, Business, Goals, Context, Settings, onboarding, and detail routes.

It must not create a new navigation hierarchy or redesign individual feature layouts.

### 4.4 Native navigation transitions

Prefer system/native navigation transitions supplied by Expo Router / React Native Screens for stack push/pop behavior. Do not replace native stack behavior with bespoke JavaScript page animation when the system transition already communicates the spatial relationship correctly.

Rules:

- push and pop use symmetric spatial paths;
- back gestures remain interruptible where the platform supports them;
- modal presentation uses a platform-appropriate native presentation when compatible with the existing route;
- tab changes are immediate, with local tab feedback rather than page slides;
- navigation never waits for decorative animation to finish.

### 4.5 Motion system

Use the already-installed React Native Reanimated 4.x for local interaction/state motion.

Motion principles:

- feedback begins on touch-down, not after the action completes;
- normal UI state changes use critically damped/no-overshoot motion by default;
- bounce/overshoot is reserved for genuine momentum-driven gestures, not ordinary menus/cards;
- animations begin from the current presentation value and remain interruptible;
- enter and exit paths are spatially symmetric;
- transform and opacity are preferred over layout-heavy per-frame work;
- no looping decorative animation;
- no animation may delay content availability or disable input unnecessarily.

Approved motion categories:

1. **Press feedback** — restrained scale response (approximately 0.97–0.99 depending on control size) and immediate recovery.
2. **Selection/state feedback** — short spring/fade for selected tabs, chips, cards, toggles, and disclosure controls where this clarifies the state change.
3. **Loaded-state reveal** — subtle fade/very small translation only when content meaningfully changes from loading/empty to ready; do not replay merely because a user revisits a tab.
4. **Sheet/modal materialization** — spatially anchored appearance using native presentation or bounded Reanimated motion, with matching dismissal.
5. **Expansion/collapse** — short, interruptible motion for existing progressive-disclosure surfaces.

Large decorative parallax, background motion, carousel theatrics, and gamified celebration effects are out of scope.

### 4.6 Material/glass strategy

For Expo SDK 54, `expo-glass-effect` provides native iOS 26+ Liquid Glass through `GlassView` and is documented as included in Expo Go. VS-28 may use it only on iOS after checking runtime/API availability.

Material policy:

- use `GlassView` only for the approved floating/structural surfaces;
- guard the native glass path by platform and runtime/API availability rather than assuming cross-platform glass support;
- if iOS Reduce Transparency is enabled, use the solid Atlas fallback;
- on unsupported iOS/runtime conditions, use the solid Atlas fallback;
- on Android, use a stable solid/elevated Atlas surface in VS-28 rather than introducing experimental blur behavior merely for visual parity;
- do not stack translucent surfaces;
- keep text/icons high-contrast over material;
- do not animate `opacity` on `GlassView` or a parent in a way that breaks native glass rendering; material arrival must use supported glass animation behavior or animate surrounding/non-glass content instead.

The fallback is a first-class design, not an error state.

### 4.7 Reduced motion and reduced transparency

Accessibility preferences are independent controls.

**Reduce Motion enabled:**

- suppress spatial slides, large scale changes, elastic motion, parallax, and overshoot;
- use short opacity cross-fades or static state changes where feedback is still useful;
- preserve immediate pressed/selected state communication;
- native/system transitions should respect the operating-system preference where the platform handles it.

**Reduce Transparency enabled (iOS):**

- disable Liquid Glass for Atlas chrome;
- render an opaque/near-opaque Atlas navigation/sheet surface with adequate contrast;
- preserve the same geometry and touch targets so accessibility does not alter navigation semantics.

The implementation must observe the current setting and respond to setting changes exposed by React Native `AccessibilityInfo` where practical.

### 4.8 Typography and Dynamic Type polish

Keep Atlas typography identity. VS-28 may correct size-specific line-height/tracking and clipping issues, but must not introduce a new typeface or wholesale type scale.

Requirements:

- headings remain compact and optically aligned;
- body text remains readable with comfortable leading;
- labels do not rely on a single fixed tracking value across all sizes;
- Dynamic Type/font scaling does not clip primary actions, tabs, headers, or critical content;
- truncation is used only when the complete value is available elsewhere or the content is non-critical.

### 4.9 Status bar and system chrome

Status-bar content style/background treatment must remain legible across Atlas surfaces and material chrome. Do not fake the Dynamic Island or add device-specific top artwork.

## 5. Explicit non-goals

VS-28 does **not**:

- resolve the four-vs-five persistent-destination product decision;
- change Atlas product information architecture;
- rename/reorder the certified five tabs while the section 2.1 preservation decision applies;
- redesign Today, Business Hub, Goals, Context, Settings, onboarding, or menu flows;
- introduce glass on ordinary content cards;
- add provider/API/database behavior;
- change authentication/session contracts;
- introduce a new animation framework;
- introduce experimental Android blur solely for visual matching;
- add decorative haptics/sound or a new haptics dependency;
- modify production infrastructure;
- release, deploy, submit, publish OTA updates, or enable production.

## 6. Component boundaries

Implementation should remain small and reusable. Conceptual boundaries are:

### `AtlasScreen` / safe-area layout primitive

Responsibility: compute consistent safe top/bottom/content spacing from real insets and screen role.

Depends on: `react-native-safe-area-context`, Atlas spacing tokens.

Must not know business/domain data.

### `AtlasMaterialSurface`

Responsibility: render the approved floating material with native iOS 26 glass when available and an accessible solid fallback otherwise.

Depends on: platform/accessibility signals and `expo-glass-effect`.

Must not be used as a generic wrapper around all cards.

### `AtlasPressable` / motion feedback helper

Responsibility: centralize restrained touch-down/release feedback and reduced-motion behavior for eligible controls.

Depends on: Reanimated/accessibility motion preference.

Must not hide semantic button/accessibility props.

### Navigation shell adaptation

Responsibility: integrate the material surface and safe-area geometry into the existing Expo Router tab shell without changing route semantics.

The exact names may follow existing repository conventions; the boundaries above describe responsibilities, not mandated filenames.

## 7. Data flow and state

VS-28 adds no server or persisted business data.

Local presentation state may include:

- safe-area insets;
- viewport dimensions;
- Reduce Motion preference;
- Reduce Transparency preference on iOS;
- glass runtime availability;
- transient press/animation shared values.

These values must not be persisted as Business Context or transmitted to Atlas APIs.

## 8. Error/degraded behavior

- If native glass is unavailable, navigation/sheets remain fully usable with the solid Atlas fallback.
- If accessibility preference queries fail, default to the conservative solid/no-decorative-motion presentation rather than blocking navigation.
- Motion failures must not prevent taps, routing, saving, or reading content.
- Layout must remain usable when external imagery fails or content is longer than expected.

## 9. Dependency policy

Existing relevant dependencies already include:

- `react-native-reanimated` 4.x;
- `react-native-gesture-handler`;
- `react-native-safe-area-context`;
- `react-native-screens`;
- Expo Router.

VS-28 may add only the Expo-compatible `expo-glass-effect` dependency through `npx expo install expo-glass-effect`, after Expo compatibility verification. No second animation or gesture library is justified.

Because Expo SDK 54 documents `expo-glass-effect` as included in Expo Go, the expected iOS 26 test path is Expo Go without a custom native rebuild, subject to the installed Expo Go client matching the project SDK/runtime capabilities. Unsupported/non-iOS paths must not depend on Liquid Glass to remain usable.

## 10. Testing strategy

### Deterministic tests

Add pure/unit coverage for any shared geometry and preference decision functions, including representative cases:

- modern iPhone with large top/bottom insets;
- smaller/older iPhone-style inset profile;
- Android cutout/navigation inset profile;
- compact width vs comfortable width navigation geometry;
- Reduce Motion on/off;
- Reduce Transparency on/off;
- glass available/unavailable.

Tests should assert semantic layout decisions, not snapshot arbitrary pixel dumps.

### Mobile source/component coverage

Verify:

- no regression to fixed one-device top offsets on migrated core screens;
- tab destinations/routes remain unchanged under the approved VS-28 preservation decision;
- navigation touch targets remain accessible;
- material fallback exists and is reachable;
- reduced-motion path does not execute spatial/elastic effects;
- content remains interactive during transitions.

### Manual/Expo acceptance

Required before certification:

1. iPhone 17 Pro Max / iOS 26-class device through the real Expo Go test path.
2. At least one compact iPhone viewport/profile.
3. At least one representative Android phone profile.
4. Reduce Motion enabled.
5. Reduce Transparency enabled on iOS.
6. Increased text size / Dynamic Type stress check.
7. Portrait baseline; landscape only where existing Atlas routes support it.

The Product Owner acceptance focus on iPhone 17 Pro Max is:

- no Dynamic Island/status-region collision;
- headers align naturally;
- bottom navigation clears the home indicator;
- tab labels/icons feel balanced;
- press feedback is immediate and restrained;
- push/pop/sheet transitions feel native and reversible;
- glass is limited to floating hierarchy and does not reduce readability;
- Atlas still looks unmistakably like Atlas.

## 11. Certification gates

Before certification, the exact implementation SHA must pass the repository-defined gates, including at minimum:

- `npm run governance:validate`;
- `npm run preflight`;
- mobile typecheck/lint/tests required by preflight;
- CI;
- Security baseline;
- Product Intake;
- required accessibility/responsive evidence;
- Expo Go/device acceptance evidence for the hero workflow.

Certification does not authorize merge, release, EAS build/submit, OTA update, production enablement, or production deployment unless separately approved under PES.

## 12. Sequencing

1. Written-spec review and Product Owner approval.
2. Superpowers writing-plan step.
3. Re-check `main` and other active branches/PRs before runtime work.
4. Transition certified VS-27 to superseded only on the VS-28 implementation branch, preserving its certification history.
5. Record/approve the VS-28 typed navigation-preservation decision required by section 2.1.
6. Activate VS-28 with typed scope/implementation approval.
7. Establish a green baseline.
8. Implement in small TDD/reviewable steps: safe-area shell → navigation material/geometry → press/motion primitives → core-screen migration → accessibility preferences → device acceptance.
9. Exact-head certification.
10. Human merge approval.
11. No release/deployment unless separately authorized.

## 13. Success definition

VS-28 succeeds when Atlas uses one coherent device-adaptive native shell across its current mobile experience, feels immediate and physically consistent on modern iOS, degrades safely on other platforms/settings, and retains the approved Atlas visual/product identity.

The slice fails if it solves the iPhone 17 Pro Max by adding more device-specific constants, makes glass a decorative theme, introduces motion that delays interaction, breaks reduced-motion/transparency accessibility, changes route semantics, silently overrides the navigation baseline, or drifts away from `ATLAS-DESIGN-001`.
