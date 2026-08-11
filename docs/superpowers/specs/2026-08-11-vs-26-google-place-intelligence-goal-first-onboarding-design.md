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

Atlas should avoid asking an owner factual operating questions that it can already discover confidently from a uniquely resolved public Business identity. After a Business is resolved to one canonical Google Place, Atlas may retrieve a bounded set of structured Google Place attributes, reconcile them with the existing website/Bolt/Wolt evidence graph, present the material operating facts compactly for owner confirmation, persist provenance, and suppress redundant follow-up questions.

The remaining onboarding should focus primarily on owner-only information, especially Business Goals, current priority, strategic constraints and desired outcomes.

## Approved approach

Three approaches were considered.

### A. Automatic structured Google enrichment after every unique Place resolution — APPROVED

Whenever Atlas resolves a Business to one unique Google Place, request a tightly bounded Place Details field set even when the owner originally pasted a website, Bolt or Wolt URL.

Advantages:
- maximises redundant-question suppression;
- creates one consistent enrichment path independent of source URL;
- reuses the existing canonical Place ID/location boundary;
- keeps Google data structured and provider-specific at the adapter boundary;
- improves category/context quality without requiring a Google URL.

Trade-off:
- richer fields can incur higher Google Places SKU cost, so the field mask must be deliberately minimal and independently testable.

### B. Enrich only when the owner provides a Google URL — REJECTED

Lower external cost, but website/Bolt/Wolt onboarding would continue asking questions Atlas can answer after location resolution.

### C. Do not use rich Google attributes — REJECTED

Lowest provider cost but lower coverage, more owner questioning and poorer cross-source corroboration.

## Decision delta from earlier Category Intelligence plan

The locked Category Intelligence foundation originally targeted 3–5 progressive questions after successful enrichment. The Product Owner has now approved a more aggressive evidence-aware model: when material public facts are discovered, shown and confirmed during Business confirmation, Atlas should not ask them again. A sufficiently enriched Business may therefore have zero optional non-goal context questions after Goals.

This is an intentional product-direction change, not an accidental implementation detail. When VS-26 activates, record it as a new typed decision in `delivery/decisions.json` after VS-25 has merged, rather than editing the shared decision registry while VS-25 is active.

The maximum of five optional context questions remains a degraded/missing-data safety ceiling, not a quota.

## Non-goals

VS-26 does not:

- scrape Google Maps HTML;
- ingest Google reviews/review text as authoritative Business facts;
- infer Goals from public data;
- infer sales, repeat rate, profitability, utilisation or customer demographics;
- introduce a general crawler;
- implement private Google Business Profile APIs;
- add private Bolt/Wolt/POS/reservation connectors;
- duplicate VS-25 media/menu ingestion;
- redesign the approved Atlas UI;
- change navigation architecture;
- deploy, release or production-enable Atlas.

## Approved design authority

All owner-facing work must preserve `ATLAS-DESIGN-001` v1.2.

The primary visual grammar remains the approved Atlas warm-neutral/deep-green system, shared tokens, existing `BrandMark` boundary, restrained cards/forms/buttons/depth, generous whitespace and low cognitive load. No new visual system, generic SaaS pattern or third-party branding may be introduced.

UI UX Pro Max may later be used only as a secondary product-workflow/accessibility aid. It may improve information architecture, form states, responsive behaviour and accessibility but may not override the approved Atlas visual grammar. Other installed design skills remain subordinate to the repository's conditional design-skill order and frozen baseline.

## Architecture

### 1. Place resolution remains the identity boundary

The existing `IBusinessLocationProvider` / `GoogleBusinessLocationProvider` path continues to resolve candidate Businesses and returns a canonical Google Place ID (`providerRef`) plus address, coordinates, country, timezone and type summary.

VS-26 must not create a second independent identity-matching system. Once one candidate is selected or uniquely preselected, that exact Place ID becomes the enrichment input.

### 2. Structured Place-enrichment provider port

Add a consuming-module-owned provider-neutral port, conceptually `IBusinessPlaceEnrichmentProvider`.

Input:
- canonical provider reference / Place ID;
- resolved Business/category context only where useful for selecting a bounded field set.

Output:
- a bounded provider-neutral `BusinessPlaceEnrichment` result containing structured attributes plus source metadata.

Google-specific HTTP, response shape and field names stay behind the adapter.

### 3. Place Details only; never Maps HTML scraping

The Google adapter calls Places API (New) Place Details using the resolved Place ID and an explicit `X-Goog-FieldMask`.

A field is requested only when it does at least one of these:

1. eliminates or simplifies an onboarding question;
2. materially improves Category/Context intelligence; or
3. supplies an owner-useful fact already appropriate for Business confirmation.

Initial restaurant/cafe candidate fields, subject to a fresh official pricing/terms check immediately before implementation, may include:

- `businessStatus`
- `primaryType`, `primaryTypeDisplayName`, `types`
- `regularOpeningHours` or `currentOpeningHours` only where they improve the existing hours fact
- `priceLevel` or `priceRange`
- `dineIn`
- `takeout`
- `delivery`
- `curbsidePickup` where relevant
- `reservable`
- `servesBreakfast`, `servesBrunch`, `servesLunch`, `servesDinner`
- `servesCoffee`, `servesDessert`, `servesVegetarianFood`
- `outdoorSeating` when useful

Do not request ratings, review counts, reviews, review summaries, generative summaries, photos or broad amenities merely because they exist.

The implementation plan must re-check current Google Places field availability, EEA terms and SKU billing from official Google documentation before locking the production field mask.

### 4. Provider-neutral canonical facts

Google attributes are translated into Atlas canonical facts/context rather than leaking Google terminology into Business logic.

Examples:

- `dineIn=true` -> service-channel candidate `Dine in`
- `takeout=true` -> service-channel candidate `Takeaway`
- `delivery=true` -> service-channel candidate `Delivery`
- confirmed Bolt/Wolt merchant source -> marketplace-channel candidate
- `reservable=true` -> reservation-capability candidate
- meal flags -> bounded service-period candidates
- `priceLevel` -> public price-position candidate, never profitability

Canonical values must be stable, provider-neutral and suitable for future non-Google adapters.

### 5. Provenance and confirmation states

Every accepted enrichment observation retains:

- provider/source identifier;
- canonical source identity / Place ID where provider policy permits;
- observed timestamp;
- confidence/evidence class;
- owner-confirmed state;
- reconciliation state where applicable.

External structured observations begin as `public-observed`; they never become `measured` merely because sources agree.

Material operating facts that Atlas intends to use to remove owner questions must be included in the compact Business confirmation experience. The existing owner `Confirm and continue` action confirms the material displayed facts as part of the Business setup decision. Historical public observations remain immutable provenance behind the resulting owner-confirmed canonical facts.

Hidden/non-material enrichment may remain `public-observed` for low-confidence intelligence support but must not silently become an owner-confirmed profile/context fact.

Provider attribution/display and storage requirements must be respected for Google-derived data.

## Reconciliation policy

VS-26 extends the deterministic VS-22 model.

1. Owner-confirmed facts are authoritative.
2. Existing primary-source precedence remains authoritative for ordinary source facts.
3. Google structured attributes may fill missing candidates and corroborate matching observations.
4. Google never silently overwrites an owner-confirmed or valid primary-source fact.
5. Agreement strengthens corroboration but not evidence class.
6. Material conflict preserves both provenance paths and requires owner resolution before the disputed fact becomes authoritative.
7. Weak, absent or contradictory evidence does not eliminate a material question merely to shorten onboarding.

## Goal-first onboarding and question suppression

### Current baseline

VS-17 suppresses a question when its target Context key is already owner-confirmed or the question was answered/skipped.

### VS-26 extension

Before Business confirmation, Atlas can use strong unconflicted public evidence to propose material operating facts in the confirmation summary. After the owner confirms that summary, those accepted canonical facts become eligible to suppress corresponding progressive questions.

A question is suppressed when its target is already satisfied by an owner-confirmed canonical fact produced from the confirmation flow or another existing owner-confirmed Context entry.

This deliberately preserves the PRD rule that publicly sourced Business data is labelled and owner-confirmed while still avoiding separate repetitive questions.

Example: if Place enrichment plus supplied marketplace evidence establish Dine-in / Takeaway / Delivery and those channels are shown in the confirmation summary, one `Confirm and continue` action confirms them. Atlas must then not ask `restaurant-cafe.service-channel` again.

If public evidence establishes only part of a material fact, is conflicted, or was not shown/confirmed, the question remains eligible when the active Knowledge Pack actually needs it. Do not ask it merely to reach a question-count target.

### Goals remain owner-only

Atlas must never infer Business Goals from Google, Bolt, Wolt, websites, menus, reviews or category stereotypes.

After Business confirmation, the onboarding should preferentially move to Goals whenever factual setup is sufficiently covered.

Owner-only/high-value inputs include:

- primary Business Goal;
- goal priority/order;
- current near-term priority;
- material constraint where it changes feasible recommendations;
- desired outcome/time horizon where supported by the product model;
- genuinely missing context required by a current opportunity rule.

### Target volume

For a well-enriched, owner-confirmed Business:

- Goals remain required owner input for readiness;
- optional non-goal context target: 0–3;
- maximum optional context questions: 5 in degraded/missing-data cases;
- no filler questions.

## Owner-facing confirmation UX

The Discover/Confirm Business workflow remains structurally aligned with the approved Atlas Business setup experience. Do not create a dashboard or attribute wall.

A compact optional `About your business` summary may surface only the most useful confirmation facts, for example:

- category/cuisine/type summary;
- Dine-in / Takeaway / Delivery;
- reservation capability when relevant;
- understandable price-position indicator if available;
- opening-hours summary using the existing hours presentation convention.

Display roughly 3–5 concise items/groups at most before progressive disclosure. Ratings, review counts, payment methods and broad Place attributes are not dumped onto this screen.

The confirmation copy must make clear that the owner is confirming discovered Business details. Material facts remain editable/correctable. Corrections create owner-authoritative canonical values without mutating the historical observed evidence.

Public-source wording stays provider-neutral in ordinary Atlas copy unless provider attribution is contractually required.

## Error and degraded-state behaviour

Google Place enrichment is useful but non-critical.

- Location resolution failure follows existing VS-21 behaviour.
- Place Details timeout/unavailability does not prevent Business creation when core identity/profile is otherwise valid.
- Missing rich fields mean unknown, not false.
- Provider quota/billing/configuration failure degrades to the existing discovery/context path.
- Provider internals, API keys and raw payloads are never shown.
- When enrichment is unavailable, deterministic missing-context selection may ask the relevant question later.
- Conflicted material facts require owner confirmation rather than silent suppression.

## Security and privacy

- No Google Maps HTML scraping for enrichment.
- Existing Google short-link resolution remains within current host/redirect/SSRF policy.
- Place Details uses only canonical Place IDs from the trusted resolution path.
- API keys remain server-side.
- Explicit bounded field masks only; wildcard masks are prohibited.
- Response size/time and JSON parsing bounds are required.
- Persist product-relevant normalized facts, not whole provider responses.
- Do not ingest end-customer personal data.
- Logs use stable provider/result codes and never API keys/unrestricted payloads.

## Cost controls

Rich Places fields can move requests into higher billing SKUs, so VS-26 must:

- make at most one rich Place Details enrichment request after unique Place resolution;
- never call Place Details for every search candidate;
- request a minimal explicit field mask;
- avoid reviews/review summaries/photos/generative summaries by default;
- avoid a second request where the existing Text Search response already provides the required lower-tier field;
- centralize and test the field set;
- capture provider outcome/latency and sufficient cost-class telemetry for pilot analysis without storing sensitive payloads;
- verify current Google pricing/quotas before production enablement through normal governance.

## Persistence

Prefer extending the final post-VS-25 provenance/context/evidence model rather than introducing a Google-specific table.

If VS-25 introduces generic observed-offering/media/evidence entities, VS-26 must integrate with those final contracts after merge. Add a migration only if the post-VS-25 schema genuinely requires one.

Provider-specific JSON blobs are not the desired source of truth.

## Compatibility with VS-25

VS-25 owns Business Media & Menu Intelligence and is currently changing the public-discovery/domain persistence boundary.

VS-26 therefore:

- remains planning-only until VS-25 merges;
- branches runtime work from final VS-25-integrated `main`;
- re-inspects final VS-25 entity/reconciliation contracts;
- reuses VS-25 canonical offering/media/evidence structures where relevant;
- never duplicates menu/media ingestion;
- may use final VS-25 menu/channel evidence to improve the confirmation summary and subsequent question suppression.

## Testing strategy

### Provider adapter

- exact Place ID used for Place Details;
- explicit field mask only;
- wildcard field mask rejected;
- missing fields map to unknown, not false;
- malformed values safely ignored;
- timeout/HTTP failure degrades safely;
- API key absent from logs/errors.

### Canonical mapping

- dine-in/takeout/delivery/reservable/meal flags map deterministically;
- true/false/absent remain distinct where provider semantics require it;
- no price-to-profitability inference;
- no category-to-Goal inference.

### Confirmation and reconciliation

- owner-confirmed fact wins;
- matching public sources corroborate;
- conflicting public sources remain unresolved until owner decision;
- only displayed/accepted material facts become owner-confirmed through Business confirmation;
- hidden public observations remain public-observed;
- historical provenance survives owner correction.

### Question suppression

- confirmed Dine-in / Takeaway / Delivery facts suppress the restaurant service-channel question;
- confirmed marketplace source contributes a provider-neutral marketplace channel;
- unconfirmed/weak/unknown/conflicted facts do not suppress material questions;
- Goals are never auto-populated from public data;
- enriched Business may have zero optional non-goal questions after Goals;
- five remains the degraded-case ceiling.

### Mobile/UX

- `About your business` is compact and absent when no meaningful enrichment exists;
- confirmation makes owner acceptance clear;
- existing opening-hours presentation remains consistent;
- correction remains reachable;
- provider failure does not strand onboarding;
- screen-reader semantics, focus order, dynamic type, ~44x44 targets, reduced motion and non-colour states pass;
- phone/tablet containment matches `ATLAS-DESIGN-001` v1.2.

### Regression

Retain VS-21/VS-22 URL safety, location selection, multi-source reconciliation and Confirm-and-continue coverage; VS-17 progressive-question behaviour; VS-24 hero-journey readiness; and all final VS-25 media/menu tests.

## Acceptance criteria

VS-26 is complete only when:

1. Every uniquely resolved eligible Business can be enriched through structured Place Details without Maps HTML scraping.
2. The field mask is minimal, explicit, centrally controlled and current-pricing-reviewed.
3. Canonical public observations retain provenance and never become measured automatically.
4. Material discovered facts are shown compactly and owner-confirmed before they become authoritative suppression context.
5. Confirmed strong facts suppress redundant onboarding questions deterministically.
6. Goals remain explicitly owner-selected.
7. A well-enriched restaurant does not receive a redundant service-channel question.
8. Missing/conflicting evidence results in owner confirmation or safe unknown, never fabricated fact.
9. Discover Business preserves the approved design and shows only a compact high-value summary.
10. Provider failure degrades safely without blocking valid Business creation.
11. Exact-head API/mobile/security/product, accessibility and relevant authentic runtime gates pass.
12. No release/deployment/production enablement occurs without separate exact-SHA approval.

## Implementation sequencing

After VS-25 is merged and current governance is coherent:

1. Re-read `main`, final VS-25 contracts and current official Google API/pricing policy.
2. Record the approved question-volume/goal-first direction as a new typed decision.
3. Activate VS-26 through PES with scope/policy/implementation records.
4. Invoke Superpowers writing-plans.
5. Implement provider contract + Place Details adapter under TDD.
6. Implement canonical mapping/reconciliation + confirmation promotion under TDD.
7. Extend deterministic progressive-question eligibility/suppression under TDD.
8. Add the bounded approved-design confirmation summary and mobile states.
9. Add a migration only if the post-VS-25 schema requires it.
10. Run full regression, accessibility/responsive/runtime gates.
11. Certify exact head and stop at the human merge/release boundary.
