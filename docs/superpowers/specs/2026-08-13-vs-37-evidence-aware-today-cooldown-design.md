# VS-37 — Evidence-Aware Today Cooldown Repair — Design

## Problem
Atlas currently suppresses a recently generated Opportunity using only `patternKey + cooldown window`. That prevents a pattern from being reconsidered even when the owner materially changes the evidence that made the pattern eligible. In the live pilot case, richer owner-confirmed `currentpriorities` produced eligible candidates, but Today remained empty because the prior `current-offer-visibility-review` pattern was still inside its seven-day cooldown.

This conflicts with the FR-07 intent that Today present the strongest current eligible Opportunity while still avoiding material duplicates.

## Product behavior
Cooldown identity becomes:

`pattern + goal context + evidence actually used by that pattern`

The rules are:

1. Same pattern, same goal context and same relevant evidence inside the existing cooldown remains suppressed.
2. A material change to evidence actually used by the pattern allows that pattern to be reconsidered immediately.
3. Changes to unrelated Business Context do not bypass cooldown.
4. A prior `Not Relevant`, `Applied`, `Available` or other Opportunity status continues to participate in duplicate suppression exactly as today; status does not become a cooldown bypass.
5. Existing Knowledge Pack cooldown durations remain unchanged.
6. Atlas does not re-enable policy-only filler Opportunities.

## Chosen approach
Use a deterministic `cooldownFingerprint` on each generated Opportunity candidate.

The fingerprint is SHA-256 over a canonical sequence containing:
- exact `patternKey`;
- goal id;
- normalized goal type;
- trimmed goal title;
- goal priority;
- the sorted distinct `EvidenceId` values returned by `TryResolveEvidence` for that pattern.

Only pattern-resolved evidence participates. `ResolvedKnowledgeBundle.Fingerprint` is deliberately excluded because it contains unrelated context, local-market and memory facts; using it would let an irrelevant Context edit bypass cooldown.

### Alternatives rejected
- **Pattern + time only:** current behavior; blocks useful reconsideration after material evidence changes.
- **Pattern + whole bundle fingerprint:** easy to implement but too broad; unrelated Context edits would defeat duplicate prevention.
- **Hand-maintained key subsets per pattern:** duplicates Knowledge Pack evidence rules and creates drift between eligibility and cooldown logic.

## Generation flow
For each manifest pattern and matching goal:
1. Resolve the pattern's evidence using the existing evidence rules.
2. If evidence cannot be resolved, the pattern is ineligible as today.
3. Compute the candidate `cooldownFingerprint` from the pattern, goal context and resolved evidence.
4. Inspect prior Opportunities inside the existing `CooldownDays` window.
5. Suppress only when a prior Opportunity has the same pattern and equivalent cooldown fingerprint.
6. Rank and select candidates using the existing ordering; no ranking changes are introduced.

## Historical compatibility
New EvidenceJson snapshots use `schemaVersion: 2` and add `cooldownFingerprint`.

Existing schema-version-1 Opportunities are not rewritten. When a prior snapshot has `patternKey`, `goal` and `evidence[]` but no explicit fingerprint, Atlas derives the same fingerprint from the stored goal fields and stored `evidenceId` values. This allows real historical Opportunities to participate accurately after deployment.

If a legacy/malformed snapshot contains the same pattern but does not contain enough information to derive a fingerprint, Atlas behaves conservatively and keeps the old pattern-level suppression for that prior Opportunity. A malformed historical record must never become a duplicate-bypass mechanism.

## Data and persistence
No database migration is required. `Opportunity.EvidenceJson` already stores the goal and exact evidence snapshot. The new field is additive JSON persisted on future Opportunities only.

No existing Opportunity is mutated or backfilled.

## Owner experience
No mobile UI, navigation or copy changes are required. The visible effect is only eligibility quality:
- materially new relevant evidence can produce a new Today Focus even when the same pattern was recently used;
- irrelevant edits do not create repetitive recommendations.

## Safety and trust
- Existing evidence provenance and `EvidenceId` generation remain unchanged.
- Existing 7/14-day Knowledge Pack cooldown values remain unchanged.
- Existing no-filler and `OpportunityFocusService` evidence eligibility stay unchanged.
- No model/provider behavior, prompt, connector, external action or owner authority changes.
- No production release, EAS/OTA or production database mutation is part of this slice.

## Acceptance criteria
- Same relevant evidence + same goal context + same pattern is suppressed within cooldown.
- Changed pattern-relevant evidence can make the same pattern eligible within cooldown.
- Unrelated Context changes do not bypass cooldown.
- `Not Relevant` with identical identity remains suppressed.
- Schema-v1 historical snapshots with complete goal/evidence data derive equivalent cooldown identity.
- Incomplete same-pattern legacy snapshots remain conservatively suppressed.
- New snapshots persist schema version 2 plus deterministic `cooldownFingerprint`.
- Full existing mobile/API, migration, dashboard, Security and Product Intake gates remain green.
