# Atlas Roadmap

## Completed foundation

- VS-01 — initial identity and Business foundation
- VS-02 — Business profile, goals, and context

## Current slice

### VS-03 — Knowledge Pack Foundation

Status: implementation in progress in PR #7.

Delivers:

- modular pack/version/section persistence
- immutable publication lifecycle
- exact business assignment and history
- business-isolated effective-version APIs
- internal management APIs
- mobile unified Knowledge Pack view with offline cache
- audit and optimistic concurrency foundation

Certification remains pending because GitHub Actions has not created workflow runs for the branch.

## Future consumers

### VS-05 — AI Context Engine

Will assemble deterministic, token-efficient context from the exact assigned Knowledge Pack version and ordered sections.

### VS-06 — Conversation Engine

Will consume the resolved Knowledge Pack context without changing VS-03 persistence or assignment architecture.

### VS-07 — Prompt Studio

Will use versioned content and metadata while preserving published-version immutability.

## Explicitly deferred from VS-03

- AI provider execution
- embeddings and vector storage
- semantic retrieval
- production deployment
- production credentials
- paid third-party services
- a new standalone admin application
