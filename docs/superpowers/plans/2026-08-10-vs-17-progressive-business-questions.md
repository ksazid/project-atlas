# VS-17 Progressive Business Questions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a resumable, deterministic, category-aware post-Business onboarding step that asks at most 3–5 useful missing questions, persists owner answers as Business Context, records skipped/answered progress separately, and never blocks the owner from reaching Today.

**Architecture:** The API owns one immutable `progressive-onboarding` catalogue v1, deterministic eligibility/ranking, answer validation, Business isolation, and progress persistence. Answered facts continue to use the existing `BusinessContextEntry`; a new `BusinessQuestionProgress` table stores catalogue/question status without duplicating facts. Expo uses a dedicated one-question-at-a-time route and a small API/model layer; Business creation routes into it, while app resume probes the optional endpoint and falls through to Today if enrichment is unavailable or complete.

**Tech Stack:** ASP.NET Core / C# 13 on .NET 10, EF Core + PostgreSQL, Expo Router / React Native / TypeScript, Node test runner, xUnit, GitHub Actions.

## Global Constraints

- Every question is optional and exposes `Skip for now`.
- Selection is deterministic and versioned; no LLM/model call may generate or rank onboarding questions.
- Return no more than five questions; return fewer than three when fewer than three useful unknowns remain; never create filler.
- Support Restaurant & Cafe, Beauty & Personal Care, Retail, Ecommerce, Home & Local Services, Professional Services, Fitness & Wellness, Hospitality / Accommodation, and Generic Business fallback.
- Answers persist as `BusinessContextEntry` with `Source = owner` and `OwnerConfirmed = true`.
- Skips create progress only; never create empty/fake Business Context.
- A catalogue version change alone must never cause a skipped question to be re-asked. Future re-asking requires a separately implemented material-value trigger and is out of scope here.
- Business must already exist before VS-17 starts; existing VS-16 Business creation/provenance semantics remain unchanged.
- The optional enrichment endpoint must never trap the owner: load failure/offline must allow continuing to `/(tabs)`.
- Primary visual grammar remains ATLAS-DESIGN-001 and the approved Starbucks-derived Atlas system through existing tokens/BrandMark; secondary motion polish cannot override layout or visual authority.
- Approximately 44×44pt minimum enabled targets, semantic headings, non-colour selection state, dynamic-type-safe copy, reduced-motion support, keyboard-safe short text, and no phone/tablet horizontal overflow.
- Combined fast onboarding continues to target roughly 60–90 seconds when URL enrichment succeeds and tap-first answers are used.
- No Knowledge Pack Schema v2, category metric engine, opportunity recipes, generated first Opportunity, private provider connectors, release, deployment, or production enablement.

---

## File Structure

### API/domain

- Create `apps/api/ProgressiveQuestions.cs` — catalogue v1 definitions, deterministic selection, request/response DTOs, service operations, and endpoint mapping.
- Modify `apps/api/AtlasDomain.cs` — add `BusinessQuestionProgress`, DbSet, indexes/constraints, and progress status constants only; keep existing Business Context model authoritative.
- Modify `apps/api/Program.cs` — map progressive-question endpoints using the existing application/authorization pipeline.
- Create `apps/api/Migrations/20260810010000_ProgressiveBusinessQuestions.cs` — PostgreSQL table/index migration for progress.

### API tests

- Create `tests/api/ProgressiveQuestionCatalogueTests.cs` — category coverage, known-context suppression, skip/answer suppression, deterministic ranking, no filler, and validation.
- Create `tests/api/ProgressiveQuestionPersistenceTests.cs` — owner isolation, answer/skip persistence, no fake context on skip, duplicate/idempotent behavior, and stale catalogue handling.

### Mobile

- Create `apps/mobile/src/api/progressive-questions.ts` — typed list/answer/skip API calls.
- Create `apps/mobile/src/features/progressive-questions/progressive-question-model.ts` — answer draft, option toggling, validation, payload building, and progress labels.
- Create `apps/mobile/app/progressive-questions.tsx` — dedicated one-question-at-a-time optional enrichment UI.
- Modify `apps/mobile/app/create-business.tsx` — after successful Business creation/session save, route to `/progressive-questions` rather than directly to tabs.
- Modify `apps/mobile/app/index.tsx` — on existing Business session, probe pending questions; route to `/progressive-questions` only when useful questions exist, otherwise tabs; API failure falls through to tabs.
- Modify `apps/mobile/src/auth/session-routing.ts` — extend pure destination logic to represent an explicit `hasPendingProgressiveQuestions` decision without embedding network work.

### Mobile tests/runtime

- Create `tests/mobile/progressive-question-model.test.mjs` — tap-first draft/model behavior and route-copy invariants.
- Create `tests/mobile/progressive-question-runtime.test.mjs` — authentic Expo Web phone/tablet hero flow: Business session → category-aware question → answer → skip → completion → Today, plus degraded optional bypass.
- Modify `tests/mobile/mvp-integrated-acceptance.test.mjs` — assert the new route is part of the integrated owner journey without weakening existing routes.

### Governance/docs

- Create `docs/slices/VS-17.md` — acceptance criteria/DoD derived from the approved spec.
- Modify `delivery/current-slice.json` — activate VS-17 before production code and later record progress/certification against exact implementation SHA.
- Modify `delivery/decisions.json` — add one VS-17 decision capturing the approved deterministic/versioned/skippable catalogue approach and post-Business sequence.
- Create `docs/evidence/VS-17-RUNTIME-2026-08-10.md` during certification from retained exact-head runtime artifact.

---

### Task 1: Activate VS-17 Governance

**Files:**
- Create: `docs/slices/VS-17.md`
- Modify: `delivery/current-slice.json`
- Modify: `delivery/decisions.json`

**Interfaces:**
- Consumes: approved design `docs/superpowers/specs/2026-08-10-vs-17-progressive-business-questions-design.md`, VS-16 merge `4febc069a796ebcc7cdc871629695ab7631bb71c`.
- Produces: active VS-17 governance boundary with allowed paths `apps/api/**`, `apps/mobile/**`, `tests/api/**`, `tests/mobile/**`, `delivery/**`, `docs/**`; release/production remain not authorized.

- [ ] **Step 1: Write the VS-17 slice acceptance document**

Create `docs/slices/VS-17.md` with acceptance criteria that explicitly require: post-Business optional entry; deterministic catalogue v1; 3–5 max/fewer when useful set is smaller; all eight categories + Generic fallback; known-context suppression; answered Context + separate skipped/answered progress; skip/non-blocking resume; API isolation/idempotence/stale-version safety; one-question tap-first UI; exact-head API/mobile/runtime/migration/Security/Product/CI gates; no release/deployment.

- [ ] **Step 2: Activate `delivery/current-slice.json`**

Use:

```json
{
  "sliceId": "VS-17",
  "title": "VS-17 — Progressive Business Questions",
  "status": "active",
  "lifecycle": "implementing",
  "riskLevel": "medium",
  "implementationMode": "runtime-enabled",
  "requirements": ["FR-03", "FR-05"],
  "dependencies": [
    "ATLAS-PRD-001",
    "ATLAS-TRD-001",
    "ATLAS-DESIGN-001",
    "CATEGORY-INTELLIGENCE-FOUNDATION",
    "VS-16@4febc069a796ebcc7cdc871629695ab7631bb71c"
  ],
  "release": { "status": "not-authorized", "releaseId": null }
}
```

Preserve the schema/owners/protected-path structure used by VS-16, set scope/policy/implementation approvals to the Product Owner's approved design decision, certification pending, and progress `discovery=100`, `decisions=100`, `implementation=0`, `testing=0`, `certification=0`, `release=0`, `validation=0`.

- [ ] **Step 3: Record DEC-05**

Append a decision with question `How should VS-17 collect optional category-aware onboarding context?`, options `AI-generated questions`, `fixed static form`, `deterministic versioned skippable catalogue selected from missing context`, and decision equal to the third option plus post-Business/resumable/tap-first semantics.

- [ ] **Step 4: Run governance validation**

Run:

```bash
npm run planning:validate
npm run governance:validate
npm run dashboard:check
```

Expected: all exit 0.

- [ ] **Step 5: Commit**

```bash
git add docs/slices/VS-17.md delivery/current-slice.json delivery/decisions.json
git commit -m "docs(vs17): activate progressive questions slice"
```

---

### Task 2: Catalogue, Selection, and Progress Domain — TDD

**Files:**
- Create: `apps/api/ProgressiveQuestions.cs`
- Modify: `apps/api/AtlasDomain.cs`
- Create: `tests/api/ProgressiveQuestionCatalogueTests.cs`
- Create: `apps/api/Migrations/20260810010000_ProgressiveBusinessQuestions.cs`

**Interfaces:**
- Produces:
  - `ProgressiveQuestionDefinition(string QuestionKey, string TargetContextKey, IReadOnlySet<string> Categories, int Priority, string Prompt, string? Helper, string AnswerType, IReadOnlyList<string> Options, int? MaxSelections, int? MaxLength, IReadOnlySet<string> MaterialityTags)`
  - `ProgressiveQuestionSetResponse(string CatalogueKey, string CatalogueVersion, IReadOnlyList<ProgressiveQuestionResponse> Questions)`
  - `ProgressiveQuestionCatalogueV1.CatalogueKey = "progressive-onboarding"`
  - `ProgressiveQuestionCatalogueV1.Version = "1"`
  - `ProgressiveQuestionCatalogueV1.Select(string category, IReadOnlyCollection<BusinessContextEntry> context, IReadOnlyCollection<BusinessQuestionProgress> progress)`
  - `BusinessQuestionProgress` persistence entity.
- Consumes: canonical category strings introduced by VS-16 and existing `BusinessContextEntry`.

- [ ] **Step 1: Write failing catalogue tests**

Create tests with exact assertions such as:

```csharp
[Theory]
[InlineData("restaurant-cafe")]
[InlineData("beauty-personal-care")]
[InlineData("retail")]
[InlineData("ecommerce")]
[InlineData("home-local-services")]
[InlineData("professional-services")]
[InlineData("fitness-wellness")]
[InlineData("hospitality-accommodation")]
[InlineData("generic-business")]
public void Catalogue_SelectsAtMostFiveUsefulQuestionsForEveryFamily(string category)
{
    var selected = ProgressiveQuestionCatalogueV1.Select(category, [], []);
    Assert.InRange(selected.Count, 1, 5);
    Assert.Equal(selected.Select(x => x.QuestionKey).Distinct().Count(), selected.Count);
}

[Fact]
public void Selection_SuppressesKnownAndSkippedContextWithoutFiller()
{
    var context = new[] {
        new BusinessContextEntry { Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), Key = "primarychannels", Value = "In person", Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = DateTimeOffset.UtcNow }
    };
    var progress = new[] {
        BusinessQuestionProgress.Skipped(context[0].BusinessId, "progressive-onboarding", "1", "generic.primary-constraint", DateTimeOffset.UtcNow)
    };

    var selected = ProgressiveQuestionCatalogueV1.Select("generic-business", context, progress);

    Assert.DoesNotContain(selected, x => x.TargetContextKey == "primarychannels");
    Assert.DoesNotContain(selected, x => x.QuestionKey == "generic.primary-constraint");
    Assert.True(selected.Count <= 5);
}
```

Also test deterministic ordering/tie-break, generic fallback for unknown category, all returned question keys/options stable, and no catalogue question targets VS-16 canonical required fields such as timezone/currency/name.

- [ ] **Step 2: Run tests to verify RED**

Run:

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter ProgressiveQuestionCatalogueTests
```

Expected: FAIL because catalogue/progress types do not exist.

- [ ] **Step 3: Implement minimal catalogue/domain types**

In `ProgressiveQuestions.cs`, add constants:

```csharp
public static class ProgressiveQuestionAnswerTypes
{
    public const string SingleChoice = "single-choice";
    public const string MultiChoice = "multi-choice";
    public const string ShortText = "short-text";
}

public static class BusinessQuestionProgressStatuses
{
    public const string Answered = "answered";
    public const string Skipped = "skipped";
}
```

Build a small immutable v1 catalogue with a shared generic core (`primarychannels`, `busyperiods`, `constraints`, `customergroups`, `currentpriorities`) plus lightweight category-specific higher-priority definitions/options for all eight categories. Implement `Select` by category applicability, known owner-confirmed context suppression, answered/skipped progress suppression across the `progressive-onboarding` catalogue lineage, deterministic `Priority desc → category specificity desc → QuestionKey asc`, and `Take(5)`.

In `AtlasDomain.cs`, add:

```csharp
public sealed class BusinessQuestionProgress
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public required string CatalogueKey { get; set; }
    public required string CatalogueVersion { get; set; }
    public required string QuestionKey { get; set; }
    public required string Status { get; set; }
    public string? AnsweredContextKey { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public uint ConcurrencyVersion { get; set; }

    public static BusinessQuestionProgress Skipped(Guid businessId, string catalogueKey, string catalogueVersion, string questionKey, DateTimeOffset at) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, CatalogueKey = catalogueKey, CatalogueVersion = catalogueVersion,
        QuestionKey = questionKey, Status = BusinessQuestionProgressStatuses.Skipped, CompletedAt = at
    };
}
```

Add DbSet, row version, max lengths, and unique index `(BusinessId, CatalogueKey, CatalogueVersion, QuestionKey)`.

- [ ] **Step 4: Add PostgreSQL migration**

Create table `BusinessQuestionProgress` with UUID PK, BusinessId UUID, CatalogueKey/Version/QuestionKey/Status text with bounded varchar mappings, nullable AnsweredContextKey, CompletedAt timestamptz, concurrency/version column matching existing Npgsql row-version pattern, and the unique composite index. Add FK to `Businesses(Id)` with restrictive/cascade behavior matching other Business-owned metadata; do not change existing Context tables.

- [ ] **Step 5: Run catalogue tests GREEN**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter ProgressiveQuestionCatalogueTests
```

Expected: PASS.

- [ ] **Step 6: Run migration compile check**

```bash
dotnet build apps/api/Atlas.Api.csproj --configuration Release
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add apps/api/ProgressiveQuestions.cs apps/api/AtlasDomain.cs apps/api/Migrations/20260810010000_ProgressiveBusinessQuestions.cs tests/api/ProgressiveQuestionCatalogueTests.cs
git commit -m "feat(vs17): add progressive question catalogue"
```

---

### Task 3: Business-Isolated List / Answer / Skip API — TDD

**Files:**
- Modify: `apps/api/ProgressiveQuestions.cs`
- Modify: `apps/api/Program.cs`
- Create: `tests/api/ProgressiveQuestionPersistenceTests.cs`

**Interfaces:**
- Produces:
  - `GET /api/v1/businesses/{businessId:guid}/progressive-questions`
  - `POST /api/v1/businesses/{businessId:guid}/progressive-questions/{questionKey}/answer`
  - `POST /api/v1/businesses/{businessId:guid}/progressive-questions/{questionKey}/skip`
  - `ProgressiveQuestionAnswerRequest(string CatalogueVersion, IReadOnlyList<string>? Selections, string? Text)`
  - `ProgressiveQuestionMutationResponse(string Status, string QuestionKey, string CatalogueVersion, ProgressiveQuestionSetResponse Remaining)`.
- Consumes: `OwnerAccount` ownership semantics, `BusinessContextEntry`, catalogue v1 selection, `BusinessQuestionProgress`.

- [ ] **Step 1: Write failing service/persistence tests**

Cover:

```csharp
[Fact]
public async Task Answer_WritesOwnerConfirmedContextAndProgressAtomically()
{
    // seed owner/business
    var result = await ProgressiveQuestionService.AnswerAsync(
        db, owner.ProviderSubject, business.Id, "generic.primary-channel",
        new ProgressiveQuestionAnswerRequest("1", ["In person"], null), CancellationToken.None);

    var context = await db.BusinessContextEntries.SingleAsync(x => x.BusinessId == business.Id && x.Key == "primarychannels");
    Assert.Equal(FieldSources.Owner, context.Source);
    Assert.True(context.OwnerConfirmed);
    Assert.Equal("In person", context.Value);
    Assert.Single(await db.BusinessQuestionProgress.Where(x => x.BusinessId == business.Id && x.Status == "answered").ToListAsync());
}

[Fact]
public async Task Skip_WritesProgressButNeverFakeContext()
{
    await ProgressiveQuestionService.SkipAsync(db, owner.ProviderSubject, business.Id, "generic.primary-constraint", "1", CancellationToken.None);
    Assert.Empty(await db.BusinessContextEntries.Where(x => x.BusinessId == business.Id && x.Key == "constraints").ToListAsync());
    Assert.Single(await db.BusinessQuestionProgress.Where(x => x.BusinessId == business.Id && x.Status == "skipped").ToListAsync());
}
```

Also cover foreign owner → not found; unknown question → stable `progressive_question_not_found`; stale version → `progressive_catalogue_stale`; invalid choice/count/text length → validation error; duplicate same answer/skip returns stable current state rather than duplicate row; skipped v1 remains suppressed when a synthetic later catalogue version is evaluated unless explicit re-ask policy is supplied (which VS-17 does not supply).

- [ ] **Step 2: Run persistence tests RED**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter ProgressiveQuestionPersistenceTests
```

Expected: FAIL because service/endpoints do not exist.

- [ ] **Step 3: Implement service operations**

Add `ProgressiveQuestionService.GetAsync`, `AnswerAsync`, `SkipAsync` with server-owned catalogue lookup. Normalize multi-choice values in catalogue option order and persist as a delimiter-safe JSON string or a stable joined representation chosen once in this task; do not accept client `targetContextKey`.

Validation rules:

```csharp
single-choice: exactly one Selections value, must match an allowed option
multi-choice: 1..MaxSelections allowed options, no duplicates
short-text: non-empty trimmed Text <= MaxLength
```

Use a DB transaction for answer: upsert target Context + upsert progress + audit `business.progressive-question.answered:{questionKey}`. Skip transaction: upsert skipped progress + audit `business.progressive-question.skipped:{questionKey}`. A repeated identical mutation returns current state; changing an already answered question in this onboarding endpoint is rejected unless the question is still eligible, leaving later Context editing to the existing Context surface.

- [ ] **Step 4: Map endpoints**

Expose a `MapProgressiveQuestionEndpoints(this WebApplication app)` extension in `ProgressiveQuestions.cs` and call it from `Program.cs` next to other bounded endpoint maps. Require `BusinessOwner`. Translate service exceptions into safe 404/409/validation responses with stable codes.

- [ ] **Step 5: Run API tests GREEN**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter "ProgressiveQuestionCatalogueTests|ProgressiveQuestionPersistenceTests"
```

Expected: PASS.

- [ ] **Step 6: Run full API test suite**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release
```

Expected: PASS with no VS-16 regression.

- [ ] **Step 7: Commit**

```bash
git add apps/api/ProgressiveQuestions.cs apps/api/Program.cs tests/api/ProgressiveQuestionPersistenceTests.cs
git commit -m "feat(vs17): add progressive question API"
```

---

### Task 4: Mobile API/Model and Post-Business Routing — TDD

**Files:**
- Create: `apps/mobile/src/api/progressive-questions.ts`
- Create: `apps/mobile/src/features/progressive-questions/progressive-question-model.ts`
- Modify: `apps/mobile/src/auth/session-routing.ts`
- Modify: `apps/mobile/app/create-business.tsx`
- Modify: `apps/mobile/app/index.tsx`
- Create: `tests/mobile/progressive-question-model.test.mjs`

**Interfaces:**
- Produces TypeScript types:

```ts
export type ProgressiveQuestion = {
  questionKey: string;
  targetContextKey: string;
  prompt: string;
  helper?: string | null;
  answerType: 'single-choice' | 'multi-choice' | 'short-text';
  options: string[];
  maxSelections?: number | null;
  maxLength?: number | null;
};

export type ProgressiveQuestionSet = {
  catalogueKey: string;
  catalogueVersion: string;
  questions: ProgressiveQuestion[];
};
```

- API functions: `getProgressiveQuestions(token, businessId)`, `answerProgressiveQuestion(token, businessId, questionKey, request)`, `skipProgressiveQuestion(token, businessId, questionKey, catalogueVersion)`.
- Routing function: `getSessionDestination(session, hasPendingProgressiveQuestions?: boolean)` returning `/welcome | /create-business | /progressive-questions | /(tabs)`.

- [ ] **Step 1: Write failing model/routing tests**

Assert single-select replaces selection, multi-select toggles without exceeding max, text trims only for payload not while editing, continue disabled without an answer, progress label e.g. `2 of 4`, zero-question route bypass, and Business+pending route returns `/progressive-questions`.

Example:

```js
test('multi-choice draft respects max selections without mutating input', () => {
  const question = { answerType: 'multi-choice', maxSelections: 2, options: ['A','B','C'] };
  const first = toggleSelection([], 'A', question);
  const second = toggleSelection(first, 'B', question);
  const blocked = toggleSelection(second, 'C', question);
  assert.deepEqual(blocked, ['A', 'B']);
  assert.deepEqual(first, ['A']);
});
```

- [ ] **Step 2: Run mobile model tests RED**

```bash
node --test tests/mobile/progressive-question-model.test.mjs
```

Expected: FAIL because model/API/routing additions do not exist.

- [ ] **Step 3: Implement typed API and pure model**

Use the existing API base/auth error style from `apps/mobile/src/api/business-discovery.ts`; expose no provider/server internals in UI errors. Model must be side-effect free.

- [ ] **Step 4: Update Business creation handoff**

In `create-business.tsx`, replace only the successful post-save destination:

```ts
await saveSession({ ...session, businessId: business.id });
router.replace('/progressive-questions');
```

for both discovery and manual creation because both share the same `submit()` success path. Do not alter VS-16 discovery confirmation semantics.

- [ ] **Step 5: Update resume routing**

`index.tsx` behavior:

```ts
if (!session) => /welcome
if (!session.businessId) => /create-business
if (session.businessId) {
  try {
    const set = await getProgressiveQuestions(session.accessToken, session.businessId);
    destination = getSessionDestination(session, set.questions.length > 0);
  } catch {
    destination = '/(tabs)'; // optional enrichment must not trap owner
  }
}
```

Keep loading state accessible.

- [ ] **Step 6: Run model/type tests GREEN**

```bash
node --test tests/mobile/progressive-question-model.test.mjs
npm run mobile:typecheck
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add apps/mobile/src/api/progressive-questions.ts apps/mobile/src/features/progressive-questions/progressive-question-model.ts apps/mobile/src/auth/session-routing.ts apps/mobile/app/create-business.tsx apps/mobile/app/index.tsx tests/mobile/progressive-question-model.test.mjs
git commit -m "feat(vs17): add progressive question handoff"
```

---

### Task 5: One-Question-at-a-Time Atlas UI — TDD

**Files:**
- Create: `apps/mobile/app/progressive-questions.tsx`
- Modify: `tests/mobile/progressive-question-model.test.mjs`

**Interfaces:**
- Consumes: `ProgressiveQuestionSet`, answer/skip API functions, pure model helpers, existing `loadSession`, `BrandMark`, `tokens`.
- Produces: `/progressive-questions` route with loading, question, saving, error/retry, optional bypass, completion states.

- [ ] **Step 1: Add source-level failing UI invariants**

Extend test to assert the route source contains `Skip for now`, progress semantics, `accessibilityRole="header"`, `accessibilityState` for selected controls, reduced-motion handling if transition animation is used, and no Starbucks screen-level URL/demo labels.

- [ ] **Step 2: Run test RED**

```bash
node --test tests/mobile/progressive-question-model.test.mjs
```

Expected: FAIL because screen does not exist.

- [ ] **Step 3: Implement the route**

Required behavior:

- load session/business; missing session uses existing guard destination;
- GET current eligible set;
- zero questions → `router.replace('/(tabs)')`;
- display one server-returned question at a time with eyebrow `A LITTLE MORE CONTEXT`, semantic heading, `Question X of Y`, helper, tap-first controls, primary Continue for multi/text and optionally single, secondary `Skip for now` always visible;
- answer/skip waits for server confirmation before advancing;
- save failure preserves current draft and shows safe retry copy;
- load failure offers `Try again` and `Continue for now`;
- completion copy `That’s enough to get started.` then Continue to Today;
- all buttons/choice cards minHeight >=44; selected state uses check/text/border plus accessibility state, never colour alone;
- use restrained press feedback and no decorative motion requirement; if any animation exists, query `AccessibilityInfo.isReduceMotionEnabled()` and bypass it.

- [ ] **Step 4: Run type/lint/model tests GREEN**

```bash
npm run mobile:typecheck
npm run mobile:lint
node --test tests/mobile/progressive-question-model.test.mjs
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add apps/mobile/app/progressive-questions.tsx tests/mobile/progressive-question-model.test.mjs
git commit -m "feat(vs17): add progressive question experience"
```

---

### Task 6: Authentic Expo Web Runtime and Regression Coverage

**Files:**
- Create: `tests/mobile/progressive-question-runtime.test.mjs`
- Modify: `tests/mobile/mvp-integrated-acceptance.test.mjs`
- Modify: `package.json` only if a dedicated script is required; prefer existing `tests/mobile/*.test.mjs` discovery.

**Interfaces:**
- Consumes: real exported Expo app, temporary `session.web.ts` runtime shim pattern, local HTTP fixture.
- Produces: retained phone/tablet screenshots and machine-readable runtime summary under `dashboard/runtime-vs17/` in CI artifact.

- [ ] **Step 1: Write runtime test using existing authentic fixture pattern**

Fixture endpoints:

```text
GET  /api/v1/businesses/dev-business/progressive-questions
POST /api/v1/businesses/dev-business/progressive-questions/generic.primary-channel/answer
POST /api/v1/businesses/dev-business/progressive-questions/generic.primary-constraint/skip
```

First GET returns two questions for runtime speed while proving one answer + one skip. After mutations, GET/response `Remaining` becomes empty. Record Authorization header and mutation bodies.

- [ ] **Step 2: Runtime assertions**

At 390×844:

1. seed `runtime-token` + `dev-business`;
2. navigate `/progressive-questions`;
3. verify loading then first question, progress `1 of 2`, category/tap choice controls, >=44px targets, no horizontal overflow;
4. choose an option and Continue; assert answer POST body contains catalogue version and allowed selection only;
5. second question shows `2 of 2`; press `Skip for now`; assert skip POST and no answer body/context fabrication in fixture;
6. verify completion copy and Continue to Today;
7. capture question/selected/completion screenshots;
8. simulate load error on a fresh page and verify `Continue for now` routes to tabs without mutation.

At 768×1024 verify no horizontal overflow and >=44px enabled targets. Use `expo export --clear` for this runtime as well so runtime-specific `EXPO_PUBLIC_API_URL` cannot leak across VS-16/VS-15/VS-17 fixtures.

- [ ] **Step 3: Update integrated acceptance**

Assert `/progressive-questions` is an implemented owner route and that VS-17 does not remove existing Today/Profile/Goals/Context routes or authorize release.

- [ ] **Step 4: Run mobile suite locally/deterministically**

```bash
npm run mobile:test
npm run mobile:typecheck
npm run mobile:lint
```

Expected: model/source tests PASS; authentic runtime is skipped outside GitHub Actions by the same `CI && GITHUB_ACTIONS` guard used by existing runtime tests.

- [ ] **Step 5: Commit**

```bash
git add tests/mobile/progressive-question-runtime.test.mjs tests/mobile/mvp-integrated-acceptance.test.mjs
git commit -m "test(vs17): add progressive question runtime coverage"
```

---

### Task 7: Full Exact-Head Verification, Review, Certification, and Merge

**Files:**
- Modify: `delivery/current-slice.json`
- Modify: `docs/slices/VS-17.md`
- Create: `docs/evidence/VS-17-RUNTIME-2026-08-10.md`
- PR metadata only; no release/deployment files.

**Interfaces:**
- Consumes: final implementation SHA, GitHub Actions CI/Security/Product runs, retained runtime artifact.
- Produces: certified VS-17 PR merged to `main` only after all exact-head gates and post-merge main CI.

- [ ] **Step 1: Fresh local/full deterministic verification**

Run:

```bash
npm run preflight
dotnet restore apps/api/Atlas.Api.csproj
dotnet build apps/api/Atlas.Api.csproj --configuration Release --no-restore
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release
```

With PostgreSQL available, also run the same clean migration application command used by CI:

```bash
dotnet ef database update --project apps/api/Atlas.Api.csproj --context AtlasDbContext
```

Expected: all exit 0.

- [ ] **Step 2: Structured code review against `main`**

Review for: duplicate fact storage; category-specific logic leaking into future Knowledge Pack concerns; stale/foreign Business access; arbitrary client question/context keys; skip creating fake data; catalogue-version-only re-ask; optional flow trapping owner; accessibility target/selection semantics; runtime fixture weakening existing tests. Resolve every critical/important finding before certification.

- [ ] **Step 3: Open/update PR as draft during verification**

PR title: `VS-17: Progressive Business Questions`.

Body must list exact scope, explicit exclusions, design/plan paths, current verification state, and `No release/deployment/production enablement`.

- [ ] **Step 4: Verify exact implementation head workflows**

Require on the same implementation SHA:

- CI — complete success, including authentic VS-17 Expo Web runtime plus existing runtime regressions;
- Security baseline — success;
- Product Intake — success;
- clean PostgreSQL migration step — success;
- full API tests — success.

Do not certify from a previous SHA.

- [ ] **Step 5: Retain runtime evidence**

Download the exact-head `pes-dashboard` artifact. Create `docs/evidence/VS-17-RUNTIME-2026-08-10.md` recording implementation SHA, run IDs, artifact ID/digest, runtime summary assertions, phone/tablet screenshot hashes, degraded optional-bypass evidence, and native-device limitations. Do not claim iOS physical-device/VoiceOver validation unless actually executed.

- [ ] **Step 6: Record certification governance**

Update `delivery/current-slice.json`:

```json
{
  "lifecycle": "certified",
  "progress": {
    "discovery": 100,
    "decisions": 100,
    "implementation": 100,
    "testing": 100,
    "certification": 100,
    "release": 0,
    "validation": 0
  },
  "certification": {
    "status": "passed",
    "commitSha": "<EXACT_IMPLEMENTATION_SHA>"
  },
  "release": { "status": "not-authorized", "releaseId": null }
}
```

Add exact CI/Security/Product/runtime evidence URLs and PR link. Update `docs/slices/VS-17.md` certification section.

- [ ] **Step 7: Commit governance-only certification and verify new head**

```bash
git add docs/evidence/VS-17-RUNTIME-2026-08-10.md docs/slices/VS-17.md delivery/current-slice.json
git commit -m "docs(vs17): record certification evidence"
```

Because this changes the PR head, require fresh CI + Security + Product Intake success on this governance head before merge.

- [ ] **Step 8: Mark PR ready and merge with expected-head guard**

Only after no unresolved review threads/findings and all final-head gates pass. Merge method should match the established project convention; use expected head SHA to reject races.

- [ ] **Step 9: Verify post-merge `main` CI**

Require `main` push CI success on the merge SHA. Only then declare VS-17 complete.

- [ ] **Step 10: Do not deploy**

No release workflow, production enablement, hosting deploy, EAS publish, OTA update, or infrastructure change is part of VS-17.

---

## Plan Self-Review

- **Spec coverage:** Every approved design decision maps to Tasks 2–6; governance/certification maps to Tasks 1 and 7. All eight categories + Generic fallback, skip semantics, resume, post-Business sequencing, deterministic selection, separate progress, optional bypass, accessibility, runtime evidence, and no-deploy boundary are explicitly covered.
- **Placeholder scan:** The only `<EXACT_IMPLEMENTATION_SHA>` token appears in the certification JSON example because that value cannot exist until implementation completes; Task 7 explicitly instructs replacement with the observed tested SHA before commit. No implementation task contains TBD/TODO/"similar to" placeholders.
- **Type consistency:** `CatalogueVersion` is a string end-to-end; `QuestionKey` and `TargetContextKey` are server-owned; answers use `Selections` for choice types and `Text` for short text; `BusinessQuestionProgress` never duplicates answer values.
- **Scope check:** VS-17 remains one bounded subsystem: optional onboarding context acquisition. Knowledge Pack v2 and Opportunity generation are explicitly excluded.
