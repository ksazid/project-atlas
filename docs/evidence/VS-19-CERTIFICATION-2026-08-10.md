# VS-19 Certification Evidence — 2026-08-10

- Certified implementation SHA: `c2214599fa56269820eb3695bef4ad0eec08a99f`
- PR: #26
- TDD RED head: `a0577ca35282066fee5b10c436f3512fafea58e1`
- RED CI run: `31346161417` — preflight/build/migration passed; API test compilation failed only because `RestaurantCafeKnowledgeManifestV2` did not yet exist.
- GREEN CI run: `31346398914`
- GREEN Security baseline: `31346398910`
- GREEN Product Intake: `31346398915`

## Certified behavior

The packaged Manifest v2 targets only canonical `restaurant-cafe`, version `1.0`, layer `category`, with subcategories restaurant/cafe/bakery/takeaway. It contains provider-neutral KPIs, evidence rules, four bounded Opportunity patterns, execution templates, measurement suggestions, seasonality and guardrails, and validates under the VS-18 server-owned manifest policy.

No database migration, public endpoint, private marketplace API, paid provider, remote pack installation, release, deployment or production enablement is included.

The close-out PR head must pass all required gates again before merge.