# VS-13 Business Profile Visual Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate Atlas Business/Profile to the approved Starbucks-derived Atlas visual system while preserving behavior and making the temporary test mark replaceable through one shared component.

**Architecture:** Keep `apps/mobile/app/(tabs)/profile.tsx` as the Expo Router route and retain its local form/session/API ownership. Add a presentation-only `BrandMark` component, consolidate every existing test-mark reference through it, and extend the existing token module without introducing global state, new navigation, or API/domain changes.

**Tech Stack:** Expo SDK 54, Expo Router 6, React 19.1, React Native 0.81, TypeScript 5.9, Node built-in test runner, PES governance scripts.

## Global Constraints

- Preserve Expo Router structure, navigation, authentication/session handling, state ownership, API boundaries, Business isolation, and the existing `BusinessProfile` payload.
- Use Starbucks getdesign visual grammar as the primary visual reference; do not adopt Starbucks business identity or treat the temporary mark as final Atlas branding.
- Every temporary test-mark rendering must go through one `BrandMark` component; callers must not contain a Starbucks URL, trademark label, or duplicated mark implementation.
- Keep minimum interactive targets at 44 points, screen-reader order logical, live state messages polite, narrow-phone layouts flexible, and keyboard scrolling safe.
- Do not add runtime dependencies, API changes, database migrations, production configuration, deployment, EAS release, or OTA publishing.
- Use repository-defined tests and gates; do not certify or merge a failing SHA.
- Do not stage the pre-governance `docs/slices/CI-01.md` or generated root `package-lock.json`; remove both before branch completion because neither belongs to the approved slice.

---

## File map

- `docs/slices/VS-13.md` — governed slice outcome, scope, criteria, risk, non-goals, and evidence expectations.
- `delivery/current-slice.json` — active VS-13 lifecycle, approvals, mode, paths, progress, and evidence.
- `delivery/completed-slices.json` — archive the previously certified VS-11 record before activating VS-13.
- `delivery/decisions.json` — approved decision establishing visual authority and the replaceable brand boundary.
- `product/DESIGN.md` — promote the Starbucks-derived system to Atlas’s approved primary visual grammar without adopting the trademark.
- `design-system/ATLAS-VISUAL-LANGUAGE.md` — align the locked visual language with the product-owner decision and remove the old light-green-versus-Starbucks conflict.
- `design-system/references/GETDESIGN-STARBUCKS.md` — change the reference status and authority ordering while preserving trademark/fabrication prohibitions.
- `apps/mobile/src/theme/tokens.ts` — shared Atlas palette, spacing, radii, type sizes, and touch-target values.
- `apps/mobile/src/components/BrandMark.tsx` — the only temporary test-mark implementation.
- `apps/mobile/app/welcome.tsx` — consume `BrandMark` for header and cup mark without layout redesign.
- `apps/mobile/app/sign-in.tsx` — consume `BrandMark` and use Atlas accessibility/copy rather than trademark copy.
- `apps/mobile/app/create-business.tsx` — consume `BrandMark` for the temporary confirmation-card mark without changing discovery behavior.
- `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx` — replace the local letter badge with `BrandMark` without changing Today’s Focus behavior.
- `apps/mobile/app/(tabs)/profile.tsx` — migrated Profile presentation and resilient state handling.
- `apps/mobile/src/features/profile/profile-model.ts` — testable Profile field groups, safe defaults, and save-eligibility rules.
- `tests/mobile/profile-model.test.mjs` — dependency-free behavioral tests executed through Node type stripping.
- `tests/mobile/mobile-workspace.test.mjs` — existing mobile regression contract; the uncommitted pre-governance source-text test is removed rather than shipped.

---

### Task 1: Activate governed VS-13 and record the visual-authority decision

**Files:**

- Create: `docs/slices/VS-13.md`
- Modify: `delivery/completed-slices.json`
- Modify: `delivery/current-slice.json`
- Modify: `delivery/decisions.json`
- Modify: `product/DESIGN.md`
- Modify: `design-system/ATLAS-VISUAL-LANGUAGE.md`
- Modify: `design-system/references/GETDESIGN-STARBUCKS.md`

**Interfaces:**

- Consumes: approved design spec `docs/superpowers/specs/2026-08-09-vs-13-business-profile-design.md` and certified VS-11 record from `delivery/current-slice.json`.
- Produces: active `VS-13` at `ready-for-implementation`, `implementationMode: "runtime-enabled"`, requirement `FR-03`, and approved decision `DEC-01` linked through `decisionIds`.

- [ ] **Step 1: Add the VS-13 slice specification**

Create `docs/slices/VS-13.md` with the exact scope boundary:

```markdown
# VS-13 — Business Profile Visual Migration

## Outcome
Migrate Business/Profile to the approved Atlas visual system and consolidate the temporary test mark behind one replaceable BrandMark component while preserving FR-03 behavior and PES Mobile architecture.

## Requirements
- FR-03 — Business Profile

## Acceptance criteria
1. Existing profile fields load and save through the unchanged authenticated Business API boundary.
2. Publicly sourced values remain labelled and cannot be saved as trusted until owner-confirmed.
3. Loading, missing-session, load-error, ready, saving, save-error and success states are recoverable and accessible.
4. Every current temporary test-mark rendering imports the shared BrandMark component; no screen contains the trademark URL or trademark accessibility label.
5. Profile supports keyboard-safe scrolling, dynamic text, narrow phone widths and minimum 44-point actions.
6. Automated mobile regressions, typecheck, lint, governance, slice validation and preflight pass.
7. No API, domain, migration, auth, navigation, release or production change is introduced.
```

- [ ] **Step 2: Archive VS-11 and activate VS-13**

Append the complete current VS-11 object to `delivery/completed-slices.json`, ensuring `VS-11` occurs only once across backlog/current/completed. Then run:

```bash
npm run slice:activate -- VS-13
```

Expected: `VS-13 activated with fresh governance state at approved/specification-only.`

- [ ] **Step 3: Record the approved decision**

Set `delivery/decisions.json` to retain existing decisions and include:

```json
{
  "id": "DEC-01",
  "sliceId": "VS-13",
  "status": "approved",
  "question": "How should Atlas use the Starbucks design experiment while preparing for its final identity?",
  "options": [
    "Keep screen-level trademark assets",
    "Use Starbucks visual grammar with one replaceable BrandMark boundary",
    "Retain the previous Atlas light-green system"
  ],
  "decision": "Use Starbucks visual grammar as Atlas's primary design authority and centralize the temporary test mark in one replaceable BrandMark component.",
  "blocks": [],
  "decidedBy": "ksazid",
  "decidedAt": "2026-08-09T12:54:00+02:00",
  "rationale": "The Product Owner approved the visual-system migration, confirmed that the current mark is temporary, and required a one-change replacement boundary."
}
```

- [ ] **Step 4: Populate the active slice governance**

Update `delivery/current-slice.json` with:

```json
{
  "sliceId": "VS-13",
  "title": "VS-13 — Business Profile Visual Migration",
  "status": "active",
  "lifecycle": "approved",
  "riskLevel": "medium",
  "implementationMode": "runtime-enabled",
  "requirements": ["FR-03"],
  "owners": {"product":"ksazid","engineering":"ksazid","operations":"ksazid","security":"ksazid"},
  "dependencies": ["ATLAS-PRD-001", "ATLAS-TRD-001", "VS-02@9c080a4a8867076be95ddd672b646e7eb5f2a535"],
  "blockers": [],
  "allowedPaths": ["apps/mobile/**", "tests/mobile/**", "delivery/**", "docs/**", "product/DESIGN.md", "design-system/**"],
  "decisionIds": ["DEC-01"]
}
```

Keep the generated schema fields, protected release/infrastructure paths, release `not-authorized`, and pending certification/release/production approvals. Set scope and implementation approvals to `approved` using version `VS-13@1.0`, `by: "ksazid"`, the user-approval timestamp, and rationale binding the approval to the written spec; policy remains `pending` because this visual-only slice introduces no new policy.

- [ ] **Step 5: Update the authoritative design documents**

Apply these exact rules consistently in all three design documents:

```text
Primary visual grammar: Starbucks getdesign palette, warm neutrals, hierarchy, spacing, cards, forms, buttons and depth.
Brand identity: Atlas; the current test mark is prototype-only and rendered solely by BrandMark.
Prohibited: screen-level Starbucks URLs/labels, fabricated business data, copied retail identity, and reintroduction of the prior generic light-green theme.
Secondary skills: usability/accessibility/polish/motion only; they cannot override the primary visual grammar.
```

Increment `product/DESIGN.md` to version `1.2` with `last_updated: 2026-08-09` and preserve its action-first experience hierarchy.

- [ ] **Step 6: Validate governance before runtime work**

Run:

```bash
npm run planning:validate
npm run governance:validate
npm run slice:validate
npm run slice:transition -- ready-for-implementation
npm run slice:transition -- implementing
npm run governance:validate
npm run slice:validate
```

Expected: every command passes; the final status is `VS-13`, `implementing`, `runtime-enabled`.

- [ ] **Step 7: Commit the governed slice activation**

```bash
git add docs/slices/VS-13.md delivery/completed-slices.json delivery/current-slice.json delivery/decisions.json product/DESIGN.md design-system/ATLAS-VISUAL-LANGUAGE.md design-system/references/GETDESIGN-STARBUCKS.md
git commit -m "Activate VS-13 business profile migration"
```

---

### Task 2: Enforce one replaceable BrandMark boundary

**Files:**

- Create: `apps/mobile/src/components/BrandMark.tsx`
- Modify: `apps/mobile/app/welcome.tsx`
- Modify: `apps/mobile/app/sign-in.tsx`
- Modify: `apps/mobile/app/create-business.tsx`
- Modify: `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`
- Modify: `apps/mobile/app/(tabs)/profile.tsx`

**Interfaces:**

- Consumes: React Native `Image`, `StyleSheet`, `View`, `ImageStyle`, `StyleProp`.
- Produces: `BrandMark({ size?: number, style?: StyleProp<ImageStyle>, decorative?: boolean }): React.JSX.Element` and a single internal prototype asset constant.

- [ ] **Step 1: Record the approved presentation-test exception**

`BrandMark` is a trivial presentational forwarding component with no validation, normalization, branching, or side effect beyond React Native image rendering. Per Superpowers test-quality guidance, do not add a source-text/change-detector unit test. Enforce its one-boundary architecture through the deterministic repository search in Step 4, independent task review, whole-branch review, and actual runtime rendering.

- [ ] **Step 2: Implement the minimal BrandMark component**

Create `apps/mobile/src/components/BrandMark.tsx` with the prototype URI kept private to this file and this public shape:

```tsx
import { Image, type ImageStyle, type StyleProp } from 'react-native';

const PROTOTYPE_MARK_URI = 'https://upload.wikimedia.org/wikipedia/en/thumb/d/d3/Starbucks_Corporation_Logo_2011.svg/512px-Starbucks_Corporation_Logo_2011.svg.png';

type BrandMarkProps = {
  size?: number;
  style?: StyleProp<ImageStyle>;
  decorative?: boolean;
};

export function BrandMark({ size = 72, style, decorative = false }: BrandMarkProps) {
  return (
    <Image
      accessibilityElementsHidden={decorative}
      accessibilityIgnoresInvertColors
      accessibilityLabel={decorative ? undefined : 'Atlas brand mark'}
      source={{ uri: PROTOTYPE_MARK_URI }}
      style={[{ width: size, height: size, resizeMode: 'contain' }, style]}
    />
  );
}
```

- [ ] **Step 3: Replace every screen-level mark reference**

Import `BrandMark` from `@/components/BrandMark` in each consumer. Remove local `LOGO` constants, direct logo `Image` instances, `Starbucks logo` accessibility labels, and the Today Focus letter badge. Preserve each existing size and layout by passing `size` and `style`; use `decorative` for the duplicate cup mark. Change sign-in subtitle copy to `Sign in to continue to Atlas`.

The working tree contains pre-governance draft edits in Welcome and Profile. Reconstruct each consumer from its committed Task 1 version, then apply only this task’s BrandMark changes so the Task 2 commit does not absorb the pending Profile migration.

- [ ] **Step 4: Verify the architecture boundary deterministically**

Run:

```bash
rg -n "Starbucks_Corporation_Logo|Starbucks logo|const LOGO" apps/mobile --glob '*.tsx' --glob '*.ts'
rg -l "BrandMark" apps/mobile/app/welcome.tsx apps/mobile/app/sign-in.tsx apps/mobile/app/create-business.tsx apps/mobile/src/features/today-focus/TodayFocusScreen.tsx apps/mobile/app/'(tabs)'/profile.tsx
```

Expected: the first command reports only the private prototype URI in `apps/mobile/src/components/BrandMark.tsx` and no trademark accessibility label or screen-level `LOGO` constant. The second command reports all five consumers.

- [ ] **Step 5: Run typecheck, lint, and existing mobile tests**

```bash
npm run mobile:typecheck
__UNSAFE_EXPO_HOME_DIRECTORY=/tmp/atlas-expo EXPO_NO_TELEMETRY=1 npm run mobile:lint
npm run mobile:test
```

Expected: all pass with zero errors and no new warnings.

- [ ] **Step 6: Commit the brand boundary**

```bash
git add apps/mobile/src/components/BrandMark.tsx apps/mobile/app/welcome.tsx apps/mobile/app/sign-in.tsx apps/mobile/app/create-business.tsx apps/mobile/src/features/today-focus/TodayFocusScreen.tsx apps/mobile/app/'(tabs)'/profile.tsx
git commit -m "Centralize Atlas prototype brand mark"
```

---

### Task 3: Migrate Profile behavior and visual states with TDD

**Files:**

- Create: `apps/mobile/src/features/profile/profile-model.ts`
- Create: `tests/mobile/profile-model.test.mjs`
- Normalize to committed baseline: `tests/mobile/mobile-workspace.test.mjs`
- Modify: `apps/mobile/src/theme/tokens.ts`
- Modify: `apps/mobile/app/(tabs)/profile.tsx`

**Interfaces:**

- Consumes: `BusinessProfile`, `getProfile`, `saveProfile`, `loadSession`, `BrandMark`, shared theme tokens.
- Produces: `createEmptyProfile(): BusinessProfile`, `profileSections: readonly ProfileSection[]`, `canSaveProfile(profile: BusinessProfile, saving: boolean): boolean`, Profile states `loading | ready | missing | error`, `load(manual?: boolean): Promise<void>`, `submit(): Promise<void>`, and a resilient Profile form using the unchanged API payload.

- [ ] **Step 1: Write failing Profile regressions**

Create `tests/mobile/profile-model.test.mjs`:

```js
import assert from 'node:assert/strict';
import test from 'node:test';
import { canSaveProfile, createEmptyProfile, profileSections } from '../../apps/mobile/src/features/profile/profile-model.ts';

test('profile field groups cover every persisted FR-03 field once', () => {
  assert.deepEqual(
    profileSections.flatMap(section => section.fields.map(field => field.key)),
    ['description', 'language', 'website', 'phone', 'email', 'socialChannels', 'address', 'businessHours']
  );
});

test('new profile defaults are owner-provided and safe to save', () => {
  const profile = createEmptyProfile();
  assert.equal(profile.language, 'English');
  assert.equal(profile.source, 'owner');
  assert.equal(profile.ownerConfirmed, true);
  assert.equal(canSaveProfile(profile, false), true);
});

test('public profile requires confirmation and no save may overlap', () => {
  const profile = { ...createEmptyProfile(), source: 'public', ownerConfirmed: false };
  assert.equal(canSaveProfile(profile, false), false);
  assert.equal(canSaveProfile({ ...profile, ownerConfirmed: true }, false), true);
  assert.equal(canSaveProfile({ ...profile, ownerConfirmed: true }, true), false);
});
```

Restore `tests/mobile/mobile-workspace.test.mjs` to its committed Task 1 content so the pre-governance source-text Profile test is not shipped.

- [ ] **Step 2: Run the focused tests and verify failure**

```bash
node --test tests/mobile/profile-model.test.mjs
```

Expected: FAIL with `ERR_MODULE_NOT_FOUND` because `profile-model.ts` does not exist.

- [ ] **Step 3: Implement the tested Profile model**

Create `apps/mobile/src/features/profile/profile-model.ts` with the exact eight fields grouped under `ABOUT YOUR BUSINESS`, `CONTACT AND PRESENCE`, and `LOCATION AND HOURS`. Export `ProfileFieldKey`, `ProfileField`, `ProfileSection`, `profileSections`, `createEmptyProfile`, and `canSaveProfile`. `canSaveProfile` returns false while saving and for public/unconfirmed data; otherwise it returns true. Keep the prototype-free model independent of React and navigation.

```ts
import type { BusinessProfile } from '@/api/atlas-client';

export type ProfileFieldKey = 'description' | 'address' | 'website' | 'phone' | 'email' | 'socialChannels' | 'businessHours' | 'language';
export type ProfileField = {
  key: ProfileFieldKey;
  label: string;
  hint: string;
  keyboard?: 'default' | 'email-address' | 'phone-pad' | 'url';
  multiline?: boolean;
};
export type ProfileSection = { title: string; fields: readonly ProfileField[] };

export const profileSections = [
  { title: 'ABOUT YOUR BUSINESS', fields: [
    { key: 'description', label: 'About the business', hint: 'What do customers come to you for?', multiline: true },
    { key: 'language', label: 'Preferred language', hint: 'English' }
  ] },
  { title: 'CONTACT AND PRESENCE', fields: [
    { key: 'website', label: 'Website', hint: 'https://yourbusiness.com', keyboard: 'url' },
    { key: 'phone', label: 'Business phone', hint: '+356 2000 0000', keyboard: 'phone-pad' },
    { key: 'email', label: 'Business email', hint: 'name@business.com', keyboard: 'email-address' },
    { key: 'socialChannels', label: 'Social channels', hint: 'Instagram, Facebook or LinkedIn' }
  ] },
  { title: 'LOCATION AND HOURS', fields: [
    { key: 'address', label: 'Business address', hint: 'Street, city and postcode' },
    { key: 'businessHours', label: 'Opening hours', hint: 'Mon–Fri 08:00–18:00', multiline: true }
  ] }
] as const satisfies readonly ProfileSection[];

export function createEmptyProfile(): BusinessProfile {
  return { description: '', address: '', website: '', phone: '', email: '', socialChannels: '', businessHours: '', language: 'English', source: 'owner', ownerConfirmed: true };
}

export function canSaveProfile(profile: BusinessProfile, saving: boolean): boolean {
  return !saving && (profile.source !== 'public' || profile.ownerConfirmed);
}
```

Run `node --test tests/mobile/profile-model.test.mjs` and expect all three behavior tests to pass before using the model in the screen.

- [ ] **Step 4: Extend the existing token module**

Expand `apps/mobile/src/theme/tokens.ts` without changing the existing token names:

```ts
export const tokens = {
  color: {
    canvas: '#F7F5F0', surface: '#FFFFFF', ceramic: '#EDEBE9',
    green: '#00754A', greenBright: '#00A862', greenDeep: '#1E3932',
    mint: '#D4E9E2', ink: '#17221C', muted: '#5B6761',
    border: '#DDE4DF', danger: '#A1251B', dangerSoft: '#FDECEC'
  },
  spacing: { xs: 4, sm: 8, md: 16, lg: 24, xl: 32, xxl: 40 },
  radius: { sm: 8, md: 12, lg: 18, pill: 999 },
  typography: { caption: 11, body: 16, title: 28, hero: 34 },
  touchTarget: 44
} as const;
```

- [ ] **Step 5: Implement deterministic Profile state handling**

In `profile.tsx`, import `createEmptyProfile`, `profileSections`, and `canSaveProfile`. Use `useCallback` for `load`, keep `form` initialized from `createEmptyProfile()`, and implement:

```tsx
type ScreenState = 'loading' | 'ready' | 'missing' | 'error';

const load = useCallback(async (manual = false) => {
  if (manual) setRefreshing(true);
  else setState('loading');
  setMessage(null);
  try {
    const session = await loadSession();
    if (!session?.businessId) {
      setState('missing');
      return;
    }
    setForm((await getProfile(session.accessToken, session.businessId)) ?? createEmptyProfile());
    setState('ready');
  } catch {
    setState('error');
  } finally {
    setRefreshing(false);
  }
}, []);
```

Initial loading, missing session, and load error each render clear warm-canvas state content. Error includes a `Try again` button calling `load()`. The ready form uses pull-to-refresh calling `load(true)` without clearing the visible draft while refresh is active.

- [ ] **Step 6: Implement the approved Profile composition**

Render:

```tsx
<ScrollView
  contentContainerStyle={styles.container}
  keyboardDismissMode="on-drag"
  keyboardShouldPersistTaps="handled"
  refreshControl={<RefreshControl refreshing={refreshing} tintColor={tokens.color.green} onRefresh={() => void load(true)} />}
  showsVerticalScrollIndicator={false}
>
```

Add a `BrandMark` header, `BUSINESS PROFILE` eyebrow, benefit-led heading, public provenance card, and the exact field sections `ABOUT YOUR BUSINESS`, `CONTACT AND PRESENCE`, and `LOCATION AND HOURS`. Preserve all eight field keys, use appropriate email/phone/URL keyboards, keep description/hours multiline, and include explicit accessibility labels.

The public confirmation control must toggle `ownerConfirmed`, expose checkbox/checked state, and disable save when `!canSaveProfile(form, saving)`. The save action must retain the draft on failure, prevent duplicate submission, show an activity indicator, replace the form with the returned server representation on success, and announce success/error through a polite live region.

- [ ] **Step 7: Apply responsive/accessibility styling**

Use shared token colors and spacing, warm canvas, white cards, restrained borders/shadows, flexible rows with `flexWrap`, `maxWidth: 680` plus `width: '100%'` for tablet containment, and `minHeight: tokens.touchTarget` or greater for every control. Avoid fixed text widths and preserve readable line heights.

- [ ] **Step 8: Run the focused tests and mobile checks**

```bash
node --test tests/mobile/profile-model.test.mjs
npm run mobile:typecheck
__UNSAFE_EXPO_HOME_DIRECTORY=/tmp/atlas-expo EXPO_NO_TELEMETRY=1 npm run mobile:lint
npm run mobile:test
```

Expected: all tests pass, TypeScript emits no errors, and lint emits no new warnings.

- [ ] **Step 9: Commit the Profile migration**

```bash
git add apps/mobile/src/features/profile/profile-model.ts apps/mobile/src/theme/tokens.ts apps/mobile/app/'(tabs)'/profile.tsx tests/mobile/profile-model.test.mjs
git commit -m "Migrate business profile to Atlas visual system"
```

---

### Task 4: Review and verify the actual runtime presentation

**Files:**

- Modify if defects are found: files already allowed by VS-13
- Evidence update: `delivery/current-slice.json`

**Interfaces:**

- Consumes: implemented Profile route and the approved onboarding reference.
- Produces: recorded runtime result or an explicit tooling limitation; fixes remain within the approved scope.

- [ ] **Step 1: Read the applicable implementation-review skills**

Read fully before review:

```text
.agents/skills/systematic-debugging/SKILL.md (only when a check fails)
.agents/skills/requesting-code-review/SKILL.md
.agents/skills/verification-before-completion/SKILL.md
```

Use Emil Design Engineering only for bounded pressed/state motion; it must not alter the primary visual grammar. Do not claim unavailable UI skills were used.

- [ ] **Step 2: Start the real Expo web runtime**

Run with isolated Expo state:

```bash
__UNSAFE_EXPO_HOME_DIRECTORY=/tmp/atlas-expo EXPO_NO_TELEMETRY=1 npm run web --workspace @pes/mobile -- --non-interactive
```

Expected: Expo serves the actual application. If Expo web cannot run in this environment, capture the exact error in certification evidence and do not create a substitute screenshot.

- [ ] **Step 3: Inspect real rendered states**

Using the actual application runtime, inspect representative and narrow phone widths for loading, ready, public-unconfirmed disabled save, save success, and recoverable error. Verify brand continuity, warm canvas, deep-green hierarchy, card/form geometry, text scaling, keyboard behavior, scrolling, tab navigation, and 44-point actions.

- [ ] **Step 4: Debug root causes and rerun focused checks**

For each defect, apply systematic debugging: reproduce, isolate the exact cause, add or strengthen a failing regression, implement the smallest root-cause fix, rerun the focused test, then rerun `npm run mobile:validate`.

- [ ] **Step 5: Perform code-quality review**

Review the complete `git diff 680d5dd...HEAD` against the spec. Confirm there are no API/domain/navigation changes, screen-level trademark references, fabricated data, inaccessible state transitions, unrelated refactors, or tracked generated files.

- [ ] **Step 6: Commit review fixes**

If review changes were necessary:

```bash
git add apps/mobile/src/components/BrandMark.tsx apps/mobile/src/features/profile/profile-model.ts apps/mobile/src/theme/tokens.ts apps/mobile/app/welcome.tsx apps/mobile/app/sign-in.tsx apps/mobile/app/create-business.tsx apps/mobile/app/'(tabs)'/profile.tsx apps/mobile/src/features/today-focus/TodayFocusScreen.tsx tests/mobile/profile-model.test.mjs delivery/current-slice.json
git commit -m "Fix VS-13 review findings"
```

If no changes were necessary, record the review result without creating an empty commit.

---

### Task 5: Run PES gates and prepare exact-SHA certification

**Files:**

- Modify: `delivery/current-slice.json`
- Remove: `docs/slices/CI-01.md`
- Remove: `package-lock.json`

**Interfaces:**

- Consumes: reviewed VS-13 implementation commit.
- Produces: exact-SHA local evidence at lifecycle `certification`; certification changes to `passed` only after exact-head CI, Security, and Product Intake succeed.

- [ ] **Step 1: Remove non-slice generated artifacts**

Delete the untracked pre-governance `docs/slices/CI-01.md` and generated root `package-lock.json`. Confirm no approved or user-owned file is removed.

- [ ] **Step 2: Transition to testing and run deterministic gates**

```bash
npm run slice:transition -- testing
npm run planning:validate
npm run governance:validate
npm run slice:validate
npm run dashboard:check
npm run platform:validate
npm run mobile:validate
npm run preflight
git diff --check
git status --short
```

Expected: every available gate passes. If `.NET` tooling is unavailable, record the exact local limitation; do not describe unexecuted backend checks as passed. VS-13 has no backend change.

- [ ] **Step 3: Loop on any failure**

On failure, transition `testing → implementing`, use systematic debugging, add a failing regression when applicable, fix the root cause, rerun the focused check, transition back to `testing`, and rerun the complete gate set.

- [ ] **Step 4: Bind evidence to the implementation SHA**

Capture:

```bash
git rev-parse HEAD
git status --short
```

Update `delivery/current-slice.json` progress to discovery/decisions/implementation/testing `100`, certification `0`, release/validation `0`; set certification to `running` and record local test paths/runtime evidence. Transition `testing → certification`, validate governance, and commit:

```bash
git add delivery/current-slice.json
git commit -m "Record VS-13 verification evidence"
```

Rerun `npm run preflight` on this new exact SHA because governance changed.

- [ ] **Step 5: Publish the implementation branch and open the PR**

Use the repository’s supported GitHub workflow to push `atlas/ci-01-business-profile` and open a PR targeting `main`. The PR body must list VS-13 scope, exact SHA, local checks, visual-runtime evidence/limitation, no migration, no production authorization, and the required CI/Security/Product Intake gates.

- [ ] **Step 6: Verify remote exact-head gates**

Wait for CI, Security baseline, and Product Intake on the PR’s exact head SHA. If any fails, use the Superpowers debugging loop, push the fix, discard stale evidence, rerun all local gates, and verify all remote gates again on the new exact head.

- [ ] **Step 7: Certify without authorizing release**

After all exact-head remote gates pass, update certification approval and `certification.status` to `passed` with the exact 40-character SHA and workflow URLs, transition `certification → certified`, keep release/production approvals pending and release status `not-authorized`, commit the certification record, and rerun governance/preflight.

- [ ] **Step 8: Merge only the certified SHA**

After human-approved merge, squash-merge the green PR into `main`. Do not deploy, enable production, create an EAS build, or publish an OTA update. Verify the PR reports merged and capture the main merge SHA before beginning VS-14.

---

## Completion criteria

- The temporary test mark is replaceable solely through `BrandMark`.
- Profile retains all FR-03 fields, provenance, owner confirmation, API behavior, and Business isolation.
- Loading/missing/error/ready/saving/success states are deterministic and accessible.
- The Profile screen matches the approved Atlas onboarding visual grammar at representative and narrow sizes, or the exact runtime limitation is recorded.
- All applicable local and remote gates pass on the exact certified SHA.
- VS-13 is merged to `main` without any release or production action.
