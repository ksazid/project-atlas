# VS-32 — Feedback, Safety Reporting & Support Design

## Authority

- Product: `ATLAS-PRD-001` FR-17, with FR-16 degraded-state requirements.
- Technical: `ATLAS-TRD-001` modular monolith, Business isolation, append-oriented auditability and mobile-first UX.
- Design: `ATLAS-DESIGN-001@1.2` and the post-VS-31 native navigation model.
- Base: post-VS-31 `main@9cd153aae1f0e0962548ecf2cfb5f112350a5a73`.
- Product Owner standing authorization on 2026-08-12 covers implementation, bounded design changes, fixes, certification and merge. Release/production remain separately unauthorized.

## Problem

Atlas already records Action decisions, Execution Kit usefulness and Outcomes, but FR-17 is not complete. An owner cannot yet:

1. rate an Opportunity directly as useful/not useful;
2. report incorrect Business Context;
3. report unsafe guidance;
4. submit general product feedback; or
5. request support.

Those signals must be captured without overloading Action Decision or Outcome semantics and without automatically mutating Business data or withdrawing an Opportunity.

## Chosen approach

Add a small, provider-neutral, append-only Feedback module inside the existing ASP.NET Core modular monolith, plus one reusable mobile Feedback & Support flow.

Rejected alternatives:

- **Reuse ActionDecision/Outcome records:** avoids a table but conflates execution/outcome semantics with product trust/support feedback and makes operator triage ambiguous.
- **Send directly to an external helpdesk:** introduces an unapproved external provider and prevents Atlas from retaining the provenance needed for the closed pilot.

## Domain contract

Create one immutable owner-submitted `FeedbackRecord` per submission.

### Feedback kinds

- `opportunity-rating`
- `incorrect-context`
- `unsafe-guidance`
- `general-feedback`
- `support-request`

### Stored fields

- `Id` — server-generated GUID.
- `BusinessId` — required, server-authorised Business owner scope.
- `SubmittedByAccountId` — required authenticated account.
- `Kind` — one of the five approved values.
- `OpportunityId` — optional generally; required for `opportunity-rating` and `unsafe-guidance`; when supplied it must belong to the same Business.
- `ContextKey` — optional; only valid for `incorrect-context`; max 120 characters.
- `Usefulness` — `useful` or `not-useful`; required only for `opportunity-rating`.
- `Message` — optional bounded owner note, trimmed, max 1200 characters. Empty/whitespace becomes null.
- `CreatedAt` — server UTC timestamp.

The record is append-only in VS-32. There is no edit/delete/triage state in this slice. FR-18 Pilot Operations may add separate operator intervention/triage records later rather than rewriting owner feedback history.

## Validation and safety rules

- Owner membership is derived from validated identity; a client Business ID is never trusted by itself.
- Opportunity references must resolve inside the authorised Business or return the same safe not-found shape used by existing Business-scoped APIs.
- `opportunity-rating` requires `OpportunityId` and `Usefulness`, and rejects `ContextKey`.
- `unsafe-guidance` requires `OpportunityId` and rejects `Usefulness`/`ContextKey`.
- `incorrect-context` may include `ContextKey`, rejects `Usefulness`, and does not require an Opportunity.
- `general-feedback` and `support-request` reject `Usefulness` and `ContextKey`.
- Notes are optional, text-only, bounded to 1200 characters, and UI copy explicitly asks owners not to include customer names, contact details or other end-customer personal data.
- No attachments in v1.
- Submitting unsafe guidance **does not** automatically withdraw, alter or suppress an Opportunity. It records a trust/safety signal for later authorised operator intervention under FR-18.
- Feedback never overwrites Business Profile or Context. Incorrect context is a report; the existing Context editor remains the correction mechanism.

## API

Add an owner endpoint under the existing Business-scoped v1 API:

`POST /api/v1/businesses/{businessId}/feedback`

Request:

```json
{
  "kind": "unsafe-guidance",
  "opportunityId": "<guid-or-null>",
  "contextKey": null,
  "usefulness": null,
  "message": "Optional owner note"
}
```

Success: `201 Created` with a provider-neutral receipt containing `id`, `kind`, and `createdAt`.

Errors use existing stable validation / not-found / forbidden patterns and do not leak whether another Business owns a referenced Opportunity.

No owner feedback-history endpoint is required by FR-17; YAGNI keeps VS-32 write-focused. FR-18 may add an operator read model later.

## Persistence

Add a forward-only EF Core migration for `FeedbackRecords` with:

- primary key on `Id`;
- required Business/account foreign-scope identifiers following existing schema conventions;
- index on `(BusinessId, CreatedAt)` for later pilot review;
- optional index on `OpportunityId` if consistent with current migration conventions;
- bounded column lengths matching validation.

No new service, queue, Redis, external support provider or background job is introduced.

## Mobile UX

### 1. Opportunity Detail

Add a compact trust/feedback section below the decision/outcome controls:

- prompt: **“Was this Opportunity useful?”**
- two explicit buttons: **Useful** / **Not useful**;
- submit one `opportunity-rating` record per tap; disable controls while submitting and show provider-neutral success/error copy;
- link/button: **Report unsafe guidance** → opens `/feedback?kind=unsafe-guidance&opportunityId=<id>`.

A rating is feedback only; it does not change the Opportunity lifecycle.

### 2. Context

Add a secondary action near the end of the Context screen:

**Report incorrect context** → `/feedback?kind=incorrect-context`.

The report flow explicitly reminds the owner that they can also correct editable context directly on the current screen.

### 3. Settings

Add a **Feedback & support** card linking to `/feedback`.

### 4. Feedback & Support screen

Root Stack route `/feedback`, consistent with the VS-31 pushed-detail pattern.

It provides four report/request choices when no kind is preselected:

- Incorrect business context
- Unsafe guidance
- General feedback
- Support request

When preselected by a deep link, preserve that kind and the optional Opportunity ID.

Form behavior:

- short explanation tailored to the kind;
- optional note field, max 1200 characters;
- no attachment control;
- clear privacy helper: do not include customer names, contact details or other end-customer personal data;
- submit button with disabled/busy/error/success states;
- after success show a receipt confirmation and a Back to Profile/previous-screen action;
- unsafe-guidance submission copy says the report was recorded for review and does not claim immediate removal.

The screen uses the existing Atlas native/product visual primitives; no new visual system or navigation model is introduced.

## Error and degraded states

- Offline/network/API failure: keep the drafted note in memory and show a retryable provider-neutral message; do not claim the report was recorded.
- Missing Business session: route through the existing session/business recovery path.
- Invalid/deleted Opportunity: show a safe unavailable message and allow returning without leaking tenancy information.
- Duplicate taps are blocked client-side while a submission is in flight. The backend remains append-only; no heuristic deduplication is introduced in VS-32.

## Testing strategy

Follow Superpowers TDD.

### API/domain RED → GREEN

- kind-specific validation matrix;
- bounded/trimmed note behavior;
- same-Business Opportunity reference enforcement;
- cross-Business reference fails safely;
- append-only persistence with Business/account scope and timestamp;
- no Opportunity state mutation on unsafe report;
- clean PostgreSQL migration replay.

### Mobile RED → GREEN

- Opportunity useful/not-useful controls submit the right payload and do not mutate Action state;
- unsafe-guidance route carries Opportunity ID;
- Context exposes incorrect-context report route;
- Settings exposes Feedback & Support;
- form kind selection, note limit, privacy helper, busy/error/success states;
- root Stack pushed-detail semantics and accessibility labels;
- existing VS-31 four-tab shell remains unchanged.

### Full gates

Exact-head repository preflight, TypeScript, Expo lint, mobile tests/runtime checks, API build/tests, PostgreSQL migration replay, dashboard build, Security baseline and Product Intake.

## Scope exclusions

- operator feedback queue or triage;
- unsafe-content withdrawal;
- profile correction by operators;
- helpdesk/CRM integration;
- email/chat support transport;
- attachments;
- automated moderation or AI classification of reports;
- analytics dashboards;
- release, deployment, EAS build/submit/OTA or production database mutation.

These exclusions keep VS-32 focused. FR-18 Pilot Operations follows as a separate governed slice and may consume the immutable feedback records created here.
