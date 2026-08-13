# VS-37 Evidence-Aware Today Cooldown Repair Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Today duplicate cooldown sensitive to the goal and evidence actually used by an Opportunity pattern, so material relevant evidence changes can be reconsidered without allowing unrelated Context edits to create repetitive recommendations.

**Architecture:** Keep the existing Opportunity generator, Knowledge Pack evidence rules, ranking and persistence model. Resolve pattern evidence before cooldown, compute a deterministic SHA-256 cooldown fingerprint from pattern + goal context + resolved EvidenceIds, persist it in EvidenceJson schema v2, and derive the same identity from complete schema-v1 snapshots for backward compatibility. No database or mobile changes.

**Tech Stack:** .NET 10, C#, System.Text.Json, SHA-256, xUnit, EF Core/PostgreSQL regression gate, existing PES/Loop/Superpowers workflow.

## Global Constraints

- Preserve FR-07 one-primary-Today behavior and existing no-filler eligibility.
- Existing Knowledge Pack `CooldownDays` values remain unchanged.
- Cooldown considers only evidence returned by the pattern's existing evidence rules; never use the whole bundle fingerprint.
- Prior Opportunity status does not bypass duplicate suppression.
- Complete schema-v1 snapshots must be interpreted without database backfill; incomplete same-pattern legacy snapshots suppress conservatively.
- No database migration, mobile UI change, connector/provider change, model prompt change, EAS/OTA, production release or production database mutation.

---

### Task 1: Evidence-aware cooldown identity for new snapshots

**Files:**
- Modify: `tests/api/OpportunityGenerationTests.cs`
- Modify: `apps/api/OpportunityGeneration.cs`

**Interfaces:**
- Consumes: `TryResolveEvidence(...)`, `OpportunityEvidenceReference.EvidenceId`, `BusinessGoal`, `KnowledgeOpportunityPattern`, `Opportunity.EvidenceJson`.
- Produces: `GeneratedOpportunityCandidate.CooldownFingerprint`, `OpportunityGenerationSnapshot.Serialize(...)` schema v2 with `cooldownFingerprint`, and evidence-aware `IsSuppressed(...)` behavior for new snapshots.

- [ ] **Step 1: Write the failing tests**

Add focused tests that generate a real prior candidate and serialize it into a prior `Opportunity`:

```csharp
[Fact]
public void Changed_relevant_evidence_can_reconsider_same_pattern_inside_cooldown()
{
    var businessId = Guid.NewGuid();
    var goal = Goal(businessId, "revenue", "Increase revenue", 1);
    var oldBundle = Bundle("restaurant-cafe",
        context: [Fact("context", "currentpriorities", "Demand", "owner")]);
    var oldCandidate = Assert.Single(
        OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], oldBundle, [], Now).Candidates,
        x => x.PatternKey == "current-offer-visibility-review");
    var prior = PriorOpportunityFromCandidate(businessId, oldCandidate, Now.AddHours(-1), OpportunityStatuses.Applied);
    var newBundle = Bundle("restaurant-cafe",
        context: [Fact("context", "currentpriorities", "Improve weekday lunch demand", "owner")]);

    var result = OpportunityGenerator.Generate(ConfirmedProfile(businessId), [goal], newBundle, [prior], Now);

    Assert.Contains(result.Candidates, x => x.PatternKey == "current-offer-visibility-review");
}
```

Add companion contracts:
- same `currentpriorities` plus changed unrelated `customers` context remains suppressed;
- prior status `NotRelevant` with unchanged evidence remains suppressed;
- `Snapshot_separates_evidence_from_interpretation_and_retains_exact_versions` now expects `schemaVersion == 2`, a non-empty `cooldownFingerprint`, and deterministic equality across repeated generation with identical inputs.

Add this test helper:

```csharp
private static Opportunity PriorOpportunityFromCandidate(
    Guid businessId,
    GeneratedOpportunityCandidate candidate,
    DateTimeOffset createdAt,
    string status) => new()
{
    Id = Guid.NewGuid(),
    BusinessId = businessId,
    Title = candidate.Title,
    WhyItMatters = candidate.Reason,
    WhyNow = candidate.WhyNow,
    ExpectedImpact = candidate.ExpectedImpact,
    Effort = candidate.Effort,
    Confidence = candidate.Confidence,
    EvidenceSummary = "Prior",
    EvidenceJson = OpportunityGenerationSnapshot.Serialize(candidate),
    Status = status,
    KnowledgePackKey = candidate.KnowledgePackKey,
    KnowledgePackVersion = candidate.KnowledgePackVersion,
    KnowledgePackVersionId = Guid.NewGuid(),
    GoalId = candidate.GoalId,
    CreatedAt = createdAt,
    ExpiresAt = createdAt.AddDays(1)
};
```

- [ ] **Step 2: Run the exact test file and verify RED**

Run:

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter FullyQualifiedName~OpportunityGenerationTests
```

Expected: the changed-relevant-evidence contract and schema-v2/fingerprint assertion fail under the old pattern-only suppression. Existing same-evidence suppression contracts remain green.

- [ ] **Step 3: Implement the minimal new-snapshot behavior**

In `GeneratedOpportunityCandidate`, add:

```csharp
string CooldownFingerprint,
```

Compute it only after `TryResolveEvidence` succeeds. Reorder generation to:

```csharp
if (!MatchesGoal(pattern, goal)) continue;
if (!TryResolveEvidence(manifest, pattern, profile, goal, bundle, out var evidence)) continue;
var cooldownFingerprint = CooldownFingerprint(pattern.Key, goal, evidence);
if (IsSuppressed(pattern.Key, pattern.CooldownDays, cooldownFingerprint, priorOpportunities, now)) continue;
candidates.Add(CreateCandidate(manifest, pattern, goal, bundle, evidence, cooldownFingerprint, now));
```

Canonical fingerprint input must be:

```text
patternKey\u001fgoalId(D)\u001fnormalizedGoalType\u001ftrimmedGoalTitle\u001fpriorityInvariant\u001f<sorted-distinct-evidenceId-1>\u001f...
```

Hash UTF-8 bytes with SHA-256 and return lowercase hex. Evidence IDs are sorted with `StringComparer.Ordinal`; do not include bundle fingerprint.

Update `OpportunityGenerationSnapshot.SchemaVersion` to `2` and serialize:

```csharp
cooldownFingerprint = candidate.CooldownFingerprint,
```

For this task, `IsSuppressed` may read an explicit `cooldownFingerprint`; if a same-pattern prior does not yet have one, preserve conservative pattern-level suppression until Task 2 adds precise legacy derivation.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run the same filtered command. Expected: all `OpportunityGenerationTests` pass.

- [ ] **Step 5: Commit**

```bash
git add apps/api/OpportunityGeneration.cs tests/api/OpportunityGenerationTests.cs
git commit -m "fix(vs37): make Today cooldown evidence aware"
```

---

### Task 2: Schema-v1 cooldown identity derivation

**Files:**
- Modify: `tests/api/OpportunityGenerationTests.cs`
- Modify: `apps/api/OpportunityGeneration.cs`

**Interfaces:**
- Consumes: schema-v1 `EvidenceJson` fields `patternKey`, `goal.id`, `goal.type`, `goal.title`, `goal.priority`, and `evidence[].evidenceId`.
- Produces: `OpportunityGenerationSnapshot.TryReadCooldownIdentity(...)` that returns pattern plus a derived/explicit fingerprint when possible and preserves conservative suppression when derivation is impossible.

- [ ] **Step 1: Write failing legacy tests**

Create a helper that serializes a candidate in the historical v1 shape with no `cooldownFingerprint`:

```csharp
private static string LegacySnapshot(GeneratedOpportunityCandidate candidate) => JsonSerializer.Serialize(new
{
    schemaVersion = 1,
    patternKey = candidate.PatternKey,
    goal = new
    {
        id = candidate.GoalId,
        type = candidate.GoalType,
        title = candidate.GoalTitle,
        priority = candidate.GoalPriority
    },
    evidence = candidate.Evidence.Select(x => new { evidenceId = x.EvidenceId }).ToArray()
});
```

Add two real-behavior tests:
- a complete v1 prior with unchanged goal/evidence suppresses the same pattern;
- a complete v1 prior generated from `currentpriorities = "Demand"` does **not** suppress the same pattern when current relevant evidence becomes `"Improve weekday lunch demand"`.

Retain the existing minimal `{ schemaVersion, patternKey }` prior test as the malformed/incomplete conservative-suppression contract.

- [ ] **Step 2: Run focused tests and verify RED**

Run the filtered `OpportunityGenerationTests` command.

Expected: changed relevant evidence remains suppressed when the prior is schema v1 because Task 1 does not yet derive a legacy fingerprint.

- [ ] **Step 3: Implement legacy identity reader**

Add an internal snapshot reader with this behavior:

```csharp
public static bool TryReadCooldownIdentity(
    string? json,
    out string? patternKey,
    out string? cooldownFingerprint)
```

Rules:
- parse `patternKey`; false only when no usable pattern key exists;
- if `cooldownFingerprint` is a non-empty string, return it;
- otherwise require complete `goal` fields and at least the stored evidence array shape, collect non-empty `evidenceId` values, and call the same canonical fingerprint helper used for new candidates;
- if any required legacy identity field is absent/malformed, return the pattern key with `cooldownFingerprint = null` so `IsSuppressed` conservatively suppresses the same pattern;
- never derive from evidence values/source text independently; use stored `evidenceId` values.

Update `IsSuppressed`:

```csharp
if (!OpportunityGenerationSnapshot.TryReadCooldownIdentity(prior.EvidenceJson, out var priorPattern, out var priorFingerprint))
    continue;
if (!string.Equals(priorPattern, patternKey, StringComparison.Ordinal))
    continue;
if (priorFingerprint is null || string.Equals(priorFingerprint, candidateFingerprint, StringComparison.Ordinal))
    return true;
```

- [ ] **Step 4: Run focused tests and verify GREEN**

Expected: all Opportunity generation cooldown contracts pass, including v1 compatibility and conservative malformed fallback.

- [ ] **Step 5: Commit**

```bash
git add apps/api/OpportunityGeneration.cs tests/api/OpportunityGenerationTests.cs
git commit -m "fix(vs37): derive cooldown identity from legacy evidence"
```

---

### Task 3: Regression, review and certification

**Files:**
- Modify only if a real regression is found: `apps/api/**`, `tests/api/**`
- Modify for certification: `delivery/current-slice.json`, `docs/slices/VS-37.md`

**Interfaces:**
- Consumes: final VS-37 runtime commit.
- Produces: frozen runtime SHA, deterministic verification evidence and certified governance head.

- [ ] **Step 1: Run full deterministic gates**

Run/require the repository CI equivalents:

```bash
npm run preflight
dotnet build apps/api/Atlas.Api.csproj --configuration Release
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release
npm run dashboard:build
```

Require clean PostgreSQL migration replay through existing migrations. No new migration should appear.

- [ ] **Step 2: Perform changed-file review**

Confirm:
- fingerprint uses only pattern-resolved evidence and goal context;
- whole bundle fingerprint is not used for suppression identity;
- unchanged evidence is still suppressed;
- legacy malformed records fail closed to conservative suppression;
- no ranking, cooldown-day, policy-only eligibility, owner authority, mobile, connector or database-schema change slipped into scope.

- [ ] **Step 3: Freeze runtime and certify**

Record the final runtime SHA in `delivery/current-slice.json`, set implementation/testing/certification to 100%, add RED/GREEN/full-gate evidence, keep release/production-enable unauthorized, and update `docs/slices/VS-37.md` with final verification.

- [ ] **Step 4: Run post-certification exact-head gates**

Require CI, Security baseline and Product Intake green on the governance-only certification head and verify the diff from frozen runtime contains only tests/docs/governance as documented.

- [ ] **Step 5: Merge under the Product Owner's explicit authorization**

Mark the PR ready and merge with an exact-head SHA guard only after all gates are green. Do not perform production release or EAS/OTA.
