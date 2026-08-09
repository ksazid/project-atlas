# VS-13 Business Profile Visual Migration — Design

**Status:** Approved for implementation planning

**Date:** 2026-08-09

**Product owner:** ksazid

**Parent capability:** VS-02 Business Profile and Context / FR-03

## Context

Atlas is continuing the visual language established by the approved Welcome → Sign In → Discover → Confirm flow. VS-13 applies that language to the existing Business/Profile screen without changing its product behavior or the PES Mobile architecture.

The Starbucks getdesign system is the primary visual reference for palette, warm surfaces, typography hierarchy, spacing, card geometry, forms, buttons, depth, and premium service tone. This is a visual-system migration, not adoption of Starbucks identity. The currently tested Starbucks mark is temporary and must be replaceable in one place.

## Approved decisions

1. Implement Profile, Goals, and Context as separate sequential PES slices. VS-13 covers Profile only; VS-14 and VS-15 will receive their own specifications and gates.
2. Add one shared `BrandMark` component as the only application-level rendering boundary for the temporary test mark. Screens may render `BrandMark`; they must not embed a Starbucks logo URL, asset, trademark label, or duplicated logo implementation.
3. The eventual Atlas brand asset is deliberately deferred. Replacing it must require changing only the `BrandMark` implementation or its supplied asset, not individual screens.
4. Use Starbucks visual grammar but no Starbucks business identity, proprietary retail content, fabricated Starbucks data, or production trademark claim.
5. Preserve current Expo Router navigation, authentication/session handling, state ownership, API client boundaries, Business isolation, and profile persistence contract.

## Scope

VS-13 will:

- migrate the Business/Profile screen to the approved warm-neutral and deep-green Atlas presentation;
- retain all existing `BusinessProfile` fields and their saved values;
- group fields into understandable sections while preserving the existing payload shape;
- clearly label publicly sourced information and require owner confirmation before saving it as trusted context;
- provide deterministic loading, ready, saving, success, validation, missing-session, and recoverable error behavior;
- make keyboard, accessibility, touch-target, and small-screen behavior explicit;
- introduce the shared replaceable `BrandMark` boundary and replace every current screen-level test-logo reference with that component;
- add focused tests and update existing mobile regression coverage;
- update Atlas design-authority and PES decision records to reflect the approved visual-system/brand-boundary decision.

## Non-goals

VS-13 will not:

- change API endpoints, DTOs, domain rules, migrations, authentication, or navigation;
- add profile fields that the current `BusinessProfile` contract cannot persist;
- redesign Goals, Context, Today’s Focus, or other screens;
- create the final Atlas logo or brand identity;
- deploy, publish an OTA update, enable production, or perform an EAS release;
- fabricate public facts or infer private operational data.

## Architecture and component boundaries

### Route screen

`apps/mobile/app/(tabs)/profile.tsx` remains the Expo Router entry point. It owns the form draft and screen states, loads the session, calls the existing `getProfile` and `saveProfile` functions, and maps the result back into the current `BusinessProfile` model.

The screen may be decomposed into local presentation units when that improves clarity, but no new global state or navigation layer will be introduced.

### BrandMark

A reusable `BrandMark` presentation component will own the temporary mark. Its public contract will be limited to presentation needs such as size and accessibility labeling. It will not know about business discovery, authentication, or screen navigation.

The component is the swap boundary for the future Atlas identity:

- callers do not import or reference a Starbucks asset;
- the temporary implementation is visibly treated as prototype-only in code and documentation;
- accessibility language describes Atlas branding, not Starbucks;
- later replacement does not alter screen layout contracts.

As a bounded support change, existing Welcome, Sign In, Confirm, and Today’s Focus mark renderings will switch to this component without changing their composition or workflows. This consolidation is part of VS-13 because leaving any screen-level copy would defeat the approved one-change replacement boundary.

### Visual tokens

Reusable colors and spacing should come from the repository’s existing design-system conventions where available. Any new shared tokens must be narrowly scoped and compatible with the approved Atlas onboarding flow:

- deep green for primary actions and identity anchors;
- warm neutral canvas and ceramic section washes;
- white cards with restrained borders and soft depth;
- disciplined 4/8/16/24/32 spacing rhythm;
- clear sans-serif hierarchy and readable secondary contrast;
- minimum 44-point interactive targets;
- subtle pressed states only.

The previous generic light-green visual treatment must not be mixed into the migrated screen.

## Screen design

### Header and orientation

The Profile screen opens with a compact brand/header area, a clear “Business profile” eyebrow, and a benefit-led heading that explains why confirmed facts improve Atlas. The layout must remain useful without decorative imagery.

### Provenance and confirmation

When `source === 'public'`, a dedicated provenance card explains that the details were discovered from a public page. The owner-confirmation control remains explicit and blocks saving until confirmed. Owner-provided profiles do not show unnecessary confirmation friction.

### Form organization

The existing fields are grouped without changing their keys or payload:

- **About:** description and language;
- **Contact and presence:** website, phone, email, and social channels;
- **Location and hours:** address and business hours.

Every field has a visible label, an accessible name, suitable keyboard/content hints where React Native supports them, and adequate height. Multiline fields grow without obscuring surrounding controls.

### Primary action

“Save business profile” is the single primary action. It is disabled while saving and whenever a publicly sourced profile still lacks owner confirmation. Saving shows in-button progress, prevents duplicate submission, and retains the current draft if the request fails.

## Data flow and state model

1. On mount, load the existing authenticated session.
2. If no Business session exists, show a recoverable missing-session state; do not send an API request.
3. If a Business exists, load its profile with the existing authenticated API client.
4. Populate the form with the returned profile, or the current empty profile defaults when none exists.
5. Allow edits locally without background writes.
6. On save, validate the client-side conditions, call the existing `saveProfile`, replace the draft with the returned representation, and announce success.
7. On failure, retain edits and show a clear, polite live-region message with a retry path.

The screen must never log or expose access tokens, cross-Business identifiers, or profile values outside the existing API boundary.

## Validation and failure behavior

- Publicly sourced data cannot be saved as confirmed context until the owner explicitly confirms it.
- Website and email inputs receive appropriate keyboards; server validation remains authoritative.
- Whitespace-only optional values may be sent according to the existing contract; this slice does not redefine domain validation.
- Initial-load failure shows a recoverable error state with retry.
- Save failure keeps the populated form on screen and re-enables the primary action.
- Success and error text use `accessibilityLiveRegion="polite"`.
- Busy states expose an understandable label and do not create an empty white screen.

## Accessibility and responsive behavior

- Maintain logical screen-reader order from heading through form and action.
- Use semantic header, button, and confirmation roles.
- Provide visible focus/pressed/disabled differences without relying on color alone.
- Keep interactive targets at least 44 points high.
- Support narrow phone widths, text scaling, multiline labels, and keyboard-safe scrolling.
- Avoid fixed widths that clip localized copy.
- Preserve adequate foreground/background contrast across warm and green surfaces.

## Motion

Motion is optional and bounded to tactile pressed feedback or a short state transition. It must respect reduced-motion expectations and must not change the Starbucks-derived Atlas visual language or delay task completion.

## Testing and verification

Implementation follows PES/Loop with Superpowers inside the loop.

### Focused deterministic tests

- loading an existing profile populates all supported fields;
- no existing profile uses safe defaults;
- missing session does not call the profile API;
- publicly sourced data requires owner confirmation before save;
- save success uses the returned profile and announces completion;
- load/save failures present recoverable states while preserving edits;
- the shared brand boundary is used instead of screen-level Starbucks logo references;
- field labels, roles, disabled states, and live regions remain accessible.

Tests should use behavior-level React Native coverage where supported by the current repository and retain a small static regression check only where runtime component testing is not available.

### Required gates

Run repository-defined formatting/lint/type checks, unit and relevant integration/mobile tests, accessibility/responsive checks, governance validation, slice validation, preflight, and the configured CI/Security/Product Intake gates. Migration validation is required only if a migration unexpectedly becomes necessary; the approved design does not call for one.

### Visual verification

Render the actual Expo application when runtime tooling permits. Capture and inspect the real Profile screen at a representative phone width and a small-screen width, covering ready, public-confirmation, loading, and error/disabled states. Compare spacing, hierarchy, typography, forms, cards, buttons, keyboard behavior, and navigation continuity against the approved onboarding reference.

If actual runtime rendering is unavailable, record that limitation explicitly and do not substitute a hand-created image as an application screenshot.

## PES lifecycle and completion

Before implementation, activate VS-13 with recorded scope and implementation approvals, required decision linkage, allowed paths, and required gates. Progress through the repository-defined lifecycle only when each transition is valid.

VS-13 is complete only when the exact commit passes all applicable local gates, CI, Security, Product Intake, governance/preflight, review, and certification requirements. Merge remains human-approved and no production deployment is authorized.

## Follow-on slices

After VS-13 is certified and merged, repeat the same governed design/specification/plan/implementation cycle for VS-14 Goals and then VS-15 Context. Shared visual primitives and `BrandMark` may be reused, but each slice must preserve its existing behavior and pass its own exact-SHA gates.
