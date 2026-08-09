# Atlas Visual Language — Locked Source of Truth

Status: **APPROVED / LOCKED**

Selected reference: `https://chatgpt.com/s/m_6a77ea16d68c8191b66c05c93a2a0679`

This visual language is the source of truth for Atlas. Future screens must preserve its soul rather than introducing a new theme per slice.

## Core character

Atlas should feel intelligent, premium, calm, modern, focused, and easy to use. It must not look like a generic enterprise form, admin template, or conventional dashboard.

## Visual DNA

- Light neutral application canvas (`#F5F6FA`) with strong whitespace.
- Dark intelligence surfaces (`#11131A` / `#171922`) used deliberately for AI analysis, focus, signal, and premium moments.
- Purple intelligence accent (`#6D28D9`) with soft lavender support (`#EEEAFD`, `#C4B5FD`, `#A78BFA`).
- White elevated cards with subtle borders rather than heavy shadows.
- Large, confident headings with tight tracking and strong hierarchy.
- Rounded geometry: ~14px controls, 18–24px cards/actions, 28–30px hero/intelligence surfaces.
- Compact icon-led status and metadata instead of verbose labels.
- Progress must feel like a connected journey rather than isolated pages.
- Green is reserved for verified/success/outcome states, not as a competing brand accent.

## Interaction DNA

- Directional screen flow: Welcome → Sign In → Find Business → Discover → Confirm → Atlas.
- Press states use subtle scale/opacity feedback.
- Discovery/analysis uses restrained pulse/scanning motion.
- Information should reveal progressively instead of dumping full forms.
- Confirmation should feel rewarding and confident.
- Motion should communicate progress, hierarchy, state change, or intelligence; never decorative noise.
- Respect reduced-motion accessibility when system-level support is introduced.

## Iconography

Use simple, consistent intelligence/status symbols in compact rounded containers. Icons should support comprehension: insight, growth, location, category, confirmation, source, measurement, and action. Avoid decorative icon overload.

## Product rules

1. One primary action per screen.
2. Ask only what Atlas cannot infer reliably.
3. Prefer discovery + confirmation over manual entry.
4. Use dark intelligence surfaces for AI reasoning/analysis moments, not every card.
5. Preserve the purple intelligence accent throughout customer-facing Atlas UI.
6. New category, dashboard, opportunity, execution, outcome, review, settings, and notification screens must extend this language.
7. Do not replace this visual identity with generic UI skill output. Design skills improve hierarchy, accessibility, motion, responsiveness, and polish while this document remains authoritative.
8. PES Mobile architecture and business logic remain structurally independent of this visual language.

## Current reference implementation

- `apps/mobile/app/welcome.tsx`
- `apps/mobile/app/sign-in.tsx`
- `apps/mobile/app/create-business.tsx`

These screens establish the initial implementation baseline. New screens should reuse their proportions, surfaces, typography hierarchy, accent semantics, and interaction character rather than copying layouts mechanically.
