# Changelog

## 2026-08-07 — VS-03 Knowledge Pack Foundation

### Added

- Modular `KnowledgePack`, `KnowledgePackVersion`, `KnowledgeSection`, and `BusinessKnowledgeAssignment` domain entities.
- Draft, review, published, and archived lifecycle with immutable published content.
- Exact published-version assignment, effective resolution, and assignment history.
- Internal management APIs for packs, versions, sections, transitions, assignment, history, and comparison.
- Business-owner read APIs with safe cross-business not-found behavior.
- Incremental EF Core migration with optimistic concurrency and filtered current-assignment uniqueness.
- Domain-policy and business-isolation tests.
- Mobile unified Knowledge Pack presentation, version indicator, refresh, retry, and secure offline cache.
- Architecture, domain-model, roadmap, decisions, and slice documentation.

### Changed

- Business creation now provisions and assigns the Generic Business Knowledge Pack version.
- PR #7 scope was corrected from a single serialized document model to the approved modular versioned model.
- Legacy VS-02 delivery metadata now uses valid PES lifecycle and certification status values.

### Certification

- Certified exact SHA: `c2fad5f7a6e4fe517932ca6371de41fb132233e5`.
- CI #47 passed preflight, mobile TypeScript/lint/tests, .NET restore, EF Core migration, API tests and dashboard build.
- Security baseline #44 passed.
- Product intake #42 passed.
- Release, deployment and production enablement remain unauthorized.

### Not included

- AI execution, prompts, embeddings, vector database, semantic search, production deployment, production credentials, or paid-service activation.
- A standalone admin UI, because no admin application currently exists in the repository.
