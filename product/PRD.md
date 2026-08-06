---
title: Atlas Product Requirements Document
document_id: ATLAS-PRD-001
version: 1.0
status: Approved
owner: Product
last_updated: 2026-08-06
source: Innovation-Lab/ventures/atlas/artifacts/PRD.md
---

# Atlas Product Requirements Document v1.0

## Product authority

This document is the approved product authority for Atlas v1.0. Stable functional requirements are `FR-01` through `FR-18`. PES may decompose them into numbered vertical slices but may not expand scope without an approved product decision.

## Product definition

Atlas is a mobile-first, industry-agnostic Business Intelligence platform for owner-operated small and medium-sized businesses.

Atlas helps an owner identify the strongest practical business action available now, understand why it matters, execute it with less effort, record what happened and improve future guidance.

> Know your next best business move—and act on it.

Atlas is not a POS, ERP, CRM, accounting system, generic chatbot, social scheduler or dashboard-first analytics product. It is an evidence-aware Decision Intelligence layer above tools a Business already uses.

## Primary customer and problem

The primary customer is an owner-operated or owner-led SMB with no dedicated BI or strategy team, limited time, fragmented information and a preference for actions over reports.

The core problem is failure to convert information into prioritised, explainable and executable action.

Primary job to be done:

> When I begin or review my business day, I want Atlas to identify the most valuable action I can take, explain why it matters and prepare what I need, so I can improve my Business without analysing multiple systems.

## Product principles

1. Action before information.
2. Customer outcomes before feature count.
3. Quality before recommendation volume.
4. Explainability before automation.
5. Owner authority before autonomous action.
6. Simplicity before completeness.
7. Evidence before confident language.
8. Mobile-first use.
9. Minimal setup and progressive profiling.
10. Generic core, modular Knowledge Packs.
11. Better decisions, not longer sessions.
12. Learn from acceptance, rejection, completion and outcomes.
13. Never create filler recommendations.
14. Never guarantee business results.
15. Never make the owner manage Atlas more than the Business.

## Core product loop

```text
Business Goal
  -> Business Context
  -> Signals + Memory + Knowledge Pack
  -> Growth Opportunity
  -> Prioritisation
  -> Today’s Focus
  -> Explanation
  -> Execution Kit
  -> Owner Action
  -> Outcome
  -> Business Learning
  -> Better Future Intelligence
```

## Knowledge Pack model

Atlas Core remains industry-agnostic. Domain expertise is provided by immutable, versioned Knowledge Packs containing terminology, KPIs, seasonal patterns, risks, workflows, opportunity patterns, evidence rules, prompts, execution templates, outcome models and prohibited guidance.

The MVP uses the **Generic Business Knowledge Pack**. The core model must not hardcode a single industry.

## Opportunity eligibility

An Opportunity may be shown only when it supports a material Business Goal, is actionable, contains explainable evidence, includes a reason and why-now explanation, has assessed confidence and effort, avoids unsupported guarantees, is not materially duplicated, has not expired and complies with safety, privacy and owner-control policy.

Atlas shows one primary Today’s Focus. If no candidate qualifies, Atlas must say so honestly.

## Functional requirements

### FR-01 — Identity and access
The owner can register, sign in, sign out, recover access, maintain a secure session, request data export and delete the Account. MVP supports one primary owner per Business and restricted internal pilot roles.

### FR-02 — Business creation
The owner can create a Business with name, category, country, timezone, primary location, currency, contact details and operating status.

### FR-03 — Business Profile
The owner can review and update identity, category, description, address, locations, hours, website, social and business channels, goals, language, timezone, currency and preferences. Publicly sourced data must be labelled and owner-confirmed.

### FR-04 — Business Goals
The owner can select, prioritise and update generic and custom goals, including revenue, profitability, acquisition, retention, reputation, reduced waste, saved time and operational consistency.

### FR-05 — Business Context
Atlas collects only context needed to improve intelligence. MVP context may include confirmed profile, goals, prior Opportunities, decisions, feedback, outcomes, public data, enabled sources and active Knowledge Packs. The product must remain useful with limited data.

### FR-06 — Knowledge Pack support
Each Business has at least one active Knowledge Pack. MVP requires the Generic Business Knowledge Pack. Pack key and exact version are retained with historical Opportunities.

### FR-07 — Today’s Focus
The home experience presents the primary current Opportunity with concise action title, why it matters, why now, Expected Impact, effort, Confidence, Evidence summary, timing or expiry, route to the Execution Kit and Apply, Skip and Not Relevant actions. The owner should understand it within ten seconds.

### FR-08 — Opportunity Detail
The owner can inspect goal alignment, Evidence, Reason, Why Now, Confidence, Expected Impact, effort, assumptions, expiry, source categories, limitations, Action and Execution Kit. Evidence and Atlas interpretation remain distinct.

### FR-09 — Execution Kit
Supported MVP assets include checklists, review responses, business updates, social captions, offer wording, message templates, campaign briefs, operational checklists and measurement suggestions. The owner can review, edit where relevant, copy, mark used, rate usefulness and complete the Action. Autonomous publishing is excluded.

### FR-10 — Action decisions
The owner can record Applied, Completed, Skipped, Not Relevant or Rejected, with structured reasons where relevant.

### FR-11 — Outcome capture
Atlas supports completion, usefulness, owner-reported result, time spent, optional notes, measurable results and follow-up date. Outcomes are classified as measured, owner-reported, estimated or unknown.

### FR-12 — History
The owner can view chronological Opportunities, Actions, statuses, Execution Kits, feedback, outcomes, expiry and learning summaries, filtered by status, category, goal and date.

### FR-13 — Weekly Review
Atlas produces a narrative-first review of Opportunities shown, Actions applied and completed, skips, feedback, available outcomes, what Atlas learned and the next focus. It must not claim causation without evidence.

### FR-14 — Business Memory
Atlas retains structured, relevant Business Memory covering profile, goals, active pack, Opportunity and Action history, rejection reasons, preferences, outcomes and learning summaries. Memory is transparent, exportable where applicable, deletable and limited to product relevance.

### FR-15 — Notifications
Opt-in notifications may cover a daily briefing, time-sensitive Opportunity, expiry, outcome follow-up and Weekly Review, with quiet hours, timezone correctness, consent, frequency controls, duplicate prevention and deep links.

### FR-16 — Empty and degraded states
Atlas supports no suitable Opportunity, insufficient context, missing confirmation, AI or source unavailability, expiry, offline mode, network failure, unsupported pack, first use, empty History and unknown Outcome. Every state explains the next useful action.

### FR-17 — Feedback and support
The owner can rate an Opportunity, report incorrect context or unsafe guidance, submit feedback and request support.

### FR-18 — Pilot operations
Authorised internal operators may inspect pilot Businesses, generation failures and quality indicators; assist profile correction; manually or collaboratively prepare Opportunities; record provenance and support notes; and withdraw unsafe content. This is a minimal internal capability, not a full admin platform.

## Hero journey

1. Create Account.
2. Create Business.
3. Confirm Business Profile.
4. Select goals.
5. Establish minimum context.
6. Receive first Today’s Focus.
7. Inspect evidence and reasoning.
8. Use the Execution Kit.
9. Record status.
10. Capture outcome.
11. Atlas updates Business Memory.
12. Review progress through History and Weekly Review.

## AI behaviour

Atlas may use AI for context summarisation, candidate generation, explanations, Execution Kits, review summaries, outcome interpretation and Knowledge Pack application.

Atlas must remain provider-neutral, use versioned prompts, validate structured outputs, communicate uncertainty, avoid fabricated evidence and guaranteed outcomes, distinguish facts from interpretation, require owner review before external action, degrade safely and record cost and quality metadata.

OpenRouter may be used as an initial customer-inference provider adapter, but free routing is development/pilot-only. Production requires an approved model allowlist, budgets, quality evaluation and safe fallback. Atlas application policy—not model prose—controls Confidence and eligibility.

## Success metrics

Product metrics include onboarding completion, time to first value, Today’s Focus and Opportunity Detail views, Execution Kit use, Apply/completion rates, rejection reasons, usefulness, Weekly Review views, weekly active Businesses and retention.

Initial pilot hypotheses:

- at least 20 active pilot Businesses;
- at least 70% weekly active during structured pilot;
- at least 50% of shown Opportunities rated useful or acted upon;
- at least 60% of retained pilot Businesses willing to continue at a recurring price;
- no unresolved critical trust, privacy or safety failures.

## Business model

Recurring subscription per Business. Initial hypotheses: Founder pilot €49/month, Core €79/month and Growth €129/month. Pricing remains unproven until paid willingness-to-pay evidence exists.

## Privacy, security and accessibility

Atlas applies data minimisation, transparent collection, consent where required, encryption, Business-level isolation, deletion, export, retention, auditability and owner approval before public actions. The MVP avoids storing end-customer personal data unless strictly necessary.

Core screens support screen readers, dynamic text, adequate contrast, minimum touch targets, reduced motion, meaningful labels, non-colour status communication and accessible errors.

## Explicitly out of scope

- POS, ERP, accounting, payroll or full CRM;
- inventory, scheduling, reservation or loyalty platforms;
- marketplace or agency white label;
- enterprise role hierarchy;
- unrestricted chatbot;
- image and video generation;
- autonomous advertising, publishing or financial action;
- complex dashboard-first BI;
- deep POS integrations;
- real-time competitor scraping;
- guaranteed revenue claims;
- fully automated ROI attribution.

## Approved slice mapping

- VS-01 — Identity and Business Setup;
- VS-02 — Business Profile and Context;
- VS-03 — Knowledge Pack Foundation;
- VS-04 — Today’s Focus;
- VS-05 — Opportunity Detail;
- VS-06 — Execution Kit;
- VS-07 — Action Status and Feedback;
- VS-08 — Outcome Capture and Business Memory;
- VS-09 — History;
- VS-10 — Weekly Review;
- VS-11 — Notifications;
- VS-12 — Pilot Operations.

## Final product decision

Atlas v1.0 will prove whether owner-operated Businesses value a product that understands goals and context, identifies the strongest practical next action, explains why it matters, prepares execution, captures results and learns over time. No major product expansion is permitted before pilot evidence.
