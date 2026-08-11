# VS-26 Google Place Intelligence & Goal-First Onboarding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enrich a uniquely resolved Business with a bounded, structured Google Places operating snapshot, let the owner confirm only the important operating facts, then move onboarding to Goals first and suppress redundant factual questions.

**Architecture:** Keep `GoogleBusinessLocationProvider` as the Business identity/location boundary. After one location is selected, call a new `IBusinessPlaceEnrichmentProvider` through an owner-authorized snapshot endpoint; return Places content only for the active confirmation interaction, retain only the Place ID as provider content, and persist confirmed canonical operating facts as owner data. Progressive-question selection consumes those owner-confirmed canonical facts and asks at most three high-value non-goal questions after Goals when enrichment succeeded.

**Tech Stack:** ASP.NET Core / C# / `HttpClient`, EF Core + PostgreSQL, Expo React Native / TypeScript / Expo Router, xUnit, Node mobile model/runtime tests, PES governance and Superpowers TDD.

## Global Constraints

- Runtime implementation MUST NOT start until VS-25 / PR #45 is merged and the final VS-25-integrated `main` is re-read.
- Runtime implementation MUST branch from the then-current `main`; do not turn `atlas/vs26-google-place-goal-first-design` into the implementation branch.
- Preserve `ATLAS-DESIGN-001` v1.2: existing Atlas warm-neutral/deep-green visual grammar, shared tokens, `BrandMark`, low cognitive load, no redesign and no new navigation model.
- Google Maps HTML scraping is prohibited. Use Places API (New) Place Details only after an exact Place ID is selected.
- Never call rich Place Details for every search candidate. One selected Place may trigger one request in the current owner interaction.
- Use an explicit field mask; wildcard field masks are prohibited.
- Google Places policy is authoritative at execution time. Current policy says Places API content must not be pre-fetched, cached or stored beyond allowed exceptions; Place IDs are exempt. Therefore Google response values remain transient and only the Place ID is retained as Google Maps Content. Owner-confirmed canonical facts are stored as owner data after the owner explicitly confirms them.
- Any displayed Google Maps Content must carry required Google Maps and third-party attribution in the same visual container. Use compliant text attribution (`Google Maps`) if the official logo asset is not already present; do not invent or modify a Google logo.
- Goals are always owner-selected. Never infer, preselect or persist Business Goals from Google, Bolt, Wolt, websites, menus, category stereotypes or reviews.
- Public provider failure is non-blocking. Valid Business creation must continue through the existing degraded path.
- No production release, production enablement, EAS build/submit/OTA or deployment is authorized by this plan.
- Before readiness claims run `npm run governance:validate`, `npm run preflight`, the exact focused tests below, full API/mobile tests, Security baseline and Product Intake on the exact head.

---

### Task 0: Rebase the design onto the final VS-25 baseline and activate governed VS-26

**Files:**
- Modify: `delivery/current-slice.json`
- Modify: `delivery/decisions.json`
- Create: `docs/slices/VS-26.md`
- Review: `AGENTS.md`
- Review: `product/PRD.md`
- Review: `product/TRD.md`
- Review: `product/DESIGN.md`
- Review: `docs/slices/VS-25.md`
- Review: `docs/superpowers/specs/2026-08-11-vs-25-business-media-menu-intelligence-design.md`
- Review: `docs/superpowers/plans/2026-08-11-vs-25-business-media-menu-intelligence.md`

**Interfaces:**
- Consumes: the final merged VS-25 `main` SHA and its exact entity/reconciliation contracts.
- Produces: a fresh implementation branch `atlas/vs26-google-place-intelligence`, active VS-26 scope/policy/implementation records, and `DEC-08` documenting the approved provider/caching/question-volume policy.

- [ ] **Step 1: Verify the dependency is actually merged**

Run:

```bash
gh pr view 45 --repo ksazid/project-atlas --json state,mergedAt,mergeCommit,headRefOid
```

Expected: `mergedAt` is non-null. If it is null, STOP. Do not create runtime files.

- [ ] **Step 2: Create the implementation branch from final `main`**

Run:

```bash
git switch main
git pull --ff-only
git switch -c atlas/vs26-google-place-intelligence
```

Then record:

```bash
git rev-parse HEAD
```

Expected: the SHA equals GitHub's current `main` and contains the merged VS-25 files/migration.

- [ ] **Step 3: Re-read authority and final VS-25 contracts**

Run:

```bash
cat AGENTS.md
cat product/PRD.md
cat product/TRD.md
cat product/DESIGN.md
cat delivery/governance.json
cat delivery/decisions.json
cat delivery/current-slice.json
cat docs/slices/VS-25.md
cat docs/superpowers/specs/2026-08-11-vs-25-business-media-menu-intelligence-design.md
```

Expected: no active implementing/testing slice conflicts with VS-26 activation. If VS-25 is still active in a runtime lifecycle, close/transition it through the repository's legal governance transitions before activating VS-26; never overwrite an active slice silently.

- [ ] **Step 4: Re-check current Google provider policy before writing runtime code**

Review the official current pages:

```text
https://developers.google.com/maps/documentation/places/web-service/place-details
https://developers.google.com/maps/documentation/places/web-service/policies
https://developers.google.com/maps/documentation/places/web-service/reference/rest/v1/places
https://developers.google.com/maps/comms/eea/places
```

Lock the implementation to these rules unless current official terms are stricter: exact Place ID request, explicit field mask, required attribution, Place ID persistence allowed, no persistence of other Places content.

- [ ] **Step 5: Record `DEC-08` and activate VS-26**

Add an approved decision with this decision text:

```text
After Atlas uniquely resolves a Business to one Google Place, use one bounded Places API (New) Place Details request for operating enrichment regardless of whether discovery began from a website, Bolt, Wolt or Google URL. Google response content is interaction-transient; Atlas retains the Place ID only as provider content and stores canonical operating facts only after explicit owner confirmation. For sufficiently enriched Businesses, optional non-goal onboarding targets 0-3 questions, with five retained only as the degraded/missing-context ceiling. Goals remain owner-only.
```

Activate `VS-26 — Google Place Intelligence & Goal-First Onboarding` with:
- requirements: `FR-02`, `FR-03`, `FR-04`, `FR-05`, `FR-16`;
- risk: `medium`;
- implementationMode: `runtime-enabled`;
- dependency on the exact merged VS-25 SHA;
- allowed paths: `apps/api/**`, `apps/mobile/**`, `tests/api/**`, `tests/mobile/**`, `delivery/**`, `docs/**`;
- release and production-enable pending/not authorized.

- [ ] **Step 6: Validate governance before runtime edits**

Run:

```bash
npm run governance:validate
npm run preflight
```

Expected: PASS before Task 1 starts.

- [ ] **Step 7: Commit governance activation**

```bash
git add delivery/current-slice.json delivery/decisions.json docs/slices/VS-26.md
git commit -m "chore(vs26): activate place intelligence onboarding"
```

---

### Task 1: Add a bounded Google Place enrichment provider

**Files:**
- Create: `apps/api/BusinessPlaceEnrichment.cs`
- Create: `tests/api/BusinessPlaceEnrichmentTests.cs`
- Modify: `apps/api/Program.cs` only if DI registration is required by the final VS-25 baseline

**Interfaces:**
- Consumes: canonical Google `ProviderRef` / Place ID from `BusinessLocationCandidate`.
- Produces:

```csharp
public sealed record BusinessPlaceAttribution(string Provider, string? ProviderUri);

public sealed record BusinessPlaceEnrichment(
    string ProviderRef,
    DateTimeOffset ObservedAt,
    IReadOnlyList<string> OperatingChannels,
    bool? Reservable,
    IReadOnlyList<string> ServicePeriods,
    string? PricePosition,
    IReadOnlyList<string> OpeningHours,
    IReadOnlyList<BusinessPlaceAttribution> Attributions);

public interface IBusinessPlaceEnrichmentProvider
{
    bool IsConfigured { get; }
    Task<BusinessPlaceEnrichment?> GetAsync(string providerRef, CancellationToken ct);
}
```

- [ ] **Step 1: Write RED provider tests**

Create tests proving:

```csharp
[Fact]
public async Task Google_provider_uses_exact_place_id_and_explicit_field_mask()
{
    // Arrange a fake HttpMessageHandler that records request URI/headers and returns:
    // dineIn=true, takeout=true, delivery=true, reservable=true,
    // servesLunch=true, servesDinner=true, PRICE_LEVEL_MODERATE,
    // weekdayDescriptions and one attribution.
    // Assert GET /v1/places/ChIJAtlas123 and exact X-Goog-FieldMask.
}

[Fact]
public async Task Google_provider_maps_absent_boolean_as_unknown_not_false()
{
    // Response omits reservable and delivery.
    // Assert enrichment.Reservable is null and channels do not contain Delivery.
}

[Fact]
public async Task Google_provider_never_uses_wildcard_field_mask()
{
    Assert.DoesNotContain("*", GoogleBusinessPlaceEnrichmentProvider.PlaceDetailsFieldMask);
}

[Fact]
public async Task Google_provider_degrades_on_404_without_leaking_key()
{
    // Assert null / provider-unavailable result according to implementation below,
    // and no exception message contains the configured API key.
}
```

Use this exact initial field mask unless the execution-time official-policy check removes a field:

```text
id,dineIn,takeout,delivery,reservable,servesBreakfast,servesBrunch,servesLunch,servesDinner,priceLevel,regularOpeningHours,attributions
```

- [ ] **Step 2: Run RED**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessPlaceEnrichmentTests
```

Expected: FAIL because `IBusinessPlaceEnrichmentProvider` / `GoogleBusinessPlaceEnrichmentProvider` do not exist.

- [ ] **Step 3: Implement the provider minimally**

Implement:

```csharp
public sealed class GoogleBusinessPlaceEnrichmentProvider(
    HttpClient client,
    IConfiguration configuration) : IBusinessPlaceEnrichmentProvider
{
    internal const string PlaceDetailsFieldMask =
        "id,dineIn,takeout,delivery,reservable,servesBreakfast,servesBrunch,servesLunch,servesDinner,priceLevel,regularOpeningHours,attributions";

    private string? ApiKey => configuration["GoogleMaps:ApiKey"]?.Trim();
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public async Task<BusinessPlaceEnrichment?> GetAsync(string providerRef, CancellationToken ct)
    {
        var placeId = providerRef.Trim();
        if (placeId.Length is < 1 or > 2048)
            throw new BusinessDiscoveryException("business_place_ref_invalid", "That business location reference is invalid.");
        if (!IsConfigured) return null;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(placeId)}");
        request.Headers.TryAddWithoutValidation("X-Goog-Api-Key", ApiKey);
        request.Headers.TryAddWithoutValidation("X-Goog-FieldMask", PlaceDetailsFieldMask);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 24 }, ct);
        return GoogleBusinessPlaceEnrichmentMapper.Map(placeId, document.RootElement, DateTimeOffset.UtcNow);
    }
}
```

Map only positive capabilities into `OperatingChannels` (`Dine in`, `Takeaway`, `Delivery`) and only present meal booleans into service-period labels. Preserve absent booleans as unknown. Map price enums to `Free`, `Inexpensive`, `Moderate`, `Expensive`, `Very expensive`; never map price to profit or revenue.

- [ ] **Step 4: Run GREEN**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessPlaceEnrichmentTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add apps/api/BusinessPlaceEnrichment.cs tests/api/BusinessPlaceEnrichmentTests.cs apps/api/Program.cs
git commit -m "feat(vs26): add structured place enrichment provider"
```

---

### Task 2: Add owner-authorized transient enrichment endpoint

**Files:**
- Modify: `apps/api/BusinessLocationResolution.cs`
- Create: `tests/api/BusinessPlaceEnrichmentEndpointTests.cs`

**Interfaces:**
- Consumes: `IBusinessPlaceEnrichmentProvider` and existing owned `BusinessDiscoverySnapshot`.
- Produces:

```csharp
public sealed record EnrichBusinessPlaceRequest(string? ProviderRef);

public sealed record BusinessPlaceEnrichmentResponse(
    string ProviderRef,
    IReadOnlyList<string> OperatingChannels,
    bool? Reservable,
    IReadOnlyList<string> ServicePeriods,
    string? PricePosition,
    IReadOnlyList<string> OpeningHours,
    IReadOnlyList<BusinessPlaceAttribution> Attributions,
    string AttributionLabel);
```

Endpoint:

```text
POST /api/v1/business-discovery/{snapshotId}/place-enrichment
```

- [ ] **Step 1: Write RED endpoint tests**

Cover:

```csharp
[Fact]
public async Task Place_enrichment_requires_snapshot_ownership() { /* another owner's snapshot => 404 */ }

[Fact]
public async Task Place_enrichment_returns_provider_neutral_operating_shape() { /* owned snapshot + fake provider => 200 */ }

[Fact]
public async Task Place_enrichment_unavailable_returns_safe_degraded_response() { /* provider null => 503 stable code */ }

[Fact]
public async Task Place_enrichment_does_not_write_google_content_to_snapshot() { /* DbContext Facts/Evidence unchanged */ }
```

- [ ] **Step 2: Run RED**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessPlaceEnrichmentEndpointTests
```

Expected: FAIL because the endpoint does not exist.

- [ ] **Step 3: Implement endpoint**

Inside the existing business-location endpoint module:
1. derive subject from claims;
2. load account;
3. load snapshot by `snapshotId + UserAccountId`;
4. validate non-empty `ProviderRef`;
5. invoke `IBusinessPlaceEnrichmentProvider.GetAsync`;
6. return a bounded response with `AttributionLabel = "Google Maps"`;
7. do **not** add the response values to `BusinessDiscoveryFact`, `BusinessDiscoveryEvidence`, `BusinessContextEntry`, media/offering records or provider JSON storage.

Use stable degraded code:

```text
business_place_enrichment_unavailable
```

- [ ] **Step 4: Run GREEN and existing location regression**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter "BusinessPlaceEnrichmentEndpointTests|BusinessLocationResolutionTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add apps/api/BusinessLocationResolution.cs tests/api/BusinessPlaceEnrichmentEndpointTests.cs
git commit -m "feat(vs26): expose transient place enrichment"
```

---

### Task 3: Carry selected Place identity and owner-confirmed operating context through Business confirmation

**Files:**
- Modify: `apps/mobile/src/api/business-discovery.ts`
- Modify: `apps/mobile/src/features/business-discovery/discovery-model.ts`
- Modify: `apps/api/BusinessDiscoveryPersistence.cs`
- Modify: `tests/api/BusinessDiscoveryPersistenceTests.cs`
- Modify: `tests/mobile/business-discovery-model.test.mjs`

**Interfaces:**
- Consumes: transient `BusinessPlaceEnrichmentResponse` from Task 2.
- Produces mobile request additions:

```ts
export type ConfirmedOperatingContext = {
  providerRef: string;
  operatingChannels: string[];
  reservable: boolean | null;
  servicePeriods: string[];
  pricePosition: string | null;
};

export type CreateBusinessFromDiscoveryRequest = DiscoveryDraft & {
  ownerConfirmed: true;
  confirmedOperatingContext?: ConfirmedOperatingContext;
};
```

Persisted owner context keys:

```text
operatingchannels
reservationcapability
serviceperiods
priceposition
```

- [ ] **Step 1: Write RED model/persistence tests**

Mobile test:

```js
test('build request carries only explicitly confirmed operating context', async () => {
  const request = buildCreateBusinessFromDiscoveryRequest(draft, {
    providerRef: 'ChIJAtlas123',
    operatingChannels: ['Dine in', 'Takeaway', 'Delivery'],
    reservable: true,
    servicePeriods: ['Lunch', 'Dinner'],
    pricePosition: 'Moderate',
  });
  assert.deepEqual(request.confirmedOperatingContext.operatingChannels, ['Dine in', 'Takeaway', 'Delivery']);
});
```

API persistence test:

```csharp
[Fact]
public async Task Confirmation_materializes_operating_context_as_owner_confirmed_not_google_cached_content()
{
    // Create owned discovery snapshot, submit confirmed operating context.
    // Assert BusinessContextEntry.Source == FieldSources.Owner,
    // OwnerConfirmed == true for operatingchannels/reservationcapability/serviceperiods/priceposition.
    // Assert no raw Places JSON and no Google enrichment value was appended to discovery Evidence/Facts.
}
```

- [ ] **Step 2: Run RED**

```bash
node --test tests/mobile/business-discovery-model.test.mjs
dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessDiscoveryPersistenceTests
```

Expected: new assertions FAIL because request/persistence lacks confirmed operating context.

- [ ] **Step 3: Implement canonical owner-confirmed persistence**

Use deterministic bounded serializers:

```csharp
static string JoinValues(IEnumerable<string> values) =>
    string.Join(" | ", values.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));
```

Allowed operating channel labels:

```text
Dine in
Takeaway
Delivery
Marketplace/platform
Own website/app
```

Allowed service-period labels:

```text
Breakfast
Brunch
Lunch
Dinner
```

Allowed price positions:

```text
Free
Inexpensive
Moderate
Expensive
Very expensive
```

Reject values outside these allowlists with the existing validation-problem path. Set every materialized context row to:

```csharp
Source = FieldSources.Owner;
OwnerConfirmed = true;
```

Persist `providerRef` only in the existing allowed Place-ID/provider-reference boundary if final VS-25 schema has one; otherwise retain it only on the canonical Business location model. Do not create a Google response JSON column.

- [ ] **Step 4: Run GREEN plus PostgreSQL persistence regressions**

```bash
node --test tests/mobile/business-discovery-model.test.mjs
dotnet test tests/api/Atlas.Api.Tests.csproj --filter "BusinessDiscoveryPersistenceTests|BusinessDiscoveryConfirmationRegressionTests|BusinessDiscoveryMultiSourcePersistenceTests|BusinessDiscoveryMediaMenuPersistenceTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add apps/mobile/src/api/business-discovery.ts apps/mobile/src/features/business-discovery/discovery-model.ts apps/api/BusinessDiscoveryPersistence.cs tests/api/BusinessDiscoveryPersistenceTests.cs tests/mobile/business-discovery-model.test.mjs
git commit -m "feat(vs26): persist confirmed operating context"
```

---

### Task 4: Show compact approved-design `About your business` confirmation

**Files:**
- Modify: `apps/mobile/app/create-business.tsx`
- Create: `apps/mobile/src/features/business-discovery/place-enrichment-model.ts`
- Create: `tests/mobile/place-enrichment-model.test.mjs`
- Modify: `tests/mobile/business-discovery-runtime.test.mjs` if this is the final VS-25 runtime filename; otherwise update the existing authentic create-business runtime test discovered on final `main`.

**Interfaces:**
- Consumes: `BusinessPlaceEnrichmentResponse` and selected `BusinessLocationCandidate`.
- Produces: at most five concise confirmation groups and `ConfirmedOperatingContext` for Task 3.

- [ ] **Step 1: Read UI UX Pro Max before changing the workflow UI**

Read `.agents/skills/ui-ux-pro-max/SKILL.md` if present. Apply it only for form hierarchy, states, accessibility and responsive behavior; `ATLAS-DESIGN-001` v1.2 remains visual authority.

- [ ] **Step 2: Write RED presentation-model tests**

Create pure helpers:

```ts
export function buildAboutBusinessItems(enrichment: BusinessPlaceEnrichmentResponse): AboutBusinessItem[];
export function buildConfirmedOperatingContext(enrichment: BusinessPlaceEnrichmentResponse): ConfirmedOperatingContext;
```

Assert:

```js
test('about summary shows high-value groups only and caps at five', () => {
  const items = buildAboutBusinessItems(enrichment);
  assert.ok(items.length <= 5);
  assert.equal(items.some(x => x.label === 'Service'), true);
  assert.equal(items.some(x => /rating|review/i.test(x.label)), false);
});

test('empty enrichment produces no about card', () => {
  assert.deepEqual(buildAboutBusinessItems(emptyEnrichment), []);
});
```

- [ ] **Step 3: Run RED**

```bash
node --test tests/mobile/place-enrichment-model.test.mjs
```

Expected: FAIL because the model does not exist.

- [ ] **Step 4: Implement the pure presentation model**

Render groups in this order when meaningful:
1. Service — `Dine in · Takeaway · Delivery`;
2. Reservations — `Reservations available` only when true;
3. Service periods — e.g. `Breakfast · Lunch · Dinner`;
4. Price — e.g. `Moderate price range`;
5. Hours — existing Google weekday descriptions only if the current discovery hours are absent or the design can show them without duplicating the existing hours row.

Do not render false/unknown capability statements as negative claims.

- [ ] **Step 5: Wire enrichment to location selection**

Add API function:

```ts
export async function enrichBusinessPlace(
  accessToken: string,
  snapshotId: string,
  providerRef: string,
): Promise<BusinessPlaceEnrichmentResponse>
```

In `create-business.tsx`:
- clear previous enrichment when location changes;
- automatically load enrichment after a unique `result.selected` or explicit branch selection;
- keep enrichment failure non-blocking and provider-neutral (`Atlas could not add extra public details. You can still continue.`);
- store the result only in component state;
- pass `buildConfirmedOperatingContext(enrichment)` into `buildCreateBusinessFromDiscoveryRequest` only when the About card was visible during confirmation;
- never persist the Places payload to AsyncStorage/SecureStore/local cache.

- [ ] **Step 6: Add compliant attribution inside the About card**

When Google Maps content is displayed, include visible text attribution exactly:

```text
Google Maps
```

Use a system/body sans-serif, normal weight, 12–16sp, accessible contrast, within the same card. If third-party provider attributions are returned, render their provider names/links in the same container according to official policy. Do not style or alter the words `Google Maps`.

- [ ] **Step 7: Run GREEN and authentic mobile regression**

```bash
node --test tests/mobile/place-enrichment-model.test.mjs
npm --prefix apps/mobile run typecheck
npm --prefix apps/mobile run lint
npm --prefix apps/mobile test
```

Expected: PASS; runtime confirms compact About card, degraded state, 44px targets, screen-reader labels and no horizontal overflow at the repository's phone/tablet acceptance widths.

- [ ] **Step 8: Commit**

```bash
git add apps/mobile/app/create-business.tsx apps/mobile/src/api/business-discovery.ts apps/mobile/src/features/business-discovery/place-enrichment-model.ts tests/mobile/place-enrichment-model.test.mjs tests/mobile
git commit -m "feat(vs26): confirm important place intelligence"
```

---

### Task 5: Make Goals the first owner-only onboarding step

**Files:**
- Modify: `apps/mobile/app/create-business.tsx`
- Modify: `apps/mobile/app/(tabs)/goals.tsx`
- Create: `apps/mobile/src/features/goals/goals-onboarding.ts`
- Create: `tests/mobile/goals-onboarding.test.mjs`

**Interfaces:**
- Consumes: newly created Business session.
- Produces: deterministic route `Business confirmation -> Goals -> optional progressive questions -> Today`.

- [ ] **Step 1: Write RED routing tests**

```js
test('new business routes to goals in onboarding mode', () => {
  assert.equal(getPostBusinessDestination(), '/(tabs)/goals?onboarding=1');
});

test('saved onboarding goals continue to progressive questions', () => {
  assert.equal(getGoalsOnboardingContinuation({ onboarding: true, starter: false, goalCount: 2 }), '/progressive-questions');
});
```

- [ ] **Step 2: Run RED**

```bash
node --test tests/mobile/goals-onboarding.test.mjs
```

Expected: FAIL because routing helpers do not exist.

- [ ] **Step 3: Implement routing helpers and Business submit route**

Use:

```ts
export const getPostBusinessDestination = () => '/(tabs)/goals?onboarding=1' as const;
```

After `saveSession`, replace the current direct `/progressive-questions` route with the Goals onboarding route.

- [ ] **Step 4: Add bounded Goals onboarding continuation**

Read `onboarding` with `useLocalSearchParams`. Preserve the existing Goals screen layout and save behavior. In onboarding mode only:
- keep the existing `Goals saved.` confirmation;
- once goals are persisted (`starter === false`, at least one goal, no operation in flight), show one additional primary continuation action labeled `Continue`;
- route it to `/progressive-questions`;
- do not auto-save or infer goals;
- do not alter normal tab-mode Goals behavior.

- [ ] **Step 5: Run GREEN and Goals regressions**

```bash
node --test tests/mobile/goals-onboarding.test.mjs
npm --prefix apps/mobile test
npm --prefix apps/mobile run typecheck
npm --prefix apps/mobile run lint
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add apps/mobile/app/create-business.tsx apps/mobile/app/'(tabs)'/goals.tsx apps/mobile/src/features/goals/goals-onboarding.ts tests/mobile/goals-onboarding.test.mjs
git commit -m "feat(vs26): make goals first in onboarding"
```

---

### Task 6: Suppress redundant restaurant operating questions and cap enriched context at three

**Files:**
- Modify: `apps/api/ProgressiveQuestions.cs`
- Modify: `tests/api/ProgressiveQuestionCatalogueTests.cs`
- Modify: `tests/api/ProgressiveQuestionPersistenceTests.cs`

**Interfaces:**
- Consumes: owner-confirmed `BusinessContextEntry` keys from Task 3.
- Produces: catalogue v2 with deterministic enriched-context eligibility.

- [ ] **Step 1: Write RED catalogue tests**

Add:

```csharp
[Fact]
public void Restaurant_service_channel_is_suppressed_by_confirmed_operating_channels()
{
    var context = new[] { OwnerContext("operatingchannels", "Dine in | Takeaway | Delivery") };
    var selected = ProgressiveQuestionCatalogueV2.Select("restaurant-cafe", context, []);
    Assert.DoesNotContain(selected, x => x.QuestionKey == "restaurant-cafe.service-channel");
    Assert.DoesNotContain(selected, x => x.QuestionKey == "generic.primary-channel");
}

[Fact]
public void Unconfirmed_public_operating_channels_do_not_suppress_questions()
{
    var context = new[] { PublicContext("operatingchannels", "Dine in | Takeaway") };
    var selected = ProgressiveQuestionCatalogueV2.Select("restaurant-cafe", context, []);
    Assert.Contains(selected, x => x.QuestionKey == "restaurant-cafe.service-channel");
}

[Fact]
public void Enriched_business_gets_at_most_three_owner_only_context_questions()
{
    var context = new[] { OwnerContext("operatingchannels", "Dine in | Takeaway") };
    var selected = ProgressiveQuestionCatalogueV2.Select("restaurant-cafe", context, []);
    Assert.InRange(selected.Count, 0, 3);
    Assert.All(selected, q => Assert.DoesNotContain("service-channel", q.QuestionKey));
}
```

- [ ] **Step 2: Run RED**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter "ProgressiveQuestionCatalogueTests|ProgressiveQuestionPersistenceTests"
```

Expected: FAIL because v2/coverage rules are absent.

- [ ] **Step 3: Introduce catalogue v2 without invalidating prior progress**

Set catalogue `Version = "2"`. Keep the same `CatalogueKey` and same restaurant question key so existing answered/skipped progress remains suppressive across versions.

Change the fallback restaurant question to factual capability wording:

```text
Which service channels do you currently offer?
```

with options:

```text
Dine in
Takeaway
Delivery
Own website/app
Marketplace/platform
```

- [ ] **Step 4: Implement evidence-aware satisfaction**

Add a pure helper:

```csharp
private static bool IsQuestionSatisfied(
    ProgressiveQuestionDefinition question,
    IReadOnlySet<string> authoritativeContextKeys)
{
    if (authoritativeContextKeys.Contains(question.TargetContextKey)) return true;
    if (question.MaterialityTags.Contains("channel") && authoritativeContextKeys.Contains("operatingchannels")) return true;
    return false;
}
```

Only `OwnerConfirmed == true` context belongs in `authoritativeContextKeys`.

When `operatingchannels` is owner-confirmed, cap the returned set at 3 and order the remaining owner-only questions by this enriched-business preference before the existing deterministic tie breakers:

```text
currentpriorities
constraints
busyperiods
customers
```

In degraded/no-enrichment cases keep the existing five-question ceiling.

- [ ] **Step 5: Run GREEN and existing stale-version behavior**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter "ProgressiveQuestionCatalogueTests|ProgressiveQuestionPersistenceTests"
```

Expected: PASS, including stale catalogue request rejection and previous answered/skipped progress suppression.

- [ ] **Step 6: Commit**

```bash
git add apps/api/ProgressiveQuestions.cs tests/api/ProgressiveQuestionCatalogueTests.cs tests/api/ProgressiveQuestionPersistenceTests.cs
git commit -m "feat(vs26): suppress redundant onboarding questions"
```

---

### Task 7: Prove the integrated hero journey and preserve VS-25 media/menu intelligence

**Files:**
- Modify: `tests/api/CategoryIntelligenceHeroJourneyTests.cs`
- Modify: `tests/api/BusinessDiscoveryMediaMenuPersistenceTests.cs` only for compatibility assertions if final VS-25 contracts require it
- Create: `tests/mobile/vs26-goal-first-onboarding.test.mjs`
- Create: `docs/evidence/VS-26-RUNTIME-2026-08-11.md`

**Interfaces:**
- Consumes: Tasks 1-6.
- Produces: deterministic and authentic runtime evidence for `discover -> select place -> enrich -> confirm -> goals -> 0-3 context questions -> Today`.

- [ ] **Step 1: Write integrated RED journey test**

API journey must prove:
1. multi-source discovery still reconciles website/Bolt/Wolt facts;
2. VS-25 media/menu observations still materialize unchanged;
3. selected Place enrichment is transient;
4. owner confirmation creates owner-confirmed canonical operating context;
5. Goals are required independently;
6. restaurant service-channel question is absent after confirmed operating context;
7. no more than three optional context questions remain;
8. Today readiness remains truthful.

- [ ] **Step 2: Run RED**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter "CategoryIntelligenceHeroJourneyTests|BusinessDiscoveryMediaMenuPersistenceTests"
node --test tests/mobile/vs26-goal-first-onboarding.test.mjs
```

Expected: any missing cross-boundary integration fails before final certification.

- [ ] **Step 3: Fix only demonstrated integration defects**

Use `superpowers:systematic-debugging` for any unexpected failure. Do not add new provider fields, new questions, new UI surfaces or schema outside the approved VS-26 design to make the test pass.

- [ ] **Step 4: Run full deterministic verification**

```bash
npm run governance:validate
npm run preflight
dotnet test tests/api/Atlas.Api.Tests.csproj
npm --prefix apps/mobile test
npm --prefix apps/mobile run typecheck
npm --prefix apps/mobile run lint
npm run migrations:registry
```

Expected: PASS.

- [ ] **Step 5: Run authentic Expo runtime acceptance**

Use the repository's existing authentic Expo Web/device harness, not a new auth bypass. Verify at the established phone and tablet acceptance sizes:
- compact About card with required Google Maps attribution;
- enrichment unavailable path remains continuable;
- owner correction remains reachable;
- Goals are the first post-confirmation step;
- saved Goals continue to optional questions;
- enriched restaurant does not get the redundant service-channel question;
- no more than three optional questions;
- no horizontal overflow;
- enabled controls are approximately 44x44 minimum;
- screen-reader semantics, focus order, dynamic text containment and reduced motion remain valid.

Record exact SHA, commands/results and screenshots/artifact references in `docs/evidence/VS-26-RUNTIME-2026-08-11.md`.

- [ ] **Step 6: Commit integrated evidence**

```bash
git add tests/api tests/mobile docs/evidence/VS-26-RUNTIME-2026-08-11.md
git commit -m "test(vs26): prove goal-first enriched onboarding"
```

---

### Task 8: Review, exact-head certification and human merge checkpoint

**Files:**
- Modify: `delivery/current-slice.json`
- Modify: `docs/slices/VS-26.md`
- Modify: `README.md` only if current README tracks active/completed Category Intelligence slices

**Interfaces:**
- Consumes: final implementation head.
- Produces: exact-SHA certification evidence and a merge-ready PR; no release/deployment.

- [ ] **Step 1: Use verification-before-completion**

Read `.agents/skills/verification-before-completion/SKILL.md` and execute its checklist against the final head.

- [ ] **Step 2: Run independent code review**

Review specifically for:
- accidental storage of Google Place response fields;
- missing Google Maps/third-party attribution;
- repeated Place Details calls;
- provider errors leaking API keys/raw payloads;
- owner-confirmed/source-class confusion;
- goals inferred or preselected from external data;
- question suppression from unconfirmed context;
- VS-25 media/menu regressions;
- approved Atlas design drift;
- accessibility regressions.

Any Critical/Important finding loops through systematic debugging/TDD before certification.

- [ ] **Step 3: Run exact-head gates**

```bash
npm run governance:validate
npm run preflight
git diff --check
```

Push the branch and require exact-head GitHub:
- CI — PASS;
- Security baseline — PASS;
- Product Intake — PASS.

- [ ] **Step 4: Bind certification to the exact 40-character SHA**

Update `delivery/current-slice.json` certification and `docs/slices/VS-26.md` with the exact tested SHA and evidence IDs. Leave:

```text
release = not-authorized
production-enable = pending/not-authorized
```

- [ ] **Step 5: Re-run governance after certification metadata**

```bash
npm run governance:validate
npm run preflight
```

Expected: PASS on the certification metadata head; if the SHA changed because of metadata, follow repository certification convention and bind evidence correctly rather than claiming the prior SHA is the current head.

- [ ] **Step 6: Stop at human merge approval**

Open/update the VS-26 PR with exact-head evidence. Do **not** merge, deploy, run EAS release actions or production-enable without separate Product Owner approval.
