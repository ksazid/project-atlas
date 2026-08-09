# Atlas MVP Integrated Acceptance

Baseline: `main` merge `c54fa224321424aafb8029830d1c960f52249c0e` after certified VS-15 Context Visual Migration.

## Purpose

This pass validates the integrated MVP as one governed product baseline after the Profile → Goals → Context visual migration. It is not a new feature slice and must not restructure PES, enable production runtime, deploy, activate paid/external services, or weaken any existing certification gate.

PR #17 originally branched from the certified VS-11 baseline. Its useful platform, deployment-test, notification-policy, planning, and acceptance changes subsequently landed on `main` through other governed work. The stale branch was therefore reset to the current certified `main` baseline rather than replaying obsolete commits over the later API and mobile implementation.

## Required automated gates

The refreshed PR must pass on its exact head:

- PES planning and governance validation.
- Mobile dependency, TypeScript, lint and platform validation.
- Focused Profile, Goals and Context model tests.
- Authentic CI-only Expo Web runtime verification already carried by the mobile test suite.
- Sequential EF Core migration application to a clean PostgreSQL database.
- API policy tests.
- Dashboard build.
- Security baseline.
- Product Intake.

## Integrated owner journey

| Journey | Acceptance criterion | Current evidence |
| --- | --- | --- |
| Authentication | Unauthenticated user is directed to sign-in; authenticated owner stays within owned Business boundaries. | Existing auth/API policy implementation; full native device pass remains release-level. |
| Business onboarding | Owner can create the initial Business and retain Business isolation. | Existing governed implementation and API tests. |
| Profile | Owner can maintain the complete Business Profile with loading, retry, validation and owner-confirmation states. | VS-13 certified runtime evidence. |
| Goals | Owner can maintain ordered Business Goals without changing the existing route/API semantics. | VS-14 certified runtime evidence. |
| Context | Owner can maintain optional Business Context, confirm public-source values and preserve unknown API context safely. | VS-15 certified Expo Web runtime evidence. |
| Knowledge Pack | Assigned published version resolves with version provenance preserved. | Existing governed implementation/API policy coverage. |
| Today’s Focus | Owner sees a grounded Opportunity or an explicit insufficient-context state. | Existing governed implementation; integrated native traversal remains pending release-level validation. |
| Opportunity Detail | Evidence, assumptions, limitations, goal alignment and Knowledge Pack provenance remain visible. | Existing governed implementation. |
| Action decision | Apply / complete / skip / not relevant / reject transitions obey policy and stale versions are rejected. | API policy and concurrency coverage. |
| Execution Kit | Assets remain attached to the correct Business/Opportunity. | Existing governed implementation. |
| Outcome | Completed Action can capture Outcome with explicit evidence class and stale-write protection. | Existing governed implementation/API tests. |
| Business Memory | Derived memory remains transparent and Business-scoped. | Existing governed implementation. |
| History | Chronological projection reflects authoritative records. | Existing route and implementation. |
| Weekly Review | Seven-day review reflects recorded facts only and distinguishes missing Outcomes. | Existing route and implementation. |
| Notifications | In-app records deduplicate, unread state persists, preferences are owner-controlled and deep links are restricted to approved Atlas routes. | Notification policy tests. |
| Navigation | Implemented screens remain reachable through existing PES routes without tab/framework restructuring. | `mvp-integrated-acceptance.test.mjs` plus mobile validation. |
| Logout | Session can end without changing Business data. | Existing session implementation; native traversal remains release-level. |

## Visual migration acceptance

The integrated baseline requires the three migrated Business-input screens to continue using the shared Atlas primitives:

- `BrandMark` is the only direct owner of the temporary prototype mark reference.
- Profile, Goals and Context consume shared Atlas design tokens.
- No migrated screen may directly reference the temporary Starbucks asset.
- Existing routes, authentication, APIs, persistence and five-tab navigation remain unchanged.

The integrated acceptance test enforces those structural boundaries so later work cannot silently reintroduce direct temporary-logo coupling.

## Runtime evidence already in the baseline

The certified repository includes authentic Expo Web runtime evidence for:

- VS-13 Profile at phone/tablet widths and recoverable/saving states.
- VS-14 Goals at phone/tablet widths and governed interaction states.
- VS-15 Context at 390×844 and 768×1024, including loading, recoverable failure, retry, public provenance confirmation, draft-safe save failure, saving/success, authenticated Business boundary, no horizontal overflow and minimum target sizing.

These records are implementation/certification evidence. They do not replace native iOS release validation.

## Cross-cutting acceptance

- Business isolation is deny-by-default for owner APIs.
- Optimistic concurrency prevents stale writes where versioned mutations exist.
- Published Knowledge Pack versions remain immutable.
- No feature fabricates evidence, causal ROI, Action completion, or Outcome impact.
- Runtime remains controlled outside development/test.
- No production credentials, external push/SMS/email provider, paid third-party service, or production deployment is activated by this acceptance pass.
- Existing PES navigation, routing, authentication, state-management and folder conventions remain intact.

## Native/device release boundary

Before any release or production enablement, perform authentic native-device acceptance using approved test/demo accounts. At minimum:

1. Sign in and enter the owned Business.
2. Traverse Profile, Goals and Context with the native keyboard and enlarged text.
3. Open Knowledge Pack and Today’s Focus.
4. Open Opportunity Detail, exercise governed Action decisions, Execution Kit and Outcome capture.
5. Open Business Memory, History and Weekly Review.
6. Open Notifications, change preferences, mark notifications read and follow each supported deep link.
7. Refresh/reopen key screens and confirm persistence and navigation.
8. Confirm a second Business/account cannot access the first Business resources.
9. Validate VoiceOver/focus order and native keyboard insets on migrated screens.

Native/device completion is a release-level prerequisite and is not claimed by this PR.
