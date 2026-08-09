---
title: Atlas Design Baseline
document_id: ATLAS-DESIGN-001
version: 1.2
status: Approved
owner: Product Design
last_updated: 2026-08-09
depends_on:
  - ATLAS-PRD-001
source: Innovation-Lab/ventures/atlas/artifacts/DESIGN.md
---

# Atlas Design Baseline v1.2

## Frozen direction

Atlas uses a calm, action-first, mobile-first product language. Its primary visual grammar is the Starbucks getdesign palette, warm neutrals, hierarchy, spacing, cards, forms, buttons and depth. Atlas retains its own brand identity: the current test mark is prototype-only and rendered solely by `BrandMark`.

The experience remains calm and low-cognitive-load, with polished progressive detail, disciplined native navigation, and purposeful feedback. These are product experience constraints, not visual-authority sources or prescribed screen treatments. Secondary skills may improve usability, accessibility, polish and motion only; they cannot override the primary visual grammar. Atlas must retain its own identity and must not imitate or reproduce third-party artwork, branding or layouts.

## Design objective

Atlas should help a busy business owner think clearly and act confidently. It must feel calm, focused, credible, practical, supportive and transparent—not like a dense analytics dashboard, alarm system, generic chatbot or generic SaaS admin surface.

> Atlas starts with the strongest practical action, not with a wall of information.

## Non-negotiable experience rules

1. The home screen is a decision screen, not a dashboard.
2. Show one primary Today’s Focus.
3. Evidence and metrics use progressive disclosure.
4. Do not use KPI walls or competing primary actions.
5. Confidence communicates evidence strength, never certainty.
6. Expected Impact remains distinct from measured Outcome.
7. Motion adds clarity and never delays content.
8. Accessibility and degraded states are part of the primary design, not follow-up polish.

## Experience hierarchy

1. What should I do?
2. Why does it matter?
3. Why now?
4. What impact could it have?
5. How much effort will it require?
6. What evidence supports it?
7. What do I need to execute it?
8. What happened afterward?

## Primary navigation

Four persistent destinations:

- Today
- History
- Goals
- Profile

Weekly Review is reached through Today or History.

## Core screens

### Authentication
One dominant action, clear recovery and clear privacy language.

### Business setup
Progressive, resumable and minimum-fields-first. Publicly discovered data requires owner confirmation.

### Goals
Generic cross-industry goals with clear priority and custom-goal support.

### Today
One primary Today’s Focus with action, reason, Expected Impact, effort, Confidence and a clear route to execution. No dashboard-first composition.

### Opportunity Detail
Show Evidence, Why Now, assumptions, limitations, goal alignment, expiry and Execution Kit. Evidence and Atlas interpretation are visually distinct.

### Execution Kit
Allow review, editing where permitted, copying, checklist use and completion. Avoid unnecessary gamification.

### Feedback and Outcome
Use compact status/reason choices, optional notes and measurable results. Never turn this into a long survey.

### History
Chronological, action-oriented and filterable by status, goal, category and date.

### Weekly Review
Narrative-first: what happened, what remains uncertain, what Atlas learned and what matters next.

### Profile and preferences
Business Profile, goals, language, timezone, currency, notifications, privacy, export and deletion.

## Component baseline

Required components include app shell, top bar, bottom navigation, primary/secondary/destructive buttons, text and selection fields, goal selector, Opportunity card, Evidence item, Confidence/Impact/Effort/status indicators, Execution Asset, checklist item, feedback sheet, loading skeleton, empty/error panels, stale/offline banner, confirmation dialog and inline/toast confirmation.

All components use shared design tokens.

## Visual foundation

- Semantic colour roles only; no one-off page colours.
- Colour is never the only status signal.
- Readable cross-platform sans-serif typography.
- Dynamic type support.
- Consistent spacing scale.
- Restrained radii, elevation and card grouping.
- One icon family with labels for ambiguity.
- Generous whitespace without reducing information clarity.
- Premium polish without decorative clutter.

Do not use screen-level Starbucks URLs or labels, fabricated business data, copied retail identity, or reintroduce the prior generic light-green theme. Final brand colours may evolve through an approved design decision without changing the experience hierarchy.

## Confidence, impact and effort

- Confidence: Low, Medium, High.
- Impact: Low, Moderate, High.
- Effort: Quick, Moderate, Significant.
- Avoid fake numerical precision.
- Expected Impact must remain visually distinct from measured Outcome.

## Loading, empty, error and offline states

Prefer skeletons for known structure, preserve safe cached content during refresh, explain long-running generation, avoid indefinite spinners and provide safe retry.

Cover first use, no suitable Opportunity, insufficient context, no History, no Weekly Review, unknown Outcome and unsupported Knowledge Pack. Every empty state explains why and gives the next useful action.

Preserve owner input where possible. Distinguish offline, timeout, validation, authorization, stale version and service unavailability. Never expose stack or provider details.

Cached content may remain visible when stale and must be labelled. Never claim a mutation succeeded before server confirmation.

## Accessibility

All MVP screens support screen readers, logical focus order, dynamic type, sufficient contrast, approximate 44×44-point targets, reduced motion, non-colour status communication, accessible validation/errors, semantic headings and plain language.

Accessibility defects in the hero workflow block slice completion.

## Motion and haptics

Use short purposeful motion for navigation, state change, completion, loading, expansion and list-detail relationships. Respect reduced-motion settings. Haptics confirm meaningful user actions only; they must not create noise or substitute for visual/accessibility feedback.

## Content and tone

Copy is direct, respectful, concise, evidence-aware, non-judgmental and honest about uncertainty. Avoid hype, fear, technical AI terminology, guaranteed-growth claims, excessive urgency and blaming the owner.

## Privacy and trust cues

Clearly identify what the owner provided, what came from public or connected sources, what Atlas generated, when an action affects an external system, when approval is required and how data can be corrected, exported or deleted.

## Design gate

A slice fails design review when the primary action is unclear, analytics dominate, Evidence and interpretation are mixed, required states are missing, accessibility is incomplete, Confidence implies certainty, competing primary actions exist, third-party references are copied rather than interpreted, or industry assumptions leak into Atlas Core.
