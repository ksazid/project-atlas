# VS-19 — Restaurant/Café Reference Intelligence Design

## Goal

Add the first Category-layer Atlas Knowledge Pack Manifest v2 for the canonical `restaurant-cafe` category. This is a reference intelligence pack, not a provider integration and not an expansion to all eight categories.

## Boundaries

The pack is backend-packaged, exact-versioned and deterministic. It supports the canonical Restaurant & Café category plus the current `restaurant`, `cafe`, `bakery` and `takeaway` subcategories. It contains evidence-aware KPI definitions, evidence rules, Opportunity patterns, execution templates, measurement suggestions, seasonality and guardrails.

No paid source, private Wolt/Bolt API, scraping bypass, credential, dynamic remote installation, release or production enablement is introduced.

## Intelligence model

The pack provides four bounded Opportunity patterns:

1. ordering-path clarity — only useful when a confirmed service/ordering channel exists;
2. public-hours consistency review — only when hours evidence exists;
3. current-offer visibility review — only when an owner-confirmed current priority/offer supports it;
4. reputation-signal follow-up — only when a public/owner-confirmed reputation signal exists.

Patterns are suggestions for review. They do not assert that a tactic will improve revenue, ranking or conversion. Every action remains owner-controlled.

## Compatibility

VS-19 adds no database migration and does not change the current single persisted Core Knowledge Pack assignment. VS-20 will resolve packaged category intelligence alongside that exact Core assignment.

## Testing

The reference manifest must validate under `KnowledgePackManifestV2Policy`, target only `restaurant-cafe`, include exactly the canonical supported subcategories, have resolvable references, remain provider-neutral, and contain no private-API dependency language.