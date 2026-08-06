# Atlas Domain Model

## KnowledgePack

Stable Knowledge Pack identity.

- `Id`
- stable `Key`
- `Name` and `Description`
- archive state
- creator and creation timestamp
- optimistic concurrency version

A pack contains multiple versions. Pack metadata is separate from versioned content.

## KnowledgePackVersion

A lifecycle-controlled snapshot of a Knowledge Pack.

- exact `VersionNumber`
- `Status`: draft, review, published, archived
- `Locale`
- creator, reviewer, and publisher identities
- lifecycle timestamps
- optimistic concurrency version
- ordered `KnowledgeSection[]`

Publication requires at least one section. Published and archived versions cannot be mutated.

## KnowledgeSection

An ordered, retrieval-friendly content unit.

- stable entity ID
- stable logical key
- category
- title and content
- optional metadata JSON
- ordering
- locale and translation group
- provenance/source
- created and updated timestamps
- optimistic concurrency version

Initial categories are Business Overview, Services, FAQs, Brand Voice, Policies, Pricing, Promotions, Sales Guidance, SOPs, and Custom.

## BusinessKnowledgeAssignment

An explicit association between a Business and one exact published Knowledge Pack version.

- Business ID
- Knowledge Pack ID
- Knowledge Pack Version ID
- retained pack key and exact version
- current/effective state
- assigned-by identity
- assignment, effective, and end timestamps
- optimistic concurrency version

Historical rows are retained. A filtered unique index allows many historical assignments while permitting only one current assignment per business.

## Invariants

- Only published versions may be assigned.
- Publishing a newer version never changes a business assignment automatically.
- A published or archived version is immutable.
- Editing published content creates a new draft.
- Section stable keys and order values are unique within a version.
- Business-owner reads are business-isolated.
