# Atlas V2 Visual Consistency Retrofit Design

## Status
Approved by Product Owner through the instruction to continue work and update the pages created after the four approved onboarding pages.

## Purpose
Bring the post-onboarding Atlas screens into the same visual family as the four approved V2 onboarding screens without changing their functional behavior, API contracts, information architecture, navigation, persistence, or authorization.

## Visual authority
1. `product/DESIGN.md` remains the repository design contract.
2. The approved V2 onboarding screens remain the implementation comparison baseline.
3. Starbucks getdesign (`npx getdesign@latest add starbucks`) is the primary visual reference: four-tier greens, warm neutral surfaces, restrained depth, clear hierarchy, reusable controls, and consistent spacing.
4. Emil Kowalski's design-engineering guidance is secondary and may improve motion, transition, press feedback, perceived responsiveness, and reduced-motion behavior only. It cannot override the primary visual grammar.
5. Atlas keeps its own identity. `BrandMark` remains the only temporary brandmark boundary; no Starbucks labels, URLs, business facts, or copied artwork may appear in production UI.

## Frozen screens
The first four approved onboarding surfaces are the comparison baseline and are not redesigned in this retrofit:
- Welcome
- Sign In
- Business discovery
- Business confirmation

## Screens to retrofit
- Today / Home (`TodayFocusScreen`)
- Business / Profile
- Goals
- Context

## Design rules
- Match the approved onboarding screens' white/warm-neutral canvas treatment, green hierarchy, compact rounded controls, restrained borders and shadows, and editorial heading treatment.
- Preserve the existing single-primary-action hierarchy on Today.
- Use consistent horizontal gutters and top spacing across tab screens.
- Use shared Atlas tokens where they already represent the approved grammar; screen-specific values may be used only where required to match the approved baseline.
- Buttons must provide immediate subtle press feedback. No decorative or slow motion is introduced.
- Do not animate frequently repeated navigation or data-entry interactions.
- Respect reduced-motion settings for any future motion additions.
- Preserve minimum 44-point touch targets and existing accessibility semantics.
- Preserve loading, empty, error, retry, stale, confirmation and save states.

## Functional invariants
The retrofit must not change:
- API endpoints or request/response models;
- database entities or migrations;
- authentication/session routing;
- tab/navigation destinations;
- goal ordering semantics;
- context validation/provenance semantics;
- profile confirmation semantics;
- Today opportunity decision semantics.

## Acceptance
A screen passes when:
- it reads as a natural continuation of the approved onboarding screens;
- no generic light-green/dashboard visual language reappears;
- there is no third-party branding leakage;
- existing functional tests continue to pass;
- accessibility targets and state coverage remain intact;
- runtime evidence can be captured at phone and narrow widths without clipping or hidden primary actions.
