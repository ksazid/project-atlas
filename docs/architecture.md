# Atlas Architecture

## PES boundary

PES remains the application and delivery framework. VS-03 does not alter navigation, routing, authentication, state management, folder conventions, design tokens, API-client architecture, or the Expo mobile framework.

Knowledge Pack is implemented as a Business Domain feature module.

## Knowledge Pack architecture

```text
Business
  -> BusinessKnowledgeAssignment
      -> KnowledgePackVersion
          -> KnowledgeSection[]
      -> KnowledgePack
```

`KnowledgePack` provides stable identity and display metadata. `KnowledgePackVersion` owns lifecycle and publication state. `KnowledgeSection` provides ordered, retrieval-friendly content boundaries. `BusinessKnowledgeAssignment` resolves the exact published version effective for a business and retains assignment history.

## Lifecycle

```text
Draft -> Review -> Published -> Archived
          -> Draft
```

Published and archived versions are immutable. Changes to published content begin from a new draft copied from the published version.

## API and authorization

Business-owner endpoints resolve only assignments belonging to the authenticated owner and return safe not-found responses for foreign businesses. Management endpoints use the existing `InternalOperator` policy. All lifecycle, section, and assignment mutations emit audit records and use optimistic concurrency.

## Mobile integration

The mobile Knowledge Pack experience is delivered through the existing Home route. New code is isolated under `src/features/knowledge-pack`; no tab, route, or framework restructuring is introduced. The screen renders ordered sections as one cohesive document and uses the existing secure-storage abstraction for an offline copy.

## Runtime boundary

VS-03 contains no AI provider calls, prompts, embeddings, vector database, deployment configuration, production credentials, or paid-service activation. The model is prepared for VS-05, VS-06, and VS-07 through stable IDs, locale, metadata, ordering, exact-version resolution, and retrieval boundaries.
