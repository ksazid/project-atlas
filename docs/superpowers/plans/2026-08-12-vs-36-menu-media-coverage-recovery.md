# VS-36 Menu & Media Coverage Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve truthful public menu/media extraction and distinguish renderer-dependent provider pages from genuine no-menu/no-media observations without adding a new provider, migration, browser scraper, or owner workflow.

**Architecture:** Extend the existing `PublicBusinessMediaMenuExtractor` with explicit coverage classification and graph-linked JSON-LD resolution, propagate renderer-required coverage through `PublicBusinessSnapshot` into the existing `BusinessSourceObservation.WarningCode -> BusinessDiscoverySource.WarningCode` provenance path, and leave `BusinessMediaReference` / `BusinessOffering` materialisation unchanged. Only parse data present in the fetched public HTML; do not impersonate crawlers, call private provider APIs, execute JavaScript, or add speculative unknown embedded-state parsing.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core/PostgreSQL existing persistence, xUnit, Expo SDK 54 unchanged.

## Global Constraints

- Start from merged VS-35 main `ff4ca1ac2245ef8f29f4efc5d04693d99f7ec597`.
- Scope is limited to FR-02, FR-03 and FR-05 under DEC-04 and DEC-07.
- Preserve `PublicBusinessUrlPolicy`, SSRF-safe fetching, standard HTTPS-only source/media boundaries and existing 750,000-character public HTML limit.
- Preserve `PublicBusinessMediaMenuExtractor.MaxMediaPerSource = 24` and `MaxOfferingsPerSource = 250`.
- Never copy/rehost third-party image binaries; persist remote HTTPS references and structured factual offerings only.
- No private/undocumented Bolt or Wolt APIs, crawler impersonation, production browser automation, search-cache evidence or speculative generic embedded-state parser.
- No database migration solely for coverage diagnostics.
- Do not alter Today / History / Goals / Profile, Pilot Operations, Knowledge Packs, Google enrichment or owner authority.
- No deployment, EAS build/submit/OTA, release, production enablement or production database mutation.

---

### Task 1: Explicit menu/media coverage classification

**Files:**
- Modify: `apps/api/BusinessDiscoveryMediaMenu.cs`
- Modify: `apps/api/BusinessDiscovery.cs`
- Test: `tests/api/BusinessDiscoveryMediaMenuTests.cs`

**Interfaces:**
- Produces `PublicBusinessMediaMenuCoverage` constants: `Structured`, `SemanticHtml`, `EmbeddedPublicState`, `MediaOnly`, `RendererRequired`, `None`.
- Extends `PublicBusinessMediaMenuExtraction` with `string Coverage`.
- Extends `PublicBusinessSnapshot` with `string MediaMenuCoverage { get; init; } = PublicBusinessMediaMenuCoverage.None`.
- `PublicBusinessExtractor.Extract` copies `enrichment.Coverage` to `PublicBusinessSnapshot.MediaMenuCoverage`.

- [ ] **Step 1: Write failing coverage tests**

Add focused tests to `BusinessDiscoveryMediaMenuTests.cs`:

```csharp
[Fact]
public void Coverage_distinguishes_structured_semantic_media_renderer_and_none()
{
    var now = new DateTimeOffset(2026, 8, 12, 20, 0, 0, TimeSpan.Zero);
    var website = new Uri("https://restaurant.example.com/");
    var bolt = new Uri("https://food.bolt.eu/en/324-valletta/p/1257-chickn-bites/");

    var structured = PublicBusinessMediaMenuExtractor.Extract("website", website,
        """<script type="application/ld+json">{"@type":"Restaurant","name":"Atlas Kitchen","hasMenu":{"@type":"Menu","hasMenuItem":{"@type":"MenuItem","name":"Kebab"}}}</script>""", now);
    Assert.Equal(PublicBusinessMediaMenuCoverage.Structured, structured.Coverage);

    var semantic = PublicBusinessMediaMenuExtractor.Extract("bolt-food", bolt,
        """<h2 class="provider-menu-category-title">Mains</h2><li class="provider-menu-dish"><img alt="Kebab" src="https://images.bolt.eu/kebab.jpg"><span class="provider-menu-dish-price">€9.50</span></li>""", now);
    Assert.Equal(PublicBusinessMediaMenuCoverage.SemanticHtml, semantic.Coverage);

    var mediaOnly = PublicBusinessMediaMenuExtractor.Extract("website", website,
        """<meta property="og:image" content="https://cdn.example.com/cover.jpg">""", now);
    Assert.Equal(PublicBusinessMediaMenuCoverage.MediaOnly, mediaOnly.Coverage);

    var renderer = PublicBusinessMediaMenuExtractor.Extract("bolt-food", bolt,
        """<html><body>Oh no! It looks like JavaScript is not enabled in your browser.</body></html>""", now);
    Assert.Equal(PublicBusinessMediaMenuCoverage.RendererRequired, renderer.Coverage);
    Assert.Empty(renderer.Media);
    Assert.Empty(renderer.Offerings);

    var none = PublicBusinessMediaMenuExtractor.Extract("website", website, "<html><body>Open daily</body></html>", now);
    Assert.Equal(PublicBusinessMediaMenuCoverage.None, none.Coverage);
}
```

Also assert an existing structured fixture returns `structured` and the certified Hasan Bolt semantic fixture returns `semantic-html`.

- [ ] **Step 2: Run RED**

Run:
```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter "FullyQualifiedName~BusinessDiscoveryMediaMenuTests"
```
Expected: FAIL because `Coverage`, `PublicBusinessMediaMenuCoverage`, and `MediaMenuCoverage` do not exist.

- [ ] **Step 3: Implement minimal classification**

In `BusinessDiscoveryMediaMenu.cs`, add:

```csharp
public static class PublicBusinessMediaMenuCoverage
{
    public const string Structured = "structured";
    public const string SemanticHtml = "semantic-html";
    public const string EmbeddedPublicState = "embedded-public-state";
    public const string MediaOnly = "media-only";
    public const string RendererRequired = "renderer-required";
    public const string None = "none";
}
```

Extend the extraction record:

```csharp
public sealed record PublicBusinessMediaMenuExtraction(
    IReadOnlyList<PublicBusinessMedia> Media,
    IReadOnlyList<PublicBusinessOffering> Offerings,
    string? MenuUrl,
    string Coverage);
```

Track whether JSON-LD contributed media/offerings/menu data and whether Bolt semantic markup contributed media/offerings. Determine coverage in this precedence:
1. structured contribution -> `structured`;
2. semantic contribution -> `semantic-html`;
3. any safe media only -> `media-only`;
4. supported provider (`bolt-food` or `wolt`) + a bounded case-insensitive renderer marker such as `JavaScript is not enabled` and no parsed menu/media -> `renderer-required`;
5. otherwise `none`.

Do not classify an OG-only image as structured. Do not classify ordinary websites with no data as renderer-required.

In `BusinessDiscovery.cs`, extend `PublicBusinessSnapshot`:

```csharp
public string MediaMenuCoverage { get; init; } = PublicBusinessMediaMenuCoverage.None;
```

and return:

```csharp
Media = enrichment.Media,
Offerings = enrichment.Offerings,
MediaMenuCoverage = enrichment.Coverage
```

- [ ] **Step 4: Run GREEN**

Run the same focused command. Expected: PASS, including all existing VS-25 media/menu tests.

- [ ] **Step 5: Commit**

```bash
git add apps/api/BusinessDiscoveryMediaMenu.cs apps/api/BusinessDiscovery.cs tests/api/BusinessDiscoveryMediaMenuTests.cs
git commit -m "feat(vs36): classify public menu media coverage"
```

---

### Task 2: Resolve graph-linked schema.org menus and MenuItem images

**Files:**
- Modify: `apps/api/BusinessDiscoveryMediaMenu.cs`
- Test: `tests/api/BusinessDiscoveryMediaMenuTests.cs`

**Interfaces:**
- Consumes the Task 1 extraction model and limits.
- Produces no new public API type; improves `Extract(...)` results only.

- [ ] **Step 1: Write failing graph-linked JSON-LD test**

Add:

```csharp
[Fact]
public void Structured_graph_resolves_menu_references_and_menu_item_images()
{
    var now = new DateTimeOffset(2026, 8, 12, 20, 15, 0, TimeSpan.Zero);
    var source = new Uri("https://restaurant.example.com/");
    const string html = """
      <script type="application/ld+json">
      {
        "@context":"https://schema.org",
        "@graph":[
          {"@type":"Restaurant","@id":"#business","name":"Atlas Kitchen","hasMenu":{"@id":"#menu"}},
          {"@type":"Menu","@id":"#menu","hasMenuSection":{"@id":"#mains"}},
          {"@type":"MenuSection","@id":"#mains","name":"Mains","hasMenuItem":{"@id":"#kebab"}},
          {"@type":"MenuItem","@id":"#kebab","name":"Chicken Kebab","description":"Chargrilled chicken","image":{"@type":"ImageObject","contentUrl":"https://cdn.example.com/kebab.jpg"},"offers":{"@type":"Offer","price":"12.50","priceCurrency":"EUR"}}
        ]
      }
      </script>
      """;

    var result = PublicBusinessMediaMenuExtractor.Extract("website", source, html, now);

    var offering = Assert.Single(result.Offerings);
    Assert.Equal("Mains", offering.Section);
    Assert.Equal("Chicken Kebab", offering.Name);
    Assert.Equal(12.50m, offering.Price);
    Assert.Equal("EUR", offering.Currency);
    Assert.Contains(result.Media, media =>
        media.Kind == "menu-item-image" &&
        media.RemoteUrl == "https://cdn.example.com/kebab.jpg" &&
        media.AltText == "Chicken Kebab");
    Assert.Equal(PublicBusinessMediaMenuCoverage.Structured, result.Coverage);
}
```

Add a second assertion/fixture proving `http://` MenuItem images are rejected while the offering remains.

- [ ] **Step 2: Run RED**

Run the focused media/menu test class. Expected: graph-linked menu produces no offering/image with current traversal.

- [ ] **Step 3: Implement bounded graph resolution**

For each JSON-LD root, build a case-sensitive `Dictionary<string, JsonElement>` from objects with non-empty `@id`. Add a local resolver used only within that root:

```csharp
JsonElement ResolveReference(JsonElement element)
{
    if (element.ValueKind == JsonValueKind.Object &&
        TryGetString(element, "@id", out var id) &&
        graphById.TryGetValue(id!, out var resolved))
        return resolved;
    return element;
}
```

Resolve object references before reading Menu/MenuSection/MenuItem properties and when traversing `hasMenuSection`, `hasMenuItem`, and `itemListElement`. Add a visited `HashSet<string>` for resolved `@id` values to prevent cycles.

Inside `CaptureOffering`, after adding the offering, inspect `image` and reuse `ImageUrls(...)`. For each safe HTTPS image under the existing media cap, add:

```csharp
new PublicBusinessMedia(
    "menu-item-image",
    canonical,
    provider,
    sourceUrl,
    observedAt,
    "high",
    AltText: name.Trim())
```

Do not alter business-image logic or copy image bytes.

- [ ] **Step 4: Run GREEN**

Run focused tests. Expected: graph reference + menu-item image tests pass and all previous extraction tests remain green.

- [ ] **Step 5: Commit**

```bash
git add apps/api/BusinessDiscoveryMediaMenu.cs tests/api/BusinessDiscoveryMediaMenuTests.cs
git commit -m "feat(vs36): recover graph linked structured menus"
```

---

### Task 3: Propagate renderer-required coverage through existing discovery provenance

**Files:**
- Modify: `apps/api/MultiSourceBusinessDiscoveryService.cs`
- Test: `tests/api/BusinessDiscoveryMultiSourcePersistenceTests.cs`
- Test: `tests/api/BusinessDiscoveryReconciliationTests.cs`

**Interfaces:**
- Consumes `PublicBusinessSnapshot.MediaMenuCoverage`.
- Produces existing warning code `business_source_menu_renderer_required` through `BusinessSourceObservation.WarningCode` and existing `BusinessDiscoverySource.WarningCode` persistence.
- No schema change.

- [ ] **Step 1: Write failing warning propagation tests**

Add a reconciliation test where a successful Bolt `BusinessSourceObservation` has useful `name/category` facts, no media/offerings and `WarningCode: "business_source_menu_renderer_required"`. Assert:

```csharp
Assert.Contains("business_source_menu_renderer_required", result.Warnings);
Assert.Equal("business_source_menu_renderer_required", Assert.Single(result.SourceResults).WarningCode);
Assert.DoesNotContain(result.Snapshot.Facts, fact => fact.Key.Contains("coverage", StringComparison.OrdinalIgnoreCase));
```

Add a persistence test creating a `BusinessDiscoveryReconciliationResult` with the warning and asserting:

```csharp
var source = Assert.Single(snapshot.Sources);
Assert.Equal("business_source_menu_renderer_required", source.WarningCode);
Assert.DoesNotContain(snapshot.Facts, fact => fact.Key.Contains("coverage", StringComparison.OrdinalIgnoreCase));
Assert.Empty(snapshot.Offerings);
Assert.Empty(snapshot.Media);
```

Add a source-service characterization using a fake/fixture `BusinessDiscoveryService` path already used by multi-source tests, or source-level static assertion if the existing test harness is constructor-bound, proving `renderer-required` maps to that warning while source status remains `success`.

- [ ] **Step 2: Run RED**

Run:
```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter "FullyQualifiedName~BusinessDiscoveryMultiSourcePersistenceTests|FullyQualifiedName~BusinessDiscoveryReconciliationTests"
```
Expected: provenance/persistence tests may already pass when warning is supplied manually; the service-level mapping assertion must fail because `MediaMenuCoverage` is not yet mapped to `WarningCode`.

- [ ] **Step 3: Implement mapping in `MultiSourceBusinessDiscoveryService`**

Add:

```csharp
private const string RendererRequiredWarning = "business_source_menu_renderer_required";

private static string? CoverageWarning(PublicBusinessSnapshot snapshot) =>
    snapshot.MediaMenuCoverage == PublicBusinessMediaMenuCoverage.RendererRequired
        ? RendererRequiredWarning
        : null;
```

When adding successful owner-supplied and accepted official-site observations, pass:

```csharp
WarningCode: CoverageWarning(snapshot),
Media: snapshot.Media,
Offerings: snapshot.Offerings
```

Keep `Status = "success"` because menu/media enrichment absence does not invalidate useful Business identity/profile facts.

- [ ] **Step 4: Run GREEN**

Run the focused multi-source/reconciliation/persistence tests. Expected: warning survives existing provenance path with no Business fact, no materialized menu/media, and no migration.

- [ ] **Step 5: Commit**

```bash
git add apps/api/MultiSourceBusinessDiscoveryService.cs tests/api/BusinessDiscoveryMultiSourcePersistenceTests.cs tests/api/BusinessDiscoveryReconciliationTests.cs
git commit -m "feat(vs36): retain renderer dependent coverage warning"
```

---

### Task 4: Preserve materialisation and source-isolation invariants

**Files:**
- Test: `tests/api/BusinessDiscoveryMediaMenuPersistenceTests.cs`
- Test: `tests/api/BusinessDiscoveryMediaMenuTests.cs`
- Production changes only if these regressions expose an actual defect in existing persistence/reconciliation code.

**Interfaces:**
- Consumes existing `BusinessMediaMenuPersistence`, reconciliation and Task 1-3 extraction/provenance.
- Produces no new runtime API.

- [ ] **Step 1: Add regression assertions**

Extend existing tests to prove:
- mismatched secondary Business media/menu remains excluded;
- renderer-required source warning creates zero `BusinessOffering` and zero `BusinessMediaReference` records after confirmation;
- structured/semantic accepted media and offerings remain `OwnerConfirmed = false` after materialisation unless existing explicit policy says otherwise;
- safe remote media URLs remain HTTPS references, not copied content.

- [ ] **Step 2: Run focused tests**

Run:
```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter "FullyQualifiedName~BusinessDiscoveryMediaMenuPersistenceTests|FullyQualifiedName~BusinessDiscoveryMediaMenuTests"
```
Expected: PASS if existing VS-25 invariants already hold. If any test fails, invoke `systematic-debugging` before changing production code and make the smallest TDD fix inside the existing persistence/reconciliation files.

- [ ] **Step 3: Commit tests or bounded fix**

```bash
git add tests/api/BusinessDiscoveryMediaMenuPersistenceTests.cs tests/api/BusinessDiscoveryMediaMenuTests.cs apps/api/BusinessMediaMenuPersistence.cs apps/api/BusinessDiscoveryReconciliation.cs
git commit -m "test(vs36): lock menu media provenance invariants"
```
Only stage production files if they actually changed.

---

### Task 5: Full verification, review, PES certification and merge

**Files:**
- Modify: `delivery/current-slice.json` only for certification metadata after the frozen runtime head passes all gates.
- Modify: `docs/slices/VS-36.md` with final evidence/status.
- Update PR # created for VS-36 with exact test evidence.

**Interfaces:**
- Consumes the frozen runtime SHA produced after Tasks 1-4.
- Produces exact-SHA PES certification; release/production remain unauthorized.

- [ ] **Step 1: Run deterministic repository gates on frozen runtime SHA**

Required:
```bash
npm run governance:validate
npm run preflight
dotnet build apps/api/Atlas.Api.csproj --configuration Release
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release
npm run dashboard:build
```
CI must also replay the full PostgreSQL migration chain on clean PostgreSQL 17 even though VS-36 adds no migration.

- [ ] **Step 2: Exact-head GitHub gates**

Require CI, Security baseline and Product Intake success for the same 40-character runtime SHA. Record mobile/API counts, PostgreSQL replay and dashboard artifact evidence.

- [ ] **Step 3: Changed-file review**

Confirm:
- no private provider API/crawler impersonation/browser automation;
- no migration;
- no copied binary media;
- no Today/navigation/Pilot Operations changes;
- renderer-required is provenance warning, not a Business fact/owner claim;
- current VS-25 semantic extraction and mismatch isolation remain green.

- [ ] **Step 4: PES certification commit**

Update `delivery/current-slice.json` lifecycle to `certified`, certification approval to the frozen runtime SHA, progress implementation/testing/certification to 100, release to 0, and add exact evidence. Update `docs/slices/VS-36.md` accordingly. This commit must be governance/docs-only.

- [ ] **Step 5: Post-certification exact-head gates and merge**

Require CI + Security baseline + Product Intake green on the governance-only certification head. Then mark the VS-36 PR ready and merge with an expected-head guard under the Product Owner’s standing merge approval. Do not deploy or release.
