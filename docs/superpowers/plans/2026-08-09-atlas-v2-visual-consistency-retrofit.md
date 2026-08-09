# Atlas V2 Visual Consistency Retrofit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Today/Home, Business/Profile, Goals and Context visually consistent with the four approved Atlas V2 onboarding screens while preserving all existing behavior.

**Architecture:** This is a presentation-only retrofit. Existing screen components, state machines, API calls and domain models remain intact; changes are limited to styles, layout wrappers and non-functional presentation details. The approved onboarding screens and `product/DESIGN.md` remain the comparison baseline.

**Tech Stack:** Expo Router, React Native, TypeScript, existing Atlas theme tokens, Starbucks getdesign reference, Emil Kowalski motion guidance.

## Global Constraints

- No API, database, auth, navigation or persistence changes.
- Do not modify the four approved onboarding screens.
- No Starbucks labels, URLs, copied artwork or fabricated business data.
- BrandMark remains the only brandmark boundary.
- Preserve accessibility semantics and minimum 44-point touch targets.
- Motion is restrained and limited to immediate press/state feedback; no animation library is added.
- No deployment or production enablement.

---

### Task 1: Add visual regression contract

**Files:**
- Create: `tests/mobile/v2-visual-consistency.test.mjs`
- Read: `apps/mobile/app/welcome.tsx`
- Read: `apps/mobile/app/sign-in.tsx`
- Read: `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`
- Read: `apps/mobile/app/(tabs)/profile.tsx`
- Read: `apps/mobile/app/(tabs)/goals.tsx`
- Read: `apps/mobile/app/(tabs)/context.tsx`

**Interfaces:**
- Consumes: source files only.
- Produces: static guardrails preventing regression to the superseded generic visual language or third-party branding leakage.

- [ ] **Step 1: Write failing source-contract tests**

Assert the retrofit targets use the approved editorial heading family, approved white/warm canvas treatment, compact primary-button radius, subtle press transform, BrandMark, and contain no screen-level Starbucks URLs/labels.

- [ ] **Step 2: Run mobile tests and verify RED**

Run: `node --test tests/mobile/v2-visual-consistency.test.mjs`
Expected: FAIL on at least Profile, Goals and Context before the retrofit.

- [ ] **Step 3: Commit RED contract**

---

### Task 2: Retrofit Business/Profile

**Files:**
- Modify: `apps/mobile/app/(tabs)/profile.tsx`
- Test: `tests/mobile/v2-visual-consistency.test.mjs`
- Existing behavioral tests: `tests/mobile/profile-*.test.mjs`

**Interfaces:**
- Consumes: existing profile state/model/API behavior.
- Produces: visually aligned Profile screen with unchanged behavior.

- [ ] **Step 1: Update presentation only**

Use white/warm-neutral canvas, onboarding-consistent green hierarchy, Georgia/editorial heading treatment, 10px-class controls, restrained border/shadow, 55px primary action, and subtle scale press feedback.

- [ ] **Step 2: Run visual and profile tests**

Expected: PASS with no behavior changes.

- [ ] **Step 3: Commit**

---

### Task 3: Retrofit Goals

**Files:**
- Modify: `apps/mobile/app/(tabs)/goals.tsx`
- Test: `tests/mobile/v2-visual-consistency.test.mjs`
- Existing behavioral tests: `tests/mobile/goals-*.test.mjs`

**Interfaces:**
- Consumes: existing goals coordinator/model/API behavior.
- Produces: visually aligned Goals screen with unchanged ordering, custom-goal and retry/save behavior.

- [ ] **Step 1: Update presentation only**

Apply the same header, surface, control, card and press-feedback grammar as the approved onboarding screens.

- [ ] **Step 2: Run visual and goals tests**

Expected: PASS.

- [ ] **Step 3: Commit**

---

### Task 4: Retrofit Context

**Files:**
- Modify: `apps/mobile/app/(tabs)/context.tsx`
- Test: `tests/mobile/v2-visual-consistency.test.mjs`
- Existing behavioral tests: `tests/mobile/context-*.test.mjs`

**Interfaces:**
- Consumes: existing context operation coordinator, provenance and confirmation behavior.
- Produces: visually aligned Context screen with unchanged provenance/confirmation semantics.

- [ ] **Step 1: Update presentation only**

Preserve public/owner provenance distinctions while adopting the same white/warm surface, editorial header, restrained green/mint accents, compact inputs/buttons and subtle press feedback.

- [ ] **Step 2: Run visual and context tests**

Expected: PASS.

- [ ] **Step 3: Commit**

---

### Task 5: Retrofit Today/Home

**Files:**
- Modify: `apps/mobile/src/features/today-focus/TodayFocusScreen.tsx`
- Test: `tests/mobile/v2-visual-consistency.test.mjs`
- Existing behavioral tests: Today/Opportunity mobile tests.

**Interfaces:**
- Consumes: existing Today focus API and decision behavior.
- Produces: visually aligned Today screen retaining one-primary-action hierarchy.

- [ ] **Step 1: Update presentation only**

Keep the current decision-first structure, but align surfaces, header, buttons, cards, typography and press feedback with approved V2 onboarding screens.

- [ ] **Step 2: Run visual and Today tests**

Expected: PASS.

- [ ] **Step 3: Commit**

---

### Task 6: Resume VS-16 feature implementation

**Files:**
- Continue existing VS-16 plan and files already listed in `docs/superpowers/plans/2026-08-09-vs-16-business-discovery.md`.

**Interfaces:**
- Consumes: completed visual retrofit and current business-discovery backend work.
- Produces: URL-first discovery/confirmation flow using the same approved visual language.

- [ ] **Step 1: Return to the existing VS-16 TDD plan**
- [ ] **Step 2: Complete persistence/backend GREEN**
- [ ] **Step 3: Implement mobile discovery/confirmation against real facts only**
- [ ] **Step 4: Run runtime/accessibility/narrow-screen evidence**
- [ ] **Step 5: Run exact-head CI, Security and Product Intake**
- [ ] **Step 6: Certify and merge only if all gates pass**
