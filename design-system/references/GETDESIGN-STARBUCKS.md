# Atlas Primary Visual Grammar — getdesign Starbucks

Status: **APPROVED PRIMARY VISUAL GRAMMAR**

Source command requested by Product Owner:

```bash
npx getdesign@latest add starbucks
```

Source catalog: https://getdesign.md/starbucks/design-md

## Authority and identity

This Starbucks getdesign reference is Atlas's primary visual grammar: palette, warm neutrals, hierarchy, spacing, cards, forms, buttons and depth. Atlas remains the brand identity; the current test mark is prototype-only and rendered solely by `BrandMark`.

Screen-level Starbucks URLs or labels, fabricated business data, copied retail identity, and reintroduction of the prior generic light-green theme are prohibited. Secondary skills may improve usability, accessibility, polish and motion only; they cannot override the primary visual grammar.

The approved Atlas four-screen reference — Welcome → Sign In → Discover → Confirm — remains the product composition reference. This visual grammar must never replace Atlas branding, screen composition, information architecture, or approved onboarding layout.

## Useful patterns to borrow

### Green hierarchy
- Deep brand green: `#006241`
- Primary action green: `#00754A`
- Deep supporting green: `#1E3932`
- Mid supporting green: `#2B5148`
- Mint validation/support: `#D4E9E2`

Atlas may translate these visual values into its own identity rather than copying Starbucks branding or proprietary retail identity.

### Warm surfaces
- Primary warm canvas: `#F2F0EB`
- Ceramic zone wash: `#EDEBE9`
- Cards: `#FFFFFF`
- Cool utility surface: `#F9F9F9`

Use this to prevent Atlas from feeling sterile while preserving the approved light/airy canvas.

### Typography discipline
- Sans-serif throughout; Inter/System Sans is the Atlas substitute.
- Tight tracking around `-0.01em`.
- Strong but not oversized headings.
- Comfortable body rhythm around 1.5 line-height.
- Secondary text should visibly step down without becoming low-contrast.

### Spacing rhythm
Use a disciplined 4/8/16/24/32/40/48/56/64 scale, with **16px as the primary rhythm unit**.

### Cards and depth
- 12px baseline card radius.
- Whisper-soft layered shadows only.
- No heavy floating-card shadows.
- Elevated surfaces should feel tactile but calm.

### Buttons / touch
- Strongly rounded / pill-like CTAs where compatible with the approved Atlas reference.
- Active press: subtle scale around `0.95–0.98`.
- Minimum mobile touch target 44px.
- Filled green CTA is the default conversion action.

### Forms
- Floating/clear labels.
- Valid fields may receive a subtle mint tint.
- Invalid fields receive pale red tint rather than harsh borders.
- Inputs must retain generous touch height and breathing room.

### Motion
- Micro-interactions should be restrained.
- Press scale, soft state transition, staged reveal, and confidence/success motion are preferred.
- Never introduce decorative motion that changes the approved composition.

## Atlas-specific application

Use these patterns to improve:
- Welcome screen spacing, illustration/card depth, CTA tactility.
- Sign In field states, social-button rhythm, provider logo alignment and touch feedback.
- Discover screen scanning/list state hierarchy and progress transitions.
- Confirm screen restaurant information grouping, chips, source badges, menu/services metadata and verification state.
- Later dashboard/opportunity/execution screens, while retaining Atlas visual DNA.

## Restaurant confirmation requirements

When the discovered business is a restaurant/café, the Confirm screen should progressively show real supported facts such as:
- business name and public image/logo when genuinely discovered;
- address / primary location;
- canonical category + cuisine/subcategory when supported;
- operating/service modes: dine-in, takeaway, delivery, catering only when evidenced or owner-confirmed;
- opening hours if discovered;
- delivery/public channels (Bolt Food, Wolt, website, etc.) only when actually observed;
- menu snapshot: number of categories/items, price band and notable public menu signals when extracted;
- rating/review/public popularity signals only when source data is available and provenance is retained;
- data-source icons/badges corresponding only to real discovered sources;
- confidence / owner-confirmation state where useful.

Do **not** claim sales, repeat orders, table bookings, profitability, customer cohorts or other private operational facts from public discovery alone.

## Prohibited use

- Do not copy Starbucks trademarks, logos, retail imagery or proprietary brand identity.
- Do not make Atlas look like Starbucks.
- Do not introduce screen-level Starbucks URLs or labels.
- Do not override the approved Atlas screenshot.
- Do not fabricate business or restaurant data to fill visual slots.
- Do not reintroduce the prior generic light-green theme.
