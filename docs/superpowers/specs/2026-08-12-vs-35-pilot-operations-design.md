# VS-35 — Pilot Operations Design

## Goal
Deliver the minimal FR-18 internal operator capability without creating a general admin platform or weakening owner authority.

## Architecture
Add one bounded `PilotOperations` vertical module inside the existing ASP.NET Core modular monolith. Persist append-only `IntelligenceRunRecord` diagnostics and `PilotOperationRecord` intervention provenance, expose dedicated `InternalOperator` endpoints, and add root `/operator` Stack routes outside the owner tab shell. Reuse current Business/Profile/Context/Opportunity models and the existing evidence-aware Opportunity generator.

## Authority and provenance
DEC-11: internal operators never impersonate Business owners. Every operator mutation records the acting internal account, Business target, action, entity target where relevant, reason/support note, timestamp and audit record. Operator-assisted profile edits use source `operator-assisted` and clear `OwnerConfirmed` until the owner reconfirms.

## Diagnostics
Record only compact stable fields: Business, actor where available, outcome (`ready`, `no-focus`, `insufficient-context`, `degraded`), stable code, candidate count, selected Opportunity id and timestamp. Never persist raw prompts, model/provider payloads, stack traces or end-customer data.

## Queue and detail
Provide bounded operator read models showing Business identity/readiness, latest generation outcome, current/recent Opportunities, recent feedback including unsafe guidance, and latest operator activity. Do not create a synthetic quality score.

## Support and profile assistance
Support notes are append-only, bounded and audited. Profile correction uses a dedicated operator endpoint, server-generated changed-field metadata, `operator-assisted` source and `OwnerConfirmed=false`.

## Collaborative Opportunity preparation
No free-form recommendation authoring. Regenerate current candidates from the existing evidence-aware generator; require confirmed Profile, Goal(s), current Knowledge Pack and factual evidence. Reject stale/ineligible/current-active conflicts.

## Withdrawal
Add terminal `withdrawn` Opportunity state. Withdrawal is `InternalOperator` only, requires reason and optimistic concurrency, records intervention + audit, cannot be reopened in VS-35, and is never triggered automatically by feedback.

## Owner experience
Owner navigation remains Today / History / Goals / Profile. Withdrawn Opportunities remain truthful in History/Detail and are non-actionable. Today ignores withdrawn content safely.

## Internal UI
Root `/operator` queue and `/operator/businesses/[businessId]` review screen outside `(tabs)`. Review-first, calm and evidence-aware. High-impact withdrawal requires explicit confirmation/reason. No new owner tab.

## Safety and privacy
No end-customer data requirement, attachments, external helpdesk, impersonation, raw provider/model diagnostics, new infrastructure, release or production changes.

## Compatibility
VS-35 starts from merged VS-34 main `345045cd40a97daac547aa2b847e793a20bb5fb5`. The old paused PR #57 is evidence only; its code may be ported only where a fresh file-overlap scan confirms compatibility and all VS-35 tests/gates rerun on the new branch.
