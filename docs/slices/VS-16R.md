# VS-16R — Business Location Resolution Remediation

## Purpose

Remediate defects exposed while validating VS-16 URL-first discovery on the isolated Expo Go test harness. Preserve the approved Atlas onboarding design while making business/branch location selection canonical and owner-friendly.

## Requirements

- FR-02 Business creation.
- FR-03 Business Profile.
- FR-16 degraded/insufficient-context states.

## Approved scope

1. Fix marketplace extraction so a Bolt/Wolt venue page does not collapse to generic provider metadata such as `Bolt Food` when merchant-specific data is available.
2. Introduce a provider-neutral location-resolution contract owned by the Businesses boundary.
3. Treat Business identity and operating Location as distinct concepts in onboarding semantics.
4. One strong location match: preselect and ask the owner to confirm/change it.
5. Multiple location matches: ask `Which location are you setting up?` and let the owner choose one branch.
6. No reliable match: offer `Find your business location` using a Google Places-backed provider adapter when configured.
7. On initial onboarding, configure one location first. Additional branches remain a later profile flow.
8. Derive canonical country, IANA timezone and ISO currency from the selected location. Do not ask the owner to type these codes.
9. If location changes, recompute the dependent country/timezone/currency values.
10. Return field-level validation messages to mobile instead of generic validation titles.
11. Preserve provenance and owner confirmation.
12. Keep provider credentials server-side and outside source/mobile bundles.

## Provider decision

Google Places/Place Details and Google Time Zone are the approved first location-resolution adapter for the test/pilot path, behind a replaceable `ILocationProvider`-style boundary. Production enablement remains separately governed. Country-to-currency resolution remains Atlas-owned deterministic data.

## Out of scope

- Production release or production enablement.
- Bulk multi-branch onboarding.
- Cross-location intelligence aggregation.
- Private Bolt/Wolt/POS connectors.
- Navigation redesign.
- Visual redesign outside the approved Atlas baseline.

## Acceptance criteria

- Aleppo Food/Bolt does not resolve the business name to generic `Bolt Food` when merchant-specific page data identifies the merchant.
- A known location produces owner-facing address plus canonical internal country/timezone/currency.
- Multiple candidate branches are shown as selectable location cards.
- Unknown/ambiguous location exposes a Google location-search action rather than country/timezone/currency text inputs.
- The owner never needs to type `MT`, `Europe/Malta`, `EUR`, or equivalent technical codes during discovery onboarding.
- Server validates canonical values and mobile displays actionable field-level errors.
- Existing SSRF protections remain intact across redirects and public-source fetching.
- Tests cover merchant extraction, location result cardinality, derived metadata, validation, and mobile state mapping.

## Implementation mode

Runtime-enabled on the isolated test branch only. Merge, release and production enablement are not authorized by this slice.
