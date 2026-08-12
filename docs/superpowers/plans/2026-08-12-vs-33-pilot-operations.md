# VS-33 Pilot Operations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the minimal FR-18 internal Pilot Operations surface so authorised operators can review pilot quality/safety signals, record support work, assist profile correction, collaboratively prepare evidence-backed Opportunities, and explicitly withdraw unsafe content with full provenance and audit.

**Architecture:** Add a bounded `PilotOperations` vertical module inside the existing ASP.NET Core modular monolith. Persist compact `IntelligenceRunRecord` diagnostics from the current Today generation path and append-only `PilotOperationRecord` interventions; expose only `InternalOperator` endpoints and a root Expo Router `/operator` Stack surface outside the owner tab shell. Reuse the current evidence-aware Opportunity generator and existing Profile/Opportunity entities rather than adding impersonation or a parallel admin domain.

**Tech Stack:** ASP.NET Core / C# / EF Core / PostgreSQL 17 / xUnit, Expo React Native / TypeScript / Expo Router NativeTabs + Stack, Node test runner, GitHub Actions PES gates.

## Global Constraints

- Base exactly on post-VS-32 `main@66e2b7979d68b390d74b395f650b0b6d215e71a8`.
- FR-18 only; this is a minimal internal capability, not a full admin platform.
- DEC-11: internal operators never impersonate Business owners.
- Every operator mutation requires the existing `InternalOperator` policy and records explicit actor, target, provenance/reason and audit.
- Operator-assisted Profile corrections set `Source = "operator-assisted"` and `OwnerConfirmed = false`.
- Opportunity preparation must regenerate from the current evidence-aware candidate pipeline; no free-form recommendation authoring.
- Unsafe feedback never automatically withdraws content; withdrawal is an explicit operator command with required reason and optimistic concurrency.
- `withdrawn` is terminal in VS-33 and cannot be reopened.
- Owner persistent navigation remains exactly Today / History / Goals / Profile; no operator tab.
- No Redis, queue service, warehouse, microservice, external helpdesk/CRM, attachment system or paid infrastructure.
- Do not persist raw provider/model payloads, stack traces, prompts or end-customer data in operational diagnostics.
- No production release, deployment, EAS build/submit/OTA, production enablement or production database mutation.

---

## File map

### Server files

- Create `apps/api/PilotOperations.cs` — operator records, policy/service/read models and endpoints.
- Modify `apps/api/OpportunityFocusService.cs` — append compact generation diagnostics.
- Modify `apps/api/Opportunities.cs` — add terminal `withdrawn` lifecycle and owner-safe presentation/action rules.
- Modify `apps/api/AtlasDomain.cs` — DbSets/model configuration for pilot records and safe Profile source constant.
- Modify `apps/api/Program.cs` — map Pilot Operations endpoints.
- Create `apps/api/Migrations/20260812150000_PilotOperations.cs` — forward-only tables/indexes.

### Server tests

- Create `tests/api/PilotOperationsPolicyTests.cs`.
- Create `tests/api/PilotOperationsPersistenceTests.cs`.
- Create `tests/api/PilotOperationsEndpointWiringTests.cs`.
- Create `tests/api/PilotOperationsOpportunityTests.cs`.
- Modify `tests/api/OpportunityPolicyTests.cs` and/or focused Opportunity tests only where withdrawn owner behavior needs regression coverage.

### Mobile files

- Modify `apps/mobile/src/api/atlas-client.ts` — internal operator contracts/API calls and widened Profile source type.
- Create `apps/mobile/src/features/pilot-operations/pilot-operations-model.ts` — pure presentation helpers.
- Create `apps/mobile/src/features/pilot-operations/PilotOperationsScreen.tsx` — queue.
- Create `apps/mobile/src/features/pilot-operations/PilotBusinessReviewScreen.tsx` — detail/interventions.
- Create `apps/mobile/app/operator.tsx` — root operator Stack route.
- Create `apps/mobile/app/operator/businesses/[businessId].tsx` — Business review route.
- Modify Profile-related mobile typing/presentation only where `operator-assisted` source must render safely.

### Mobile tests

- Create `tests/mobile/vs33-pilot-operations-model.test.mjs`.
- Create `tests/mobile/vs33-pilot-operations-screen.test.mjs`.
- Create `tests/mobile/vs33-owner-boundary.test.mjs`.

---

### Task 1: Persist pilot diagnostics and intervention provenance

**Files:**
- Create: `apps/api/PilotOperations.cs`
- Modify: `apps/api/AtlasDomain.cs`
- Create: `apps/api/Migrations/20260812150000_PilotOperations.cs`
- Test: `tests/api/PilotOperationsPolicyTests.cs`
- Test: `tests/api/PilotOperationsPersistenceTests.cs`

**Interfaces:**
- Produces `IntelligenceRunRecord`, `PilotOperationRecord`, `PilotOperationActions`, `PilotOperationsPolicy`.
- Later tasks consume `AtlasDbContext.IntelligenceRuns` and `AtlasDbContext.PilotOperationRecords`.

- [ ] **Step 1: Write failing policy/persistence tests**

Add tests that require these exact shapes:

```csharp
public sealed class IntelligenceRunRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? ActorUserAccountId { get; set; }
    public required string Outcome { get; set; }
    public string? Code { get; set; }
    public int CandidateCount { get; set; }
    public Guid? OpportunityId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class PilotOperationRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OperatorUserAccountId { get; set; }
    public required string Action { get; set; }
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? Reason { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
```

Require action constants:

```csharp
public static class PilotOperationActions
{
    public const string SupportNote = "support-note";
    public const string ProfileCorrection = "profile-correction";
    public const string OpportunityPrepared = "opportunity-prepared";
    public const string OpportunityWithdrawn = "opportunity-withdrawn";
}
```

Require policy bounds:

```csharp
Assert.True(PilotOperationsPolicy.ValidateSupportNote("Need owner follow-up.").Count == 0);
Assert.Contains(nameof(PilotSupportNoteRequest.Note), PilotOperationsPolicy.ValidateSupportNote(new string('x', 2001)).Keys);
Assert.Contains(nameof(PilotWithdrawRequest.Reason), PilotOperationsPolicy.ValidateWithdrawal(" ").Keys);
```

Persistence tests must assert:
- both tables are Business-scoped;
- indexes exist on `(BusinessId, OccurredAt)`;
- `IntelligenceRunRecord.Outcome` max length 40, `Code` max length 120;
- `PilotOperationRecord.Action` max length 40, `TargetType` max length 40, `Reason` max length 2000;
- `MetadataJson` is `jsonb`;
- Business FKs use restrictive/cascade behavior consistent with retained audit history;
- migration creates both tables and indexes.

- [ ] **Step 2: Run RED**

Run through the PR CI API test step after the clean slice baseline is established.
Expected: focused tests fail because the two records/policies/DbSets/migration do not exist; all previously green API tests continue passing.

- [ ] **Step 3: Implement minimal domain/persistence**

In `AtlasDomain.cs` add:

```csharp
public static class FieldSources
{
    public const string Owner = "owner";
    public const string Public = "public";
    public const string OperatorAssisted = "operator-assisted";
}
```

Do **not** widen `UpsertBusinessProfileRequest.Validate()` to let BusinessOwner submit operator-assisted; that source is internal-command-only.

Add DbSets:

```csharp
public DbSet<IntelligenceRunRecord> IntelligenceRuns => Set<IntelligenceRunRecord>();
public DbSet<PilotOperationRecord> PilotOperationRecords => Set<PilotOperationRecord>();
```

Configure them in `OnModelCreating` with the bounds/indexes above. Create the forward-only migration with no destructive edits to earlier migrations.

In `PilotOperations.cs`, implement static validators with exact bounds:
- support note: trimmed, required, max 2000;
- intervention reason: trimmed, required, max 2000;
- metadata JSON only produced server-side.

- [ ] **Step 4: Run GREEN**

Expected: focused tests pass; clean PostgreSQL 17 replay includes `20260812150000_PilotOperations`.

- [ ] **Step 5: Commit**

```bash
git add apps/api/PilotOperations.cs apps/api/AtlasDomain.cs apps/api/Migrations/20260812150000_PilotOperations.cs tests/api/PilotOperationsPolicyTests.cs tests/api/PilotOperationsPersistenceTests.cs
git commit -m "feat(vs33): add pilot operations records"
```

---

### Task 2: Record safe generation diagnostics

**Files:**
- Modify: `apps/api/OpportunityFocusService.cs`
- Test: `tests/api/PilotOperationsPersistenceTests.cs`

**Interfaces:**
- Produces `OpportunityFocusService.RecordDiagnosticAsync(...)` or equivalent private helper.
- Consumes `AtlasDbContext.IntelligenceRuns` from Task 1.

- [ ] **Step 1: Write failing diagnostic tests**

Cover these exact outcomes:

```csharp
// current valid opportunity
Assert.Equal("ready", diagnostic.Outcome);
Assert.Equal(current.Id, diagnostic.OpportunityId);

// missing profile/goals/pack
Assert.Equal("insufficient-context", diagnostic.Outcome);
Assert.Equal(OpportunityReadinessCodes.ProfileMissing, diagnostic.Code);

// no eligible candidate
Assert.Equal("no-focus", diagnostic.Outcome);
Assert.Equal("opportunity_no_eligible_candidate", diagnostic.Code);

// bundle resolution failure
Assert.Equal("degraded", diagnostic.Outcome);
Assert.NotNull(diagnostic.Code);
```

Require `CandidateCount` to be the number of generated candidates when generation actually ran, otherwise `0`.
Require no diagnostic field that stores exception text, stack trace, provider payload or prompt.

- [ ] **Step 2: Run RED**

Expected: tests fail because `GenerateAsync` does not append diagnostics.

- [ ] **Step 3: Implement minimal instrumentation**

Use a private helper:

```csharp
private static void AddDiagnostic(
    AtlasDbContext db,
    Guid businessId,
    Guid? actorUserAccountId,
    string outcome,
    string? code,
    int candidateCount,
    Guid? opportunityId,
    DateTimeOffset now)
{
    db.IntelligenceRuns.Add(new IntelligenceRunRecord
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        ActorUserAccountId = actorUserAccountId,
        Outcome = outcome,
        Code = code,
        CandidateCount = candidateCount,
        OpportunityId = opportunityId,
        OccurredAt = now
    });
}
```

Before every normal return from `GenerateAsync`, append exactly one diagnostic and save it in the same persistence boundary as any status/opportunity change. Do not turn a diagnostic-write failure into fabricated success.

- [ ] **Step 4: Run GREEN**

Expected: all focused diagnostic tests pass and existing Today generation tests remain green.

- [ ] **Step 5: Commit**

```bash
git add apps/api/OpportunityFocusService.cs tests/api/PilotOperationsPersistenceTests.cs
git commit -m "feat(vs33): record generation diagnostics"
```

---

### Task 3: Internal identity, queue/detail read models, notes and profile assistance

**Files:**
- Modify: `apps/api/PilotOperations.cs`
- Modify: `apps/api/Program.cs`
- Test: `tests/api/PilotOperationsEndpointWiringTests.cs`
- Test: `tests/api/PilotOperationsPersistenceTests.cs`

**Interfaces:**
- Produces `PilotOperationsService.ResolveOperatorAsync`.
- Produces endpoints:
  - `GET /api/v1/pilot-operations/businesses`
  - `GET /api/v1/pilot-operations/businesses/{businessId}`
  - `POST /api/v1/pilot-operations/businesses/{businessId}/notes`
  - `PUT /api/v1/pilot-operations/businesses/{businessId}/profile`
- All endpoint mappings end with `.RequireAuthorization("InternalOperator")`.

- [ ] **Step 1: Write failing authorization/read/write tests**

Require Program wiring:

```csharp
app.MapPilotOperationsEndpoints();
```

Require all operator routes to use `InternalOperator`, never `BusinessOwner`.

Test internal identity resolution:
- provider subject from `NameIdentifier` or `sub`;
- existing internal `UserAccount` reused;
- missing internal `UserAccount` created without Business membership;
- an authenticated owner without operator role cannot invoke operator routes at the authorization layer.

Queue contract:

```csharp
public sealed record PilotBusinessListItem(
    Guid BusinessId,
    string Name,
    string Category,
    string PrimaryLocation,
    bool ProfileConfirmed,
    int GoalCount,
    Guid? CurrentOpportunityId,
    string? CurrentOpportunityTitle,
    string? CurrentOpportunityStatus,
    string? LatestGenerationOutcome,
    string? LatestGenerationCode,
    DateTimeOffset? LatestGenerationAt,
    int UnsafeFeedbackCount,
    int UsefulFeedbackCount,
    int NotUsefulFeedbackCount,
    DateTimeOffset? LatestOperatorActivityAt);
```

Do not add a single synthetic quality score.

Profile assistance request:

```csharp
public sealed record PilotProfileCorrectionRequest(
    string? Description,
    string? Address,
    string? Website,
    string? Phone,
    string? Email,
    string? SocialChannels,
    string? BusinessHours,
    string Language,
    string Reason);
```

Tests must assert a correction:
- sets `profile.Source == FieldSources.OperatorAssisted`;
- sets `profile.OwnerConfirmed == false`;
- records `PilotOperationActions.ProfileCorrection` with changed-field metadata;
- records `AuditRecord`;
- never uses the owner profile endpoint validator to pretend the operator is the owner.

- [ ] **Step 2: Run RED**

Expected: missing service/endpoints/contracts/behavior.

- [ ] **Step 3: Implement queue/detail + notes + profile assistance**

Resolve internal account using the authenticated subject after `InternalOperator` authorization.

Queue ordering must be deterministic and attention-first without fake scoring:
1. Businesses with recent unsafe feedback;
2. latest generation `degraded`;
3. latest generation `no-focus`/`insufficient-context`;
4. remaining Businesses by most recent generation/operator activity, then Business id.

Cap queue to a bounded default such as 50; do not build pagination infrastructure unless required by existing API convention.

Business detail returns recent bounded collections (for example last 20 diagnostics/feedback/operations and recent Opportunities) plus Profile/Goals/Context counts.

Support note creation:

```csharp
var note = new PilotOperationRecord
{
    Id = Guid.NewGuid(),
    BusinessId = businessId,
    OperatorUserAccountId = operatorAccount.Id,
    Action = PilotOperationActions.SupportNote,
    Reason = request.Note.Trim(),
    OccurredAt = now
};
```

Profile correction records changed field names in server-generated JSON metadata, never arbitrary client JSON.

Audit Business detail access using an action such as `pilot-operations.business.viewed` and mutations with stable actions.

- [ ] **Step 4: Run GREEN**

Expected: authorization, queue/detail, note and profile tests pass; existing owner profile tests stay green.

- [ ] **Step 5: Commit**

```bash
git add apps/api/PilotOperations.cs apps/api/Program.cs tests/api/PilotOperationsEndpointWiringTests.cs tests/api/PilotOperationsPersistenceTests.cs
git commit -m "feat(vs33): add operator review and profile assistance"
```

---

### Task 4: Collaboratively prepare evidence-backed Opportunities

**Files:**
- Modify: `apps/api/PilotOperations.cs`
- Test: `tests/api/PilotOperationsOpportunityTests.cs`

**Interfaces:**
- Produces candidate read contract inside Business detail or dedicated endpoint.
- Produces `POST /api/v1/pilot-operations/businesses/{businessId}/opportunities/prepare`.

Use request:

```csharp
public sealed record PilotPrepareOpportunityRequest(string PatternKey, string? SupportNote);
```

- [ ] **Step 1: Write failing preparation tests**

Tests require:
- current Business Profile must be owner-confirmed;
- at least one Goal;
- current Knowledge Pack assignment;
- regenerate `ResolvedKnowledgeBundle` and `OpportunityGenerator.Generate(...)` at command time;
- requested `PatternKey` must match a currently generated candidate;
- candidate must have at least one non-policy factual evidence item;
- if an unexpired `available` Opportunity exists, return conflict and do not replace it;
- persisted Opportunity uses existing `OpportunityGenerationSnapshot.Serialize(candidate)`;
- exact pack/version/goal/evidence retained;
- `PilotOperationActions.OpportunityPrepared` + `AuditRecord` written;
- operator support note is provenance only and is not injected into owner-facing generated evidence.

- [ ] **Step 2: Run RED**

Expected: no preparation command exists.

- [ ] **Step 3: Implement minimal collaborative preparation**

Factor shared candidate-loading logic only if needed to prevent duplicated generator policy. Do not create a free-form title/body endpoint.

Persist the selected candidate using the same field mapping currently used by `OpportunityFocusService`:

```csharp
var opportunity = new Opportunity
{
    Id = Guid.NewGuid(),
    BusinessId = businessId,
    GoalId = candidate.GoalId,
    Title = candidate.Title,
    WhyItMatters = candidate.Reason,
    WhyNow = candidate.WhyNow,
    ExpectedImpact = candidate.ExpectedImpact,
    Effort = candidate.Effort,
    Confidence = candidate.Confidence,
    EvidenceSummary = /* same factual count/goal/pack semantics */,
    EvidenceJson = OpportunityGenerationSnapshot.Serialize(candidate),
    Status = OpportunityStatuses.Available,
    KnowledgePackKey = candidate.KnowledgePackKey,
    KnowledgePackVersion = candidate.KnowledgePackVersion,
    KnowledgePackVersionId = assignment.KnowledgePackVersionId,
    CreatedAt = now,
    ExpiresAt = candidate.ExpiresAt
};
```

Record operator provenance separately in `PilotOperationRecord`, not by rewriting the evidence source as owner-provided.

- [ ] **Step 4: Run GREEN**

Expected: all preparation tests pass plus existing Opportunity generation/detail tests.

- [ ] **Step 5: Commit**

```bash
git add apps/api/PilotOperations.cs tests/api/PilotOperationsOpportunityTests.cs
git commit -m "feat(vs33): prepare assisted Opportunities"
```

---

### Task 5: Add audited terminal withdrawal and owner-safe behavior

**Files:**
- Modify: `apps/api/Opportunities.cs`
- Modify: `apps/api/PilotOperations.cs`
- Test: `tests/api/PilotOperationsOpportunityTests.cs`
- Modify focused existing Opportunity tests as needed.

**Interfaces:**
- Adds `OpportunityStatuses.Withdrawn = "withdrawn"`.
- Produces `POST /api/v1/pilot-operations/businesses/{businessId}/opportunities/{opportunityId}/withdraw`.

Request:

```csharp
public sealed record PilotWithdrawRequest(string Reason, uint Version);
```

- [ ] **Step 1: Write failing withdrawal tests**

Require:
- blank reason => validation error;
- >2000 chars => validation error;
- stale version => `opportunity_stale` conflict;
- cross-Business Opportunity => not found;
- available/applied/non-terminal target may be withdrawn when policy allows;
- already terminal `withdrawn`, expired, skipped/not-relevant/completed-style terminal states cannot be reopened/re-withdrawn;
- sets `Status = withdrawn`;
- does **not** populate `DecidedByUserAccountId` as if owner chose it;
- writes intervention + audit with operator id/reason;
- no automatic path from `FeedbackRecord.Kind == unsafe-guidance` to withdrawal.

Owner regression tests:

```csharp
Assert.False(OpportunityPolicy.CanDecide(withdrawn, now));
Assert.Equal(OpportunityStatuses.Withdrawn, OpportunityPolicy.StatusFor(withdrawn, now));
```

Require Today current-opportunity query to consider only `available`; withdrawn content never returns as current Today Focus.
Require `OpportunityPolicy.Detail` to expose withdrawn status and `ExecutionKitAvailable == false`.

- [ ] **Step 2: Run RED**

Expected: status/endpoint/policy do not exist.

- [ ] **Step 3: Implement terminal withdrawal**

Add:

```csharp
public const string Withdrawn = "withdrawn";
```

Make Execution Kit/actionability policy treat withdrawn as non-actionable through existing status checks; add explicit guard only where existing logic would otherwise permit it.

Use optimistic concurrency against `ConcurrencyVersion`; return the same stable stale-message style as existing Opportunity decisions.

- [ ] **Step 4: Run GREEN**

Expected: withdrawal tests and all existing Opportunity/Execution Kit/History tests pass.

- [ ] **Step 5: Commit**

```bash
git add apps/api/Opportunities.cs apps/api/PilotOperations.cs tests/api/PilotOperationsOpportunityTests.cs tests/api/OpportunityPolicyTests.cs
git commit -m "feat(vs33): withdraw unsafe Opportunities"
```

---

### Task 6: Add mobile operator contracts and pure presentation model

**Files:**
- Modify: `apps/mobile/src/api/atlas-client.ts`
- Create: `apps/mobile/src/features/pilot-operations/pilot-operations-model.ts`
- Modify Profile typing/presentation only as required for `operator-assisted`.
- Test: `tests/mobile/vs33-pilot-operations-model.test.mjs`
- Test: `tests/mobile/vs33-owner-boundary.test.mjs`

**Interfaces:**
- Produces `PilotBusinessListItem`, `PilotBusinessDetail`, `PilotProfileCorrectionInput`, `PilotPrepareOpportunityInput`, `PilotWithdrawInput`.
- Produces API functions:
  - `getPilotBusinesses(accessToken)`
  - `getPilotBusiness(accessToken, businessId)`
  - `addPilotSupportNote(accessToken, businessId, note)`
  - `correctPilotProfile(accessToken, businessId, input)`
  - `preparePilotOpportunity(accessToken, businessId, input)`
  - `withdrawPilotOpportunity(accessToken, businessId, opportunityId, input)`

- [ ] **Step 1: Write failing mobile model/API tests**

Require Profile source type to accept:

```ts
export type ProfileSource = 'owner' | 'public' | 'operator-assisted';
```

but owner save APIs still only accept the owner/public input contract if the current server owner endpoint has that restriction.

Pure model helpers should:
- turn generation outcomes into readable labels (`Needs context`, `No focus`, `Degraded`, `Ready`);
- surface unsafe feedback before degraded/no-focus in deterministic attention ordering;
- never produce a synthetic percentage/quality score;
- format `operator-assisted` as `Operator assisted — owner confirmation required`;
- identify whether withdrawal is available from status/version only, without mutating data.

Owner boundary test must assert `(tabs)/_layout.tsx` still contains exactly Today/History/Goals/Profile and no `operator` trigger.

- [ ] **Step 2: Run RED**

Expected: missing types/functions/model.

- [ ] **Step 3: Implement minimal client/model**

Keep operator API calls in the existing `atlas-client.ts` request helper so authentication/error handling remains consistent.

Do not parse JWT roles client-side to decide authorization; the server remains authority. The operator screen can treat a forbidden response as unavailable.

- [ ] **Step 4: Run GREEN**

Expected: focused model/API tests pass, TypeScript passes, owner navigation tests remain green.

- [ ] **Step 5: Commit**

```bash
git add apps/mobile/src/api/atlas-client.ts apps/mobile/src/features/pilot-operations/pilot-operations-model.ts tests/mobile/vs33-pilot-operations-model.test.mjs tests/mobile/vs33-owner-boundary.test.mjs
git commit -m "feat(vs33): add pilot operations mobile contract"
```

---

### Task 7: Build the minimal internal operator Stack experience

**Files:**
- Create: `apps/mobile/app/operator.tsx`
- Create: `apps/mobile/app/operator/businesses/[businessId].tsx`
- Create: `apps/mobile/src/features/pilot-operations/PilotOperationsScreen.tsx`
- Create: `apps/mobile/src/features/pilot-operations/PilotBusinessReviewScreen.tsx`
- Test: `tests/mobile/vs33-pilot-operations-screen.test.mjs`

**Interfaces:**
- Consumes Task 6 API/model.
- Produces root internal routes only; no owner-tab registration.

- [ ] **Step 1: Write failing route/screen tests**

Queue screen contract:
- header `Pilot operations`;
- concise explanation that it is an internal review surface;
- loading, empty, forbidden/unavailable and recoverable error states;
- Business cards show textual attention labels, not KPI tiles;
- card press pushes `/operator/businesses/{businessId}`;
- uses `AtlasScreen`, shared tokens and accessible >=44-point actions.

Business review contract:
- `Business readiness`;
- `Generation history`;
- `Owner feedback`;
- `Profile assistance`;
- `Support notes`;
- `Prepare Opportunity`;
- `Withdraw Opportunity` only when server/model says eligible;
- reason/note fields preserve text after failure;
- withdrawal requires an explicit confirmation step and non-empty reason;
- copy says owner confirmation is required after operator-assisted Profile changes;
- unsafe reports never display an automatic-withdraw action/state.

Route contract must verify both routes live outside `(tabs)`.

- [ ] **Step 2: Run RED**

Expected: routes/screens do not exist.

- [ ] **Step 3: Implement queue screen**

Use `loadSession()` only for the access token; `businessId` is not required for an internal queue.

On API authorization failure, show safe copy such as:
`Pilot operations is available only to authorised internal operators.`
Do not expose role/provider details.

- [ ] **Step 4: Implement Business review screen**

Use existing Atlas visual primitives and calm section hierarchy. Do not introduce charts or a dashboard grid.

For profile correction, show current fields + reason and make the submit result explicitly say owner confirmation is now required.

For candidate preparation, render server-provided eligible candidate choices with evidence summary; do not allow arbitrary title/body text.

For withdrawal, require reason and confirmation; on stale conflict refresh the Business review rather than retrying blindly.

- [ ] **Step 5: Run GREEN**

Expected: screen tests, TypeScript, Expo lint and existing authentic runtime suites pass.

- [ ] **Step 6: Commit**

```bash
git add apps/mobile/app/operator.tsx apps/mobile/app/operator/businesses/[businessId].tsx apps/mobile/src/features/pilot-operations/PilotOperationsScreen.tsx apps/mobile/src/features/pilot-operations/PilotBusinessReviewScreen.tsx tests/mobile/vs33-pilot-operations-screen.test.mjs
git commit -m "feat(vs33): add pilot operator review surface"
```

---

### Task 8: Full exact-head review, certification and merge

**Files:**
- Modify: `docs/slices/VS-33.md`
- Modify: `delivery/current-slice.json`
- Update `README.md` only if the current operator capability belongs in the repo's current-status section.

**Interfaces:**
- Produces one frozen runtime SHA and one governance-only certification commit.

- [ ] **Step 1: Run changed-file review before freeze**

Confirm changed files are limited to:
- Pilot Operations API/domain/migration;
- Opportunity/Today lifecycle adjustments required for withdrawn;
- internal operator mobile routes/screens/contracts;
- focused tests;
- governance/docs.

Reject scope drift into:
- release workflow/infrastructure;
- payments/uploads;
- owner auth/navigation redesign;
- free-form recommendation authoring;
- external admin/helpdesk systems.

- [ ] **Step 2: Run complete exact-head deterministic gates**

Required evidence:

```bash
npm run preflight
dotnet restore apps/api/Atlas.Api.csproj
dotnet build apps/api/Atlas.Api.csproj --configuration Release --no-restore
dotnet ef database update --project apps/api/Atlas.Api.csproj --context AtlasDbContext
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release
npm run dashboard:build
```

Also require exact-head GitHub:
- CI ✅
- Security baseline ✅
- Product Intake ✅

Do not certify from partial or superseded runs.

- [ ] **Step 3: Review high-risk invariants explicitly**

Verify from tests/diff:
- non-operator users cannot access internal endpoints;
- no operator impersonation through BusinessOwner endpoints;
- operator-assisted Profile source is truthful and owner confirmation becomes false;
- generation diagnostics contain stable operational metadata only;
- collaborative prep uses current evidence-aware generator;
- unsafe feedback alone cannot withdraw;
- withdrawal is explicit, reasoned, versioned, audited and terminal;
- withdrawn content is non-actionable for owners;
- owner tabs remain Today/History/Goals/Profile.

- [ ] **Step 4: Freeze runtime SHA and write PES certification**

Update `delivery/current-slice.json`:
- `lifecycle: "certified"`;
- implementation/testing/certification progress 100;
- certification `commitSha` = exact 40-char runtime SHA;
- evidence includes each RED→GREEN cycle, final test counts, migration replay, Security and Product Intake;
- release remains `not-authorized`;
- decisionIds includes `DEC-11`.

Update `docs/slices/VS-33.md` with the same bounded evidence and safety boundary.

Commit only governance/docs:

```bash
git commit -m "chore(vs33): certify pilot operations"
```

- [ ] **Step 5: Run post-cert exact-head gates**

Require CI/Security/Product Intake green on the governance-only head. The certification must still bind the frozen runtime SHA, not the metadata commit.

- [ ] **Step 6: Merge under standing Product Owner approval**

Mark the PR ready, re-fetch head/mergeability, ensure no unresolved review threads, then merge with an exact-head SHA guard.

Do not deploy after merge.
