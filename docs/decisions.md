# Architecture Decisions

## VS-03 — Knowledge Pack Foundation

### PES framework remains unchanged

Knowledge Pack capabilities are implemented as a Business Domain feature module. Existing PES navigation, routing, authentication, state management, folder conventions, design system and API-client architecture remain unchanged.

### Modular internal model

Knowledge Packs are persisted as `KnowledgePack`, `KnowledgePackVersion` and ordered `KnowledgeSection` entities. The user-facing experience remains one cohesive document.

### Immutable publication

Published and archived versions are immutable. Subsequent edits start from a new draft version and follow the review and publish workflow.

### Exact business assignment

A Business assignment references an exact published Knowledge Pack version. Publishing a newer version never automatically replaces the assigned version.

### Runtime feature boundary

Knowledge Pack runtime remains disabled outside development and test until separately authorized.

### AI-ready and provider-neutral

Sections use stable identifiers, categories, metadata, ordering and locale-aware retrieval boundaries. VS-03 does not introduce AI-provider execution, embeddings or vector storage.

### Backward compatibility

VS-03 uses incremental migrations and preserves existing VS-01 and VS-02 APIs, authorization boundaries, Business isolation and mobile framework behavior.
