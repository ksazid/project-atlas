# VS-23 — Evidence-Aware Restaurant/Café Opportunity Generation

Date: 2026-08-11
Status: Approved for implementation by Product Owner instruction to proceed with all remaining slices
Depends on: ATLAS-PRD-001, ATLAS-TRD-001, VS-20 certified baseline
Parallel constraint: VS-23 must not modify VS-22 multi-source discovery files or VS-21 onboarding/location remediation files. It is based on the stable VS-20 Knowledge Bundle contract and will be rebased/revalidated after VS-22 lands in main.

## Outcome

Replace the current generic Today’s Focus placeholder generation with deterministic, evidence-aware candidate generation that consumes `ResolvedKnowledgeBundle` directly and uses the applicable immutable Knowledge Pack v2 manifests. Restaurant/Café Businesses receive category-specific candidates only when the required evidence exists. Unsupported categories receive Core-only behavior. If no candidate qualifies, Atlas returns an honest no-focus state rather than filler.

## Product rules

1. Candidate generation consumes the exact `ResolvedKnowledgeBundle` from VS-20; it does not re-query provider schemas or re-infer public facts independently.
2. Every candidate must materially align to an owner-selected Business Goal.
3. Every candidate must be actionable, bounded, evidence-backed and explainable.
4. Evidence and Atlas interpretation remain distinct in persisted snapshots and API output.
5. Missing evidence causes a pattern to be ineligible; Atlas never invents facts to satisfy an evidence rule.
6. Restaurant/Café patterns are available only when the resolved bundle includes the Restaurant/Café category manifest.
7. Unsupported categories may use Core patterns only; Restaurant/Café terminology, evidence rules and templates must never leak.
8. The application, not model prose, controls eligibility and Confidence.
9. VS-23 is deterministic. No external AI provider call is required for candidate generation.
10. No qualifying candidate means no Today’s Focus. Atlas must not manufacture a recommendation solely to populate the screen.
11. Exact pack key/version and bundle fingerprint are retained with the generated candidate/opportunity evidence snapshot.
12. Duplicate/cooldown suppression is deterministic and based on the pattern key plus the pattern’s configured cooldown.
13. One primary Today’s Focus is selected by deterministic practical-value ranking. Ties are resolved stably.
14. No guaranteed outcome language is introduced.

## Candidate contract

A generated candidate contains stable pattern key, action title, Goal identity/type/title/priority and alignment, Reason, Why Now, Expected Impact, Effort, application-controlled Confidence, structured evidence references, assumptions, limitations, execution template key, cooldown, exact manifest references, bundle fingerprint, generation timestamp and expiry.

Evidence items use stable IDs derived from the resolved input layer/key/value/source so later explanation snapshots can reference exactly the facts that qualified the candidate.

## Evidence rule mapping

### Core

- `confirmed-profile`: satisfied only when the generation use case receives an owner-confirmed Business Profile.
- `priority-goal`: satisfied by the candidate’s current owner-selected goal.

### Restaurant/Café

- `restaurant-category-confirmed`: resolved bundle category exactly `restaurant-cafe` and the Restaurant/Café manifest is present.
- `ordering-channel-confirmed`: owner-confirmed Context evidence. The current progressive-onboarding canonical key is `primarychannels`; compatible explicit aliases such as `orderingChannel`, `orderingChannels`, `primaryOrderingChannel`, `serviceChannel`, and `serviceChannels` are accepted for imported/future canonical context.
- `hours-evidence-present`: explicit opening/business-hours evidence from the confirmed Business Profile or a resolved fact whose key explicitly represents hours. No inference from unrelated text.
- `current-offer-confirmed`: owner-confirmed Context evidence representing a current offer or near-term priority. The current progressive-onboarding key `currentpriorities` is accepted, together with explicit aliases `currentOffer`, `promotion`, `currentPromotion`, `nearTermPriority`, and `commercialPriority`.
- `reputation-signal-present`: attributable resolved reputation/review signal or owner-confirmed concern with `reputationSignal`, `reviewSignal`, `ratingSignal`, `reputationConcern`, or `reviewConcern`.

The VS-20 resolver already filters Context entries to owner-confirmed values. Therefore `Source` is provenance, not a substitute for owner-confirmation. Key matching is case-insensitive but otherwise explicit. No fuzzy semantic inference is used in VS-23.

## Goal matching

Patterns are evaluated only for Business Goals whose canonical `Type` appears in the pattern `GoalTypes`. Lower numerical priority ranks higher. No matching goal means no candidate.

## Confidence policy

Manifest confidence is only an upper bound. `Medium` is allowed when all required rules are satisfied by explicit eligible evidence and the profile is confirmed. Confidence becomes `Low` when a qualifying rule depends on non-owner attributable evidence where that evidence rule permits it. VS-23 never emits `High` confidence.

## Ranking

Candidates rank deterministically by goal priority, confidence, effort, category specificity and stable pattern key. Only one becomes Today’s Focus.

## Duplicate and cooldown suppression

Prior Opportunities for the Business are examined. A candidate is suppressed when a prior Opportunity with the same persisted `patternKey` was created within the pattern cooldown. Legacy opportunities without a pattern key do not suppress VS-23 candidates.

## Persistence

The existing `Opportunity` entity remains the aggregate; no new table is added. `EvidenceJson` stores a schema-versioned immutable snapshot containing pattern key, bundle fingerprint, goal identity/type/title/priority, exact manifest references, structured evidence items, assumptions, limitations, execution template key, cooldown and generated-at time.

`KnowledgePackKey` and `KnowledgePackVersion` use the most specific manifest responsible for the selected pattern. The existing `KnowledgePackVersionId` FK continues to identify the current persisted Core assignment because category manifests are packaged runtime manifests rather than separately assigned database versions; the complete exact manifest set is authoritative in `EvidenceJson`.

## Today’s Focus integration

When no current unexpired Opportunity exists, the endpoint loads Business, confirmed profile, goals, current Core assignment, profile fields, context and memory; resolves the VS-20 bundle; generates eligible candidates; suppresses cooldown duplicates; ranks candidates; persists only the selected Opportunity; and returns `ready`.

Missing minimum setup returns `insufficient-context`. Valid setup with no evidence-qualified pattern returns `no-focus` with truthful copy and persists nothing.

## Opportunity detail

Detail parses the VS-23 evidence snapshot and exposes goal alignment, Evidence separately from Reason/Why Now, assumptions, limitations, source categories and exact pack version. Legacy snapshots retain the current safe fallback.

## Degraded behaviour

Bundle resolution failures do not create Opportunities and return a stable degraded/no-focus response. Unsupported categories are Core-only. Malformed legacy evidence remains readable via summary fallback.

## Testing

Tests prove Restaurant/Café rule eligibility, missing-evidence rejection, unsupported-category isolation, goal matching, no-focus behavior, evidence-ID integrity, confidence policy, exact manifest/fingerprint retention, cooldown suppression, deterministic ranking, Today’s Focus integration, tenant isolation, detail parsing and legacy compatibility. Existing Opportunity/Execution/Decision/Outcome/History/Weekly Review suites must remain green.

## Out of scope

External AI inference, OpenRouter configuration, new providers/POS, public-source scraping changes, VS-21/VS-22 implementation, autonomous publishing and production deployment.

## Branch and integration

Implementation branch: `atlas/vs-23-opportunity-generation`.

Before merge, it must update to then-current `main`, resolve conflicts, rerun deterministic tests and required CI/Security/Product Intake gates, and obtain exact-SHA certification approval. Deployment remains separate.

## Acceptance

VS-23 is acceptable when Atlas consumes the exact VS-20 knowledge bundle, generates only evidence-qualified goal-aligned opportunities, selects one deterministic Today’s Focus, preserves evidence/interpretation/pack/fingerprint provenance, suppresses cooldown duplicates, returns honest no-focus when evidence is insufficient, prevents Restaurant/Café leakage, preserves legacy behavior and passes exact-head certification gates.