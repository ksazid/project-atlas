---
title: Atlas Technical Requirements Document
document_id: ATLAS-TRD-001
version: 1.0
status: Approved
owner: Architecture and Engineering
last_updated: 2026-08-06
depends_on:
  - ATLAS-PRD-001
source: Innovation-Lab/ventures/atlas/artifacts/TRD.md
---

# Atlas Technical Requirements Document v1.0

## Technical authority

This is the approved technical authority for Atlas v1.0. PES may decompose the architecture into numbered vertical slices but may not replace approved technical policy without an approved ADR.

## Engineering objectives

Atlas optimises for fast pilot delivery, clear module boundaries, low fixed cost, replaceable providers, explainable and auditable intelligence, Business-level isolation, mobile reliability, safe degradation, end-to-end slices and future Knowledge Pack expansion.

## Architecture

Atlas uses a mobile-first modular monolith with vertical-slice delivery.

```text
Expo React Native App
       |
       | HTTPS / JSON
       v
ASP.NET Core API + background processing
       |
       +-- PostgreSQL
       +-- AI provider adapters
       +-- identity adapter
       +-- notification adapter
       +-- optional public-data adapters
```

MVP excludes microservices, Kubernetes, a distributed event bus, mandatory Redis and a data warehouse.

## Technology direction

### Mobile
- Expo React Native;
- TypeScript;
- Expo Router;
- server-state query library;
- secure device storage for credentials and tokens;
- safe local cache for read-only degraded access;
- Expo notifications and updates where approved.

### Backend
- ASP.NET Core and C#;
- modular monolith;
- vertical-slice request handling;
- EF Core;
- PostgreSQL;
- OpenAPI;
- structured logging;
- OpenTelemetry-compatible tracing and metrics.

### Testing
- xUnit domain, application and integration tests;
- PostgreSQL integration tests;
- React Native Testing Library or an approved compatible alternative;
- Expo-compatible mobile tests;
- Maestro or equivalent end-to-end mobile flows;
- architecture and contract tests;
- versioned AI evaluation datasets.

## Repository direction

```text
apps/mobile
apps/api
packages/contracts
packages/design-tokens
product
planning
delivery
docs
infrastructure
scripts
.github
```

Generic patterns may be contributed back to PES Mobile separately. Atlas-specific logic remains in Atlas.

## Module boundaries

- **Identity** — authentication, provider mapping, sessions, account lifecycle and internal roles.
- **Businesses** — Business creation, Profile, locations, currency, ownership and Goals.
- **Context** — normalised Context Facts, attribution, freshness, confidence and sensitivity.
- **Knowledge Packs** — versioned manifests, compatibility, rules, prompts, templates and prohibited guidance.
- **Intelligence** — candidate generation, deterministic rules, AI synthesis, prioritisation, eligibility, duplicate suppression, expiry and Confidence classification.
- **Opportunities** — Growth Opportunity lifecycle, Today’s Focus, evidence snapshots, owner decisions and History.
- **Execution** — Execution Kits, assets, edits, checklist use and completion.
- **Outcomes** — usefulness, measured/owner-reported outcomes and follow-up.
- **Memory** — structured Business Memory from relevant evidence, decisions, preferences and outcomes.
- **Reviews** — Weekly Review generation.
- **Notifications** — preferences, quiet hours, scheduling, delivery and deep links.
- **Pilot Operations** — assisted/manual provenance, quality review, intervention and audit.
- **Analytics** — privacy-safe product, quality, cost and operational events; not a domain source of truth.

## Dependency rules

Domain modules never call external SDKs directly. Providers are accessed through ports owned by the consuming module. Mobile depends on API contracts, not persistence. Knowledge Packs do not depend on UI. Cross-module writes occur through application use cases. Provider deployment logic remains at the infrastructure boundary.

## Core entities

UserAccount, Business, BusinessMembership, BusinessProfileField, BusinessGoal, KnowledgePackDefinition, BusinessKnowledgePack, ContextFact, IntelligenceRun, OpportunityCandidate, GrowthOpportunity, OpportunityEvidence, ExecutionKit, ExecutionAsset, OpportunityDecision, OutcomeRecord, MemoryFact, WeeklyReview, NotificationPreference and AuditRecord.

Every Business-owned record contains `BusinessId`. Source, freshness, confidence, provenance and version metadata are retained where they affect trust or reproducibility.

## Opportunity lifecycle

```text
DraftCandidate -> Eligible -> Selected -> Presented -> Applied -> Completed -> OutcomePending -> OutcomeRecorded -> Archived
```

Alternative states include Skipped, Rejected, NotRelevant, Expired and Withdrawn.

Only one primary current Today’s Focus exists per Business. Terminal states cannot be reopened. Expired Opportunities cannot be applied. Decisions are append-oriented. Transitions use optimistic concurrency. Unsafe content may be withdrawn by an authorised operator with audit.

## Knowledge Pack contract

Each immutable versioned pack provides key, name, version, supported categories/goals, Opportunity categories, terminology, evidence and eligibility rules, prioritisation modifiers, prompt identifiers, execution templates, outcome suggestions, prohibited guidance and minimum compatible Atlas Core version.

Packs are packaged with the backend in MVP, validated at startup and referenced by exact version. Dynamic remote installation is out of scope.

## Intelligence pipeline

1. Load Business and goals.
2. Load exact Knowledge Pack version.
3. Load fresh Context Facts.
4. Load relevant Business Memory.
5. Identify missing context.
6. Generate deterministic candidates.
7. Generate AI-assisted candidates where permitted.
8. Validate structured output.
9. Verify Evidence references.
10. Apply eligibility and safety rules.
11. Suppress duplicates and cooldown violations.
12. Score practical value.
13. Select Today’s Focus.
14. Persist evidence and explanation snapshots.
15. Generate or attach Execution Kit.
16. Publish an internal event.
17. Schedule notification when appropriate.

If no candidate qualifies, Atlas returns a no-focus state.

## AI orchestration and OpenRouter boundary

AI providers are accessed through internal provider-neutral ports. OpenRouter is the initial planned adapter for customer-facing inference experiments and pilot workloads.

Business-affecting output must use validated structured output, reference supplied Evidence IDs, pass enum/length/policy/schema checks, retain prompt and pack versions, provider/model/settings, token usage, cost and safety result, and never persist before application validation.

`openrouter/free` may be used for internal development only. Closed pilot uses a fixed approved model or controlled allowlist. Production requires model quality tests, budget caps, rate limits, data-processing review and safe fallback. No automatic paid fallback is enabled without approval.

Final Confidence is controlled by application policy using evidence quality, freshness, deterministic rule certainty, context completeness and model consistency. Model prose cannot self-certify confidence.

## API model

- HTTPS JSON under `/api/v1`;
- OpenAPI generated from code;
- problem-details-compatible stable errors;
- cursor pagination for History;
- idempotency for retried commands;
- explicit resource versions or ETags for concurrency-sensitive writes.

The server derives identity and membership from validated claims. Client-provided Business IDs are never trusted without authorisation.

## Persistence

PostgreSQL is the system of record, using module-owned schemas or prefixes, EF Core contexts where practical, forward-only migrations, controlled production migration, BusinessId on owned tables, server-side membership checks, optimistic concurrency and append-oriented decisions, outcomes, audit and generation diagnostics.

Redis is not required for MVP. PostgreSQL, in-memory pack caching and mobile query caching are preferred until measured need exists.

## Background work and events

Background work covers daily Intelligence Runs, Weekly Reviews, notifications, outcome follow-ups, provider retries, retention cleanup and exports. MVP may use a PostgreSQL-backed durable queue and hosted worker.

Jobs are idempotent, leased, bounded by retry policy, traceable and dead-letter capable. An outbox-compatible pattern is used when work crosses transaction boundaries.

## Mobile architecture

Routes include authentication/onboarding, Today, Opportunity Detail, Execution Kit, History, Goals, Profile, Weekly Review and Settings. Persistent navigation contains Today, History, Goals and Profile.

Safe cached content may remain visible offline and is labelled stale. Unsupported offline mutations are blocked. Credentials and tokens use secure device storage. Deep links fail safely.

## Authentication and authorisation

Identity must support mobile authorization-code flow with PKCE, secure validation, recovery and logout. No client secret is shipped in the app. Logout clears tokens and sensitive cache. Policies cover BusinessOwner, PilotOperator and PlatformAdministrator. Internal roles are never customer-assignable.

## Security and privacy

Apply input validation, request limits, rate limits, provider secrets outside source/mobile bundles, prompt-injection controls, audit of internal access and high-impact changes, data classification, data minimisation, transparent AI usage, export/deletion and short retention/redaction for diagnostic AI payloads. Sensitive data must not be logged or sent unrestricted to models.

## Observability

Requests/jobs carry correlation ID, operation, module, authorised Business ID where applicable, outcome, duration and stable error code. Metrics include API/database latency, job depth, provider health, AI schema failures/retries/tokens/cost, no-result rate, intervention and safety failures.

Health endpoints are `/health/live` and `/health/ready`. AI unavailability does not necessarily make the entire API unready when safe fallback exists.

## Performance and reliability targets

- p95 simple API reads below 500 ms excluding client network;
- p95 non-AI writes below 800 ms;
- p95 cached Today retrieval below 700 ms;
- long AI generation asynchronous;
- mobile interactive shell target below 3 seconds;
- pilot availability target 99.5%;
- no loss of accepted owner decisions;
- automated backups and tested restoration before wider pilot.

## Feature flags and configuration

Flags may control AI generation, assisted mode, pack activation, notifications, Weekly Review, asset types, provider routing and internal tools. Configuration covers identity, database, AI providers, routing, notifications, budgets, limits, flags, retention, app versions, packs and observability. Critical invalid configuration blocks readiness.

## Testing and CI/CD

Each slice includes domain-policy, application, PostgreSQL integration/migration, tenant-isolation, API contract, Knowledge Pack schema, AI structured-output/evaluation, mobile state/accessibility, end-to-end and architecture tests where relevant.

Pull requests run formatting, linting, TypeScript checks, mobile tests, backend build/tests, architecture checks, migration validation, OpenAPI compatibility, dependency/secret scanning, slice governance and relevant AI evaluations.

Production release requires explicit Product Owner approval and an exact commit SHA, controlled migrations, health verification, rollback preparation and smoke tests.

## Cost policy

Do not introduce Redis, dedicated queue infrastructure, search engines, warehouses, Kubernetes, multiple backend services or paid observability without measured need and a recorded decision. Use managed PostgreSQL, one API service, Expo services and one initial AI provider behind abstractions.

## Slice technical mapping

- VS-01 — Identity and Businesses;
- VS-02 — Business Profile and Context;
- VS-03 — Knowledge Pack Foundation;
- VS-04 — Intelligence and Today’s Focus;
- VS-05 — Evidence and Opportunity Detail;
- VS-06 — Execution Kit;
- VS-07 — Decisions and Feedback;
- VS-08 — Outcomes and Business Memory;
- VS-09 — History read model;
- VS-10 — Weekly Review;
- VS-11 — Notifications;
- VS-12 — Pilot Operations and Audit.

## Technical Definition of Done

A slice is complete only when requirements are traceable, mobile/API work end-to-end, authorisation is enforced, migrations/tests are included, accessibility/degraded states are covered, telemetry exists, sensitive data is protected, contracts/docs are current, CI passes at the exact head SHA and the slice is deployable without critical deferred TODOs.

## Final technical decision

Atlas v1.0 uses Expo React Native, ASP.NET Core, PostgreSQL, a modular monolith, numbered vertical slices, provider-neutral AI, versioned Knowledge Packs, structured Business Memory and owner-controlled execution. Future infrastructure expansion requires evidence.
