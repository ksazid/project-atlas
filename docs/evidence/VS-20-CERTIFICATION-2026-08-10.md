# VS-20 Certification Evidence — 2026-08-10

- Certified implementation SHA: `f602451cd5b79d76e91c950fdb1662918856565e`
- PR: #27
- Valid TDD RED head: `42887b9a68121f5c54102cf8551e6a034ca16f79`
- RED CI: `31347205720` — preflight/build/migration passed; API test compilation failed only because `KnowledgeBundleResolver` and `KnowledgeBundleResolutionException` did not exist.
- GREEN CI: `31347434598`
- GREEN Security baseline: `31347434600`
- GREEN Product Intake: `31347434594`

## Certified behavior

The runtime resolver requires the exact current Generic Business Core assignment, automatically adds the packaged Restaurant/Café Category manifest when applicable, accepts subcategory only from owner-confirmed canonical profile evidence, filters Context to owner-confirmed values, includes Business Local/Market facts and Business Memory as separate evidence layers, and produces deterministic manifest and bundle fingerprints.

No persistence migration, pack-picker UI, technical Knowledge Pack endpoint, remote installation, provider dependency, release, deployment or production enablement is included.

The certification close-out head must pass all required gates again before merge.