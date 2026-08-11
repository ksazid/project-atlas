# VS-29 — Business Intelligence Enrichment & Smart Onboarding

**Status:** Design specification for Product Owner review  
**Date:** 2026-08-12  
**Implementation mode while VS-28 is active:** specification-only  
**Authoritative requirements:** FR-02, FR-03, FR-05, FR-16  
**Depends on:** ATLAS-PRD-001, ATLAS-TRD-001, DEC-04, DEC-05, DEC-07, DEC-08, certified VS-25, VS-26, VS-27  
**Delivery sequencing:** runtime implementation must start from the current `main` only after VS-28 has cleared its PES/runtime work, or after an explicit human decision that VS-28 will not proceed.

## 1. Problem

Atlas can already discover a Business from public URLs, resolve a location, transiently enrich a selected Google Place, persist remote media references and menu offerings, and present a read-first Business Hub. Recent Expo tests exposed four gaps that belong in one bounded follow-up because they share the same onboarding evidence flow:

1. **Provider/page-shape coverage is uneven.** Habibi's Kebab Sliema currently yields rich Bolt media/menu intelligence, while Chickn Bites and McDonald's Birkirkara demonstrate page variants where media/menu extraction is incomplete or empty.
2. **Public contact/presence facts are not complete end-to-end.** Atlas should prefill website, business phone, public business email, social channels, address and opening hours when trustworthy public sources expose them, while preserving provenance and owner authority.
3. **Confirmed enrichment can still lead to repetitive onboarding questions.** The current progressive-question selector suppresses only exact target keys; `operatingchannels` can therefore coexist with a later `primarychannels` question that feels duplicative during initial setup.
4. **Expo test switching is awkward.** Production correctly enforces one primary Business per owner, but the Development demo account currently surfaces a raw "account already owns a Business" error when testing another discovered Business.

VS-29 fixes these gaps without changing Atlas navigation, Today logic, Knowledge Pack architecture, production ownership rules or provider safety boundaries.

## 2. Design choice

### Chosen approach — one normalized enrichment pipeline with explicit owner confirmation

Each accepted public source contributes normalized, provenance-rich candidate evidence. Provider adapters remain responsible only for extracting what their already-authorized source exposes. A reconciliation layer decides which evidence is safe to show for review. Owner-confirmed profile/context values remain authoritative.

The onboarding sequence becomes:

```text
Accepted source URL(s)
  -> bounded provider/public extraction
  -> normalized facts + media + offerings
  -> exact location resolution
  -> optional transient Google Place enrichment
  -> reconcile evidence by identity/confidence/provenance
  -> owner reviews only useful discovered values
  -> explicit confirmation
  -> persist canonical Business/Profile/Context + public evidence
  -> goals first
  -> ask only materially missing progressive questions
```

### Alternatives considered

**A. Bolt-specific patches only.** Fastest for the current screenshots but would create repeated special cases and would not solve website/email/social enrichment or question suppression. Rejected.

**B. Let each provider write Business Profile fields directly.** Simpler adapters but would blur provider policy, overwrite precedence and owner-confirmation rules. Rejected.

**C. Re-run scraping from the Business Hub whenever data is missing.** Would make read screens slow/non-deterministic, duplicate provider work and weaken the governed discovery boundary. Rejected.

## 3. Source and policy boundaries

### 3.1 Non-Google public sources

Existing HTTPS/SSRF protections remain mandatory. Atlas may retain normalized public-observed facts, provenance-rich remote media references and structured offerings from already-authorized public business sources according to DEC-04 and DEC-07.

Supported extraction signals may include:

- schema.org/JSON-LD LocalBusiness/Restaurant fields;
- safe OpenGraph/meta fallbacks;
- public semantic provider markup already returned in the accepted HTML;
- public `tel:` and `mailto:` links where they clearly belong to the Business;
- schema.org `sameAs` links for recognized social networks;
- a bounded official-website URL exposed by the accepted source.

Atlas must not add authenticated scraping, bot bypass, private marketplace/POS APIs, production browser automation, bulk crawling or third-party image rehosting.

### 3.2 Bounded official-website enrichment

When an accepted source exposes a credible official HTTPS website and the Business identity strongly matches the current discovery, Atlas may perform at most one additional SSRF-safe public website fetch during discovery/enrichment. The website fetch may contribute profile/contact facts, social links, business images and structured offerings when exposed through the same conservative public mechanisms.

The one-hop website enrichment must fail closed when identity is ambiguous, the URL is unsafe, the host resolves to blocked/private addresses, redirects violate the existing policy, or the response exceeds existing public-source limits.

No site-wide crawl is introduced.

### 3.3 Google Places

DEC-08 remains authoritative. Google Place rich content is transient for the current owner interaction; Atlas retains the Place ID as Google provider content and persists only canonical values the owner explicitly confirms through Atlas.

VS-29 may extend the bounded Place Details field set for contact/presence values only after implementation-time verification against current official Google Maps Platform/EEA terms and attribution rules. The design does **not** authorize persistence of copied Google payloads, photos, attribution payloads or hidden Google-derived facts.

## 4. Normalized enrichment model

VS-29 does not require a new database table merely to represent these fields. The current discovery/profile/context models already support the needed persisted shapes.

The normalized enrichment result should expose four groups:

### 4.1 Profile facts

Candidate keys:

- `name`
- `description`
- `website`
- `phone`
- `email`
- `socialChannels`
- `primaryLocation`
- `country`
- `openingHours`
- category/subcategory and existing market metadata

Every public candidate retains source URL, provider, observed time, confidence and evidence class.

`BusinessProfile` already has Website, Phone, Email, SocialChannels and BusinessHours fields. VS-29 extends discovery confirmation so discovered email/social values can reach the existing profile instead of remaining manual-only.

### 4.2 Operating context

Canonical owner-confirmable values continue to include:

- operating channels;
- reservation capability;
- service periods;
- price position.

Confirmed opening hours must also reach the canonical Business Profile `BusinessHours` value. The confirmation UI must never imply that hours will be saved when the create request discards them.

### 4.3 Media

Continue the DEC-07 model:

- remote HTTPS references only;
- `business-image` for Business-level images;
- `menu-item-image` for offering imagery;
- full source/provenance;
- no binary copying/rehosting.

Business Hub continues to display `business-image`; menu-item imagery can remain associated with menu intelligence and does not need to become a Business hero image.

### 4.4 Offerings

Continue the provider-neutral `BusinessOffering` model with `menu-item` as the initial offering kind. Preserve section, item name, description, price/currency, provenance, observed time, confidence and source order.

## 5. Extraction hardening

The current generic extractor already handles structured website/phone/opening-hours signals and the current media/menu extractor handles JSON-LD plus one Bolt semantic shape. VS-29 strengthens these boundaries instead of adding an unrelated scraper.

### 5.1 Bolt regression coverage

Use deterministic fixtures representing at least:

- a rich page shape equivalent to Habibi's Kebab Sliema;
- the Chickn Bites page shape that previously produced no media/menu;
- the McDonald's Birkirkara page shape that produced a Business image but no menu.

The implementation must first diagnose the public HTML shape before changing parsing rules. If the public response genuinely does not expose menu data, Atlas must return an honest no-menu state rather than introducing a private API/browser workaround.

Any new Bolt fallback must be bounded to stable public semantic markers/data already present in the accepted HTML and must fail closed when those markers are absent.

### 5.2 Generic contact/presence extraction

Extend conservative extraction to support:

- JSON-LD `email`;
- JSON-LD `sameAs` recognized social URLs;
- safe `mailto:` business email fallback;
- safe `tel:` business phone fallback when structured phone is absent;
- canonical/official website evidence when clearly exposed;
- existing opening-hours/address/description fields.

Do not infer private/personal contact details. Only public Business-level contact endpoints are eligible.

### 5.3 Reconciliation and precedence

For persisted/confirmed Business state, precedence is:

1. existing owner-confirmed value;
2. owner-confirmed value from the current review;
3. high-confidence strongly matched public evidence;
4. medium/low public evidence shown only for review or omitted when ambiguous.

Public refresh must never silently overwrite an owner-confirmed value.

Multiple public sources may support the same candidate value. Conflicting values must retain provenance and require owner review rather than arbitrary last-write-wins behavior.

## 6. Smart onboarding question suppression

### 6.1 Problem in the current selector

The progressive-question catalogue suppresses questions by exact `TargetContextKey`. Confirmed Google operating channels are persisted as `operatingchannels`, while the restaurant question "How do most customers order from you?" targets `primarychannels`. Both can therefore appear in the same setup journey.

### 6.2 Semantic satisfaction policy

Introduce a small deterministic `ProgressiveQuestionSatisfactionPolicy` (name illustrative) that determines whether existing **owner-confirmed** evidence materially satisfies an onboarding question even when the storage keys differ.

Initial rule:

- owner-confirmed non-empty `operatingchannels` satisfies/suppresses the initial restaurant `primarychannels` setup question.

This is an onboarding simplification, not a claim that "available channels" and "dominant order channel" are analytically identical. If a future Opportunity genuinely needs channel dominance, Atlas may ask a distinct, material, contextual follow-up question later; it must not re-use the generic setup question merely because the data is absent.

Do **not** suppress genuinely different facts:

- opening hours do not answer busy periods;
- service periods do not answer demand peaks;
- public category/menu data do not answer owner constraints;
- public profile data do not answer customer groups or current priorities;
- goals remain owner-only.

DEC-05 and DEC-08 remain intact: goals first, then 0–3 useful optional questions for sufficiently enriched Businesses, with degraded/missing-context cases bounded by the existing ceiling.

## 7. Owner confirmation UX

The existing discovery confirmation remains one review flow rather than becoming a second settings form.

### 7.1 Discovered facts

Prefill fields when reliable evidence exists and label them by source. Empty/unavailable values remain editable but are not presented as failures.

Relevant review values include:

- website;
- phone;
- public business email;
- social channels;
- address;
- opening hours;
- operating channels/reservations/service periods/price position;
- media/menu summary where available.

The owner can edit/correct profile values before creating the Business.

### 7.2 Confirmation semantics

Copy must clearly distinguish:

- **Observed publicly** — evidence Atlas found;
- **Owner confirmed** — canonical value Atlas may use as authoritative Business context/profile.

A confirmation control must persist every value it claims to confirm. No field may appear inside a "Confirm these details" group while being discarded by the request/persistence path.

## 8. Development-only reset-and-switch flow

The production one-primary-Business-per-owner guard remains unchanged.

The existing Development-only `/api/v1/dev/reset-business` endpoint remains the reset primitive and continues to require:

- ASP.NET Core Development environment;
- exact Expo demo subject `atlas-expo-go-demo-owner`;
- BusinessOwner authorization.

When the Expo demo user confirms a newly discovered Business while already owning one, the mobile UI should handle the stable ownership-conflict error and present a test-only choice such as:

```text
You're currently testing Chickn Bites.

[View Chickn Bites]
[Reset and use Habibi's Kebab Sliema]
```

`Reset and use…` must:

1. require an explicit user tap;
2. call the existing guarded reset endpoint;
3. clear the local selected Business ID/state;
4. preserve the current unconsumed discovery snapshot when safe;
5. retry the create-from-discovery operation for the selected candidate;
6. continue into goals-first onboarding.

This path must not be reachable in production builds/environments and must not weaken the server-side ownership invariant.

## 9. API and code boundaries

Expected implementation areas after VS-28 clears:

### API

- `BusinessDiscovery.cs` — generic public contact/presence extraction and normalized facts;
- `BusinessDiscoveryMediaMenu.cs` — diagnosed provider/media/menu fallback hardening;
- `MultiSourceBusinessDiscoveryService.cs` / reconciliation boundary — bounded official-site contribution and source precedence as needed;
- `BusinessDiscoveryPersistence.cs` — carry email/social/hours and confirmed enrichment into existing profile/context models;
- `BusinessPlaceEnrichment.cs` and endpoints — only if current Google policy review permits extra transient contact/presence fields;
- `ProgressiveQuestions.cs` — semantic satisfaction policy;
- existing Development reset endpoint — reuse rather than duplicate.

### Mobile

- `create-business.tsx` — enriched review fields, correct confirmation semantics, test-only ownership-conflict switch flow;
- discovery/place-enrichment models and API types;
- no tab/navigation restructuring;
- adapt to the post-VS-28 shared shell rather than editing around it.

### Persistence

No migration is expected solely for website/phone/email/social/hours because the current Business Profile model already contains those fields. If implementation discovers a genuine persistence requirement not covered by existing models, stop and route that change through PES rather than adding schema opportunistically.

## 10. Error and degraded states

- Provider unavailable/unsupported detail: keep the usable discovered facts and let setup continue.
- Official website enrichment failure: non-blocking; do not invalidate the anchor source.
- Ambiguous identity across sources: do not merge the conflicting evidence.
- No public menu/media exposed: show honest empty state; do not fabricate or bypass provider boundaries.
- Google transient enrichment unavailable: continue with non-Google evidence/manual review.
- Existing Business in normal production flow: preserve the current ownership guard and user-safe error.
- Existing Business in Development Expo demo flow: offer View current or explicit Reset and use new candidate.
- Reset failure: retain current Business selection and do not attempt the new create.

## 11. Testing strategy

TDD is required for parser variants, reconciliation, owner-precedence rules, question suppression and the Development reset/switch flow.

### Deterministic API tests

- generic JSON-LD email/social extraction;
- `mailto:` / `tel:` bounded fallbacks;
- safe one-hop official website identity/SSRF/redirect limits;
- Bolt rich fixture continues to extract media/menu;
- Chickn Bites regression fixture;
- McDonald's Birkirkara regression fixture;
- remote media dedupe and limits remain enforced;
- offering dedupe/limits remain enforced;
- discovered email/social/hours persist through owner confirmation;
- existing owner-confirmed values are not silently overwritten;
- `operatingchannels` suppresses the initial restaurant `primarychannels` question;
- unrelated questions remain eligible;
- Development reset endpoint remains inaccessible for non-demo subjects and outside Development.

### Mobile tests

- enriched website/phone/email/social/hours render when available;
- absent fields do not create empty noisy cards;
- confirmation copy matches what is persisted;
- redundant restaurant channel question is absent after confirmed operating-channel enrichment;
- ownership conflict in test mode shows View current + Reset and use selected Business;
- production/non-demo path never shows the reset action;
- reset failure is recoverable and does not lose the current Business.

### Live supplemental smoke

After deterministic gates are green, optional live public-source smoke may validate current provider behavior for Habibi's, Chickn Bites and McDonald's. Live provider output is supplemental evidence only and must not replace deterministic fixtures or become a merge dependency when a provider changes externally.

## 12. Security, privacy and trust

- retain Business-level public data only; no end-customer personal data;
- preserve SSRF-safe fetching and response limits;
- never log private provider credentials or copied Google payloads;
- public facts retain provenance/confidence;
- owner-confirmed data remains distinguishable from public-observed data;
- no silent overwrite of owner authority;
- no private APIs, authenticated scraping or anti-bot bypass;
- no third-party image binary storage;
- Development reset remains fail-closed and exact-subject-gated.

## 13. VS-28 conflict boundary

VS-28 is currently working on device-adaptive mobile shell/navigation geometry/material/motion. VS-29 must not compete for that active runtime surface.

Before runtime activation:

1. fetch the latest `main` and VS-28 state;
2. require VS-28 to be merged/certified or explicitly deferred by the Product Owner;
3. create a fresh VS-29 implementation branch from that current `main`;
4. preserve VS-28 shell primitives and adapt VS-29 screens to them;
5. run a changed-file conflict scan before touching `create-business.tsx` or shared mobile primitives;
6. activate VS-29 through PES only after this sequencing check.

The present branch must not modify `delivery/current-slice.json`, application runtime code or deployment branches.

## 14. Success definition

VS-29 succeeds when:

- Atlas extracts the richest safe public Business intelligence available from an accepted source without provider-specific leakage into the core;
- website, phone, public business email, social channels, address and hours arrive prefilled when reliable evidence exists;
- media/menu intelligence is robust across diagnosed public Bolt page variants or degrades honestly when the data is not publicly present;
- explicitly confirmed operating facts persist exactly as presented;
- onboarding does not ask the owner for materially duplicated information Atlas has already received and they confirmed;
- the one-Business production rule remains intact;
- the Expo demo user can reset/switch Businesses safely for repeat testing;
- VS-28 presentation work is not overwritten;
- deterministic CI/security/product gates pass before certification;
- no production release/deployment is implied or authorized.

## 15. Non-goals

VS-29 does not add:

- multi-Business production accounts;
- production Business deletion UX;
- private marketplace/POS integrations;
- headless browser scraping in production;
- social login or social-network API ingestion;
- autonomous website crawling;
- Google photo persistence;
- third-party image rehosting;
- navigation redesign;
- Today/Opportunity generation changes;
- production deployment or release authorization.
