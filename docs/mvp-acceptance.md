# Atlas MVP Acceptance Matrix

Baseline: `main` merge `049a45d24bb794363d91a622efcd1c89a5cfa943` after certified VS-11.

## Purpose

This pass validates the integrated MVP as one product journey after VS-01 through VS-11. It is not a new feature slice and must not restructure PES, enable production runtime, deploy, or activate paid/external services.

## Required automated gates

- PES planning/governance validation passes.
- Mobile dependency, TypeScript, lint and platform tests pass.
- All EF Core migrations apply sequentially to a clean PostgreSQL database.
- API policy tests pass.
- Security baseline passes.
- Product intake passes.

## Integrated owner journey

| Journey | Acceptance criterion | Status |
| --- | --- | --- |
| Authentication | Unauthenticated user is directed to sign-in; authenticated owner can enter owned Business only. | Pending runtime validation |
| Business onboarding | Owner can create the initial Business and retain Business isolation. | Pending runtime validation |
| Profile / goals / context | Owner can maintain Business inputs without crossing Business boundary. | Pending runtime validation |
| Knowledge Pack | Assigned published version resolves as one cohesive pack; assignment is stable and version provenance is preserved. | Pending runtime validation |
| Today’s Focus | Owner sees a grounded Opportunity or an explicit insufficient-context state. | Pending runtime validation |
| Opportunity Detail | Evidence, assumptions, limitations, goal alignment and Knowledge Pack provenance are visible. | Pending runtime validation |
| Action decision | Apply / complete / skip / not relevant / reject transitions obey policy and stale versions are rejected. | Pending runtime validation |
| Execution Kit | Assets are editable/trackable and usage remains attached to the correct Business/Opportunity. | Pending runtime validation |
| Outcome | Completed Action can capture Outcome; evidence class is explicit; stale updates are rejected. | Pending runtime validation |
| Business Memory | Derived memory is transparent, Business-scoped and deletable where permitted. | Pending runtime validation |
| History | Chronological projection reflects authoritative records and deep-links to Opportunity Detail. | Pending runtime validation |
| Weekly Review | Seven-day review reflects recorded facts only and distinguishes missing Outcomes. | Pending runtime validation |
| Notifications | In-app records deduplicate, unread state persists, preferences are owner-controlled and deep links stay within approved Atlas routes. | Pending runtime validation |
| Navigation | All implemented screens are reachable through existing PES routes without tab/framework restructuring. | Pending runtime validation |
| Logout | Session can be ended without changing Business data. | Pending runtime validation |

## Cross-cutting acceptance

- Business isolation is deny-by-default for owner APIs.
- Optimistic concurrency prevents stale writes where versioned mutations exist.
- Published Knowledge Pack versions remain immutable.
- No feature fabricates evidence, causal ROI, Action completion, or Outcome impact.
- Runtime remains disabled outside development/test.
- No production credentials, external push/SMS/email provider, paid third-party service, or production deployment is activated.
- Existing PES navigation, routing, authentication, state-management and folder conventions remain intact.

## Device acceptance

After automated gates are green, validate in Expo Go/test runtime using demo/test accounts:

1. Sign in and enter the owned Business.
2. Traverse profile, goals and context.
3. Open Knowledge Pack and Today’s Focus.
4. Open Opportunity Detail, record Action decisions, use Execution Kit, mark completed and capture Outcome.
5. Open Business Memory, History and Weekly Review.
6. Open Notifications, change preferences, mark notifications read and follow each supported deep link.
7. Refresh/reopen key screens and confirm persisted state and no broken navigation.
8. Confirm another Business/account cannot access the first Business resources.

Any runtime defect found here is fixed on the acceptance branch and must pass the full gate set before merge.
