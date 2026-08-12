# VS-33 — Pilot Operations Design

## Purpose

Complete FR-18 with a minimal internal Pilot Operations capability for authorised Atlas staff. The slice exists to make the closed pilot operable and auditable; it is not a general-purpose administration platform.

Authority:
- `product/PRD.md` FR-18;
- `product/TRD.md` Pilot Operations, Opportunity lifecycle, audit, authorization and observability requirements;
- `product/DESIGN.md` calm, evidence-aware, accessible interaction rules;
- `delivery/governance.json` and approved decisions through DEC-10.

Base: post-VS-32 `main@66e2b7979d68b390d74b395f650b0b6d215e71a8`.

## Product outcome

An authorised `PilotOperator` or `PlatformAdministrator` can:

1. inspect pilot Businesses and their readiness/quality signals;
2. inspect recent Opportunity-generation outcomes, including degraded and no-focus results;
3. inspect owner feedback, especially unsafe-guidance reports;
4. record internal support notes;
5. assist a profile correction without impersonating the owner;
6. collaboratively prepare a valid Opportunity from Atlas's existing evidence-aware candidate pipeline;
7. withdraw unsafe or unsuitable Opportunity content with a required reason and full audit trail.

Business owners keep their existing customer flows and four-tab navigation unchanged.

## Approaches considered

### A — Dedicated bounded Pilot Operations module — selected

Add operator-only API commands/read models, append-oriented operational records, persisted generation diagnostics and a root `/operator` mobile route outside the owner tab shell. Reuse existing Business/Profile/Context/Opportunity services and validation where possible.

Benefits:
- clear authority boundary;
- strong auditability;
- minimal new infrastructure;
- preserves owner APIs and semantics;
- directly matches FR-18/TRD.

### B — Operator impersonates a Business owner — rejected

Reuse owner endpoints after selecting a target Business.

Rejected because actions would become indistinguishable from owner actions, owner confirmation provenance would be false, and audit/authorization would be materially weaker.

### C — Full admin console/service — rejected

Build a broad dashboard with user management, analytics, queues, workflows and configuration.

Rejected because FR-18 explicitly calls for minimal pilot capability and Atlas v1.0 remains a modular monolith with low fixed cost.

## DEC-11 authority boundary

Internal operators do not impersonate Business owners.

Every operator mutation uses a dedicated `InternalOperator` endpoint and records:
- acting internal `UserAccountId`;
- target `BusinessId`;
- action type;
- target entity/id where relevant;
- reason/support note where required;
- timestamp;
- provenance sufficient to distinguish operator-assisted state from owner-confirmed state.

Owner endpoints remain unchanged and owner confirmation is never fabricated.

## Architecture

Add one `PilotOperations` vertical module inside the existing ASP.NET Core modular monolith.

Primary server units:

- `IntelligenceRunRecord` — append-only, Business-scoped generation diagnostic written by the existing Today/Opportunity generation path;
- `PilotOperationRecord` — append-only internal intervention/support provenance;
- `PilotOperationsService` — read models plus bounded commands;
- `PilotOperationsEndpoints` — all routes require `InternalOperator`;
- existing `AuditRecord` remains the security/audit stream for internal access and high-impact changes.

No new service, Redis, queue, warehouse or external admin provider is introduced.

## Generation diagnostics

`OpportunityFocusService.GenerateAsync` records a compact diagnostic for each completed generation attempt.

Persist only operationally useful metadata:
- `Id`;
- `BusinessId`;
- actor account when available;
- outcome: `ready`, `no-focus`, `insufficient-context`, `degraded`;
- stable code;
- candidate count when generation ran;
- selected Opportunity id when created/returned;
- timestamp.

Do not persist raw prompts, provider payloads, private diagnostics, stack traces, or end-customer data.

A current still-valid Opportunity may produce a `ready` diagnostic referencing that Opportunity. This is acceptable because the record describes the observed generation request/result, not a new Opportunity creation.

## Pilot Business queue/read model

`GET /api/v1/pilot-operations/businesses`

Returns a bounded recent pilot list with operational indicators, not a KPI wall:
- Business identity/category/location;
- profile confirmed yes/no;
- goal count;
- current Opportunity status/title/id if present;
- latest generation outcome/code/time;
- recent unsafe-feedback count;
- recent useful/not-useful feedback counts;
- latest operator activity time.

No synthetic quality score or false precision is introduced.

`GET /api/v1/pilot-operations/businesses/{businessId}`

Returns the detail needed for intervention:
- Business + Profile;
- Goals and Context summary;
- current/recent Opportunities;
- recent generation diagnostics;
- recent owner feedback;
- recent internal support/intervention records.

Internal reads are audited at the Business level where they reveal operational detail.

## Internal operator identity

Authorization is claim-based through the existing `InternalOperator` policy (`PilotOperator` or `PlatformAdministrator`).

For audit persistence, the module resolves the authenticated provider subject to `UserAccount`; if a valid internal identity has no account row yet, Atlas creates that internal account record. It does not create Business membership or customer ownership.

Business IDs supplied to operator routes identify the target of an authorised internal action; they do not grant authorization by themselves.

## Support notes

`POST /api/v1/pilot-operations/businesses/{businessId}/notes`

Creates an append-only internal support note:
- bounded non-empty text;
- internal only in VS-33;
- actor/time recorded;
- auditable;
- no attachments.

Notes never alter owner data automatically.

## Assisted profile correction

Operators may correct factual profile fields through a dedicated operator command, never the BusinessOwner profile endpoint.

`PUT /api/v1/pilot-operations/businesses/{businessId}/profile`

Rules:
- same allowed Profile fields as the current Profile model;
- validate lengths/shape using shared profile rules where available;
- write `Source = "operator-assisted"`;
- set `OwnerConfirmed = false` after an operator change;
- record a `PilotOperationRecord` with reason and changed-field names;
- add an `AuditRecord`;
- owner-facing Profile remains available and can be re-confirmed by the owner through the existing Profile flow.

Mobile/API contracts that currently assume only `owner|public` source must be widened safely to display `operator-assisted` without treating it as owner confirmation.

This preserves owner authority instead of silently asserting that an operator correction was owner-confirmed.

## Collaborative Opportunity preparation

VS-33 does not add free-form recommendation authoring.

`POST /api/v1/pilot-operations/businesses/{businessId}/opportunities/prepare`

The operator selects one currently valid candidate from Atlas's existing deterministic/evidence-aware Opportunity generation pipeline and supplies an optional bounded support note.

Rules:
- Business must have confirmed Profile, Goals and current Knowledge Pack assignment;
- candidate must be regenerated from current Business evidence at command time;
- candidate identifier/pattern must match a currently eligible generated candidate;
- candidate must contain factual evidence, preserving the VS-30 compatibility rule;
- exact Knowledge Pack version/evidence snapshot is persisted through the existing Opportunity format;
- provenance records `operator-assisted` plus acting operator;
- if an actionable `available` Opportunity already exists, preparation returns conflict instead of silently replacing it;
- owner remains the only actor who applies/completes external action.

This satisfies FR-18's collaborative preparation requirement without creating an unsafe free-form manual content path.

## Withdrawal

Add `OpportunityStatuses.Withdrawn = "withdrawn"`.

`POST /api/v1/pilot-operations/businesses/{businessId}/opportunities/{opportunityId}/withdraw`

Rules:
- `InternalOperator` only;
- required bounded reason;
- optimistic concurrency using the Opportunity version;
- same-Business Opportunity lookup;
- only non-terminal/currently relevant Opportunities may be withdrawn;
- withdrawal sets status to `withdrawn`, stores no false owner decision, and records operator intervention + audit;
- withdrawn Opportunities are not actionable, cannot produce/reopen Execution Kit actions, and cannot become Today’s Focus;
- History/Opportunity Detail may display `withdrawn` truthfully;
- withdrawal cannot be undone in VS-33.

No feedback report automatically triggers withdrawal. A human operator explicitly performs the command.

## Owner-facing behavior

Owner navigation remains exactly:
- Today
- History
- Goals
- Profile

No operator tab is added.

When current content is withdrawn:
- Today generation must ignore it and safely return/generate the next valid state;
- Opportunity Detail shows withdrawn status and no actionable controls;
- History retains the record for transparency.

## Internal mobile surface

Add root `/operator` and `/operator/businesses/[businessId]` Stack routes outside `(tabs)`.

The operator experience is review-first, not dashboard-first:

### Pilot queue
- page title and concise purpose;
- Business cards ordered by operational attention signal/recency using deterministic server ordering;
- visible labels for unsafe report, degraded/no-focus generation, missing profile/goals, current Opportunity;
- loading, empty, unauthorized and recoverable-error states;
- no owner tab-shell mutation.

### Business review
Sections:
1. Business readiness;
2. current/recent Opportunity;
3. recent generation outcomes;
4. owner feedback/safety reports;
5. profile assistance;
6. internal support notes;
7. collaborative Opportunity preparation;
8. withdrawal control when applicable.

High-impact withdrawal uses an explicit confirmation and reason. Destructive styling is reserved for withdrawal.

## Error handling

- unauthenticated: normal authentication behavior;
- authenticated non-internal user: safe forbidden/unauthorized operator screen, never owner data;
- unknown Business/Opportunity: safe not found;
- stale Opportunity version: conflict requiring refresh;
- invalid profile/candidate/note/reason: validation problem with stable code;
- candidate no longer eligible: conflict requiring refresh/review;
- generation degradation: operator read model exposes stable code only, not provider/stack details;
- failed mutations preserve entered operator text where practical and never claim success before server confirmation.

## Audit and privacy

Audit:
- internal Business detail access;
- support note creation;
- profile assistance;
- Opportunity preparation;
- Opportunity withdrawal.

Operational records are append-oriented. Existing owner records are not rewritten to impersonate owner actions.

Privacy:
- no end-customer data is requested;
- support/reason fields carry explicit internal guidance not to include unnecessary personal data;
- no raw model/provider payload persistence;
- no external support system integration.

## Testing strategy

TDD batches must cover:

1. `InternalOperator` authorization and internal-account resolution;
2. generation diagnostic policy/persistence and safe metadata only;
3. pilot queue/detail read models and stable quality indicators;
4. support-note append-only persistence/audit;
5. operator-assisted Profile correction sets `OwnerConfirmed=false`, `Source=operator-assisted`, and records provenance;
6. collaborative candidate preparation reuses current generator/evidence rules and rejects stale/ineligible/current-active conflicts;
7. withdrawal lifecycle, version conflict, required reason, audit and no reopen;
8. owner Today/Detail/History behavior for withdrawn Opportunities;
9. operator mobile client/model/routes/screens, unauthorized/degraded states and accessibility;
10. no fifth owner tab and no operator impersonation path;
11. clean PostgreSQL migration replay, API regression suite, mobile/type/lint/runtime tests, Security baseline and Product Intake.

## Out of scope

- full admin/user-management platform;
- operator impersonation;
- billing/subscription administration;
- generic analytics warehouse/dashboard;
- free-form AI/recommendation authoring;
- automatic unsafe-content withdrawal;
- external helpdesk/CRM/chat/email transport;
- attachment handling;
- changing owner authentication/navigation;
- production release/deployment/EAS/OTA/production database mutation.

## Success criteria

VS-33 is complete when an authorised internal operator can review a pilot Business, understand recent quality/generation/safety signals, record support work, assist Profile correction with truthful provenance, collaboratively prepare a currently valid evidence-backed Opportunity, and explicitly withdraw unsafe content with audit—while owners cannot access operator APIs and owner authority/navigation remain intact.
