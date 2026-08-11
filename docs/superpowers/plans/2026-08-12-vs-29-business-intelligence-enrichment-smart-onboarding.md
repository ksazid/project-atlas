# VS-29 — Business Intelligence Enrichment & Smart Onboarding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Atlas discover and confirm substantially more useful public Business intelligence—media/menu, website, phone, public email, social channels and hours—while suppressing redundant onboarding questions and giving the Expo demo account a safe reset-and-switch test flow.

**Architecture:** Extend the existing provider-neutral public discovery pipeline rather than introducing a second enrichment system. Public sources emit normalized provenance-rich facts/media/offerings; reconciliation preserves identity/confidence boundaries; explicit owner confirmation materializes canonical Business Profile/Context values; progressive questions use a deterministic semantic-satisfaction policy; the Development-only reset endpoint remains the sole destructive test primitive. Google Place rich content stays transient under DEC-08.

**Tech Stack:** ASP.NET Core / C# / EF Core / PostgreSQL, Expo React Native / TypeScript / Expo Router, xUnit, Node test runner, existing PES governance and Superpowers workflow.

## Global Constraints

- Runtime execution is **blocked until VS-28 has cleared PES/runtime work or the Product Owner explicitly defers VS-28**.
- Start runtime work from the then-current `main`, not from this planning branch.
- Re-run the changed-file conflict scan against the final VS-28 merge before editing `apps/mobile/app/create-business.tsx` or shared mobile primitives.
- Preserve the production one-primary-Business-per-owner invariant.
- Preserve DEC-08: Google rich Place content is transient; do not persist copied Google payloads, photos or attribution payloads. Retain only permitted provider references plus canonical values the owner explicitly confirms.
- Preserve DEC-07: store remote HTTPS media references and structured offerings, never third-party image binaries.
- Preserve SSRF-safe fetching, redirect limits, response limits and public-only source boundaries.
- No authenticated scraping, bot bypass, private marketplace/POS APIs, production browser automation or site-wide crawling.
- Existing owner-confirmed values are authoritative and must never be silently overwritten by refresh/enrichment.
- No navigation restructuring; adapt VS-29 UI to the post-VS-28 shell.
- No database migration is expected. If a genuine new persistence requirement appears, stop and route it through PES instead of adding schema opportunistically.
- No production release/deployment, EAS build/submit/OTA or production database mutation.

---

## Task 0: Rebase the approved work onto post-VS-28 `main` and activate VS-29 in PES

**Files:**
- Create/modify after VS-28 clears: `docs/slices/VS-29.md`
- Modify after VS-28 clears: `delivery/current-slice.json`
- Modify only if a new typed decision is required: `delivery/decisions.json`
- Reference: `docs/superpowers/specs/2026-08-12-vs-29-business-intelligence-enrichment-smart-onboarding-design.md`
- Reference: this plan

**Interfaces / invariants:**
- Requirements: `FR-02`, `FR-03`, `FR-05`, `FR-16`.
- Dependencies: DEC-04, DEC-05, DEC-07, DEC-08; certified VS-25/VS-26/VS-27; final VS-28 state.
- Implementation mode: `runtime-enabled` only after scope + implementation approval are represented according to PES.

- [ ] **Step 1: Fetch the actual post-VS-28 state and refuse stale execution**

Run:

```bash
git fetch origin
git checkout main
git pull --ff-only origin main
git rev-parse HEAD
git log -1 --oneline
```

Then inspect:

```bash
cat delivery/current-slice.json
cat delivery/decisions.json
```

Expected: VS-28 is merged/certified/superseded/deferred as governed. If VS-28 still owns active runtime work, stop VS-29 runtime execution.

- [ ] **Step 2: Verify current Google policy before changing any Places field mask**

Use current **official Google Maps Platform documentation only** to verify Place Details field availability, storage/caching rules, attribution requirements and EEA restrictions relevant to any proposed contact/presence field.

Record a short implementation-time result in `docs/slices/VS-29.md`.

Stop condition: if current policy does not permit the planned use, keep that Google field transient review-only or omit it; do not weaken DEC-08.

- [ ] **Step 3: Create an isolated implementation branch/worktree from current main**

```bash
git checkout -b atlas/vs29-business-intelligence-smart-onboarding
```

Or use Superpowers worktree workflow if available.

- [ ] **Step 4: Run a changed-file conflict scan against final VS-28**

At minimum compare whether VS-28 touched:

```text
apps/mobile/app/create-business.tsx
apps/mobile/src/features/business-discovery/**
apps/mobile/src/api/business-discovery.ts
apps/api/Program.cs
```

Adapt VS-29 to the merged shell rather than reverting VS-28 structure.

- [ ] **Step 5: Create/activate VS-29 governance records**

`docs/slices/VS-29.md` must explicitly state:

```text
Scope: normalized public enrichment, media/menu hardening, confirmation persistence,
semantic progressive-question suppression, Development-only reset-and-switch.
Out of scope: navigation redesign, private provider APIs, Google payload persistence,
production ownership-rule changes, production deployment.
```

Use the repository’s supported PES transition commands rather than hand-inventing lifecycle states.

- [ ] **Step 6: Establish a green baseline before feature code**

Run:

```bash
npm run governance:validate
npm run preflight
dotnet test tests/api/Atlas.Api.Tests.csproj
```

Expected: PASS before VS-29 runtime edits. If not, diagnose baseline failure separately.

- [ ] **Step 7: Commit activation only**

```bash
git add delivery docs/slices/VS-29.md
git commit -m "chore(vs29): activate business intelligence enrichment"
```

---

## Task 1: Extract normalized public email, social channels, phone and website evidence

**Files:**
- Modify: `apps/api/BusinessDiscovery.cs`
- Modify: `tests/api/BusinessDiscoveryPolicyTests.cs`
- Optional focused new test file if existing test becomes unwieldy: `tests/api/BusinessDiscoveryContactPresenceTests.cs`

**Interfaces:**

Existing output remains:

```csharp
public sealed record PublicBusinessFact(
    string Key,
    string Value,
    string Source,
    string SourceUrl,
    DateTimeOffset ObservedAt,
    string Confidence,
    string EvidenceClass = "public-observed",
    bool OwnerConfirmed = false);
```

Add normalized fact keys only; do not create a second DTO:

```text
email
socialChannels
website
phone
openingHours
```

Use `" | "` as the deterministic delimiter for multiple social URLs.

Recognized social host families for VS-29:

```text
facebook.com
instagram.com
linkedin.com
tiktok.com
x.com
twitter.com
youtube.com
```

- [ ] **Step 1: Write RED tests for structured contact/presence data**

Add tests equivalent to:

```csharp
[Fact]
public void Extract_reads_public_email_and_sameAs_social_channels()
{
    var html = """
    <script type="application/ld+json">
    {
      "@context":"https://schema.org",
      "@type":"Restaurant",
      "name":"Atlas Test Cafe",
      "url":"https://example.test",
      "telephone":"+356 2100 0000",
      "email":"hello@example.test",
      "sameAs":[
        "https://www.instagram.com/atlas-test-cafe/",
        "https://www.facebook.com/atlas-test-cafe/"
      ]
    }
    </script>
    """;

    var result = PublicBusinessExtractor.Extract(
        "website",
        new Uri("https://example.test"),
        html,
        DateTimeOffset.Parse("2026-08-12T00:00:00Z"));

    Assert.Contains(result.Facts, x => x.Key == "email" && x.Value == "hello@example.test");
    Assert.Contains(result.Facts, x =>
        x.Key == "socialChannels" &&
        x.Value.Contains("instagram.com", StringComparison.OrdinalIgnoreCase) &&
        x.Value.Contains("facebook.com", StringComparison.OrdinalIgnoreCase));
}
```

Also test:
- duplicate `sameAs` links dedupe case-insensitively;
- unsupported social hosts are ignored;
- malformed URLs are ignored;
- all produced facts retain provider/source URL/observed time/confidence.

Run:

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessDiscovery
```

Expected: new assertions FAIL before implementation.

- [ ] **Step 2: Add conservative structured extraction**

In `PublicBusinessExtractor.Extract`, extend the existing structured-business block:

```csharp
var structuredEmail = StructuredString("email");
Add("email", Decode(structuredEmail), "high");

var socialChannels = structured is JsonElement businessValue
    ? ReadSocialChannels(businessValue)
    : null;
Add("socialChannels", socialChannels, "high");
```

Implement `ReadSocialChannels` as a bounded helper that:
- accepts schema.org `sameAs` string or array;
- keeps only absolute HTTPS URLs;
- keeps only the allowlisted social hosts or their subdomains;
- canonicalizes host casing / trims fragments where safe;
- deduplicates;
- emits at most 8 links and stays under `MaxFactValueCharacters`.

- [ ] **Step 3: Write RED tests for `mailto:` and `tel:` fallbacks**

Use public HTML without structured email/phone:

```html
<a href="mailto:hello@example.test">Email us</a>
<a href="tel:+35621000000">Call</a>
```

Expected:
- fallback is used only when the structured value is absent;
- exactly one unique normalized `mailto:` candidate is eligible;
- exactly one unique normalized `tel:` candidate is eligible;
- multiple conflicting candidates produce no fallback fact rather than arbitrary first-write-wins.

- [ ] **Step 4: Implement bounded fallbacks**

Add timeout-bounded regex or HTML-attribute helpers consistent with the existing extractor style.

Expected policy:

```csharp
structured email > one unambiguous public mailto fallback > no email fact
structured phone > one unambiguous public tel fallback > no phone fact
```

Do not extract names/contact details from arbitrary prose.

- [ ] **Step 5: Run focused + full API tests**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessDiscovery
dotnet test tests/api/Atlas.Api.Tests.csproj
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add apps/api/BusinessDiscovery.cs tests/api
git commit -m "feat(vs29): enrich public business contact facts"
```

---

## Task 2: Add one bounded official-website enrichment hop with strong identity matching

**Files:**
- Modify: `apps/api/MultiSourceBusinessDiscoveryService.cs`
- Modify as needed: `apps/api/BusinessDiscoveryReconciliation.cs`
- Modify: `tests/api/MultiSourceBusinessDiscoveryServiceTests.cs`
- Modify: `tests/api/BusinessDiscoveryReconciliationTests.cs`
- Reuse: `apps/api/BusinessDiscovery.cs`, `apps/api/PublicBusinessHttpHandlerFactory.cs`

**Interfaces:**

Add one small deterministic policy boundary, for example:

```csharp
internal static class OfficialWebsiteEnrichmentPolicy
{
    public static bool TrySelectWebsite(
        PublicBusinessSnapshot anchor,
        out string websiteUrl);

    public static bool StrongIdentityMatch(
        PublicBusinessSnapshot anchor,
        PublicBusinessSnapshot website);
}
```

Automatic official-site contribution is a **secondary observation**, never a replacement anchor.

- [ ] **Step 1: Write RED tests for selection and identity safety**

Cover:
- one high-confidence HTTPS `website` fact from the accepted anchor may be selected;
- unsafe/non-HTTPS/private-host URL is rejected through existing URL policy;
- no website fact = no extra request;
- weak/ambiguous identity = discard website contribution;
- a website matching normalized business name plus at least one supporting strong signal (same phone or compatible location/name evidence) is accepted;
- only one automatic website fetch occurs per discovery request.

- [ ] **Step 2: Implement the bounded one-hop orchestration**

After user-supplied observations are collected, inspect the reconciled/anchor evidence for one official website candidate. Fetch it using the existing `BusinessDiscoveryService`, which already applies public HTTPS/SSRF/redirect/size protections.

Do **not** recursively call `MultiSourceBusinessDiscoveryService`.

Pseudo-flow:

```csharp
var observations = await DiscoverSuppliedSourcesAsync(...);
var anchor = observations.FirstOrDefault(x => x.IsPrimary && x.Status == "success");

if (anchor is not null && OfficialWebsiteEnrichmentPolicy.TrySelectWebsite(anchorSnapshot, out var url))
{
    var websiteSnapshot = await pageDiscovery.DiscoverAsync(url, ct);
    if (OfficialWebsiteEnrichmentPolicy.StrongIdentityMatch(anchorSnapshot, websiteSnapshot))
        observations.Add(WebsiteObservation(websiteSnapshot, order: observations.Count));
}

return BusinessDiscoveryReconciler.Reconcile(observations);
```

Keep the actual implementation aligned with existing `BusinessSourceObservation` shapes; do not duplicate reconciliation logic.

- [ ] **Step 3: Ensure failure is non-blocking**

Tests must prove:
- official website timeout/unavailable/invalid content does not invalidate the successful anchor;
- blocked redirect/private address fails closed;
- diagnostics are retained only where existing reconciliation supports them; do not expose sensitive network details.

- [ ] **Step 4: Verify owner-confirmed precedence remains unchanged**

Add/retain reconciliation tests showing conflicting website facts do not silently replace a stronger selected anchor fact.

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter "MultiSourceBusinessDiscoveryService|BusinessDiscoveryReconciliation"
dotnet test tests/api/Atlas.Api.Tests.csproj
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add apps/api/MultiSourceBusinessDiscoveryService.cs apps/api/BusinessDiscoveryReconciliation.cs tests/api
git commit -m "feat(vs29): add bounded official website enrichment"
```

---

## Task 3: Diagnose and harden Bolt media/menu extraction without private fallbacks

**Files:**
- Modify: `apps/api/BusinessDiscoveryMediaMenu.cs`
- Modify: `tests/api/BusinessDiscoveryMediaMenuTests.cs`
- Add sanitized fixtures only if they improve readability: `tests/api/Fixtures/bolt-*.html`
- Update evidence notes: `docs/slices/VS-29.md`

**Interfaces / fixed limits:**

```csharp
PublicBusinessMediaMenuExtractor.MaxMediaPerSource == 24
PublicBusinessMediaMenuExtractor.MaxOfferingsPerSource == 250
```

Keep:
- `business-image`
- `menu-item-image`
- offering kind `menu-item`
- remote HTTPS only.

- [ ] **Step 1: Diagnose the three representative public page shapes before parser changes**

Use the existing safe discovery path against current public pages corresponding to:
- Habibi's Kebab Sliema — known rich case;
- Chickn Bites — prior empty media/menu case;
- McDonald's Birkirkara — prior business-image but no-menu case.

For each, record only the **semantic shape**, counts and stable public markers in `docs/slices/VS-29.md`; do not commit a full live page dump.

Classify each case as one of:

```text
A. menu/media present in already-returned public HTML with a new stable semantic shape;
B. media only present publicly;
C. menu not present in accepted public HTML.
```

If a case is C, the correct implementation is an honest no-menu result. Do not introduce browser/private API workarounds.

- [ ] **Step 2: Create minimal deterministic regression fixtures from diagnosed public shapes**

Reduce each supported public shape to the smallest HTML fixture that still demonstrates the stable markers/nesting. Strip analytics, scripts unrelated to semantic extraction, customer data and ephemeral identifiers.

Tests must assert:
- rich known Bolt shape still yields menu items and media;
- every newly supported public shape yields expected section/name/description/price/currency/image references;
- a genuine no-public-menu shape yields zero offerings without failure;
- limits/dedupe remain enforced.

- [ ] **Step 3: Run RED tests**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessDiscoveryMediaMenuTests
```

Expected: newly supported variant tests FAIL against current parser while the honest no-menu case already passes as empty.

- [ ] **Step 4: Implement the smallest semantic parser extension**

Extend `CaptureBoltSemanticMenu` only for stable public markers confirmed in Step 1. Prefer structural helper extraction over broad prose regex.

Every parser branch must:
- fail closed when its defining markers are absent;
- cap media/offerings using existing constants;
- use `TryCanonicalPublicUrl`;
- retain `bolt-food` source + canonical source page;
- never call a new endpoint.

- [ ] **Step 5: Verify no regression to the certified rich case**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessDiscoveryMediaMenuTests
dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessDiscoveryMediaMenuPersistenceTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add apps/api/BusinessDiscoveryMediaMenu.cs tests/api docs/slices/VS-29.md
git commit -m "fix(vs29): harden public Bolt media menu extraction"
```

---

## Task 4: Persist email/social and make opening-hours confirmation truthful

**Files:**
- Modify: `apps/api/BusinessDiscoveryPersistence.cs`
- Modify: `tests/api/BusinessDiscoveryPersistenceTests.cs`
- Modify: `tests/api/BusinessDiscoveryOperatingContextPersistenceTests.cs`
- Modify: `tests/api/BusinessDiscoveryConfirmationRegressionTests.cs`

**Interfaces:**

Extend the request with existing profile fields:

```csharp
public sealed record CreateBusinessFromDiscoveryRequest(
    Guid SnapshotId,
    string Name,
    string Category,
    string? Subcategory,
    string Country,
    string Timezone,
    string Currency,
    string PrimaryLocation,
    string OperatingStatus,
    string? Description,
    string? Website,
    string? Phone,
    string? Email,
    string? SocialChannels,
    string? BusinessHours,
    string Language,
    bool OwnerConfirmed,
    ConfirmedOperatingContext? ConfirmedOperatingContext = null);
```

Extend owner-confirmed operating context to carry explicitly confirmed hours:

```csharp
public sealed record ConfirmedOperatingContext(
    string ProviderRef,
    IReadOnlyList<string> OperatingChannels,
    bool? Reservable,
    IReadOnlyList<string> ServicePeriods,
    string? PricePosition,
    IReadOnlyList<string> OpeningHours);
```

- [ ] **Step 1: Write RED persistence tests for email/social**

Create a discovery snapshot with public facts:

```text
email = hello@example.test
socialChannels = https://instagram.com/example | https://facebook.com/example
```

Submit matching owner-confirmed values.

Assert:

```csharp
Assert.Equal("hello@example.test", profile.Email);
Assert.Contains("instagram.com", profile.SocialChannels);
Assert.Equal(FieldSources.Public, emailField.Source);
Assert.True(emailField.OwnerConfirmed);
```

Also prove an owner edit that differs from observed public evidence is stored as owner-reported provenance.

- [ ] **Step 2: Write RED test proving confirmed Google hours are actually persisted**

Build `ConfirmedOperatingContext` with 7 opening-hour descriptions and a request with no public `BusinessHours` fact.

Expected after creation:

```csharp
Assert.Contains("Monday", profile.BusinessHours);
Assert.True(profile.OwnerConfirmed);
```

The profile-field provenance for those hours must be owner-reported unless the exact same value existed in retained public evidence.

- [ ] **Step 3: Implement request/profile persistence**

Add validation length checks for `Email` and `SocialChannels`.

Populate existing `BusinessProfile` fields:

```csharp
Email = Clean(request.Email),
SocialChannels = Clean(request.SocialChannels),
```

Add provenance fields:

```csharp
if (!string.IsNullOrWhiteSpace(profile.Email)) AddField("email", profile.Email!);
if (!string.IsNullOrWhiteSpace(profile.SocialChannels)) AddField("socialChannels", profile.SocialChannels!);
```

- [ ] **Step 4: Implement confirmed opening-hours canonicalization**

Add bounded validation:
- at most 7 descriptions;
- trim/dedupe;
- each entry bounded so the joined value remains under `MaxValueCharacters`.

During creation choose:

```csharp
var confirmedHours = request.ConfirmedOperatingContext?.CanonicalOpeningHours();
var profileHours = !string.IsNullOrWhiteSpace(confirmedHours)
    ? confirmedHours
    : Clean(request.BusinessHours);
```

Store `profileHours` in `BusinessProfile.BusinessHours` and provenance field `openingHours`.

Do not add opening hours to `BusinessContextEntries`; hours belong to Business Profile in the current model.

- [ ] **Step 5: Confirm owner authority precedence**

Tests must show:
- prefilled public profile values require owner submission;
- differing owner value wins canonical profile state;
- public evidence remains provenance, not an overwrite mechanism.

- [ ] **Step 6: Run tests**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter "BusinessDiscoveryPersistence|BusinessDiscoveryOperatingContext|BusinessDiscoveryConfirmation"
dotnet test tests/api/Atlas.Api.Tests.csproj
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add apps/api/BusinessDiscoveryPersistence.cs tests/api
git commit -m "feat(vs29): persist confirmed business presence facts"
```

---

## Task 5: Suppress semantically redundant progressive questions

**Files:**
- Modify: `apps/api/ProgressiveQuestions.cs`
- Modify: `tests/api/ProgressiveQuestionCatalogueTests.cs`
- Modify if needed: `tests/api/ProgressiveQuestionPersistenceTests.cs`

**Interfaces:**

Use the final concrete class name:

```csharp
public static class ProgressiveQuestionSatisfactionPolicy
{
    public static bool IsSatisfied(
        ProgressiveQuestionDefinition question,
        IReadOnlyCollection<BusinessContextEntry> context);
}
```

Initial semantic rule:

```text
question = restaurant-cafe.service-channel / target primarychannels
AND owner-confirmed non-empty operatingchannels exists
=> satisfied for initial onboarding
```

- [ ] **Step 1: Write the RED regression from the observed duplicate journey**

```csharp
[Fact]
public void Restaurant_service_channel_is_suppressed_when_operating_channels_are_owner_confirmed()
{
    var context = new[]
    {
        new BusinessContextEntry
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Key = "operatingchannels",
            Value = "Takeaway | Delivery",
            Source = FieldSources.Owner,
            OwnerConfirmed = true,
            UpdatedAt = DateTimeOffset.UtcNow
        }
    };

    var selected = ProgressiveQuestionCatalogueV1.Select("restaurant-cafe", context, []);

    Assert.DoesNotContain(selected, q => q.QuestionKey == "restaurant-cafe.service-channel");
}
```

Also add negative tests:
- unconfirmed `operatingchannels` does **not** suppress;
- blank value does not suppress;
- `openingHours` does not suppress `generic.busy-periods`;
- `serviceperiods` does not suppress busy periods;
- profile/category/menu evidence does not suppress constraints/customer groups/current priorities;
- goals remain outside this catalogue.

- [ ] **Step 2: Implement the policy**

Refactor selection from exact-key-only logic:

```csharp
.Where(question => !authoritativeContextKeys.Contains(question.TargetContextKey))
.Where(question => !ProgressiveQuestionSatisfactionPolicy.IsSatisfied(question, context))
```

`IsSatisfied` must inspect only `OwnerConfirmed && non-empty` entries.

Keep the semantic map intentionally tiny:

```csharp
if (question.QuestionKey.Equals("restaurant-cafe.service-channel", StringComparison.OrdinalIgnoreCase))
    return Has("operatingchannels");
```

Do not generalize via fuzzy names/tags.

- [ ] **Step 3: Verify question ceilings/order remain deterministic**

Run existing catalogue/persistence tests to prove:
- ordering unchanged for unaffected questions;
- 0–3 enriched behavior from VS-26 remains valid where applicable;
- max/degraded ceiling behavior is not expanded.

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter ProgressiveQuestion
dotnet test tests/api/Atlas.Api.Tests.csproj
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add apps/api/ProgressiveQuestions.cs tests/api/ProgressiveQuestionCatalogueTests.cs tests/api/ProgressiveQuestionPersistenceTests.cs
git commit -m "fix(vs29): suppress redundant onboarding questions"
```

---

## Task 6: Carry enriched profile fields and confirmed hours through the mobile discovery model

**Files:**
- Modify: `apps/mobile/src/features/business-discovery/discovery-model.ts`
- Modify: `apps/mobile/src/features/business-discovery/place-enrichment-model.ts`
- Modify: `tests/mobile/business-discovery-model.test.mjs`
- Modify: `tests/mobile/place-enrichment-model.test.mjs`

**Interfaces:**

Extend `DiscoveryDraft`:

```ts
export type DiscoveryDraft = {
  // existing fields...
  email: string;
  socialChannels: string;
};
```

Extend confirmed context:

```ts
export type ConfirmedOperatingContext = {
  providerRef: string;
  operatingChannels: string[];
  reservable: boolean | null;
  servicePeriods: string[];
  pricePosition: string | null;
  openingHours: string[];
};
```

- [ ] **Step 1: Write RED model tests**

Assert `createDiscoveryDraft` maps:

```text
email -> draft.email
socialChannels -> draft.socialChannels
openingHours -> draft.businessHours
```

Assert `buildCreateBusinessFromDiscoveryRequest` emits trimmed email/social values.

Assert `buildConfirmedOperatingContext` now includes normalized `openingHours`.

- [ ] **Step 2: Implement model changes**

Use the existing `value(discovery, key)` helper:

```ts
email: value(discovery, 'email'),
socialChannels: value(discovery, 'socialChannels'),
```

And:

```ts
openingHours: values(enrichment.openingHours),
```

- [ ] **Step 3: Run tests**

```bash
node --test tests/mobile/business-discovery-model.test.mjs tests/mobile/place-enrichment-model.test.mjs
```

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add apps/mobile/src/features/business-discovery tests/mobile/business-discovery-model.test.mjs tests/mobile/place-enrichment-model.test.mjs
git commit -m "feat(vs29): carry enriched business profile facts on mobile"
```

---

## Task 7: Make the Business review UI truthful, prefilled and non-repetitive

**Files:**
- Modify: `apps/mobile/app/create-business.tsx`
- Modify: `tests/mobile/00-vs26-place-enrichment-ui.test.mjs`
- Modify: `tests/mobile/business-discovery-multi-source-ui.test.mjs`
- Modify: `tests/mobile/business-discovery-runtime.test.mjs`
- Adapt to final VS-28 shell primitives if merged there; do not revert them.

**Interfaces / UX rules:**
- Prefill reliable discovered `website`, `phone`, `email`, `socialChannels`, `businessHours`.
- Owner may edit/correct before confirmation.
- Do not render noisy empty fact rows.
- “Confirm these operating details” must persist everything shown as included in that confirmation; specifically, confirmed hours must flow through `ConfirmedOperatingContext.openingHours`.
- Public evidence and owner confirmation remain visually distinct.

- [ ] **Step 1: Write RED source-contract/mobile tests**

Tests should assert `create-business.tsx` contains accessible fields/labels for:

```text
Website
Business phone
Business email
Social channels
Opening hours
```

and that the request path uses the enriched draft fields.

Add a contract assertion that the confirmed operating context carries `openingHours`.

- [ ] **Step 2: Add prefilled review inputs without creating a second form flow**

Reuse the existing `DiscoveryDraft` state and `update` helper. Add fields to the current details/review section, not a new screen.

Example pattern:

```tsx
<Text style={s.fieldLabel}>Business email</Text>
<TextInput
  accessibilityLabel="Business email"
  autoCapitalize="none"
  keyboardType="email-address"
  value={form.email}
  onChangeText={value => update('email', value)}
  placeholder="name@business.com"
  style={s.input}
/>
```

Keep social channels as a simple editable string in VS-29; no social-account management subsystem.

- [ ] **Step 3: Align public/owner confirmation copy**

Ensure the review copy uses two distinct concepts:

```text
Observed publicly
Owner confirmed
```

Do not say a field is confirmed if it is omitted from the create request.

- [ ] **Step 4: Ensure goal-first sequencing remains unchanged**

Successful creation still saves the business ID and proceeds into the existing goals-first/progressive onboarding sequence defined by VS-26. Do not route around that logic.

- [ ] **Step 5: Run mobile tests**

```bash
node --test tests/mobile/00-vs26-place-enrichment-ui.test.mjs \
  tests/mobile/business-discovery-multi-source-ui.test.mjs \
  tests/mobile/business-discovery-runtime.test.mjs
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add apps/mobile/app/create-business.tsx tests/mobile
git commit -m "feat(vs29): enrich business confirmation review"
```

---

## Task 8: Replace the raw Expo ownership-conflict error with a safe reset-and-switch flow

**Files:**
- Modify: `apps/mobile/src/api/business-discovery.ts`
- Reuse: `apps/mobile/src/features/business-hub/business-hub-api.ts`
- Modify: `apps/mobile/app/create-business.tsx`
- Modify: `tests/mobile/business-hub-reset.test.mjs`
- Add focused test if useful: `tests/mobile/business-discovery-reset-switch.test.mjs`
- Verify backend policy: `tests/api/DevelopmentResetPolicyTests.cs`

**Existing server primitive:**

```text
POST /api/v1/dev/reset-business
Development environment only
exact subject: atlas-expo-go-demo-owner
BusinessOwner authorization
```

Do not add another delete endpoint.

**Existing client guard to reuse:**

```ts
const EXPO_DEMO_TOKEN = 'atlas-expo-go-demo';
const isExpoDemo = __DEV__ && session?.accessToken === EXPO_DEMO_TOKEN;
```

- [ ] **Step 1: Write RED tests for ownership-conflict interpretation**

`createBusinessFromDiscovery` already throws `BusinessDiscoveryApiError` with stable server code. Test that code `initial_business_exists` can be distinguished from generic failures.

No string matching on “already owns a Business”.

- [ ] **Step 2: Write RED UI behavior tests**

For Development + exact demo token + `initial_business_exists`, assert the screen offers:

```text
You're currently testing another business.
View current business
Reset and use <selected business name>
```

For production/non-demo session, assert **no reset-and-use action** exists and normal safe error behavior remains.

- [ ] **Step 3: Implement a small switch state, not implicit deletion**

Add explicit state in `create-business.tsx`, for example:

```ts
type ExistingBusinessConflict = {
  candidateName: string;
  request: CreateBusinessFromDiscoveryRequest;
} | null;
```

In `submit()`:

```ts
catch (cause) {
  if (
    cause instanceof BusinessDiscoveryApiError &&
    cause.code === 'initial_business_exists' &&
    __DEV__ &&
    session.accessToken === EXPO_DEMO_TOKEN &&
    discovery
  ) {
    setExistingBusinessConflict({ candidateName: form.name.trim(), request });
    return;
  }
  setError(...);
}
```

Refactor enough to avoid duplicating the create request construction.

- [ ] **Step 4: Implement explicit Reset and use…**

On tap:

1. reload session;
2. verify exact demo token again;
3. call existing `resetExpoDemoBusiness(session.accessToken)`;
4. clear local business selection while preserving auth token;
5. retry `createBusinessFromDiscovery` with the **same unconsumed discovery request**;
6. save the returned business ID;
7. continue to the existing goals-first route.

Do not run reset automatically merely because a conflict occurred.

- [ ] **Step 5: Preserve current Business on reset failure**

If reset fails:
- do not clear business selection first;
- do not retry creation;
- show a recoverable error;
- leave “View current business” available.

- [ ] **Step 6: Verify server fail-closed policy remains intact**

Run:

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter DevelopmentResetPolicyTests
```

Expected: non-demo subject/outside-Development remains inaccessible.

- [ ] **Step 7: Run focused mobile tests**

```bash
node --test tests/mobile/business-hub-reset.test.mjs tests/mobile/business-discovery-reset-switch.test.mjs
```

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add apps/mobile/src/api/business-discovery.ts apps/mobile/app/create-business.tsx tests/mobile
git commit -m "feat(vs29): add expo reset and switch business flow"
```

---

## Task 9: Optional Google contact/presence enhancement only if Task 0 policy review permits it

**Files:**
- Modify only if permitted/needed: `apps/api/BusinessPlaceEnrichment.cs`
- Modify only if permitted/needed: `apps/api/BusinessPlaceEnrichmentEndpoints.cs`
- Modify only if permitted/needed: `apps/mobile/src/features/business-discovery/place-enrichment-model.ts`
- Modify: `tests/api/BusinessPlaceEnrichmentTests.cs`
- Modify: `tests/api/BusinessPlaceEnrichmentEndpointTests.cs`
- Modify: `tests/mobile/place-enrichment-model.test.mjs`
- Update policy evidence: `docs/slices/VS-29.md`

**Boundary:** This task is conditional. VS-29 still succeeds without expanding Google fields because non-Google public source + bounded official-site enrichment covers the core contact/presence goal. Do not expand Google usage merely for feature symmetry.

- [ ] **Step 1: Decide from the official-policy review**

If a field may be used transiently in the current owner interaction under DEC-08, it may be added to `BusinessPlaceEnrichmentResponse` for review.

If persistence/caching is restricted, do not put raw Google content into discovery snapshots/profile evidence. Canonical owner-submitted values remain the only persistence route.

- [ ] **Step 2: Write RED mapper/endpoint tests before field-mask changes**

Tests must prove:
- field is transient response content only;
- no new DB persistence occurs from the enrichment GET/POST itself;
- attribution handling remains correct;
- unavailable fields degrade cleanly.

- [ ] **Step 3: Make the smallest allowed field-mask/model change**

Do not add Google photos in VS-29.

- [ ] **Step 4: Run tests and document the verified boundary**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessPlaceEnrichment
node --test tests/mobile/place-enrichment-model.test.mjs
```

- [ ] **Step 5: Commit only if this task produced approved code changes**

```bash
git add apps/api/BusinessPlaceEnrichment.cs apps/api/BusinessPlaceEnrichmentEndpoints.cs apps/mobile/src/features/business-discovery/place-enrichment-model.ts tests docs/slices/VS-29.md
git commit -m "feat(vs29): refine transient place enrichment"
```

If policy review says no code change is needed, record the decision in `docs/slices/VS-29.md` and move on without an empty commit.

---

## Task 10: End-to-end regression, Expo acceptance and exact-head certification

**Files:**
- Modify evidence/docs only as required: `docs/slices/VS-29.md`, `delivery/current-slice.json`
- No production release files.

**Acceptance matrix:**

| Journey | Expected |
|---|---|
| Habibi's-like rich Bolt source | Business image(s) + menu intelligence retained; contact/hours shown when public evidence exposes them |
| Chickn Bites-like source | Supported public variant extracts available media/menu; otherwise honest empty state |
| McDonald's-like source | Business image retained; menu only if accepted public HTML exposes it |
| Official website available | At most one safe strongly matched website enrichment contributes public Business facts |
| Confirmed Google operating details | channels/reservation/service periods/price and displayed confirmed hours persist through owner submission |
| Restaurant with confirmed operating channels | duplicate initial `primarychannels` question is suppressed |
| Expo demo already owns Business | View current + explicit Reset and use… flow |
| Production/non-demo ownership conflict | server invariant remains enforced; no destructive reset UI |

- [ ] **Step 1: Run deterministic API suites**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj
```

Expected: 0 failures.

- [ ] **Step 2: Run deterministic mobile suites**

```bash
node --test tests/mobile/*.test.mjs
```

Expected: 0 failures.

- [ ] **Step 3: Run repository gates**

```bash
npm run governance:validate
npm run preflight
```

Expected: PASS.

- [ ] **Step 4: Run a clean database migration replay even though no VS-29 migration is expected**

Use the repository’s existing CI/local PostgreSQL migration-replay process. The purpose is to prove VS-29 did not accidentally introduce schema drift.

Expected: all existing migrations apply cleanly and model snapshot remains consistent.

- [ ] **Step 5: Request code review through Superpowers**

Use `superpowers:requesting-code-review` after deterministic checks. Fix findings through the PES/Loop cycle and rerun affected tests plus full gates.

- [ ] **Step 6: After exact-head green, update test runtime only**

Only after CI/Security/Product Intake are green on one exact implementation SHA:
- inspect `atlas/test-deployment` and `atlas/expo-go-test-harness` ancestry;
- fast-forward only when safe;
- redeploy `atlas-api-test` manually because Render auto-deploy is disabled;
- do not touch production.

- [ ] **Step 7: Expo Go device acceptance**

Restart Metro with cache clear:

```bash
npx expo start -c
```

Manually verify at least:
1. reset current demo business;
2. discover a rich business source;
3. inspect image/menu/contact/hours review;
4. confirm operating details;
5. confirm/create Business;
6. verify no redundant service-channel question when operating channels are confirmed;
7. repeat with another candidate and exercise Reset and use…;
8. open Business Hub and verify materialized image/menu data when the source exposed it.

- [ ] **Step 8: Supplemental live public-source smoke**

Use live Habibi's/Chickn Bites/McDonald's pages only as supplemental evidence. Record counts/diagnostics, not full copied page content. Provider drift does not invalidate deterministic fixtures unless it reveals a product bug.

- [ ] **Step 9: Bind certification to the exact green implementation SHA**

Update PES certification evidence only after exact-head gates and device acceptance are complete.

Required evidence:

```text
implementation SHA
CI run ID/status
Security baseline run ID/status
Product Intake run ID/status
mobile test count/result
API test result
migration replay result
Expo device acceptance notes
provider live-smoke notes if run
```

- [ ] **Step 10: Run fresh governance-head gates**

If certification metadata creates a new governance-only head, require CI/Security/Product Intake green on that fresh head before requesting merge.

- [ ] **Step 11: Human merge approval**

Do not merge autonomously. Present the exact certified implementation SHA, governance head, gate status and test deployment status to the Product Owner.

No production release/deployment is implied by merge approval.

---

## Plan self-check requirements before execution

Before Task 0 begins, the executing agent must confirm all of the following against the then-current repository:

- [ ] VS-28 runtime work has cleared or been explicitly deferred.
- [ ] This plan has been rebased/reconciled with any VS-28 changes to mobile shell/layout primitives.
- [ ] `CreateBusinessFromDiscoveryRequest` still owns Business Profile creation; no replacement flow was introduced.
- [ ] `BusinessProfile` still contains `Email`, `SocialChannels`, `BusinessHours` so no migration is necessary.
- [ ] `/api/v1/dev/reset-business` remains Development + exact-demo-subject guarded.
- [ ] `initial_business_exists` remains the stable ownership-conflict code.
- [ ] Google enrichment remains transient under DEC-08.
- [ ] Public media/menu retention still follows DEC-07.
- [ ] No production deployment permission has been granted.

## Expected commit sequence

```text
chore(vs29): activate business intelligence enrichment
feat(vs29): enrich public business contact facts
feat(vs29): add bounded official website enrichment
fix(vs29): harden public Bolt media menu extraction
feat(vs29): persist confirmed business presence facts
fix(vs29): suppress redundant onboarding questions
feat(vs29): carry enriched business profile facts on mobile
feat(vs29): enrich business confirmation review
feat(vs29): add expo reset and switch business flow
feat(vs29): refine transient place enrichment        # only if policy-approved/needed
... review/fix commits as required by Loop
... certification governance commit only after exact-head green
```

## Success definition

VS-29 is successful when Atlas automatically captures every trustworthy public Business-level fact available through its approved source boundaries, retains media/menu intelligence when publicly exposed, clearly distinguishes observed evidence from owner-confirmed canonical state, does not ask an initial question that confirmed evidence has already materially answered, and lets the Expo demo user safely switch test Businesses without weakening the production ownership invariant.
