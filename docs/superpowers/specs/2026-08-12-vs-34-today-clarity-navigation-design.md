# VS-34 — Today Clarity & Navigation Consistency

## Status

Design direction approved in chat from direct Expo harness review. This document is the written Superpowers design gate. No runtime implementation is authorized until the Product Owner reviews and approves this committed spec.

## Problem

The current Today screen is structurally simpler than the previous Today’s Focus experience, but it still reads like an explanatory analytics card rather than an obvious daily task. The recommendation title competes typographically with editorial headings, Impact/Effort/Confidence occupy too much visual space, and the separate reasoning card duplicates the `Why?` path.

The pushed-screen back control is also visually inconsistent: screens such as Settings show a black pill containing `Profile`, while the rest of Atlas uses a light white/green visual language.

## Goal

Make Today understandable in a few seconds:

1. What should I do?
2. Why is Atlas suggesting it?
3. What can I do next?

The primary recommendation must visually read as a task/action, not as another editorial heading or report.

## Chosen approach

Use a **single-task action surface** rather than a dashboard or multi-card feed.

Alternatives considered:

- **Dense decision card:** keeps all current metadata visible, but remains cognitively heavy.
- **Single-task action surface — chosen:** one recommendation, one short reason, compact evidence strength, and one dominant action.
- **Conversational feed:** feels friendly but makes deterministic state, scanning and accessibility less predictable.

The chosen approach preserves Atlas as an action-first Decision Intelligence product and leaves deeper reasoning on Opportunity Detail.

## Today information hierarchy

### Header

- Page title: `Today` using the established Atlas editorial/serif page-title treatment.
- Supporting line: `1 thing worth doing today` when a ready recommendation exists.
- Freshness remains visible but quiet: `Updated just now`.
- Native pull-to-refresh remains available.

### Primary recommendation

The recommendation is the only dominant card above the fold.

Order:

1. Small semantic label: `BEST MOVE`.
2. Recommendation/task title.
3. One concise reason sentence.
4. One compact evidence-strength row.
5. Primary and secondary actions.

The recommendation title must use the app’s strong **sans-serif action/task typography**, not the serif editorial heading family. This creates a stable semantic distinction:

- Serif = page/place headings.
- Sans serif = tasks, recommendations, actions, metrics and controls.

### Evidence-strength row

Replace three large Impact/Effort/Confidence mini-cards with one compact readable row such as:

`High impact · Low effort · High confidence`

Use the server-provided values only. Do not synthesize percentages or business metrics that are not in the contract.

### Actions

Visible by default:

- Primary: `I’ll do this` — maps to existing Apply semantics.
- Secondary: `Why this?` — opens Opportunity Detail.
- Overflow (`…`): `Later`, `Not relevant`.

Common actions remain within one or two taps. No new Action Decision state is introduced.

### Progressive disclosure

Remove the separate `Want the reasoning?` supporting card from Today. It duplicates `Why this?`.

Opportunity Detail remains the source for:

- Why now;
- evidence;
- assumptions;
- limitations;
- expiry;
- Knowledge Pack metadata;
- detailed confidence context.

## Visual treatment

- Default page surface remains white.
- Best Move may use a very light mint tint or a white card with a mint accent; it must not become a large decorative color block.
- Blue is reserved for neutral informational content.
- Amber is reserved for attention states.
- Lavender is reserved for Atlas interpretation when such a supporting state exists.
- Red is reserved for genuine error/risk states.
- Do not add decorative cards simply to show colors.

The first viewport should feel calm and sparse.

## Ready-state target composition

Conceptually:

`Today`

`1 thing worth doing today`

`Updated just now`

`BEST MOVE`

`<clear task title>`

`<one concise reason>`

`High impact · Low effort · High confidence`

`I’ll do this`  `Why this?`  `…`

No additional reasoning card appears underneath.

## Non-ready states

Loading, missing business, missing goal, insufficient context, no-focus, degraded and network-error states keep the concise VS-33 recovery behavior.

Changes are limited to improving hierarchy/copy only where needed for consistency. Safety rules remain unchanged:

- no filler recommendation;
- no fabricated data;
- stale refresh results are not labelled current;
- owner confirmation remains authoritative;
- recovery routes remain explicit.

## Navigation consistency

Replace the black `Profile` back pill on pushed screens with a native/light Atlas back treatment:

- back chevron plus `Profile` where a text label is useful;
- transparent/white surface rather than black fill;
- Atlas green text/icon treatment;
- minimum accessible target size preserved;
- use the same pushed-screen treatment consistently for Settings, Context, Feedback and similar Profile-owned detail screens.

This is a visual/navigation consistency correction only. Route topology remains Today / History / Goals / Profile.

## Architecture and scope

This slice is mobile presentation/interaction only.

Expected runtime areas:

- `apps/mobile/src/features/today-focus/**`;
- pushed-screen/native stack header configuration needed to remove the black Profile pill;
- focused mobile tests for Today hierarchy and navigation consistency.

No changes are intended to:

- Opportunity generation/eligibility;
- API contracts;
- database schema or migrations;
- Knowledge Packs;
- connectors;
- Business Profile data;
- menu/media enrichment;
- production infrastructure;
- release workflows.

If the black pill is owned by a shared native routing primitive, the implementation may change that primitive only to the minimum extent necessary to produce the approved pushed-screen appearance. Unrelated navigation refactoring is out of scope.

## Cross-industry rule

Today remains industry-neutral. Recommendation presentation must not include restaurant-specific terminology or assumptions in Atlas Core. The task text continues to come from the evidence-qualified Opportunity generated for the active business.

## Accessibility

- Preserve Dynamic Type behavior.
- Do not encode meaning by color alone.
- Maintain accessible role/labels for all action controls.
- Overflow actions must be keyboard/screen-reader reachable on supported platforms.
- Maintain minimum touch-target sizing.
- Preserve Reduce Motion behavior and existing native press-feedback policy.

## Testing

Implementation must use TDD and include:

- structural tests proving one dominant Best Move task and no duplicate reasoning card;
- tests proving recommendation/task typography is separated from page-title typography;
- tests proving Impact/Effort/Confidence are compact rather than three competing cards;
- action-semantic tests for Apply/Why/Later/Not relevant;
- navigation tests proving pushed Profile-owned detail screens no longer render the black Profile pill;
- existing Today state regression tests;
- authentic Expo Web Today → Opportunity Detail runtime coverage;
- full mobile validation, CI, Security baseline and Product Intake on the exact candidate SHA.

## Explicit non-goals

- Menu/image recovery;
- Wolt/Bolt enrichment expansion;
- Google photo integration;
- Business Pulse;
- What Changed;
- new metrics/connectors;
- Ask Atlas;
- Opportunity queue;
- API/database changes;
- production deployment;
- EAS build/submit/OTA.

## Sequencing

1. Complete and merge VS-34 Today Clarity & Navigation Consistency.
2. Re-point the Expo test harness to the merged SHA for visual verification when authorized.
3. Then start the separate Menu & Media Coverage Recovery design/slice.
4. Pilot Operations PR #57 remains preserved and paused until explicitly resumed by the Product Owner.
