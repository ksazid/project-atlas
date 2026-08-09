# VS-17 — Progressive Business Questions

Status: Approved design candidate
Date: 2026-08-10
Product authority: ATLAS-PRD-001, ATLAS-TRD-001, ATLAS-DESIGN-001, Category Intelligence Foundation locked direction
Depends on: VS-16 merged to `main` at `4febc069a796ebcc7cdc871629695ab7631bb71c`

## Outcome

VS-17 adds a lightweight, resumable intelligence-enrichment step immediately after Business creation:

URL discovery/manual setup → owner confirms Business → Business is created → Atlas asks 3–5 highest-value missing questions → owner answers or skips → owner reaches Today/first-value handoff.

The questions improve Business Context without becoming a new onboarding blocker. Every question is skippable. A skipped answer remains unknown and reduces evidence coverage/confidence until Atlas has a justified reason to ask again later.

VS-17 does not implement Knowledge Pack Schema v2, Restaurant/Cafe opportunity recipes, category-specific metrics or the first category-aware Opportunity. Those remain later slices.

## Approved product decisions

1. Questions are optional and skippable.
2. Question selection is deterministic and versioned; AI does not invent onboarding questions.
3. Questions are category-aware and selected only from facts/context Atlas still does not know reliably.
4. UI is tap-first and one-question-at-a-time, using single/multi-select controls where possible and short free text only when necessary.
5. Business is created before VS-17 begins, so answers persist directly against a real Business and onboarding can resume safely.
6. All eight initial category families receive lightweight category-specific question overrides on top of a shared generic core.
7. A skipped question is suppressed for its current catalogue/version and may reappear later only when the missing answer becomes materially valuable to a recommendation or data-coverage decision.

## Approaches considered

### A. AI-generated onboarding questions

Generate questions dynamically from public facts and category context.

Rejected because question quality, relevance and wording would be non-deterministic; runtime tests would be brittle; provenance and schema evolution would be harder to govern; and onboarding should not depend on model availability.

### B. Static questionnaire per category

Show the same fixed 3–5 questions for every Business in a category.

Rejected because it re-asks facts Atlas may already know, undermining the URL-first onboarding promise and increasing completion time.

### C. Deterministic versioned catalogue with relevance selection — chosen

Maintain a versioned catalogue of generic and lightweight category questions. Select only unanswered high-value items based on canonical Business/Profile/Context/public discovery evidence and previous onboarding progress.

This preserves predictable UX, testability, provenance and global/category extensibility while keeping VS-17 bounded.

## Scope

### In scope

- a versioned progressive-question catalogue;
- shared generic questions plus lightweight overrides for all eight initial category families;
- deterministic question eligibility/ranking;
- 3–5 questions maximum for first-run enrichment;
- suppression of questions whose target context is already known reliably;
- one-question-at-a-time mobile flow with progress (`2 of 4`);
- tap-first single choice and multi-select controls;
- short free-text input only for questions that cannot be represented safely by bounded choices;
- `Skip for now` on every question;
- resumable progress after app interruption;
- answered values persisted as owner-confirmed Business Context;
- skipped/completed question status persisted separately from Business facts;
- reopening a skipped question later only through an explicit material-value policy;
- generic fallback for unsupported/unknown categories;
- accessibility, reduced motion, offline/error/retry and small-screen behavior;
- authentic Expo Web runtime evidence for the hero flow.

### Out of scope

- AI-authored onboarding questions;
- Knowledge Pack Schema v2;
- Restaurant/Cafe metric engine or opportunity recipes;
- generated first category-aware Opportunity;
- private provider/POS/reservation/accounting connectors;
- long-form onboarding surveys;
- mandatory completion of optional questions;
- changing VS-16 Business creation/provenance semantics;
- navigation redesign;
- release, deployment or production enablement.

## Initial category families

The catalogue supports:

1. Restaurant & Cafe
2. Beauty & Personal Care
3. Retail
4. Ecommerce
5. Home & Local Services
6. Professional Services
7. Fitness & Wellness
8. Hospitality / Accommodation
9. Generic Business fallback

Category-specific coverage in VS-17 is deliberately lightweight. It captures broadly useful operating context; it does not encode category intelligence rules that belong in Knowledge Pack v2.

## Question catalogue model

The server owns the catalogue definition. A catalogue has:

- `catalogueKey` — stable family key, e.g. `progressive-onboarding`;
- `version` — immutable integer/string version;
- `questions[]` — deterministic ordered definitions.

Each question definition contains:

- `questionKey` — immutable stable identifier;
- `targetContextKey` — canonical Business Context key written when answered;
- `appliesToCategories[]` — generic or one/more canonical category keys;
- optional `appliesToSubcategories[]` for future compatibility, unused unless explicitly populated;
- `priority` — deterministic ranking weight;
- `prompt` — concise owner-facing question;
- optional `helper` — why Atlas is asking;
- `answerType` — `single-choice`, `multi-choice`, or `short-text`;
- `options[]` for bounded choice types;
- `maxSelections` where relevant;
- `maxLength` for text;
- `materialityTags[]` — later policy hooks such as demand, capacity, customer, channel, constraint;
- `suppressWhenKnown` — normally true.

Published catalogue versions are immutable. A new wording/option/eligibility change creates a new catalogue version rather than mutating history.

## Question content principles

Questions must be:

- directly useful to future decision quality;
- answerable quickly by an owner without analysis;
- category-aware but not category-intelligence logic;
- business-level only, with no end-customer personal data;
- evidence-aware and safe to leave unknown;
- concise enough for one mobile screen;
- free of guaranteed-growth language.

Examples of generic high-value targets:

- immediate operating constraint;
- busiest demand period/pattern;
- primary customer group at a non-personal aggregate level;
- primary sales/service channels;
- near-term business priority beyond saved Goals where materially distinct.

Category overrides may replace wording/options or add higher-priority questions. Example Restaurant/Cafe candidates include dominant service channels (dine-in/takeaway/delivery), busiest service period and main operational constraint. They do not ask for unsupported revenue, margin or repeat-rate estimates.

## Deterministic selection algorithm

For a Business and catalogue version:

1. load canonical Business fields, Profile, Business Context and retained public/discovery provenance;
2. load onboarding progress for the current catalogue/version;
3. resolve canonical category/subcategory;
4. form candidate set from generic questions plus matching category overrides;
5. suppress a question when its target context key already has a trustworthy owner-confirmed or accepted canonical value;
6. suppress questions already answered for that catalogue lineage where their target context remains present;
7. suppress questions skipped in the current catalogue/version during onboarding;
8. rank remaining candidates by deterministic priority, category specificity and stable question key tie-break;
9. return at most five and at least three when that many useful candidates exist;
10. if fewer than three useful unknowns remain, return only the useful questions—never create filler.

If zero useful questions remain, onboarding enrichment is considered complete and the owner proceeds immediately.

No LLM/model call is allowed in selection.

## Known-context rule

VS-17 must not re-ask information Atlas already knows reliably. Suppression sources include:

- required canonical Business fields from VS-16;
- owner-confirmed Profile values;
- owner-confirmed Business Context;
- public values accepted by the owner and persisted as canonical/provenance-backed data;
- previous VS-17 answered context whose current value still exists.

Raw unconfirmed public observations do not count as authoritative Business Context and cannot by themselves suppress a question that requires owner knowledge unless the question explicitly targets confirmation already handled by VS-16.

## Persistence model

### Business facts

Answered questions use the existing `BusinessContextEntry` model as the canonical answer store:

- `Key = targetContextKey`;
- `Value = normalized owner answer`;
- `Source = owner`;
- `OwnerConfirmed = true`;
- `UpdatedAt = server timestamp`.

VS-17 must not create a second facts table containing duplicate authoritative Business answers.

### Onboarding progress

Add a dedicated progress record because `BusinessContextEntry` cannot represent skipped state or catalogue/question version safely.

Recommended entity: `BusinessQuestionProgress`:

- `Id`;
- `BusinessId`;
- `CatalogueKey`;
- `CatalogueVersion`;
- `QuestionKey`;
- `Status` — `answered` or `skipped`;
- `AnsweredContextKey` nullable for skipped records;
- `CompletedAt`;
- optional row/concurrency version if the existing persistence pattern warrants it.

Unique key: `(BusinessId, CatalogueKey, CatalogueVersion, QuestionKey)`.

Answered progress references the Business Context key conceptually rather than duplicating the value.

## Skip and re-ask policy

`Skip for now`:

- records `skipped` for the exact catalogue version/question;
- immediately moves to the next selected question;
- does not create an empty/fake Business Context value;
- never blocks completion;
- is not treated as negative feedback or a refusal to share data globally.

During the same onboarding run/version, skipped questions remain suppressed.

A later product surface may re-surface a skipped question only if:

- the current intelligence/opportunity/data-coverage workflow explicitly declares that context materially valuable;
- the owner has not subsequently answered equivalent context;
- the UI explains why the answer would help;
- skipping remains available unless a future separately approved requirement makes the data mandatory.

VS-17 records the metadata needed for this future behavior but does not implement recommendation-triggered re-asking beyond basic resume semantics.

## API design

Recommended bounded endpoints:

### `GET /api/v1/businesses/{businessId}/progressive-questions`

Returns the current catalogue/version and selected unanswered questions for the authenticated Business owner.

Response shape conceptually:

```json
{
  "catalogueKey": "progressive-onboarding",
  "catalogueVersion": "1",
  "questions": [
    {
      "questionKey": "generic.primary-channel",
      "targetContextKey": "primarychannels",
      "prompt": "How do customers usually buy from you?",
      "helper": "This helps Atlas avoid suggesting actions that do not fit how you operate.",
      "answerType": "multi-choice",
      "options": ["In person", "Phone/message", "Own website/app", "Marketplace/platform"],
      "maxSelections": 3
    }
  ]
}
```

The endpoint is Business-owner isolated and returns only currently eligible questions, never the whole internal catalogue.

### `POST /api/v1/businesses/{businessId}/progressive-questions/{questionKey}/answer`

Input contains `catalogueVersion` and normalized answer payload. Server:

- verifies Business ownership;
- resolves the exact server-owned catalogue definition;
- rejects unknown/stale question/version combinations safely;
- validates allowed choice values/count/length;
- persists/updates the target `BusinessContextEntry` as owner-confirmed;
- records `answered` progress;
- writes audit;
- returns updated progress/next-state information.

### `POST /api/v1/businesses/{businessId}/progressive-questions/{questionKey}/skip`

Server verifies the same catalogue/question ownership/eligibility boundary, records `skipped` progress and returns updated progress/next-state information.

The client never submits arbitrary target context keys or arbitrary catalogue definitions.

## Mobile flow

VS-17 starts after VS-16/manual Business creation has succeeded and the authenticated session already contains `businessId`.

### Entry

Route to a dedicated progressive-question onboarding screen before the normal Today handoff when the current catalogue has eligible questions.

If the API returns zero questions, route directly to the existing authenticated owner journey.

### Question screen

One question per screen:

- Atlas/BrandMark visual continuity;
- small eyebrow such as `A LITTLE MORE CONTEXT`;
- progress `2 of 4`;
- one clear question and optional one-line helper;
- tap-first answer controls;
- one dominant `Continue` action where an explicit continue is necessary;
- `Skip for now` secondary action always available;
- no dense form, analytics or competing primary actions.

Single choice can advance immediately after a deliberate selection only if this remains accessible and reversible; otherwise use a Continue action. Multi-choice uses bounded chips/cards plus Continue. Short text uses one compact field plus Continue.

### Completion

After all selected questions are answered/skipped:

- show a brief non-gamified completion state such as `That’s enough to get started.`;
- explain that Atlas can learn more later;
- continue to Today/existing owner journey.

No promise of an immediate generated Opportunity is made in VS-17 because category-aware Opportunity generation belongs to VS-19.

## Resume behavior

Because Business exists before VS-17:

- answered/skipped progress is server-persisted after each question;
- reopening the app requests the current eligible set again;
- completed/skipped questions for the current version remain suppressed;
- the owner resumes at the next useful question;
- local draft for the current unsaved text/multi-select question may be preserved in component state but is not considered saved until server confirmation;
- a failed save/skip preserves the current answer selection and exposes retry.

## Catalogue v1 content strategy

VS-17 ships one first immutable catalogue version containing:

- a generic core sufficient for Generic Business;
- lightweight overrides/additions for all eight category families;
- enough candidates that deterministic filtering normally produces 3–5 useful questions;
- no more than a small bounded catalogue per category; YAGNI applies.

The catalogue should prefer shared target context keys across categories where semantics truly match. Category-specific keys are allowed only where generic wording/data would lose material meaning.

This is not Knowledge Pack v2. The catalogue is onboarding context metadata, not a diagnostic/opportunity rules engine.

## Error and degraded states

- question load unavailable: explain that Atlas can continue without these optional details; offer Retry and `Continue for now`;
- stale catalogue version during answer/skip: refresh the eligible set without losing current local draft where safely possible;
- answer validation error: retain selection/input and show actionable inline copy;
- save failure: do not advance; retain current answer and retry;
- auth/business ownership loss: route through existing session guard without claiming progress;
- zero eligible questions: continue immediately;
- offline: optional enrichment must not trap the owner; allow continuing to existing owner journey with questions remaining unanswered;
- duplicate submit: server behavior must be idempotent for same Business/catalogue/question state or return stable current state.

## Accessibility and motion

- >=44×44pt enabled targets;
- semantic heading for each question;
- progress announced accessibly without relying on visual position;
- selected/unselected state exposed through native/web accessibility state;
- helper/error text remains readable with dynamic type;
- keyboard-safe short text screen;
- no horizontal overflow at phone/tablet widths;
- reduced motion respected;
- motion limited to short purposeful question transition/selection feedback and must never delay content;
- color is not the sole selected/status signal.

The primary visual grammar remains ATLAS-DESIGN-001/approved Starbucks-derived Atlas system. Secondary motion polish may not override layout, typography, spacing, colors, cards, controls or navigation.

## Data flow

```text
Business created by VS-16/manual flow
  → authenticated Business id
  → server loads catalogue v1
  + canonical Business/Profile/Context/provenance
  + question progress
  → deterministic eligibility/ranking
  → 0–5 selected questions
  → mobile one-question flow
      ├─ answer → validate server definition → owner-confirmed BusinessContextEntry + answered progress + audit
      └─ skip   → skipped progress + audit
  → re-evaluate remaining questions
  → complete/continue
  → existing Today owner journey
```

## Testing strategy

### Catalogue/domain

- immutable/stable catalogue key/version/question keys;
- all eight categories + Generic fallback resolve valid candidates;
- category override precedence is deterministic;
- already-known target context suppresses question;
- unconfirmed raw public observation does not incorrectly suppress owner-context question;
- skipped current-version question is suppressed;
- answered question is suppressed when target context remains present;
- deterministic priority/tie-break returns max five;
- fewer than three useful candidates returns fewer rather than filler;
- no AI/model dependency in selection.

### API/integration

- Business isolation on list/answer/skip;
- server rejects arbitrary question keys, target context keys, choice values and stale versions;
- answer writes owner-confirmed Business Context and one progress record atomically;
- skip creates no fake/empty context value;
- retry/duplicate answer/skip has stable behavior;
- progress resumes across requests;
- clean PostgreSQL migration;
- existing `/context` API remains compatible.

### Mobile/model/runtime

- post-Business handoff enters VS-17 when questions exist;
- zero questions bypasses enrichment;
- one-question-at-a-time progress;
- single/multi/text controls;
- every question has Skip for now;
- answer and skip resume correctly after reload;
- failure preserves current draft;
- Continue for now works during optional-service/offline failure;
- completion routes to existing owner journey;
- phone/tablet no-overflow and >=44pt targets;
- accessibility selected/progress/error states;
- reduced-motion behavior;
- no Starbucks/demo business data or screen-level third-party branding.

## Governance and release boundary

VS-17 must run under PES/Loop and Superpowers. Implementation begins only after this spec is approved and a writing-plans implementation plan is committed.

The implementation slice should be treated as runtime-enabled because it changes API, PostgreSQL persistence and the onboarding hero journey. Exact-head CI, Security baseline, Product Intake, authentic Expo Web runtime evidence and PES certification are required before merge.

Merge to `main` does not authorize release/deployment/production enablement.

## Definition of Done

VS-17 is complete only when:

- Business creation remains the VS-16/manual boundary and VS-17 starts afterward;
- deterministic catalogue v1 supports Generic Business plus all eight initial category families;
- Atlas selects only useful unknown context and asks at most five questions;
- every question is skippable;
- answered facts are stored only in canonical Business Context as owner-confirmed values;
- answered/skipped catalogue progress is separately versioned/persisted;
- current-version skips do not nag during onboarding;
- one-question-at-a-time tap-first mobile flow is resumable and accessible;
- optional enrichment failure cannot trap the owner from reaching the existing product;
- no AI-generated onboarding questions, Knowledge Pack v2 logic or category Opportunity generation leaks into scope;
- deterministic tests, clean migration, API tests, mobile/runtime evidence, Security, Product Intake and CI pass at exact implementation SHA;
- PES certification is recorded, the certified PR is merged and post-merge `main` CI passes;
- no deployment/release/production enablement occurs.
