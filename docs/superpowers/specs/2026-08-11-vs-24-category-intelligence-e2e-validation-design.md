# VS-24 — End-to-End Category Intelligence Validation

Date: 2026-08-11
Status: Approved by Product Owner instruction to proceed through all remaining slices
Depends on: VS-22 Multi-Source Business Discovery, VS-23 Evidence-Aware Opportunity Generation
Branch: `atlas/vs-24-category-intelligence-validation`

## Outcome

Prove the Restaurant/Café Category Intelligence hero journey as one coherent mobile product flow and close integration defects exposed only when discovery, location, context, Knowledge Bundle resolution, opportunity generation, Opportunity Detail and execution are connected.

VS-24 is primarily a validation/integration slice. It may make bounded fixes required to make the approved flow work, but it must not expand product scope or redesign Atlas.

## Hero journey under test

1. Owner signs in or uses the development-only test harness in the isolated test environment.
2. Owner provides one primary public business URL and, where desired, optional secondary URLs from VS-22.
3. Atlas sanitises/revalidates URLs and discovers one Business without unsafe redirects or cross-business contamination.
4. Owner resolves/chooses the exact operating location when needed.
5. Atlas derives canonical country, timezone and currency from the chosen location.
6. Owner confirms discovered public facts and Business creation completes without server 5xx.
7. Owner answers or skips the progressive high-value questions.
8. Owner selects/prioritises Business Goals.
9. Atlas resolves the exact VS-20 Knowledge Bundle.
10. VS-23 generates one evidence-qualified Today’s Focus or returns a truthful no-focus/degraded state.
11. Owner opens Opportunity Detail and sees Evidence separately from Atlas interpretation, plus goal alignment, assumptions, limitations and exact pack version.
12. Owner opens the Execution Kit, reviews the generated checklist/template, and retains owner control.
13. Existing Action/Outcome/Memory/History functionality remains compatible.

## Public validation source

Use a previously supplied public Bolt Food Restaurant/Café URL as supplemental live validation, for example:

`https://food.bolt.eu/en/324/p/11881-gun-turkish-kebab?utm_source=share_provider&utm_medium=product&utm_content=menu_header`

The live URL is supplemental evidence only. Deterministic CI must not depend on third-party network availability. Tests use controlled fixtures/contracts; live Bolt/Google verification is performed only on the isolated test environment after deterministic gates pass.

## Mobile Today state contract

The mobile `TodayFocus` API union must represent all server states from VS-23:

- `ready` — contains one Opportunity;
- `insufficient-context` — Atlas needs confirmed setup/context;
- `no-focus` — setup is valid but no recommendation meets eligibility/evidence rules;
- `degraded` — Atlas cannot safely resolve/generate the focus right now.

Each non-ready state preserves the server `code` when supplied and a plain-language `message`.

The Today screen must not fall through to an empty recommendation card for an unknown state.

## Today state UX

### Insufficient context

Explain what is missing without blame. Primary recovery route: Profile/Goals/Context depending on the server message; for this slice, a single safe `Review business context` route may lead to Profile/Context rather than guessing the exact missing field.

### No focus

Use explicit honest copy: Atlas has enough setup to work, but no recommendation currently meets the evidence/eligibility threshold. Do not label this as an error. Offer `Review business context` and `View history` as secondary recovery/navigation options; there is no Apply action.

### Degraded

Explain that Atlas could not safely prepare a recommendation. No Opportunity was created. Offer Retry and a safe route to review context. Never expose provider names, stack traces or internal error codes in primary copy.

### Network/client error

Remain distinct from a server-declared degraded state. Show retry and preserve existing safe behavior.

All states follow ATLAS-DESIGN-001: calm, action-first, one primary action, accessible labels/announcements, approximately 44pt targets, no dashboard composition and no competing primary CTA.

## Opportunity Detail / Execution validation

VS-24 does not redesign these screens. It verifies that VS-23 structured evidence renders through the existing Opportunity Detail client contract without provider leakage or mixed Evidence/interpretation. It verifies the current Execution Kit endpoint can consume a VS-23 generated Opportunity and select the corresponding packaged template by exact `KnowledgePackKey`/`KnowledgePackVersion` and title/template intent.

If integration reveals an actual incompatibility, VS-24 may make the smallest fix plus a regression test.

## Confirm-and-continue regression

The Business discovery confirmation server 5xx observed during real-device testing is a hard VS-24 blocker even if VS-22 owns the root fix. The integrated validation must prove a valid discovered/reconciled snapshot can be confirmed and consumed exactly once without generic server error, and that retry/stale/consumed cases return stable actionable errors instead of 5xx.

## Previously observed regressions included in validation

- public marketplace identity must be the merchant, not generic `Bolt Food`;
- marketplace ordering boilerplate must not become the Business description;
- location resolution must populate canonical country/timezone/currency;
- Google Places timezone must use the documented `timeZone.id` payload shape;
- Google Maps/share URLs must not be scraped as ordinary public pages;
- short Google share links must degrade/reroute safely rather than produce provider 429 as a generic discovery failure;
- unsafe Back navigation must not emit an unhandled `GO_BACK` action on the hero path;
- `Confirm and continue` must not produce generic server 5xx;
- Today client must understand `no-focus` and `degraded` rather than rendering an empty ready card;
- Opportunity Detail must not throw on the VS-23 structured goal/evidence object;
- the existing EF Opportunity model registration must remain valid against the already-created Opportunities migration.

VS-24 does not duplicate VS-22/VS-23 tests; it adds cross-boundary assertions that prove the integrated journey.

## Provider neutrality / trust

Customer-facing screens must not display Bolt, Wolt, Google or provider hostnames as recommendation authority. Source provenance remains internally auditable and may be exposed only through approved neutral trust language. Evidence records can retain source identifiers internally.

## Deterministic validation architecture

### Mobile contract/state tests

Add focused tests that verify:

- TodayFocus union accepts ready / insufficient-context / no-focus / degraded;
- Today screen has explicit branches for all states;
- no-focus has no Apply/Skip/Not Relevant controls;
- degraded has Retry and no recommendation card;
- network error is distinct from degraded;
- accessible heading/live-region/recovery controls exist;
- no provider-specific copy is introduced.

### API journey integration

After VS-22 is integrated, add one controlled Restaurant/Café journey test using fixture public snapshots rather than live network:

- multi-source discovery/reconciliation;
- Business confirmation/creation;
- canonical Restaurant/Café category and owner-confirmed location/context;
- progressive question context (at minimum `primarychannels` or another evidence-bearing canonical context key);
- revenue/acquisition/compatible owner goal;
- resolved Knowledge Bundle contains exact Core + Restaurant/Café manifests;
- Today’s Focus produces a Restaurant/Café pattern;
- Opportunity Detail reads exact structured evidence;
- Execution Kit selects the expected Restaurant/Café template;
- all stored Business-owned records remain isolated to the Business.

The integration test should call production services/policies directly where HTTP authentication would only add fixture complexity; existing endpoint/authorization tests continue to cover route security. A runtime/mobile acceptance test can cover the rendered navigation path.

### Runtime acceptance

Extend the authentic Expo Web runtime harness or equivalent deterministic mobile runtime fixture to exercise:

- Today no-focus state;
- Today degraded state;
- ready Today → Opportunity Detail route;
- no rendered provider leakage;
- accessible recovery controls.

### Live isolated smoke

Only after deterministic certification candidate is green:

- update isolated test deployment to the exact candidate SHA;
- use a supplied public Bolt URL;
- verify discovery/location/confirmation/questions/goals/Today/Detail/Execution;
- inspect Render logs for server 5xx/provider failures;
- capture defect evidence and loop through TDD if any failure occurs.

Live smoke does not authorize production deployment.

## Error handling

Every boundary must return one of: valid state, validation problem, stable degraded state, safe not-found/authorization behavior or explicit retryable network error. Generic 500 on a valid hero-path action is a certification blocker.

## Scope boundaries

In scope:
- Today mobile state contract/UI;
- deterministic integrated Category Intelligence tests;
- smallest fixes needed for cross-slice compatibility;
- live isolated validation using an approved public URL;
- documentation/certification evidence.

Out of scope:
- new public data providers;
- private POS/marketplace integrations;
- new AI model/provider work;
- new opportunity recipes beyond VS-23 manifests;
- redesign of approved screens;
- production deployment;
- new multi-location portfolio intelligence beyond selecting one operating location during initial setup.

## Parallel/dependency rule

VS-24 is branched from the VS-23 pre-integration head so independent mobile-state work can proceed while VS-22 finishes. It cannot be certified or merged in that form. After VS-22 and VS-23 are merged into `main`, VS-24 must be rebased/reconciled onto the final main baseline, complete the VS-22-dependent journey tests, rerun all gates, complete the isolated live smoke, and obtain exact-SHA certification approval.

## Acceptance

VS-24 is complete when the Restaurant/Café hero journey works coherently across the integrated codebase, all four Today server states render truthfully and accessibly, a qualifying context produces an evidence-aware category-specific Opportunity, no qualifying context produces no filler, Opportunity Detail/Execution remain compatible, all previously observed hero-path runtime regressions are covered, deterministic CI/Security/Product Intake pass on the final exact integrated SHA, and the isolated public-Bolt smoke shows no unresolved critical 5xx/trust/safety defect.