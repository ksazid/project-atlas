# VS-26 — Google Place Intelligence & Goal-First Onboarding — Design

Status: Product design approved; planning-only until VS-25 is integrated and current-slice governance can advance safely.

## Authority and dependency

This design is subordinate to:

- `product/PRD.md` (`ATLAS-PRD-001`)
- `product/TRD.md` (`ATLAS-TRD-001`)
- `product/DESIGN.md` (`ATLAS-DESIGN-001` v1.2)
- `delivery/governance.json`
- `delivery/decisions.json`
- existing VS-21/VS-22 public-source, location, URL-safety and provenance boundaries
- existing VS-17 progressive-question catalogue and suppression semantics
- VS-25 Business Media & Menu Intelligence, which must be integrated before VS-26 runtime implementation starts

VS-26 must not activate while VS-25 is the active runtime slice. This branch is documentation-only and must not be used as a runtime integration base after VS-25 moves `main`; implementation must branch from the then-current `main` and revalidate this spec against that baseline.

## Product outcome

Atlas should avoid asking an owner factual operating questions that it can already answer confidently from a uniquely resolved public Business identity. After a Business is resolved to one canonical Google Place, Atlas may retrieve a bounded set of structured Google Place attributes, reconcile them with the existing website/Bolt/Wolt evidence graph, persist their provenance, and use them to suppress redundant progressive onboarding questions.

The remaining onboarding should focus primarily on owner-only information, especially Business Goals, current priority, strategic constraints and desired outcomes.

This keeps the locked product principle intact: do not make the owner manage Atlas more than the Business.

## Approved approach

Three approaches were considered.

### A. Automatic structured Google enrichment after every unique Place resolution — APPROVED

Whenever Atlas resolves a Business to one unique Google Place, request a tightly bounded Place Details field set even when the owner originally pasted a website, Bolt or Wolt URL.

Advantages:
- maximises redundant-question suppression;
- creates one consistent enrichment path independent of the source URL entered;
- reuses the existing canonical Place ID/location boundary;
- keeps Google data structured and provider-specific at the adapter boundary;
- improves category/context quality without requiring the owner to paste a Google URL.

Trade-off:
- richer fields can incur higher Google Places SKU cost, so the field mask must be deliberately minimal and independently testable.

### B. Enrich only when the owner provides a Google URL — REJECTED

Lower external cost, but website/Bolt/Wolt onboarding would continue asking questions Atlas could answer after location resolution.

### C. Do not use rich Google attributes; rely only on marketplace/website facts — REJECTED

Lowest provider cost but lower coverage, more owner questioning and poorer cross-source corroboration.

## Non-goals

VS-26 does not:

- scrape Google Maps HTML;
- ingest Google reviews or review text as authoritative Business facts;
- infer Goals from public data;
- infer sales, repeat rate, profitability, utilisation or customer demographics;
- introduce a general crawler;
- implement private Google Business Profile APIs;
- add private Bolt/Wolt/POS/reservation connectors;
- redesign the approved Atlas UI;
- add a new navigation model;
- deploy, release or production-enable Atlas.

## Approved design authority

All owner-facing work must preserve `ATLAS-DESIGN-001` v1.2.

The primary visual grammar remains the approved Atlas warm-neutral/deep-green system, shared tokens, existing `BrandMark` boundary, restrained cards/forms/buttons/depth, generous whitespace and low cognitive load. No new visual system, generic SaaS pattern or third-party branding may be introduced.

For product-workflow structure and accessibility, UI UX Pro Max may be used as a secondary implementation aid only. It may improve information architecture, form state, responsive behaviour and accessibility but may not override the approved Atlas visual grammar. Impeccable/Emil/Ponytail may only be used later within the repository's conditional design-skill order and within the frozen baseline.

## Architecture

### 1. Place resolution remains the identity boundary

The existing `IBusinessLocationProvider` / `GoogleBusinessLocationProvider` path continues to resolve candidate Businesses and returns a canonical Google Place ID (`providerRef`) plus address, coordinates, country, timezone and type summary.

VS-26 must not create a second independent identity-matching system.

Once one candidate is selected or uniquely preselected, that exact Place ID becomes the input to the enrichment adapter.

### 2. New provider port: structured Place enrichment

Add a consuming-module-owned provider-neutral port, conceptually:

`IBusinessPlaceEnrichmentProvider`

Input:
- canonical provider reference / Place ID;
- category or resolved type context when useful for selecting a field mask.

Output:
- a bounded `BusinessPlaceEnrichment` result containing provider-neutral structured attributes plus source metadata.

Google-specific HTTP and field names stay behind the adapter.

### 3. Google Place Details, never Maps HTML scraping

The Google adapter calls Places API (New) Place Details using the resolved Place ID and explicit `X-Goog-FieldMask`.

Initial candidate fields are intentionally narrow and selected only when they either:

1. replace an onboarding question;
2. materially improve Category/Context intelligence; or
3. provide an owner-useful fact already shown in the Business confirmation experience.

The initial restaurant/cafe enrichment set may include, subject to a final current-pricing check immediately before implementation:

- `businessStatus`
- `primaryType` / `primaryTypeDisplayName`
- `types`
- `regularOpeningHours` or `currentOpeningHours` only where they improve the existing hours fact
- `priceLevel` or `priceRange`
- `dineIn`
- `takeout`
- `delivery`
- `curbsidePickup` where relevant
- `reservable`
- `servesBreakfast`
- `servesBrunch`
- `servesLunch`
- `servesDinner`
- `servesCoffee`
- `servesDessert`
- `servesVegetarianFood`
- `outdoorSeating` when useful

Do not request ratings, review counts, reviews, review summaries, generative summaries, photos or broad amenities merely because they exist. They do not eliminate the targeted onboarding questions and would add cost/trust complexity.

The implementation plan must re-check current Google Places field availability, EEA terms and SKU billing against official Google documentation before locking the production field mask.

### 4. Provider-neutral canonical context model

Google attributes are translated into Atlas canonical facts/context rather than leaking Google names into Business logic.

Examples:

- `dineIn=true` -> service channel evidence `Dine in`
- `takeout=true` -> service channel evidence `Takeaway`
- `delivery=true` -> service channel evidence `Delivery`
- a confirmed Bolt/Wolt merchant source -> marketplace ordering-channel evidence
- `reservable=true` -> reservation capability evidence
- meal flags -> bounded service-period context
- `priceLevel` -> public price-position evidence, never profitability

Canonical values must be stable, provider-neutral and suitable for future non-Google adapters.

### 5. Provenance and confidence

Every accepted enrichment fact retains:

- provider/source identifier;
- canonical source identity / Place ID as permitted by provider policy;
- observed timestamp;
- confidence/evidence class;
- owner-confirmed state;
- reconciliation state where applicable.

External structured facts remain `public-observed` until owner confirmation. They must never be rewritten as `measured`.

Provider attribution/display requirements must be respected where Google-derived data is shown to the owner. Internal storage must follow current Google Maps Platform terms, including any field-specific storage limitations. Place IDs may be retained only under the current applicable provider policy.

## Reconciliation policy

VS-26 extends the existing deterministic VS-22 reconciliation model rather than replacing it.

1. Owner-confirmed facts remain authoritative.
2. Existing primary-source precedence remains authoritative for ordinary source facts.
3. Google structured Place attributes may fill missing operating/context facts and corroborate matching facts.
4. Google must not silently overwrite a valid owner-confirmed or primary-source fact.
5. Exact agreement strengthens corroboration but does not change the evidence class to measured.
6. Material conflict preserves both provenance paths and leaves the fact unresolved for owner review when it affects intelligence/question suppression.
7. Weak, absent or contradictory evidence must not suppress a question merely to shorten onboarding.

## Question suppression and goal-first onboarding

### Current baseline

VS-17 currently suppresses a progressive question when its target Context key already exists as owner-confirmed Context or the question has been answered/skipped.

### VS-26 extension

Introduce a deterministic question-eligibility layer that can also suppress a question when a canonical fact is:

- relevant to that question's target;
- supported by sufficiently strong public evidence;
- not materially conflicted;
- fresh enough for the fact type; and
- safe to treat as a discoverable operating fact.

This is not equivalent to owner confirmation. The suppression decision and the retained evidence class remain separate.

For example, the restaurant question `restaurant-cafe.service-channel` should not be asked merely to learn dine-in/takeaway/marketplace participation when Atlas already has unconflicted, high-confidence canonical channel evidence from Place enrichment and/or the supplied Bolt/Wolt source.

If the evidence only establishes some channels and the missing channel information is not important to the current intelligence path, do not ask filler questions. If a missing channel is materially required by the active Knowledge Pack/opportunity rules, ask a bounded owner question.

### Goals remain owner-only

Atlas must never infer Business Goals from Google, Bolt, Wolt, websites, menus, reviews or category stereotypes.

The onboarding sequence should preferentially move the owner to Goals after Business confirmation when factual setup is already sufficiently covered.

Owner-only/high-value topics include:

- primary Business Goal;
- goal priority/order;
- current near-term priority;
- material constraint where it changes feasible recommendations;
- desired outcome/time horizon where the product model supports it;
- genuinely missing context needed for a current opportunity rule.

### Target question volume

When enrichment succeeds, target zero to three optional non-goal context questions after Goals, rather than mechanically filling the old three-to-five range.

The existing maximum of five remains a safety ceiling for degraded/missing-data cases, not a quota.

No filler questions are allowed.

## Owner-facing confirmation UX

The Discover/Confirm Business workflow remains visually and structurally aligned with the approved Atlas Business setup experience.

Do not create a new analytics panel.

A compact optional `About your business` summary may surface only the most decision-useful attributes, for example:

- category/cuisine/type summary;
- Dine-in / Takeaway / Delivery;
- reservation capability when relevant;
- price-position indicator if available and understandable;
- opening-hours summary using the existing hours presentation convention.

Display no more than roughly 3-5 concise items/groups before progressive disclosure.

The screen must not dump all Place attributes, ratings, review counts, payment methods or accessibility attributes.

Public-source wording remains provider-neutral in ordinary Atlas copy unless provider attribution is legally/contractually required.

The owner can correct material discovered facts. Correcting a fact creates owner-authoritative context/profile data and must not mutate the historical observed evidence.

## Error and degraded-state behaviour

Google Place enrichment is useful but non-critical.

- Location resolution failure follows existing VS-21 behaviour.
- Place Details timeout/unavailability must not prevent Business creation if the core Business identity/profile is otherwise valid.
- Rich-field absence is not an error; unknown remains unknown.
- Provider quota/billing/configuration failure degrades to the existing discovery/context path.
- No Place enrichment error may expose API keys, provider internals or raw provider payloads.
- If enrichment is unavailable, progressive questions may reappear only through deterministic missing-context selection.
- If a conflict exists, Atlas may ask the owner to confirm the material fact instead of suppressing the question.

## Security and privacy

- No Google URL is fetched through a generic HTML scraper for enrichment.
- The existing Google Maps short-link resolver remains constrained by its current host/redirect/SSRF policy.
- Place Details requests use only canonical Place IDs produced by the trusted resolution path.
- API keys remain server-side configuration only.
- Field masks are explicit and bounded; wildcard masks are prohibited.
- Response size/time limits and JSON parsing bounds are required.
- Persist only product-relevant provider data; do not retain whole provider responses.
- Do not ingest end-customer personal data.
- Logs contain stable provider/result codes, never API keys or unrestricted raw payloads.

## Cost controls

Rich Places fields can move a request into higher Google Places SKUs. Therefore:

- use one Place Details enrichment call only after a unique Place is resolved;
- never call Place Details for every search candidate;
- request a minimal explicit field mask;
- do not request reviews/review summaries/photos/generative summaries by default;
- avoid a second call when the existing Text Search response already provided the required lower-tier field;
- make the enrichment field set centrally configurable/testable in code rather than scattered across endpoints;
- record provider call outcome/latency and enough cost-class metadata for pilot analysis without storing sensitive payloads;
- before production enablement, verify current Google pricing/quotas and accept expected pilot cost through normal governance.

## Persistence

Prefer extending the provenance/context model produced by VS-22 and the evidence structures delivered by VS-25 rather than introducing a separate Google-specific table.

If VS-25 introduces a generic observed-offering/media/evidence persistence model, VS-26 must integrate with that current model after VS-25 lands.

Only add a migration when the final post-VS-25 schema proves one is required.

Provider-specific JSON blobs are not the desired source of truth.

## Compatibility with VS-25

VS-25 owns Business Media & Menu Intelligence and is currently changing the public-discovery/domain persistence boundary.

VS-26 therefore:

- remains planning-only until VS-25 merges;
- must branch runtime work from the final VS-25-integrated `main`;
- must inspect the final VS-25 entity/reconciliation contracts rather than relying on pre-merge shapes;
- must reuse VS-25's canonical offering/media/evidence structures where relevant;
- must not duplicate menu/media ingestion;
- may use VS-25 menu/channel evidence to improve question suppression after integration.

## Testing strategy

### Provider adapter

- exact Place ID is used for Place Details;
- explicit field mask only;
- wildcard field mask rejected by test;
- only requested fields are mapped;
- missing fields map to unknown, not false;
- malformed/unexpected values safely ignored;
- timeout/HTTP failure degrades safely;
- API key never appears in logs/errors.

### Canonical mapping

- dine-in/takeout/delivery/reservable/meal flags map deterministically;
- false, true and absent remain distinct where provider semantics require it;
- no price-level-to-profitability inference;
- no category-to-Goal inference.

### Reconciliation

- owner-confirmed fact wins;
- matching public sources corroborate;
- conflicting public sources remain unresolved;
- unresolved material fact does not suppress its question;
- source-order and provenance remain stable.

### Question suppression

- strong unconflicted dine-in/takeaway/delivery evidence suppresses the restaurant service-channel question;
- Bolt/Wolt source presence contributes marketplace-channel evidence without exposing provider branding in canonical context;
- weak/unknown/conflicted facts do not suppress required questions;
- Goals are never auto-populated or suppressed by public data;
- no filler: sufficiently enriched Business may have zero optional context questions after Goals;
- five remains the maximum in degraded cases.

### Mobile/UX

- `About your business` is compact and absent when there is no meaningful enrichment;
- important facts only, no attribute wall;
- existing opening-hours presentation remains consistent;
- owner correction remains reachable;
- provider unavailability does not strand onboarding;
- screen-reader semantics, logical focus, dynamic type, ~44x44 targets, reduced motion and non-colour states pass;
- phone/tablet responsive containment matches the approved Atlas design baseline.

### Regression

Retain VS-21/VS-22 URL safety, location selection, multi-source reconciliation and Confirm-and-continue coverage; VS-17 progressive-question behaviour; VS-24 hero-journey readiness; and all final VS-25 media/menu tests.

## Acceptance criteria

VS-26 is complete only when:

1. Every uniquely resolved eligible Business can be enriched through the structured Place provider boundary without Maps HTML scraping.
2. The field mask is minimal, explicit, centrally controlled and current-pricing-reviewed.
3. Canonical operating facts retain provenance and do not become owner-confirmed automatically.
4. Strong unconflicted discovered facts suppress redundant onboarding questions deterministically.
5. Goals remain explicitly owner-selected.
6. A well-enriched restaurant can proceed with no redundant service-channel question.
7. Missing/conflicting evidence results in a useful owner question or safe unknown, never a fabricated fact.
8. Discover Business shows at most a compact high-value enrichment summary consistent with `ATLAS-DESIGN-001` v1.2.
9. Provider failure degrades safely and does not block valid Business creation.
10. Exact-head API/mobile/security/product gates, accessibility checks and relevant authentic runtime evidence pass.
11. No release, deployment or production enablement occurs without separate exact-SHA approval.

## Implementation sequencing

After VS-25 is merged and current governance is coherent:

1. Re-read `main`, VS-25 final contracts and current Google official API/pricing policy.
2. Activate VS-26 through PES with scope/policy/implementation records.
3. Use Superpowers writing-plans.
4. Implement provider contract + Place Details adapter under TDD.
5. Implement canonical attribute mapping/reconciliation under TDD.
6. Extend deterministic progressive-question eligibility/suppression under TDD.
7. Add bounded approved-design confirmation summary and mobile states.
8. Run PostgreSQL migration tests only if schema changes are actually required.
9. Run full deterministic regression, accessibility/responsive/runtime gates.
10. Certify exact head and stop at the human merge/release boundary.
