# VS-22 Multi-Source Business Discovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let an owner safely discover one Business from one primary and up to two optional public URLs, with immediate canonicalisation, strict server-side validation, Google place-link resolution, field-level source precedence, duplicate/conflict protection and auditable provenance.

**Architecture:** Keep the existing VS-21 Business discovery/location flow and split VS-22 responsibilities into focused units: URL policy/canonicalisation, Google source resolution, multi-source reconciliation, persistence/provenance, and mobile row state. The mobile app provides immediate safe canonicalisation for UX, while the API repeats the full policy authoritatively before any outbound request. Reconciliation is deterministic and ordered: the first usable value wins, later equivalent values corroborate, later conflicting values are retained but never overwrite automatically.

**Tech Stack:** Expo React Native + TypeScript, ASP.NET Core/.NET, EF Core, PostgreSQL, xUnit, Node test runner, GitHub Actions/PES governance.

## Global Constraints

- Exactly one primary source and at most two optional secondary sources; maximum three URLs total.
- Additional URLs are optional and empty optional rows do not block discovery.
- The owner-defined row order is the source priority; Atlas does not reorder by provider.
- HTTPS only; credentials, non-443 ports, IP literals, unsafe hosts/addresses and generic provider routes are rejected before fetching.
- Client canonicalisation is UX only; server validation and canonicalisation are authoritative.
- `maps.app.goo.gl` is the only redirecting source in VS-22 and every hop must be revalidated.
- Ordinary websites, Bolt and Wolt do not follow redirects automatically.
- Primary values cannot be silently overwritten by lower-priority values.
- Provider names/marketplace hostnames remain hidden from owner-facing confirmation copy.
- Location/country/timezone/currency remain controlled by the VS-21 location-resolution boundary.
- No browser automation, script execution, arbitrary redirect following, authenticated/private source access or whole-domain crawling.
- No production deployment in this slice.

---

### Task 1: Activate governed VS-22 slice and lock scope

**Files:**
- Modify: `delivery/current-slice.json`
- Create: `docs/slices/VS-22.md`
- Modify: `delivery/traceability.json` only if the current schema requires the active slice to be registered there.

**Interfaces:**
- Consumes: approved design spec `docs/superpowers/specs/2026-08-11-vs-22-multi-source-business-discovery-design.md`.
- Produces: an active `VS-22` runtime-enabled slice with approved `scope` and `implementation` records, allowed paths limited to `apps/api/**`, `apps/mobile/**`, `tests/api/**`, `tests/mobile/**`, `delivery/**`, `docs/**`.

- [ ] **Step 1: Write the slice definition**

Set `sliceId` to `VS-22`, title to `VS-22 — Multi-Source Business Discovery & Fact Reconciliation`, status to `active`, lifecycle to the repository's implementation-ready state, risk level `medium`, implementation mode `runtime-enabled`, requirements `FR-02`, `FR-03`, `FR-05`, `FR-16`, dependencies on `VS-21`, and explicit notes covering URL sanitisation, SSRF protection, up-to-three source precedence, Google place resolution, provenance and no production release.

- [ ] **Step 2: Record the Product Owner approvals already granted in this conversation**

Add typed `scope` and `implementation` approval records with rationale: optional up-to-three source URLs; primary/secondary precedence; strict sanitisation; Google Maps/place support; provider-neutral UI; feature-branch implementation only.

- [ ] **Step 3: Run governance validation**

Run: `npm run governance:validate`
Expected: PASS with VS-22 active and no authority conflict.

- [ ] **Step 4: Commit**

```bash
git add delivery/current-slice.json delivery/traceability.json docs/slices/VS-22.md
git commit -m "chore(vs22): activate multi-source discovery slice"
```

### Task 2: Add authoritative URL canonicalisation and provider-route policy

**Files:**
- Create: `apps/api/BusinessDiscoveryUrls.cs`
- Modify: `apps/api/BusinessDiscovery.cs`
- Modify: `tests/api/BusinessDiscoveryPolicyTests.cs`
- Create: `apps/mobile/src/features/business-discovery/url-policy.ts`
- Create: `tests/mobile/business-discovery-url-policy.test.mjs`

**Interfaces:**
- Produces API types:
  - `enum BusinessSourceKind { Website, BoltFood, Wolt, GoogleMaps }`
  - `sealed record CanonicalBusinessUrl(Uri Uri, string Value, BusinessSourceKind Kind)`
  - `PublicBusinessUrlPolicy.TryCanonicalize(string? raw, out CanonicalBusinessUrl? canonical, out string? error)`
  - `PublicBusinessUrlPolicy.CanonicalizeMany(string primary, IReadOnlyList<string>? additional)` returning ordered canonical URLs or throwing `BusinessDiscoveryException` with stable codes.
- Produces mobile functions:
  - `canonicalizeBusinessUrlInput(value: string): { value: string; complete: boolean; error: string | null }`
  - `canonicalBusinessUrlKey(value: string): string | null`

- [ ] **Step 1: Extend API policy tests first**

Add failing xUnit cases proving:

```csharp
[Theory]
[InlineData("Antalya - https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians?utm_source=share_provider&utm_medium=product&utm_content=menu_header", "https://food.bolt.eu/en/324/p/122519-antalya-kebab-st-julians")]
[InlineData("https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86?g_st=ic", "https://maps.app.goo.gl/ejRnLZ8ZZovZhtm86")]
public void UrlPolicy_CanonicalizesSupportedShareInputs(string raw, string expected) { ... }
```

Also add failures for two embedded URLs, `http`, credentials, non-443 ports, IPv4/IPv6 literals, localhost/private/reserved targets, generic Bolt/Wolt routes, generic `google.com/search`, fragments, duplicate canonical sources, and more than two additional sources.

- [ ] **Step 2: Run API policy tests and verify RED**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessDiscoveryPolicyTests`
Expected: FAIL because the new canonicalisation surface does not exist.

- [ ] **Step 3: Implement `BusinessDiscoveryUrls.cs` minimally**

Implement one-URL share-text extraction using a strict HTTPS URL matcher; reject multiple detected URLs. Canonicalise scheme/host/path, remove fragments, reject user-info/non-443/IP literals, remove known tracking keys (`utm_*`, `gclid`, `fbclid`, `msclkid`, `g_st` and provider share/referral analytics keys), preserve ordinary-site non-tracking query values, and enforce provider-specific route checks:

```csharp
public static bool TryCanonicalize(string? raw, out CanonicalBusinessUrl? canonical, out string? error)
```

Bolt must require a specific `/p/<id-or-slug>` business path; Wolt must require a venue/restaurant path already identifying one venue; Google accepts `maps.app.goo.gl/<token>` or one-establishment Maps place URLs but rejects generic Search/area/directions-only URLs.

- [ ] **Step 4: Implement ordered collection validation**

`CanonicalizeMany` must discard blank optional values, preserve priority order, reject more than three total canonical inputs, and reject canonical duplicates with code `business_source_duplicate` before any fetch.

- [ ] **Step 5: Run API policy tests and verify GREEN**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessDiscoveryPolicyTests`
Expected: PASS.

- [ ] **Step 6: Write mobile canonicalisation tests first**

Add Node tests proving a pasted Bolt share string becomes the canonical URL, `maps.app.goo.gl/...?...` loses `g_st`, partial typing is not destructively rewritten, unsafe complete URLs return an inline error, and canonical duplicate keys compare equal.

- [ ] **Step 7: Run mobile URL policy test and verify RED**

Run: `node --test tests/mobile/business-discovery-url-policy.test.mjs`
Expected: FAIL because `url-policy.ts` does not exist.

- [ ] **Step 8: Implement mobile canonicalisation parity**

Use platform URL parsing only; do not perform DNS/network checks on mobile. Mirror syntactic/provider-route/tracking rules, return partial input unchanged until an absolute URL is complete, and never treat client validation as authoritative.

- [ ] **Step 9: Run mobile URL policy test and verify GREEN**

Run: `node --test tests/mobile/business-discovery-url-policy.test.mjs`
Expected: PASS.

- [ ] **Step 10: Commit**

```bash
git add apps/api/BusinessDiscoveryUrls.cs apps/api/BusinessDiscovery.cs tests/api/BusinessDiscoveryPolicyTests.cs apps/mobile/src/features/business-discovery/url-policy.ts tests/mobile/business-discovery-url-policy.test.mjs
git commit -m "feat(vs22): add strict business source URL policy"
```

### Task 3: Add controlled Google Maps short-link/place resolution

**Files:**
- Create: `apps/api/GoogleBusinessSourceResolver.cs`
- Modify: `apps/api/BusinessLocationResolution.cs`
- Modify: `apps/api/Program.cs`
- Modify: `tests/api/BusinessDiscoveryRedirectTests.cs`
- Modify: `tests/api/BusinessLocationResolutionTests.cs`

**Interfaces:**
- Consumes: `CanonicalBusinessUrl` with `Kind == BusinessSourceKind.GoogleMaps`.
- Produces:
  - `sealed record ResolvedGoogleBusinessSource(string CanonicalSourceUrl, string Query, string? PlaceId)`
  - `IGoogleBusinessSourceResolver.ResolveAsync(CanonicalBusinessUrl source, CancellationToken ct)`
  - existing Google Places adapter entrypoint is used to obtain structured place facts; Google Maps HTML is never scraped.

- [ ] **Step 1: Add failing redirect-security tests**

Cover initial `maps.app.goo.gl` acceptance; maximum 4 redirect hops; `AllowAutoRedirect=false`; HTTPS/443 at every hop; redirect-host allowlist restricted to approved Google Maps hosts; rejection of cross-provider redirect; DNS public-address enforcement on every hop; no cookies/proxy; and a final URL that does not resolve one establishment returns `business_google_place_unresolved`.

- [ ] **Step 2: Run redirect tests and verify RED**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessDiscoveryRedirectTests`
Expected: FAIL because the controlled resolver does not exist.

- [ ] **Step 3: Implement `GoogleBusinessSourceResolver`**

Create a dedicated `HttpClient`/handler with redirects disabled and no proxy. Manually process only 301/302/303/307/308, cap at 4 hops, validate each `Location` with the Google redirect allowlist and the same public-address connector policy, then extract a specific place identity/query for the existing Places adapter.

- [ ] **Step 4: Route Google source facts through the existing Places boundary**

Extend `BusinessLocationResolution.cs` with a provider-neutral method that accepts the resolved Google source and returns the same canonical place metadata used by VS-21. Do not add a second Google Time Zone API call.

- [ ] **Step 5: Run Google redirect/location tests and verify GREEN**

Run:
`dotnet test tests/api/Atlas.Api.Tests.csproj --filter "BusinessDiscoveryRedirectTests|BusinessLocationResolutionTests"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add apps/api/GoogleBusinessSourceResolver.cs apps/api/BusinessLocationResolution.cs apps/api/Program.cs tests/api/BusinessDiscoveryRedirectTests.cs tests/api/BusinessLocationResolutionTests.cs
git commit -m "feat(vs22): resolve Google business links safely"
```

### Task 4: Implement bounded multi-source discovery and deterministic reconciliation

**Files:**
- Create: `apps/api/BusinessDiscoveryReconciliation.cs`
- Modify: `apps/api/BusinessDiscovery.cs`
- Modify: `tests/api/BusinessDiscoveryPolicyTests.cs`
- Create: `tests/api/BusinessDiscoveryReconciliationTests.cs`

**Interfaces:**
- Change request contract to:
  - `sealed record DiscoverBusinessRequest(string Url, IReadOnlyList<string>? AdditionalUrls = null)`
- Produce:
  - `sealed record BusinessSourceObservation(int Order, bool IsPrimary, string Provider, string CanonicalUrl, string Status, IReadOnlyList<PublicBusinessFact> Facts, string? WarningCode = null)`
  - `BusinessDiscoveryReconciler.Reconcile(IReadOnlyList<BusinessSourceObservation> sources)` returning one `PublicBusinessSnapshot` plus evidence classifications.

- [ ] **Step 1: Add failing reconciliation tests**

Cover primary wins; secondary fills missing; third source fills when first two lack; equivalent later fact is corroborating only; conflict retains higher-priority selected value; unrelated secondary cannot contribute; primary network failure falls back to first successful secondary; optional secondary failure does not fail useful discovery; and no useful source returns `business_sources_no_facts`.

- [ ] **Step 2: Add large-page regression first**

Change the existing oversized-response expectation: the bounded reader must stop after `PublicBusinessHtmlReader.MaxCharacters` and return the prefix rather than throw merely because more response bytes exist. A fixture with useful metadata before the cap must still extract facts.

- [ ] **Step 3: Run reconciliation/policy tests and verify RED**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter "BusinessDiscoveryReconciliationTests|BusinessDiscoveryPolicyTests"`
Expected: FAIL under the current single-source/request and oversize-rejection behavior.

- [ ] **Step 4: Implement bounded reader behavior**

Keep the current 750,000-character cap. Stop reading at the cap and return the prefix; never allocate/read beyond the bounded content required. If no useful fact appears in the bounded prefix, report no useful facts for that source.

- [ ] **Step 5: Implement multi-source orchestration**

Canonicalise the primary plus optional sources before any request. Process in owner priority order. Ordinary pages use the existing SSRF-safe HTTP connector; Google source uses `IGoogleBusinessSourceResolver`. Invalid/unsafe URLs abort validation before any external request; network/source-content failures become source warnings when another source succeeds.

- [ ] **Step 6: Implement identity association**

Use normalised business name plus available location/place evidence. Strong matches may reconcile; ambiguous matches are retained as unmerged evidence; clear mismatches are excluded. If primary yields no identity, the first successful secondary becomes the temporary anchor. Prefer excluding uncertain enrichment over mixing businesses.

- [ ] **Step 7: Implement field reconciliation**

For each fact key, select the first usable value in source order. Normalize later values only for comparison. Equivalent later values are corroboration; differing values are conflicts; neither creates a duplicate selected fact nor overwrites the earlier value.

- [ ] **Step 8: Run reconciliation/policy tests and verify GREEN**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter "BusinessDiscoveryReconciliationTests|BusinessDiscoveryPolicyTests"`
Expected: PASS.

- [ ] **Step 9: Commit**

```bash
git add apps/api/BusinessDiscoveryReconciliation.cs apps/api/BusinessDiscovery.cs tests/api/BusinessDiscoveryPolicyTests.cs tests/api/BusinessDiscoveryReconciliationTests.cs
git commit -m "feat(vs22): reconcile ordered business discovery sources"
```

### Task 5: Persist source/evidence provenance and preserve single-source compatibility

**Files:**
- Modify: `apps/api/BusinessDiscoveryPersistence.cs`
- Modify: `apps/api/AtlasDomain.cs`
- Create: `apps/api/Migrations/20260811030000_MultiSourceBusinessDiscovery.cs`
- Modify: `apps/api/Migrations/AtlasDbContextModelSnapshot.cs`
- Modify: `tests/api/BusinessDiscoveryPersistenceTests.cs`

**Interfaces:**
- Add entities:
  - `BusinessDiscoverySource { Id, SnapshotId, Order, IsPrimary, Provider, CanonicalUrl, ObservedAt, Status, WarningCode, AssociationStatus }`
  - `BusinessDiscoveryEvidence { Id, SnapshotId, SourceId, Key, Value, Confidence, EvidenceClass, ReconciliationState }`
- Reconciliation states: `selected`, `corroborating`, `conflict`, `excluded`.
- Existing `BusinessDiscoveryFact` remains the selected owner-facing fact set consumed by Business creation.

- [ ] **Step 1: Add failing persistence tests**

Prove one snapshot can persist three ordered sources; every candidate fact retains source and reconciliation state; selected facts remain unique by snapshot/key; legacy single-source snapshot creation still works; consuming the snapshot still creates one Business and preserves selected provenance.

- [ ] **Step 2: Run persistence tests and verify RED**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessDiscoveryPersistenceTests`
Expected: FAIL because source/evidence entities are absent.

- [ ] **Step 3: Implement entities and EF configuration**

Add DbSets, relationships, bounded column lengths and indexes. Do not put BusinessId on pre-Business discovery records; retain account scoping through the snapshot. Keep source/evidence append-oriented after discovery creation.

- [ ] **Step 4: Add forward-only migration and snapshot metadata**

Create tables for discovery sources/evidence with FKs to snapshot/source, indexes on `(SnapshotId, Order)` and `(SnapshotId, Key)`, and no destructive changes to existing discovery tables.

- [ ] **Step 5: Update persistence mapping**

`BusinessDiscoverySnapshot.Create` must persist selected facts plus all source/evidence records. Existing single-source creation helpers should wrap the source as order 0 primary so old tests/data remain readable.

- [ ] **Step 6: Run persistence tests and migration validation**

Run:
`dotnet test tests/api/Atlas.Api.Tests.csproj --filter BusinessDiscoveryPersistenceTests`
Expected: PASS.

Run the repository migration/model validation command used by CI (or `dotnet ef migrations list --project apps/api` if no wrapper exists).
Expected: new migration is discoverable and model snapshot is consistent.

- [ ] **Step 7: Commit**

```bash
git add apps/api/BusinessDiscoveryPersistence.cs apps/api/AtlasDomain.cs apps/api/Migrations/20260811030000_MultiSourceBusinessDiscovery.cs apps/api/Migrations/AtlasDbContextModelSnapshot.cs tests/api/BusinessDiscoveryPersistenceTests.cs
git commit -m "feat(vs22): persist multi-source discovery provenance"
```

### Task 6: Add optional three-row mobile discovery UI with immediate sanitisation

**Files:**
- Modify: `apps/mobile/app/create-business.tsx`
- Modify: `apps/mobile/src/api/business-discovery.ts`
- Modify: `tests/mobile/business-discovery-model.test.mjs`
- Modify: `tests/mobile/business-discovery-url-policy.test.mjs`

**Interfaces:**
- Mobile state: `sourceUrls: string[]` with length 1..3.
- API call: `discoverBusiness(accessToken: string, url: string, additionalUrls?: string[]): Promise<BusinessDiscovery>`.

- [ ] **Step 1: Add failing UI source-row tests**

Assert source contains one initial row, explicit `Add another business page URL`, maximum three rows, `Clear primary business page URL`, `Remove additional business page URL`, immediate use of `canonicalizeBusinessUrlInput`, duplicate blocking via `canonicalBusinessUrlKey`, and provider-neutral confirmation copy.

- [ ] **Step 2: Run mobile tests and verify RED**

Run: `npm run mobile:test`
Expected: FAIL because the screen still owns one `url` string.

- [ ] **Step 3: Replace single URL state with ordered rows**

Initialize `sourceUrls` as `['']`. The existing in-field `+` Pressable adds a blank row only while length < 3. The primary row `×` clears its value; an additional row `×` removes the row and shifts following priority upward. Empty optional rows are ignored when submitting.

- [ ] **Step 4: Canonicalise complete pasted URLs immediately**

On each row `onChangeText`, call `canonicalizeBusinessUrlInput`. If complete and valid, replace visible row text with the canonical value; if invalid, retain a safe display value and show row-specific accessible error. Clear stale error when that row becomes valid or is removed.

- [ ] **Step 5: Block duplicates before submission**

Compare non-empty canonical keys. Duplicate rows get an inline error and `Discover my business` is disabled until fixed. The server still repeats duplicate validation authoritatively.

- [ ] **Step 6: Send ordered optional URLs**

Call:

```ts
await discoverBusiness(session.accessToken, sourceUrls[0].trim(), sourceUrls.slice(1).map(x => x.trim()).filter(Boolean));
```

Update `business-discovery.ts` to send `{ url, additionalUrls }`.

- [ ] **Step 7: Run mobile tests and verify GREEN**

Run: `npm run mobile:test`
Expected: PASS.

Run: `npm run mobile:typecheck && npm run mobile:lint`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add apps/mobile/app/create-business.tsx apps/mobile/src/api/business-discovery.ts tests/mobile/business-discovery-model.test.mjs tests/mobile/business-discovery-url-policy.test.mjs
git commit -m "feat(vs22): add optional multi-source discovery inputs"
```

### Task 7: Fix confirmation 5xx regression and certify end-to-end behavior

**Files:**
- Modify: `apps/api/BusinessDiscoveryPersistence.cs` only if root cause is in creation/persistence.
- Modify: `apps/api/BusinessDiscovery.cs` only if root cause is endpoint/request handling.
- Modify: `tests/api/BusinessDiscoveryPersistenceTests.cs`
- Modify: `tests/mobile/business-discovery-model.test.mjs`
- Modify: `docs/slices/VS-22.md`
- Modify: `delivery/current-slice.json`

**Interfaces:**
- A valid reconciled snapshot must be consumable exactly once by `/api/v1/businesses/from-discovery` and must return a created Business response rather than 5xx.

- [ ] **Step 1: Reproduce the confirmation failure with a failing test**

Construct a persisted discovery snapshot using the same data shape produced by multi-source discovery, resolve canonical location fields, build `CreateBusinessFromDiscoveryRequest`, and call `BusinessDiscoveryBusinessCreator.CreateAsync`. Assert Business creation succeeds and the snapshot is consumed exactly once.

- [ ] **Step 2: Run the focused regression and verify RED if the defect remains**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter "BusinessDiscoveryPersistenceTests"`
Expected: the new regression fails at the real exception boundary if the defect is still present. If it already passes after earlier refactoring, retain the test as regression evidence and do not invent a code change.

- [ ] **Step 3: Apply only the root-cause fix if needed**

Preserve transactionality, account isolation, exact snapshot consumption and existing generic Knowledge Pack assignment. Do not weaken validation or catch-and-hide server exceptions merely to make the test green.

- [ ] **Step 4: Run all API tests**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj`
Expected: PASS.

- [ ] **Step 5: Run all mobile validation**

Run: `npm run mobile:validate`
Expected: PASS.

- [ ] **Step 6: Run PES preflight/security-relevant checks**

Run: `npm run preflight`
Expected: PASS.

Run the repository's security baseline through CI on the exact branch head; do not claim certification before the workflow is green.

- [ ] **Step 7: Update slice evidence**

Record exact test commands, CI run IDs, security result, Product Intake/governance result, migration validation and exact head SHA in `docs/slices/VS-22.md` and `delivery/current-slice.json`. Certification approval remains bound to the exact tested 40-character SHA and requires Product Owner approval.

- [ ] **Step 8: Commit final evidence changes**

```bash
git add apps/api/BusinessDiscoveryPersistence.cs apps/api/BusinessDiscovery.cs tests/api/BusinessDiscoveryPersistenceTests.cs tests/mobile/business-discovery-model.test.mjs docs/slices/VS-22.md delivery/current-slice.json
git commit -m "test(vs22): certify multi-source discovery flow"
```

### Task 8: Open governed PR without merge or deployment

**Files:**
- No runtime source change required unless review finds a defect.

**Interfaces:**
- Produces a Draft PR from `atlas/vs22-multi-source-discovery` to `main` with exact-head evidence and explicit no-deployment statement.

- [ ] **Step 1: Verify final diff is scoped**

Run: `git diff --check main...HEAD`
Expected: no whitespace errors and no deployment/protected-path modifications.

- [ ] **Step 2: Verify exact-head workflow status**

Check CI, Security baseline and Product Intake/governance on the exact current SHA. Any failure loops back to the relevant TDD task; do not bypass.

- [ ] **Step 3: Open/update Draft PR to `main`**

PR body must summarize the three-URL optional UI, sanitisation/SSRF policy, Google place resolution, reconciliation/provenance, migration, confirmation regression coverage, exact tested SHA and no production deployment authorization.

- [ ] **Step 4: Stop at human merge checkpoint**

Do not merge, release, modify deployment branches or deploy until Product Owner explicitly approves the exact certified SHA.
