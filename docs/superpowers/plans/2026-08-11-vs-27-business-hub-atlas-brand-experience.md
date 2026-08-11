# VS-27 Business Hub & Atlas Brand Experience Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Atlas’s edit-first Business tab with a read-first business hub backed by persisted business/media/menu intelligence, add a dedicated read-only menu view, and replace prototype Starbucks branding/tab glyphs with locally owned Atlas visual primitives.

**Architecture:** Add a bounded, account-isolated Business Hub read model and full-menu read endpoint over existing `Business`, `BusinessProfile`, `BusinessMediaReference`, `BusinessOffering`, and `BusinessContextEntry` data. On mobile, keep the existing five-tab Expo Router structure, turn the `profile` tab into a `BusinessHubScreen`, move the existing editable form behind a dedicated edit route, and render all presentation through focused business-hub components and locally owned brand/icon primitives. No new third-party facts are persisted and no database migration is required solely for this UI slice.

**Tech Stack:** .NET 10 minimal APIs, EF Core 10/Npgsql, React Native 0.81, React 19, Expo SDK 54 / Expo Router 6, TypeScript 5.9, Node test runner, xUnit.

## Global Constraints

- VS-26 / PR #47 must be certified/merged or explicitly superseded before any runtime implementation starts.
- Re-read `AGENTS.md`, product docs, `delivery/current-slice.json`, decisions and applicable skills after VS-26 lands; activate VS-27 only from the then-current `main`.
- Preserve the existing PES five-tab routes exactly: `index`, `profile`, `goals`, `context`, `settings`.
- Do not change Today Focus readiness/recommendation logic or VS-26 onboarding/goal-first routing in this slice.
- Use persisted Atlas-owned records only; do not add a new Google/Bolt/Wolt fetch to render the Business Hub.
- Public third-party images remain remote URL references; do not copy or rehost image binaries.
- Business Hub menu is intelligence/read-only, not ordering; no cart, checkout, availability toggles or POS sync.
- Replace the remote Starbucks prototype mark completely; no Starbucks logo, siren, crown, stars, cup artwork, trade dress or remote Starbucks asset may remain.
- Add no icon dependency for VS-27. Build the Compass Orbit brandmark and tab icons from local React Native primitives.
- Keep minimum 44pt interactive targets, text labels for all tabs, reduced-motion-safe behavior and narrow-phone layouts.
- No production deployment, EAS build/submit/OTA, release, or production enablement in implementation/certification.

---

### Task 1: Post-VS-26 conflict check and PES activation

**Files:**
- Create: `docs/slices/VS-27.md`
- Modify: `delivery/current-slice.json`
- Modify only if required by current governance state: `delivery/decisions.json`
- Read/compare: PR #47 changed files against the paths in this plan

**Interfaces:**
- Consumes: the merged/certified VS-26 `main` SHA and the approved design/spec at `docs/superpowers/specs/2026-08-11-vs-27-business-hub-atlas-brand-experience-design.md`.
- Produces: an active PES `VS-27` slice with `implementationMode: "runtime-enabled"`, approved scope/implementation gates, and an implementation branch created from current `main`.

- [ ] **Step 1: Verify VS-26 is no longer the active implementation owner**

Run:
```bash
git fetch origin
git switch main
git pull --ff-only
npm run slice:status
```

Expected: VS-26 is certified/merged/superseded and `main` contains its final governed changes. If VS-26 is still `implementing`, stop VS-27 runtime work.

- [ ] **Step 2: Re-run the overlap check**

Run:
```bash
git diff --name-only main...origin/atlas/vs26-google-place-intelligence
```

Then compare the result specifically with:
```text
apps/api/BusinessHub.cs
apps/api/Program.cs
apps/mobile/src/api/atlas-client.ts
apps/mobile/src/components/BrandMark.tsx
apps/mobile/src/components/AtlasIcon.tsx
apps/mobile/src/features/business-hub/**
apps/mobile/app/(tabs)/_layout.tsx
apps/mobile/app/(tabs)/profile.tsx
apps/mobile/app/edit-business.tsx
apps/mobile/app/business-menu.tsx
tests/api/BusinessHubTests.cs
tests/mobile/business-hub-model.test.mjs
tests/mobile/business-hub-ui.test.mjs
tests/mobile/atlas-brand-navigation.test.mjs
```

Expected: any VS-26 overlap is reviewed before edits; do not carry stale pre-VS-26 copies forward.

- [ ] **Step 3: Create the runtime branch from current main**

Run:
```bash
git switch -c atlas/vs27-business-hub-atlas-brand
```

Expected: branch parent is the current post-VS-26 `main` SHA.

- [ ] **Step 4: Record the slice contract**

Create `docs/slices/VS-27.md` with this minimum governed scope:
```markdown
# VS-27 — Business Hub & Atlas Brand Experience

## Outcome
Owners get a read-first Business Hub showing persisted identity, operating facts, business imagery, compact menu intelligence and context status, plus a dedicated read-only menu view.

## In scope
- account-isolated Business Hub and menu read APIs
- Business tab read-first UI
- existing profile editor moved behind Edit business details
- persisted Business Media Reference preview
- persisted menu summary and full read-only menu
- locally owned Atlas Compass Orbit BrandMark
- coherent local five-tab icon family

## Out of scope
- new provider fetches or persistence
- menu editing, ordering, POS sync
- media uploads/rehosting
- Today Focus behavior changes
- onboarding/Goals/Context logic redesign
- PES navigation restructuring
- production release/deployment

## Acceptance
- no Starbucks asset/reference remains in runtime mobile code
- Business Hub renders real persisted media/menu when present and truthful empty states otherwise
- account isolation returns safe not-found behavior for another owner’s Business
- five tab route names remain unchanged
- existing profile save behavior remains available behind the edit route
- full preflight, API tests, mobile tests, Expo runtime acceptance are green
```

Update `delivery/current-slice.json` using the current schema with:
```json
{
  "sliceId": "VS-27",
  "title": "VS-27 — Business Hub & Atlas Brand Experience",
  "lifecycle": "implementing",
  "riskLevel": "medium",
  "implementationMode": "runtime-enabled",
  "allowedPaths": ["apps/api/**", "apps/mobile/**", "tests/api/**", "tests/mobile/**", "delivery/**", "docs/**"],
  "protectedPaths": [".github/workflows/release.yml", "infrastructure/**", "**/Payments/**", "**/Uploads/**"]
}
```
Preserve the repository’s complete schema/approval structure and add the post-VS-26 dependency SHA rather than replacing unrelated required fields.

- [ ] **Step 5: Validate governance before code**

Run:
```bash
npm run slice:validate
npm run governance:validate
```

Expected: both PASS.

- [ ] **Step 6: Commit the activation**

Run:
```bash
git add delivery/current-slice.json delivery/decisions.json docs/slices/VS-27.md
git commit -m "chore: activate VS-27 business hub slice"
```

---

### Task 2: Business Hub account-isolated API read model

**Files:**
- Create: `apps/api/BusinessHub.cs`
- Modify: `apps/api/Program.cs`
- Create: `tests/api/BusinessHubTests.cs`

**Interfaces:**
- Consumes: `AtlasDbContext`, `Business`, `BusinessProfile`, `BusinessMediaReference`, `BusinessOffering`, `BusinessContextEntry`, `BusinessMembership`, `MembershipRoles.BusinessOwner`.
- Produces: `BusinessHubResponse`, `BusinessHubMediaItem`, `BusinessHubMenuSummary`, `BusinessHubMenuPreviewItem`, `BusinessHubContextSummary`, and `app.MapBusinessHubEndpoints()` implementing `GET /api/v1/businesses/{businessId}/hub`.

- [ ] **Step 1: Write failing read-model tests**

Create `tests/api/BusinessHubTests.cs` with tests equivalent to:
```csharp
[Fact]
public async Task BuildAsync_returns_bounded_media_menu_summary_and_context_for_owned_business()
{
    await using var db = TestDb();
    var business = SeedOwnedBusiness(db, "owner-a");
    var observed = new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
    db.BusinessProfiles.Add(new BusinessProfile {
        BusinessId = business.Id, Description = "Turkish kebab restaurant", Address = "Valletta, Malta",
        Website = "https://hasans.example", Phone = "+356 2000 0000", Email = null, SocialChannels = null,
        BusinessHours = "Mon-Sun 11:00-23:00", Language = "English", Source = FieldSources.Public,
        OwnerConfirmed = true, UpdatedAt = observed
    });
    for (var i = 0; i < 8; i++) db.BusinessMediaReferences.Add(Media(business.Id, i, $"https://cdn.example/{i}.jpg", observed.AddMinutes(i)));
    db.BusinessOfferings.AddRange(
        Offering(business.Id, "Wraps & Pita", "Any Grill in Pita Bread", 9.50m, "EUR", observed),
        Offering(business.Id, "Beverages", "Ice Tea Peach", 2.50m, "EUR", observed.AddMinutes(1)),
        Offering(business.Id, "Beverages", "Water", 1.80m, "EUR", observed.AddMinutes(2)));
    db.BusinessContextEntries.AddRange(
        Context(business.Id, "service-style", "takeaway"),
        Context(business.Id, "customer-profile", "local and tourist"),
        Context(business.Id, "peak-period", "evening"),
        Context(business.Id, "differentiator", "Turkish grill"),
        Context(business.Id, "capacity", "small team"));
    await db.SaveChangesAsync();

    var result = await BusinessHubReader.BuildAsync(db, business.Id, "owner-a", CancellationToken.None);

    Assert.NotNull(result);
    Assert.Equal("Hasan's Turkish Kebab House", result!.Business.Name);
    Assert.Equal(6, result.Media.Count);
    Assert.Equal(3, result.Menu.ItemCount);
    Assert.Equal(2, result.Menu.SectionCount);
    Assert.Equal(1.80m, result.Menu.MinPrice);
    Assert.Equal(9.50m, result.Menu.MaxPrice);
    Assert.Equal("EUR", result.Menu.Currency);
    Assert.Equal("strong", result.Context.Status);
}

[Fact]
public async Task BuildAsync_returns_null_for_business_owned_by_another_subject()
{
    await using var db = TestDb();
    var business = SeedOwnedBusiness(db, "owner-a");
    await db.SaveChangesAsync();

    Assert.Null(await BusinessHubReader.BuildAsync(db, business.Id, "owner-b", CancellationToken.None));
}
```

Use local helpers in the same test file backed by `UseInMemoryDatabase(Guid.NewGuid().ToString())`; seed a `UserAccount`, `Business`, and `BusinessMembership` with `MembershipRoles.BusinessOwner`.

- [ ] **Step 2: Run the tests to prove RED**

Run:
```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter FullyQualifiedName~BusinessHubTests
```

Expected: FAIL because `BusinessHubReader` and response records do not exist.

- [ ] **Step 3: Implement the bounded read model**

Create `apps/api/BusinessHub.cs` with these public contracts:
```csharp
public sealed record BusinessHubResponse(
    BusinessResponse Business,
    BusinessHubProfileSummary? Profile,
    IReadOnlyList<BusinessHubMediaItem> Media,
    BusinessHubMenuSummary Menu,
    BusinessHubContextSummary Context,
    DateTimeOffset? LatestObservedAt);

public sealed record BusinessHubProfileSummary(
    string? Description, string? Address, string? Website, string? Phone,
    string? BusinessHours, string Source, bool OwnerConfirmed, DateTimeOffset UpdatedAt);

public sealed record BusinessHubMediaItem(
    string Kind, string RemoteUrl, string Source, string SourceUrl,
    DateTimeOffset ObservedAt, string Confidence, string EvidenceClass,
    bool OwnerConfirmed, string? AltText);

public sealed record BusinessHubMenuPreviewItem(
    string? Section, string Name, string? Description, decimal? Price,
    string? Currency, string Source, DateTimeOffset ObservedAt);

public sealed record BusinessHubMenuSummary(
    int SectionCount, int ItemCount, decimal? MinPrice, decimal? MaxPrice,
    string? Currency, IReadOnlyList<BusinessHubMenuPreviewItem> Preview,
    string? Source, DateTimeOffset? ObservedAt);

public sealed record BusinessHubContextSummary(int EntryCount, int OwnerConfirmedCount, string Status);
```

Implement:
```csharp
public static class BusinessHubReader
{
    public static async Task<BusinessHubResponse?> BuildAsync(
        AtlasDbContext db, Guid businessId, string providerSubject, CancellationToken ct)
    {
        var business = await db.Businesses
            .Where(b => b.Id == businessId && db.BusinessMemberships.Any(m =>
                m.BusinessId == b.Id &&
                m.UserAccount.ProviderSubject == providerSubject &&
                m.Role == MembershipRoles.BusinessOwner))
            .SingleOrDefaultAsync(ct);
        if (business is null) return null;

        var profile = await db.BusinessProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.BusinessId == businessId, ct);
        var media = await db.BusinessMediaReferences.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.Kind == "business-image")
            .OrderBy(x => x.SourceOrder).ThenByDescending(x => x.ObservedAt)
            .ToListAsync(ct);
        var offerings = await db.BusinessOfferings.AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.Kind == "menu-item")
            .OrderBy(x => x.SourceOrder).ThenBy(x => x.Section).ThenBy(x => x.Name)
            .ToListAsync(ct);
        var context = await db.BusinessContextEntries.AsNoTracking().Where(x => x.BusinessId == businessId).ToListAsync(ct);

        var safeMedia = media
            .Where(x => Uri.TryCreate(x.RemoteUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .GroupBy(x => x.RemoteUrl, StringComparer.OrdinalIgnoreCase).Select(x => x.First())
            .Take(6)
            .Select(x => new BusinessHubMediaItem(x.Kind, x.RemoteUrl, x.Source, x.SourceUrl, x.ObservedAt, x.Confidence, x.EvidenceClass, x.OwnerConfirmed, x.AltText))
            .ToList();

        var currencies = offerings.Where(x => x.Price is not null && !string.IsNullOrWhiteSpace(x.Currency))
            .Select(x => x.Currency!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var priced = offerings.Where(x => x.Price is not null).Select(x => x.Price!.Value).ToList();
        var singleCurrency = currencies.Count == 1 ? currencies[0].ToUpperInvariant() : null;
        var minPrice = singleCurrency is null || priced.Count == 0 ? null : priced.Min();
        var maxPrice = singleCurrency is null || priced.Count == 0 ? null : priced.Max();
        var preview = offerings.Take(5).Select(x => new BusinessHubMenuPreviewItem(x.Section, x.Name, x.Description, x.Price, x.Currency, x.Source, x.ObservedAt)).ToList();
        var latest = media.Select(x => (DateTimeOffset?)x.ObservedAt).Concat(offerings.Select(x => (DateTimeOffset?)x.ObservedAt)).Max();
        var status = context.Count >= 5 ? "strong" : context.Count >= 2 ? "partial" : "sparse";

        return new BusinessHubResponse(
            BusinessResponse.From(business),
            profile is null ? null : new BusinessHubProfileSummary(profile.Description, profile.Address, profile.Website, profile.Phone, profile.BusinessHours, profile.Source, profile.OwnerConfirmed, profile.UpdatedAt),
            safeMedia,
            new BusinessHubMenuSummary(offerings.Select(x => x.Section).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(), offerings.Count, minPrice, maxPrice, singleCurrency, preview, offerings.FirstOrDefault()?.Source, offerings.Select(x => (DateTimeOffset?)x.ObservedAt).Max()),
            new BusinessHubContextSummary(context.Count, context.Count(x => x.OwnerConfirmed), status),
            latest);
    }
}
```

Add `MapBusinessHubEndpoints(this WebApplication app)` in the same file. Resolve the subject with `ClaimTypes.NameIdentifier` then `sub`; return `NotFound()` for a missing/unauthorized business and `Ok(response)` otherwise. Require the existing `BusinessOwner` policy.

- [ ] **Step 4: Wire the endpoint once**

In `apps/api/Program.cs`, add:
```csharp
app.MapBusinessHubEndpoints();
```
after existing core business endpoints and before `app.Run()`.

- [ ] **Step 5: Run focused and full API tests**

Run:
```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter FullyQualifiedName~BusinessHubTests
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release
```

Expected: PASS.

- [ ] **Step 6: Commit**

Run:
```bash
git add apps/api/BusinessHub.cs apps/api/Program.cs tests/api/BusinessHubTests.cs
git commit -m "feat: add business hub read model"
```

---

### Task 3: Full persisted menu read endpoint

**Files:**
- Modify: `apps/api/BusinessHub.cs`
- Modify: `tests/api/BusinessHubTests.cs`

**Interfaces:**
- Consumes: existing `BusinessOffering` rows with `Kind == "menu-item"` and the same ownership guard as Task 2.
- Produces: `BusinessMenuResponse`, `BusinessMenuItemResponse`, `BusinessHubReader.ReadMenuAsync(...)`, and `GET /api/v1/businesses/{businessId}/offerings?kind=menu-item`.

- [ ] **Step 1: Add failing menu tests**

Add:
```csharp
[Fact]
public async Task ReadMenuAsync_returns_only_owned_menu_items_in_deterministic_order()
{
    await using var db = TestDb();
    var business = SeedOwnedBusiness(db, "owner-a");
    var at = DateTimeOffset.UtcNow;
    db.BusinessOfferings.AddRange(
        Offering(business.Id, "Wraps", "Chicken Wrap", 8m, "EUR", at),
        Offering(business.Id, "Beverages", "Water", 2m, "EUR", at),
        new BusinessOffering { BusinessId = business.Id, Kind = "service", Name = "Catering", Source = "owner", SourceUrl = "https://atlas.local", ObservedAt = at, Confidence = "high", EvidenceClass = "owner", CreatedAt = at });
    await db.SaveChangesAsync();

    var menu = await BusinessHubReader.ReadMenuAsync(db, business.Id, "owner-a", CancellationToken.None);

    Assert.NotNull(menu);
    Assert.Equal(2, menu!.Items.Count);
    Assert.Equal("Beverages", menu.Items[0].Section);
    Assert.Equal("Wraps", menu.Items[1].Section);
    Assert.Null(await BusinessHubReader.ReadMenuAsync(db, business.Id, "owner-b", CancellationToken.None));
}
```

- [ ] **Step 2: Prove RED**

Run:
```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter FullyQualifiedName~BusinessHubTests
```
Expected: FAIL because `ReadMenuAsync` does not exist.

- [ ] **Step 3: Implement the menu response and endpoint**

Add:
```csharp
public sealed record BusinessMenuItemResponse(
    Guid Id, string? Section, string Name, string? Description, decimal? Price,
    string? Currency, string Source, string SourceUrl, DateTimeOffset ObservedAt,
    string Confidence, string EvidenceClass, bool OwnerConfirmed);
public sealed record BusinessMenuResponse(IReadOnlyList<BusinessMenuItemResponse> Items, int Count);
```

Implement `ReadMenuAsync` with the same membership predicate as `BuildAsync`, then query only `Kind == "menu-item"`, order by `Section`, `SourceOrder`, `Name`, and map to the response. In the endpoint mapping, accept `string? kind`; return `ValidationProblem` with code `offering_kind_unsupported` unless `kind` is null or exactly `menu-item`; return safe `NotFound()` for a different owner.

- [ ] **Step 4: Run API tests**

Run:
```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter FullyQualifiedName~BusinessHubTests
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release
```
Expected: PASS.

- [ ] **Step 5: Commit**

Run:
```bash
git add apps/api/BusinessHub.cs tests/api/BusinessHubTests.cs
git commit -m "feat: expose persisted business menu"
```

---

### Task 4: Mobile Business Hub client contract and pure presentation model

**Files:**
- Modify: `apps/mobile/src/api/atlas-client.ts`
- Create: `apps/mobile/src/features/business-hub/business-hub-model.ts`
- Create: `tests/mobile/business-hub-model.test.mjs`

**Interfaces:**
- Consumes: API records from Tasks 2–3.
- Produces: TypeScript `BusinessHub`, `BusinessHubMedia`, `BusinessHubMenuSummary`, `BusinessMenuItem`, `BusinessMenu`, `getBusinessHub()`, `getBusinessMenu()`, plus pure functions `getHeroPresentation`, `getMenuPresentation`, `getContextPresentation`, `groupMenuItems`.

- [ ] **Step 1: Write failing model tests**

Create `tests/mobile/business-hub-model.test.mjs` using the repository’s existing TypeScript-source test loading pattern and assert:
```js
test('hero uses first media image and falls back truthfully', () => {
  assert.deepEqual(getHeroPresentation([{ remoteUrl: 'https://cdn.example/hero.jpg', altText: 'Hasan storefront' }]), {
    kind: 'image', uri: 'https://cdn.example/hero.jpg', altText: 'Hasan storefront'
  });
  assert.deepEqual(getHeroPresentation([]), { kind: 'brand-fallback' });
});

test('menu presentation summarizes persisted intelligence without ordering language', () => {
  const view = getMenuPresentation({ sectionCount: 6, itemCount: 48, minPrice: 2.5, maxPrice: 14, currency: 'EUR', preview: [], source: 'bolt-food', observedAt: '2026-08-11T12:00:00Z' });
  assert.equal(view.title, '48 menu items across 6 sections');
  assert.equal(view.priceRange, '€2.50–€14.00');
  assert.equal(view.actionLabel, 'View full menu');
});

test('context status maps to owner-readable copy', () => {
  assert.equal(getContextPresentation({ entryCount: 5, ownerConfirmedCount: 5, status: 'strong' }).title, 'Atlas has a strong operating picture');
  assert.equal(getContextPresentation({ entryCount: 1, ownerConfirmedCount: 1, status: 'sparse' }).actionLabel, 'Review business context');
});

test('groupMenuItems groups missing sections under Other', () => {
  const groups = groupMenuItems([{ id: '1', section: null, name: 'Special', description: null, price: null, currency: null, source: 'owner', sourceUrl: 'https://atlas.local', observedAt: '2026-08-11T12:00:00Z', confidence: 'high', evidenceClass: 'owner', ownerConfirmed: true }]);
  assert.equal(groups[0].section, 'Other');
  assert.equal(groups[0].items[0].name, 'Special');
});
```

- [ ] **Step 2: Prove RED**

Run:
```bash
node --test tests/mobile/business-hub-model.test.mjs
```
Expected: FAIL because the business-hub model does not exist.

- [ ] **Step 3: Add exact client contracts**

In `atlas-client.ts`, add types matching the server’s camel-cased JSON shape and:
```ts
export function getBusinessHub(accessToken: string, businessId: string): Promise<BusinessHub> {
  return request(`/api/v1/businesses/${businessId}/hub`, accessToken);
}
export function getBusinessMenu(accessToken: string, businessId: string): Promise<BusinessMenu> {
  return request(`/api/v1/businesses/${businessId}/offerings?kind=menu-item`, accessToken);
}
```

- [ ] **Step 4: Implement pure presentation functions**

Create `business-hub-model.ts` with:
```ts
export function getHeroPresentation(media: Pick<BusinessHubMedia, 'remoteUrl' | 'altText'>[]) {
  const first = media[0];
  return first ? { kind: 'image' as const, uri: first.remoteUrl, altText: first.altText ?? undefined } : { kind: 'brand-fallback' as const };
}

export function getContextPresentation(context: BusinessHubContextSummary) {
  if (context.status === 'strong') return { title: 'Atlas has a strong operating picture', copy: `${context.entryCount} business context details are shaping recommendations.`, actionLabel: 'Review business context' };
  if (context.status === 'partial') return { title: 'A few details would improve recommendations', copy: `${context.entryCount} context details are available today.`, actionLabel: 'Review business context' };
  return { title: 'Add more operating context', copy: 'A little more context will help Atlas make guidance fit the business.', actionLabel: 'Review business context' };
}

export function getMenuPresentation(menu: BusinessHubMenuSummary) {
  const title = menu.itemCount === 0 ? 'No menu observed yet' : `${menu.itemCount} menu items across ${menu.sectionCount} ${menu.sectionCount === 1 ? 'section' : 'sections'}`;
  const priceRange = menu.currency && menu.minPrice != null && menu.maxPrice != null
    ? new Intl.NumberFormat('en', { style: 'currency', currency: menu.currency }).format(menu.minPrice) + '–' + new Intl.NumberFormat('en', { style: 'currency', currency: menu.currency }).format(menu.maxPrice)
    : null;
  return { title, priceRange, actionLabel: menu.itemCount > 0 ? 'View full menu' : null };
}

export function groupMenuItems(items: BusinessMenuItem[]) {
  const groups = new Map<string, BusinessMenuItem[]>();
  for (const item of items) {
    const key = item.section?.trim() || 'Other';
    groups.set(key, [...(groups.get(key) ?? []), item]);
  }
  return [...groups.entries()].sort(([a], [b]) => a.localeCompare(b)).map(([section, grouped]) => ({ section, items: grouped }));
}
```

- [ ] **Step 5: Run model and type tests**

Run:
```bash
node --test tests/mobile/business-hub-model.test.mjs
npm run mobile:typecheck
```
Expected: PASS.

- [ ] **Step 6: Commit**

Run:
```bash
git add apps/mobile/src/api/atlas-client.ts apps/mobile/src/features/business-hub/business-hub-model.ts tests/mobile/business-hub-model.test.mjs
git commit -m "feat: add business hub mobile model"
```

---

### Task 5: Replace Starbucks prototype branding and tab glyphs with local Atlas primitives

**Files:**
- Modify: `apps/mobile/src/components/BrandMark.tsx`
- Create: `apps/mobile/src/components/AtlasIcon.tsx`
- Modify: `apps/mobile/app/(tabs)/_layout.tsx`
- Create: `tests/mobile/atlas-brand-navigation.test.mjs`

**Interfaces:**
- Consumes: existing `BrandMark({ size, style, decorative })` API and existing five Expo Router tabs.
- Produces: locally rendered `BrandMark` Compass Orbit and `AtlasIcon({ name, size, color })` for `home | business | goals | context | settings` without remote image/icon dependencies.

- [ ] **Step 1: Write failing source-contract tests**

Create assertions that read the three source files and verify:
```js
assert.doesNotMatch(brandMarkSource, /starbucks|wikimedia|PROTOTYPE_MARK_URI/i);
assert.doesNotMatch(brandMarkSource, /source=\{\{\s*uri:/);
for (const route of ['index', 'profile', 'goals', 'context', 'settings']) assert.match(layoutSource, new RegExp(`name="${route}"`));
for (const glyph of ['⌂', '◎', '↗', '◌', '⚙']) assert.ok(!layoutSource.includes(glyph));
assert.match(layoutSource, /AtlasIcon/);
```
Also assert the BrandMark keeps `accessibilityLabel={decorative ? undefined : 'Atlas brand mark'}` or equivalent semantics.

- [ ] **Step 2: Prove RED**

Run:
```bash
node --test tests/mobile/atlas-brand-navigation.test.mjs
```
Expected: FAIL because the current BrandMark references Wikimedia/Starbucks and the tab bar uses placeholder glyphs.

- [ ] **Step 3: Implement the Compass Orbit without a new dependency**

Replace `Image` with nested `View` primitives. Keep the component signature and render:
- a circular forest-green outer shell;
- a white inner ring;
- four small green directional ticks using absolutely positioned `View`s;
- a centered green node;
- no text/letter inside the mark.

Use this structural pattern:
```tsx
export function BrandMark({ size = 72, style, decorative = false }: BrandMarkProps) {
  const unit = size / 72;
  return (
    <View accessibilityElementsHidden={decorative} accessibilityLabel={decorative ? undefined : 'Atlas brand mark'} accessibilityRole={decorative ? undefined : 'image'} style={[{ width: size, height: size }, style]}>
      <View style={{ position: 'absolute', inset: 0, borderRadius: size / 2, backgroundColor: '#00754A', alignItems: 'center', justifyContent: 'center' }}>
        <View style={{ width: size * .70, height: size * .70, borderRadius: size * .35, borderWidth: Math.max(2, 3 * unit), borderColor: '#FFFFFF', alignItems: 'center', justifyContent: 'center' }}>
          <View style={{ width: Math.max(7, 10 * unit), height: Math.max(7, 10 * unit), borderRadius: Math.max(4, 5 * unit), backgroundColor: '#FFFFFF' }} />
        </View>
      </View>
      <View style={{ position: 'absolute', top: size * .08, left: size * .46, width: size * .08, height: size * .16, borderRadius: size * .04, backgroundColor: '#FFFFFF' }} />
      <View style={{ position: 'absolute', bottom: size * .08, left: size * .46, width: size * .08, height: size * .16, borderRadius: size * .04, backgroundColor: '#FFFFFF' }} />
      <View style={{ position: 'absolute', left: size * .08, top: size * .46, height: size * .08, width: size * .16, borderRadius: size * .04, backgroundColor: '#FFFFFF' }} />
      <View style={{ position: 'absolute', right: size * .08, top: size * .46, height: size * .08, width: size * .16, borderRadius: size * .04, backgroundColor: '#FFFFFF' }} />
    </View>
  );
}
```
Use `StyleProp<ViewStyle>` rather than `ImageStyle`.

- [ ] **Step 4: Implement one local icon family**

Create `AtlasIcon.tsx` using only `View` primitives with consistent 2px-equivalent strokes. Each icon must fit a `size x size` box. Use border/line geometry for home, compass/business, rising-line goals, concentric/context, and settings sliders; do not use Unicode glyphs or emoji.

Expose:
```ts
export type AtlasIconName = 'home' | 'business' | 'goals' | 'context' | 'settings';
export function AtlasIcon({ name, size = 20, color }: { name: AtlasIconName; size?: number; color: string }): React.ReactElement;
```

- [ ] **Step 5: Wire icons without changing routes**

In `(tabs)/_layout.tsx` replace `TabIcon` with:
```tsx
tabBarIcon: ({ focused }) => <AtlasIcon name="home" color={focused ? GREEN : MUTED} />
```
and corresponding names for all five existing routes. Keep text labels and existing tab bar navigation structure.

- [ ] **Step 6: Run tests/typecheck**

Run:
```bash
node --test tests/mobile/atlas-brand-navigation.test.mjs
npm run mobile:typecheck
npm run mobile:lint
```
Expected: PASS.

- [ ] **Step 7: Commit**

Run:
```bash
git add apps/mobile/src/components/BrandMark.tsx apps/mobile/src/components/AtlasIcon.tsx 'apps/mobile/app/(tabs)/_layout.tsx' tests/mobile/atlas-brand-navigation.test.mjs
git commit -m "feat: introduce Atlas brand and navigation icons"
```

---

### Task 6: Read-first Business Hub screen and preserved profile editor

**Files:**
- Create: `apps/mobile/src/features/business-hub/BusinessHubScreen.tsx`
- Create: `apps/mobile/src/features/business-hub/BusinessHero.tsx`
- Create: `apps/mobile/src/features/business-hub/BusinessSnapshotCard.tsx`
- Create: `apps/mobile/src/features/business-hub/BusinessMediaPreview.tsx`
- Create: `apps/mobile/src/features/business-hub/MenuIntelligenceCard.tsx`
- Create: `apps/mobile/src/features/business-hub/BusinessContextStatus.tsx`
- Replace contents: `apps/mobile/app/(tabs)/profile.tsx`
- Create: `apps/mobile/app/edit-business.tsx`
- Preserve/reuse: `apps/mobile/src/features/profile/profile-model.ts`
- Create: `tests/mobile/business-hub-ui.test.mjs`
- Preserve/extend: `tests/mobile/profile-model.test.mjs`

**Interfaces:**
- Consumes: `getBusinessHub`, `BusinessHub` and pure presentations from Task 4; `BrandMark` from Task 5; session `businessId`/token; existing profile model/save API.
- Produces: Business tab as read-first hub, `edit-business` route containing the existing editor semantics, and secondary routes to `business-menu` and `/(tabs)/context`.

- [ ] **Step 1: Write failing UI/source tests**

Assert the new Business Hub source contains owner-facing sections/actions:
```js
for (const text of ['BUSINESS', 'Business photos', 'Menu intelligence', 'Edit business details', 'Review business context']) assert.match(hubSource, new RegExp(text));
assert.match(profileRouteSource, /BusinessHubScreen/);
assert.match(editRouteSource, /saveProfile/);
assert.doesNotMatch(profileRouteSource, /TextInput/);
assert.match(hubSource, /getBusinessHub/);
```
Also assert the hub does not contain `Add to cart`, `Order now`, or `Checkout`.

- [ ] **Step 2: Prove RED**

Run:
```bash
node --test tests/mobile/business-hub-ui.test.mjs
```
Expected: FAIL because the Business Hub and edit route do not exist.

- [ ] **Step 3: Move, do not rewrite, the existing editor semantics**

Create `app/edit-business.tsx` by moving the current edit-first `profile.tsx` implementation into this route. Keep:
- `getProfile` / `saveProfile`;
- `profileSections` / `canSaveProfile` / confirmation behavior;
- loading/missing/error/retry semantics;
- existing accessibility labels and keyboard behavior.

Change only the header copy needed to identify this as `EDIT BUSINESS DETAILS`; do not change save policy.

Replace `(tabs)/profile.tsx` with:
```tsx
import { BusinessHubScreen } from '@/features/business-hub/BusinessHubScreen';
export default BusinessHubScreen;
```

- [ ] **Step 4: Implement BusinessHubScreen state orchestration**

Use `loadSession()` and `getBusinessHub()` once per load/refresh. States must be exactly `loading | ready | missing | error`. On ready, render focused components; on missing, use the existing guarded session continuation; on error, offer `Try again` without raw server text.

Use a white/warm-surface layout consistent with approved Atlas screens: max-width 680, 28px/pt horizontal padding on phone, forest editorial heading, restrained cards, no dense dashboard grid.

- [ ] **Step 5: Implement BusinessHero**

Behavior:
- first safe media URL is the hero;
- `Image` uses persisted `altText` when meaningful;
- `onError` switches locally to the Atlas BrandMark fallback without invalidating the entire screen;
- fallback includes BrandMark and business identity, never a broken-image box;
- name, category, primary location remain visible regardless of image state.

- [ ] **Step 6: Implement the read-only operating snapshot**

`BusinessSnapshotCard` displays only available facts among location, hours, phone, website and category. Do not render empty labeled rows. Source/owner-confirmation wording is secondary copy, e.g. `Owner confirmed` or `Observed from public business information`.

- [ ] **Step 7: Implement the bounded media preview**

`BusinessMediaPreview` renders up to six images, with one dominant image and smaller cards using wrapping layout. If `media.length === 0`, return `null` rather than an empty gallery shell. Individual image failures hide only that image.

- [ ] **Step 8: Implement compact menu intelligence**

`MenuIntelligenceCard` uses `getMenuPresentation()` and displays:
- item/section summary;
- price range only when supplied;
- at most 3 representative preview items on the hub;
- source/freshness as secondary copy;
- `View full menu` only when `itemCount > 0`.

Route with:
```ts
router.push('/business-menu');
```

- [ ] **Step 9: Implement context status and secondary actions**

`BusinessContextStatus` uses `getContextPresentation()` and routes to `/(tabs)/context`. `Edit business details` routes to `/edit-business`. Keep these as secondary actions; do not turn the page into an action grid.

- [ ] **Step 10: Run focused and existing profile regressions**

Run:
```bash
node --test tests/mobile/business-hub-ui.test.mjs tests/mobile/business-hub-model.test.mjs tests/mobile/profile-model.test.mjs
npm run mobile:typecheck
npm run mobile:lint
```
Expected: PASS.

- [ ] **Step 11: Commit**

Run:
```bash
git add apps/mobile/src/features/business-hub 'apps/mobile/app/(tabs)/profile.tsx' apps/mobile/app/edit-business.tsx tests/mobile/business-hub-ui.test.mjs tests/mobile/profile-model.test.mjs
git commit -m "feat: build read-first business hub"
```

---

### Task 7: Dedicated read-only Business Menu screen

**Files:**
- Create: `apps/mobile/src/features/business-hub/BusinessMenuScreen.tsx`
- Create: `apps/mobile/app/business-menu.tsx`
- Modify: `tests/mobile/business-hub-ui.test.mjs`

**Interfaces:**
- Consumes: `getBusinessMenu()`, `groupMenuItems()`, current session token/businessId.
- Produces: `/business-menu` grouped read-only menu route with truthful empty/error states and provenance detail.

- [ ] **Step 1: Add failing menu-screen source assertions**

Assert:
```js
assert.match(menuScreenSource, /getBusinessMenu/);
assert.match(menuScreenSource, /groupMenuItems/);
for (const text of ['MENU', 'No menu observed yet', 'Try again']) assert.match(menuScreenSource, new RegExp(text));
for (const forbidden of ['Add to cart', 'Checkout', 'Quantity']) assert.ok(!menuScreenSource.includes(forbidden));
```

- [ ] **Step 2: Prove RED**

Run:
```bash
node --test tests/mobile/business-hub-ui.test.mjs
```
Expected: FAIL because the menu route/screen does not exist.

- [ ] **Step 3: Implement the route and screen**

`app/business-menu.tsx` should export `BusinessMenuScreen` only. The screen must:
- load session and menu once;
- group items using `groupMenuItems()`;
- render section headers and vertical item cards, never a horizontal table;
- show description only when present;
- show localized price only when both price/currency are present;
- show source and observed date in muted secondary text;
- show `No menu observed yet` as a valid empty state;
- expose `Try again` on API failure;
- never expose raw provider/server errors.

- [ ] **Step 4: Run tests/typecheck**

Run:
```bash
node --test tests/mobile/business-hub-ui.test.mjs tests/mobile/business-hub-model.test.mjs
npm run mobile:typecheck
npm run mobile:lint
```
Expected: PASS.

- [ ] **Step 5: Commit**

Run:
```bash
git add apps/mobile/src/features/business-hub/BusinessMenuScreen.tsx apps/mobile/app/business-menu.tsx tests/mobile/business-hub-ui.test.mjs
git commit -m "feat: add business menu intelligence view"
```

---

### Task 8: Integrated regression, Expo acceptance and certification-ready evidence

**Files:**
- Modify only if required for governed evidence: `docs/slices/VS-27.md`, `delivery/current-slice.json`
- Do not modify release/deployment configuration.

**Interfaces:**
- Consumes: all Tasks 1–7.
- Produces: exact-head test evidence suitable for PES transition from `testing` to `certification`; no merge/release/deployment.

- [ ] **Step 1: Run deterministic repository gates**

Run:
```bash
git diff --check
npm run slice:validate
npm run preflight
dotnet build apps/api/Atlas.Api.csproj --configuration Release
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release
```
Expected: PASS.

- [ ] **Step 2: Assert Starbucks removal repository-wide in mobile runtime code**

Run:
```bash
grep -RniE 'starbucks|wikimedia.*starbucks' apps/mobile || true
```
Expected: no runtime matches. Documentation/spec historical references are not part of this runtime grep.

- [ ] **Step 3: Run the complete mobile suite serially**

Run:
```bash
npm run mobile:test
npm run mobile:typecheck
npm run mobile:lint
```
Expected: PASS.

- [ ] **Step 4: Run authentic Expo runtime acceptance**

From `apps/mobile`, run the repository-approved Expo test path. For Expo Go manual acceptance, start with cache cleared:
```bash
EXPO_PUBLIC_API_URL=https://atlas-api-test.onrender.com npx expo start -c
```

Verify on a narrow phone viewport/device:
1. Home still loads and the shared Atlas mark/nav looks coherent.
2. Business tab opens as a read-first hub, not an edit form.
3. Hasan's Turkish Kebab House identity/location render from persisted Atlas data.
4. Persisted restaurant imagery appears when available; a failed image falls back without breaking the screen.
5. Menu intelligence shows item/section count and price range when data exists.
6. `View full menu` opens grouped menu items and prices; there are no ordering controls.
7. `Edit business details` opens the preserved editor and a save still succeeds.
8. `Review business context` routes to Context.
9. Goals, Context and Settings tabs still work under the same five-route structure.

Do not mark acceptance passed from a screenshot alone; validate interactions.

- [ ] **Step 5: Record testing evidence without authorizing release**

Update `delivery/current-slice.json` only through allowed PES lifecycle transitions: move `implementing -> testing` once implementation is complete, then `testing -> certification` only after deterministic and runtime gates are green. Keep:
```json
"release": { "status": "not-authorized", "releaseId": null }
```
and production-enable approval pending.

- [ ] **Step 6: Final exact-head review and commit**

Run:
```bash
git status --short
git log -1 --oneline
```
Ensure only expected VS-27 paths changed, then commit governance/evidence updates:
```bash
git add delivery/current-slice.json docs/slices/VS-27.md
git commit -m "docs: record VS-27 test evidence"
```

- [ ] **Step 7: Open the implementation PR and stop before merge**

Open a PR titled:
```text
VS-27: Business Hub & Atlas Brand Experience
```
The body must include exact implementation SHA, CI/Security/Product Intake status when available, API/mobile test results, Expo runtime acceptance, and an explicit statement that no production release/deployment is authorized. Stop for human merge approval.
