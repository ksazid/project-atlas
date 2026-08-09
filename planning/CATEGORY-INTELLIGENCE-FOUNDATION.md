# Category Intelligence Foundation — Locked Product Direction

Status: LOCKED for implementation after MVP acceptance stabilization.

## Product position
Atlas turns fragmented business data into the next best action, provides the execution kit to act on it, measures the outcome, and learns what works for that specific business. The differentiator is the closed loop: data -> decision -> action -> measured learning.

## First-use journey
Welcome -> Login -> Tell Atlas about your business -> Paste business URL/public page -> Auto-discovery -> Owner confirmation -> 3-5 high-value questions -> Category intelligence -> First Opportunity.

The Welcome screen explains how Atlas helps a business grow through: understand the business -> find opportunities -> take action -> measure outcome -> get smarter.

## Fast onboarding rules
- URL-first onboarding is primary; manual setup is fallback.
- Atlas auto-detects business name, canonical category/subcategory, primary location, country/market, timezone, currency, opening hours, menu/services, ordering channels and other supported public facts when confidence is sufficient.
- Do not ask the owner to re-enter facts Atlas can reliably discover. Show them for confirmation/edit instead.
- Primary location should be extracted from the supplied public source where available; ask manually only when discovery fails or confidence is low.
- Category free text is replaced by a searchable canonical taxonomy, but category selection becomes fallback because URL enrichment should propose it automatically.
- Onboarding questions are progressive and category-aware; target 3-5 missing high-value questions and roughly 60-90 seconds when enrichment succeeds.
- After onboarding, do not show an empty dashboard. Show the first useful focus/opportunity or the single highest-value missing-data request.

## Initial category families
1. Restaurant & Cafe
2. Beauty & Personal Care
3. Retail
4. Ecommerce
5. Home & Local Services
6. Professional Services
7. Fitness & Wellness
8. Hospitality / Accommodation

Generic Business remains the fallback. Taxonomy and connector architecture must support subcategories and global expansion without rewriting category intelligence.

## Knowledge architecture
Core Pack + Category Pack + optional Subcategory Pack + owner-confirmed Business Context + Business Memory.

Knowledge Pack Schema v2 should support structured business-model knowledge, revenue/cost drivers, customer journey, KPI definitions, diagnostic rules, opportunity recipes, required evidence, execution assets, measurement rules, seasonality and guardrails.

Restaurant/Cafe is the first reference implementation. Remaining packs are built after the schema and intelligence loop are validated end-to-end.

## Public-source enrichment
Create a provider-neutral Public Business Snapshot. For restaurant/cafe it may use supported Bolt Food, Wolt, website/menu and other public business sources. Architecture must remain provider-neutral and region-independent.

Every extracted fact retains provenance: source/provider, source URL, observed timestamp, confidence and owner-confirmed state.

Separate evidence classes: measured, system-derived, owner-reported, public-observed, inferred, estimated and unknown. Atlas must never turn inference into measured fact.

For restaurant public pages, extract when available: name, category/cuisine, location, contact details, opening hours, menu categories/items/descriptions/prices, ordering channels, promotions and public popularity signals. Derive structural menu insights only when supported by observed data. Do not infer actual sales, repeat rate, profitability, table utilisation or customer demographics from public pages.

## Data maturity / ingestion
Level 0A: owner facts.
Level 0B: public-source enrichment.
Level 0C: owner confirmation.
Level 1 later: CSV/Excel/POS/reservation exports.
Level 2 later: real provider/POS/reservation/accounting APIs.
Level 3 later: webhook / near-real-time ingestion.

Do not implement private Wolt/Bolt/POS connectors as part of the first Category Intelligence slice. Define provider-neutral contracts now and validate with public enrichment plus canonical sample/import data first.

## Canonical data/metric principle
Knowledge Packs must never query provider schemas directly. Provider adapters normalize into Atlas canonical entities/facts such as Business, Location, Customer, Visit, Reservation, Order, OrderLine, Payment, MenuItem, MenuCategory, CostRecord and ExternalConnection, then the metric engine exposes provider-neutral metrics to the Opportunity Engine.

Restaurant channel model must distinguish dine-in, direct takeaway, own website/app, Wolt, Bolt Food and future marketplaces without embedding a specific provider into the Restaurant Pack.

## Global readiness
Malta can be the first validation market, but category intelligence is global. Provider availability is resolved separately by country/region. The architecture must support Europe, UAE/Dubai and other markets through regional connector catalogues/adapters without rewriting Restaurant intelligence.

## Restaurant data domains
Revenue, Demand, Capacity, Customer, Menu, Economics and Operations.

Atlas Data Coverage should explicitly show which domains have trustworthy data and what additional data would unlock. Recommendations and confidence must reflect actual evidence coverage.

## Opportunity recipes
Category Packs define signal/conditions -> hypothesis -> recommended action -> execution assets -> success measures -> prohibited claims. Recommendations must be evidence-aware. No transaction data means no sales claims; no stable customer identity means no exact repeat-rate claims; no cost data means no profitability claims.

## Learning loop
Opportunity -> Execution Kit -> Action -> Outcome -> Business Memory. As evidence accumulates, business-specific outcomes/history should increasingly outweigh generic industry assumptions and external benchmarks.

## Scope order
1. Stabilize and complete MVP acceptance/runtime testing.
2. Implement Category Intelligence Foundation: Welcome/login journey, URL-first discovery, taxonomy, provenance, progressive confirmation/questions, Knowledge Pack Schema v2 and Restaurant/Cafe reference pack.
3. Validate end-to-end with public enrichment plus canonical restaurant sample/import data.
4. Only after successful validation, expand remaining category packs and real regional/provider connectors.
