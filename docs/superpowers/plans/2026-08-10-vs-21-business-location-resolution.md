# VS-21 — Business Location Resolution Remediation Plan

## TDD sequence

1. RED: add API tests reproducing provider-generic merchant name and location-metadata normalization failures.
2. GREEN: implement conservative merchant-title extraction and deterministic country/currency/timezone normalization helpers.
3. RED: add location-search/selection contract tests for zero, one and multiple candidates and provider-unavailable state.
4. GREEN: add provider-neutral location application service and Google adapter boundary/configuration without placing credentials in mobile.
5. RED: add mobile model tests proving country/timezone/currency are not owner-entered requirements and proving one/multiple/unknown location states.
6. GREEN: update onboarding state/model and API client to support location candidates and field-level Problem Details.
7. GREEN: update the existing create-business screen within the approved design baseline: selected-location summary, branch cards, `Find your business location`, no technical-code text inputs.
8. Verification: API build/tests, mobile tests, governance validation, preflight, exact diff review and live test deploy on the isolated harness only.

## Constraints

- No `main` merge.
- No production release/enablement.
- No provider secret in repository or Expo bundle.
- No weakening of SSRF protections.
- No broad Business schema rewrite unless a failing acceptance test proves it necessary.
- Preserve owner confirmation/provenance.
