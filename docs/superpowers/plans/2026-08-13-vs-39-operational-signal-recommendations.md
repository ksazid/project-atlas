# VS-39 Operational Signal Recommendations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generate deterministic, goal-aligned Restaurant & Café Today recommendations from material normalized operational changes without causal claims or filler.

**Architecture:** Extend packaged Knowledge Pack evidence rules with optional operational requirements and evaluate them through a focused provider-neutral matcher. VS-38 change projection gains a deterministic parseable payload while preserving human-readable observational wording and exact provenance; the existing Opportunity generator, ranking, confidence, cooldown, snapshot, API, and mobile boundaries remain authoritative.

**Tech Stack:** ASP.NET Core/.NET 10, C# records, xUnit, existing Knowledge Pack Manifest v2, existing VS-38 operational entities/projection, PES/Loop and GitHub Actions.

## Global Constraints

- Supported metrics are exactly `gross-sales`, `orders`, `repeat-orders`, and `delivery-time`.
- Eligible relative deterioration is at least `0.10` over exactly `7` or `28` days; equality qualifies.
- Comparison value zero, historical, malformed, unsupported, positive, unchanged, or below-threshold evidence creates no operational candidate.
- Fresh evidence may retain manifest confidence; stale operational evidence forces `Low` confidence.
- Category-specific thresholds, eligibility, copy, goal mappings, and execution templates remain in the Restaurant & Café Knowledge Pack.
- Preserve exact change and underlying Signal IDs, observational/non-causal wording, Business isolation, existing ranking, cooldown, no-filler, API shapes, and mobile routes.
- No migration, dependency, connector, raw-file storage, PII, AI provider, external action, deployment, EAS/OTA, or production mutation.
- Do not include the unrelated local `package-lock.json` in any commit.

---

### Task 1: Activate governed VS-39 scope

**Files:**
- Create: `docs/slices/VS-39.md`
- Modify: `delivery/current-slice.json`
- Modify: `delivery/completed-slices.json`
- Modify: `README.md`

**Interfaces:**
- Consumes: certified VS-38 merge `9e439b87c21615f33690b55855c0c5add4d99388`, approved design `docs/superpowers/specs/2026-08-13-vs-39-operational-signal-recommendations-design.md`.
- Produces: active `VS-39@1.0`, lifecycle `implementing`, requirements `FR-05`, `FR-07`, `FR-08`, `FR-16`, approved scope/policy/implementation records, certification/release/production pending.

- [ ] **Step 1: Record VS-38 completion without rewriting its evidence**

Move the exact certified VS-38 record into `delivery/completed-slices.json`, preserving certification SHA `0808438a26459fe78cd60ed40eba7adf9d70e7f7`, PR #62, merge SHA `9e439b87c21615f33690b55855c0c5add4d99388`, and all CI/Security/Product evidence.

- [ ] **Step 2: Activate VS-39**

Set `delivery/current-slice.json` to `VS-39`, high risk, runtime-enabled, allowed paths `apps/api/**`, `tests/api/**`, `delivery/**`, `docs/**`, `README.md`; protect release workflow, infrastructure, mobile, payments, uploads, and production configuration. Record Product Owner approval from this conversation at the actual timestamp; keep certification, release, and production-enable pending.

- [ ] **Step 3: Write slice acceptance**

Document the four metrics, exact 10%/7d/28d thresholds, freshness rules, zero-comparison exclusion, category isolation, provenance, non-causal copy, no-filler behavior, rollback, and exact-head evidence requirements in `docs/slices/VS-39.md`.

- [ ] **Step 4: Validate governance**

Run: `npm run planning:validate && npm run governance:validate && npm run slice:validate && npm run dashboard:check`

Expected: all commands pass with `VS-39, implementing, runtime-enabled`.

- [ ] **Step 5: Commit**

```bash
git add delivery/current-slice.json delivery/completed-slices.json docs/slices/VS-39.md README.md
git commit -m "docs(vs39): activate operational recommendations"
```

### Task 2: Add typed operational requirements to Knowledge Pack manifests

**Files:**
- Modify: `apps/api/KnowledgePackManifestV2.cs`
- Test: `tests/api/KnowledgePackManifestV2Tests.cs`

**Interfaces:**
- Consumes: existing `KnowledgeEvidenceRule` and Manifest v2 validation/fingerprint policy.
- Produces: `OperationalChangeDirections`, `KnowledgeOperationalEvidenceRequirement`, and optional `KnowledgeEvidenceRule.OperationalRequirement` while preserving existing constructors.

- [ ] **Step 1: Write failing manifest-policy tests**

Add tests constructing rules with:

```csharp
new KnowledgeEvidenceRule("sales-decline-observed", "Require a material observed sales decline.", 1, false)
{
    OperationalRequirement = new("gross-sales", OperationalChangeDirections.Decrease, .10m, [7, 28],
        [OperationalFreshness.Fresh, OperationalFreshness.Stale])
}
```

Assert valid requirements pass; empty/invalid metric, invalid direction, threshold outside `(0, 1]`, windows outside `{7,28}`, duplicate windows/freshness, or historical freshness fail under `evidenceRules`; and changing any operational requirement field changes the manifest fingerprint.

- [ ] **Step 2: Run RED test**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter FullyQualifiedName~KnowledgePackManifestV2Tests`

Expected: compile failure because the requirement types/property do not exist.

- [ ] **Step 3: Implement minimal typed contract**

Add:

```csharp
public static class OperationalChangeDirections
{
    public const string Decrease = "decrease";
    public const string Increase = "increase";
    public static bool IsValid(string? value) => value is Decrease or Increase;
}

public sealed record KnowledgeOperationalEvidenceRequirement(
    string MetricKey,
    string Direction,
    decimal MinimumRelativeChange,
    IReadOnlyList<int> Windows,
    IReadOnlyList<string> Freshness);
```

Give `KnowledgeEvidenceRule` a record body with nullable init-only `OperationalRequirement`. Validate stable metric keys, directions, threshold, exact supported windows, freshness limited to fresh/stale, and uniqueness. Include a canonical sorted representation in `Fingerprint`.

- [ ] **Step 4: Run GREEN test**

Run the Task 2 filtered command.

Expected: pass.

- [ ] **Step 5: Commit**

```bash
git add apps/api/KnowledgePackManifestV2.cs tests/api/KnowledgePackManifestV2Tests.cs
git commit -m "feat(api): model operational evidence requirements"
```

### Task 3: Encode and parse deterministic operational change facts

**Files:**
- Create: `apps/api/OperationalChangeEvidence.cs`
- Modify: `apps/api/OperationalEvidenceProjection.cs`
- Test: `tests/api/OperationalEvidenceProjectionTests.cs`
- Create: `tests/api/OperationalChangeEvidenceTests.cs`

**Interfaces:**
- Produces: `OperationalChangeEvidence` record and `OperationalChangeEvidenceCodec.Encode/Parse`.
- Encoded facts retain `ResolvedKnowledgeFact(Key, Value, Source)` and exact change/signal provenance.

- [ ] **Step 1: Write failing codec/projection tests**

Use a change with current `90`, comparison `100`, absolute `-10`, relative `-.10`, 7-day window, fresh date, and two Signal IDs. Assert parse returns metric `gross-sales`, window `7`, direction decrease, magnitude `.10`, freshness fresh, confidence high, exact change ID and both Signal IDs. Assert invalid value/source, missing provenance, zero comparison, and non-7/28 windows return no parsed evidence.

- [ ] **Step 2: Run RED tests**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter "FullyQualifiedName~OperationalChangeEvidenceTests|FullyQualifiedName~OperationalEvidenceProjectionTests"`

Expected: compile failure because codec/types do not exist.

- [ ] **Step 3: Implement deterministic codec**

Create:

```csharp
public sealed record OperationalChangeEvidence(
    Guid ChangeId, string MetricKey, int WindowDays, decimal CurrentValue,
    decimal ComparisonValue, decimal AbsoluteDelta, decimal RelativeDelta,
    string Freshness, string Confidence, IReadOnlyList<Guid> SignalIds);

public static class OperationalChangeEvidenceCodec
{
    public static ResolvedKnowledgeFact Encode(BusinessChange change, string freshness);
    public static bool TryParse(ResolvedKnowledgeFact fact, out OperationalChangeEvidence? evidence);
}
```

Use one canonical JSON object in `Value` with `language: "observed"`, invariant decimal JSON numbers, periods and deltas; keep `Source` as canonical operational provenance. Reject non-operational layers, malformed JSON, causal language, missing IDs, zero comparison, unsupported windows, historical freshness, or inconsistent metric key. Sort Signal IDs. Delegate BusinessChange projection to `Encode`; retain direct BusinessSignal projection unchanged.

- [ ] **Step 4: Run GREEN tests**

Run the Task 3 filtered command.

Expected: pass, including existing assertions that Value contains `observed` and never `caused`.

- [ ] **Step 5: Commit**

```bash
git add apps/api/OperationalChangeEvidence.cs apps/api/OperationalEvidenceProjection.cs tests/api/OperationalEvidenceProjectionTests.cs tests/api/OperationalChangeEvidenceTests.cs
git commit -m "feat(api): structure operational change evidence"
```

### Task 4: Match operational evidence requirements provider-neutrally

**Files:**
- Create: `apps/api/OperationalEvidenceMatcher.cs`
- Test: `tests/api/OperationalEvidenceMatcherTests.cs`

**Interfaces:**
- Consumes: `KnowledgeOperationalEvidenceRequirement`, `ResolvedKnowledgeBundle.OperationalFacts`, `OperationalChangeEvidenceCodec.TryParse`.
- Produces: `OperationalEvidenceMatcher.Match(requirement, facts) -> IReadOnlyList<ResolvedKnowledgeFact>`.

- [ ] **Step 1: Write failing matcher matrix**

Cover each supported metric; exact `.10` equality; `.0999` exclusion; decline versus increase; 7/28 accepted and another window excluded; fresh/stale accepted; historical/malformed/unsupported excluded; zero comparison excluded; deterministic ordering by key/value/source.

- [ ] **Step 2: Run RED test**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter FullyQualifiedName~OperationalEvidenceMatcherTests`

Expected: compile failure because matcher does not exist.

- [ ] **Step 3: Implement minimal matcher**

```csharp
public static IReadOnlyList<ResolvedKnowledgeFact> Match(
    KnowledgeOperationalEvidenceRequirement requirement,
    IEnumerable<ResolvedKnowledgeFact> facts)
```

Parse facts, match metric/window/freshness, calculate direction from `RelativeDelta`, compare `Math.Abs(RelativeDelta)` to the exact decimal threshold, and return original facts so evidence IDs remain generated by the existing snapshot path. Do not switch on known metric names.

- [ ] **Step 4: Run GREEN test**

Run the Task 4 filtered command.

Expected: pass.

- [ ] **Step 5: Commit**

```bash
git add apps/api/OperationalEvidenceMatcher.cs tests/api/OperationalEvidenceMatcherTests.cs
git commit -m "feat(api): match operational evidence rules"
```

### Task 5: Add Restaurant operational patterns and execution templates

**Files:**
- Modify: `apps/api/RestaurantCafeKnowledgeManifestV2.cs`
- Test: `tests/api/RestaurantCafeKnowledgeManifestV2Tests.cs`

**Interfaces:**
- Consumes: Task 2 operational rule metadata.
- Produces: Restaurant manifest version `1.1`, four operational evidence rules, four patterns, and four owner-controlled execution templates.

- [ ] **Step 1: Write failing manifest tests**

Assert exact version `1.1`; four operational rules match the design metrics/directions/threshold/windows/freshness; patterns are `sales-decline-review`, `order-decline-review`, `repeat-order-decline-review`, `delivery-time-deterioration-review`; goal mappings match the spec; templates use review/experiment/checklist language; all copy contains no `caused`, `guarantee`, blame, invented segment, provider, employee, or menu claim.

- [ ] **Step 2: Run RED test**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter FullyQualifiedName~RestaurantCafeKnowledgeManifestV2Tests`

Expected: fail because version/rules/patterns/templates are absent.

- [ ] **Step 3: Implement packaged policy**

Update `Version` to `1.1`. Add one operational evidence rule per metric with `.10m`, `[7,28]`, `[fresh,stale]`; add the four patterns with the approved goal types and 7-day cooldown; add owner-controlled checklists that start from observed periods, review controllable operations, choose one bounded experiment, and record the later observation separately. Extend guardrails with explicit correlation-not-causation wording.

- [ ] **Step 4: Run GREEN test**

Run the Task 5 filtered command.

Expected: pass.

- [ ] **Step 5: Commit**

```bash
git add apps/api/RestaurantCafeKnowledgeManifestV2.cs tests/api/RestaurantCafeKnowledgeManifestV2Tests.cs
git commit -m "feat(api): add restaurant operational patterns"
```

### Task 6: Integrate operational rule resolution, confidence, ranking, and cooldown

**Files:**
- Modify: `apps/api/OpportunityGeneration.cs`
- Create: `tests/api/OperationalOpportunityGenerationTests.cs`
- Modify: `tests/api/OpportunityGenerationTests.cs`

**Interfaces:**
- Consumes: Tasks 2–5.
- Produces: operational candidates through existing `OpportunityGenerator.Generate`; no API-shape changes.

- [ ] **Step 1: Write failing end-to-end generator tests**

Build bundles containing encoded operational facts and assert:

- fresh material gross-sales decline + revenue goal produces `sales-decline-review` with exact operational provenance;
- orders/repeat-orders/delivery-time produce only their matching patterns and approved goals;
- generic category, missing exact Restaurant manifest, below threshold, positive/unchanged, zero comparison, historical, malformed, or unsupported metric produces no operational pattern;
- stale eligible evidence produces Low confidence;
- when multiple patterns qualify, existing goal priority then category-specific/confidence/effort/title ordering remains deterministic;
- same evidence is suppressed inside cooldown; new eligible change IDs reconsider the same pattern; unrelated facts do not bypass cooldown;
- Value/Why text uses observed/review language and contains no causal claim.

- [ ] **Step 2: Run RED tests**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj --filter "FullyQualifiedName~OperationalOpportunityGenerationTests|FullyQualifiedName~OpportunityGenerationTests"`

Expected: operational candidates absent because rule resolution does not use operational requirements.

- [ ] **Step 3: Integrate matcher into rule resolution**

Before the existing rule-key switch, if `rule.OperationalRequirement is { } requirement`, call `OperationalEvidenceMatcher.Match(requirement, bundle.OperationalFacts)`, map original facts through existing `FactEvidence`, enforce `MinimumEvidenceCount`, and return. Keep all existing switch cases unchanged.

Update confidence resolution so any operational evidence whose parsed freshness is stale forces Low; existing non-owner evidence policy remains unchanged. Add an operational limitation to candidates only when operational evidence is present: `"The observed movement does not prove what caused it; review assumptions before acting."`

- [ ] **Step 4: Run GREEN focused tests**

Run the Task 6 filtered command.

Expected: pass.

- [ ] **Step 5: Run full API suite**

Run: `dotnet test tests/api/Atlas.Api.Tests.csproj`

Expected: all tests pass; existing snapshot, cooldown, manifest, Today, and API behavior remain green.

- [ ] **Step 6: Commit**

```bash
git add apps/api/OpportunityGeneration.cs tests/api/OperationalOpportunityGenerationTests.cs tests/api/OpportunityGenerationTests.cs
git commit -m "feat(api): generate operational recommendations"
```

### Task 7: Verify, document, and stop for exact-SHA certification

**Files:**
- Modify: `docs/slices/VS-39.md`
- Modify: `delivery/current-slice.json`
- Modify: `README.md`

**Interfaces:**
- Produces: tested certification candidate and evidence package; no merge/release/deployment authorization.

- [ ] **Step 1: Run deterministic repository gates**

Run:

```bash
git diff --check
npm run planning:validate
npm run governance:validate
npm run slice:validate
npm run preflight
dotnet restore apps/api/Atlas.Api.csproj
dotnet build apps/api/Atlas.Api.csproj --configuration Release --no-restore
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release
```

Expected: all available commands pass. If local .NET or Expo state is sandbox-blocked, record the truthful boundary and use GitHub Actions RED/GREEN evidence; never represent an unavailable local check as passed.

- [ ] **Step 2: Review safety and scope**

Inspect changed files and scan for `caused|guaranteed|customer|email|phone|address|token|refresh_token|raw csv`; verify every occurrence is an intentional test/guardrail and no PII, raw content, secret, provider credential, dependency, migration, mobile route, release workflow, or infrastructure change entered scope.

- [ ] **Step 3: Publish PR and obtain exact-head gates**

Push/update a draft PR to `main`. Require CI, Security baseline, and Product Intake on the exact head. For any defect, add/reproduce a RED test, apply the smallest fix, and rerun all exact-head gates.

- [ ] **Step 4: Record evidence without self-approval**

Set lifecycle `certification`, implementation/testing 100, certification status `running`, exact tested SHA and run URLs. Keep certification approval pending and release/production not authorized.

- [ ] **Step 5: Commit governance-only evidence and verify it**

```bash
git add delivery/current-slice.json docs/slices/VS-39.md README.md
git commit -m "docs(vs39): record certification candidate evidence"
```

Require CI, Security baseline, and Product Intake again on this governance-only head.

- [ ] **Step 6: Stop**

Present the exact implementation SHA, final PR head, test/gate results, remaining risks, and explicit statement: certification, merge, release, deployment, EAS/OTA, and production enablement require the Product Owner's exact-SHA approval.
