# VS-32 Feedback, Safety Reporting & Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete FR-17 by adding Business-scoped append-only feedback persistence and a mobile owner flow for Opportunity ratings, incorrect-context reports, unsafe-guidance reports, general feedback, and support requests.

**Architecture:** Add one focused `Feedback.cs` vertical-slice module to the existing ASP.NET Core modular monolith, extend `AtlasDbContext` with a `FeedbackRecord` entity, and add one forward-only PostgreSQL migration. The mobile app extends the shared Atlas API client, adds a pure feedback model plus one pushed `/feedback` screen, and links it from Opportunity Detail, Context, and Settings without changing the VS-31 four-tab shell.

**Tech Stack:** .NET 10 minimal APIs, EF Core/Npgsql, xUnit, PostgreSQL 17, Expo SDK 54, React Native, Expo Router, TypeScript, Node test runner.

## Global Constraints

- Implement only `FR-17` and `FR-16` behavior approved for VS-32.
- Preserve Business-owner isolation and safe not-found behavior for cross-Business Opportunity references.
- Feedback is append-only in VS-32; do not add edit/delete/triage APIs.
- `unsafe-guidance` records a report only; it must not withdraw, alter, suppress, expire, or otherwise mutate an Opportunity.
- Feedback must never overwrite Business Profile or Context.
- Owner message is optional, trimmed, text-only, maximum 1200 characters; no attachments.
- UI privacy copy must tell owners not to include customer names, contact details, or other end-customer personal data.
- Do not add an external helpdesk, CRM, email/chat transport, queue, Redis dependency, automated moderation, or analytics dashboard.
- Preserve VS-31 persistent native tabs exactly: Today, History, Goals, Profile.
- No production release, deployment, EAS build/submit/OTA, production enablement, or production database mutation.

---

### Task 1: Feedback domain contract and validation

**Files:**
- Create: `apps/api/Feedback.cs`
- Create: `tests/api/FeedbackPolicyTests.cs`

**Interfaces:**
- Consumes: existing `Opportunity`, `UserAccount`, `AtlasDbContext`, and Business-owner endpoint conventions.
- Produces: `FeedbackKinds`, `FeedbackUsefulnessValues`, `FeedbackRecord`, `SubmitFeedbackRequest`, `FeedbackReceipt`, `FeedbackPolicy.Validate(SubmitFeedbackRequest)`.

- [ ] **Step 1: Write the failing policy tests**

Create `tests/api/FeedbackPolicyTests.cs` with tests equivalent to:

```csharp
using Atlas.Api;
using Xunit;

public sealed class FeedbackPolicyTests
{
    [Theory]
    [InlineData("opportunity-rating", true, null, "useful", true)]
    [InlineData("opportunity-rating", false, null, "useful", false)]
    [InlineData("opportunity-rating", true, null, null, false)]
    [InlineData("unsafe-guidance", true, null, null, true)]
    [InlineData("unsafe-guidance", false, null, null, false)]
    [InlineData("incorrect-context", false, "primarycustomers", null, true)]
    [InlineData("general-feedback", false, null, null, true)]
    [InlineData("support-request", false, null, null, true)]
    public void Validation_matches_kind_contract(string kind, bool hasOpportunity, string? contextKey, string? usefulness, bool valid)
    {
        var request = new SubmitFeedbackRequest(kind, hasOpportunity ? Guid.NewGuid() : null, contextKey, usefulness, " owner note ");
        Assert.Equal(valid, FeedbackPolicy.Validate(request).Count == 0);
    }

    [Fact]
    public void Validation_rejects_unknown_kind_and_oversized_fields()
    {
        var request = new SubmitFeedbackRequest("other", null, new string('x', 121), null, new string('x', 1201));
        Assert.NotEmpty(FeedbackPolicy.Validate(request));
    }

    [Fact]
    public void Normalize_trims_optional_text_and_whitespace_becomes_null()
    {
        Assert.Equal("note", FeedbackPolicy.NormalizeMessage("  note  "));
        Assert.Null(FeedbackPolicy.NormalizeMessage("   "));
    }
}
```

- [ ] **Step 2: Run the focused tests and verify RED**

Run:

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter FeedbackPolicyTests
```

Expected: FAIL because the feedback types/policy do not exist.

- [ ] **Step 3: Add the minimal domain contract**

Create `apps/api/Feedback.cs` with exact public contracts:

```csharp
public static class FeedbackKinds
{
    public const string OpportunityRating = "opportunity-rating";
    public const string IncorrectContext = "incorrect-context";
    public const string UnsafeGuidance = "unsafe-guidance";
    public const string GeneralFeedback = "general-feedback";
    public const string SupportRequest = "support-request";
}

public static class FeedbackUsefulnessValues
{
    public const string Useful = "useful";
    public const string NotUseful = "not-useful";
}

public sealed record SubmitFeedbackRequest(
    string Kind,
    Guid? OpportunityId,
    string? ContextKey,
    string? Usefulness,
    string? Message);

public sealed record FeedbackReceipt(Guid Id, string Kind, DateTimeOffset CreatedAt);

public sealed class FeedbackRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid SubmittedByAccountId { get; set; }
    public required string Kind { get; set; }
    public Guid? OpportunityId { get; set; }
    public string? ContextKey { get; set; }
    public string? Usefulness { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

Implement `FeedbackPolicy.Validate` so:
- allowed kinds are exactly the five constants;
- rating requires Opportunity + usefulness and rejects context key;
- unsafe requires Opportunity and rejects usefulness/context key;
- incorrect-context rejects usefulness and allows optional context key;
- general/support reject usefulness/context key;
- usefulness is exactly `useful` or `not-useful` when required;
- context key max 120;
- message max 1200;
- `NormalizeMessage` trims and converts whitespace-only to null.

- [ ] **Step 4: Run the policy tests and verify GREEN**

Run the same focused command. Expected: PASS.

- [ ] **Step 5: Commit the independently testable domain policy**

```bash
git add apps/api/Feedback.cs tests/api/FeedbackPolicyTests.cs
git commit -m "feat(vs32): define feedback policy"
```

---

### Task 2: Business-scoped persistence, endpoint, and migration

**Files:**
- Modify: `apps/api/AtlasDomain.cs`
- Modify: `apps/api/Program.cs`
- Modify: `apps/api/Feedback.cs`
- Create: `apps/api/Migrations/20260812113000_FeedbackSupport.cs`
- Create: `tests/api/FeedbackPersistenceTests.cs`
- Create: `tests/api/FeedbackEndpointWiringTests.cs`

**Interfaces:**
- Consumes: `FeedbackPolicy`, `FeedbackRecord`, `SubmitFeedbackRequest`, `FeedbackReceipt`, existing Business membership and Opportunity records.
- Produces: `AtlasDbContext.FeedbackRecords`, `FeedbackEndpoints.MapFeedbackEndpoints(WebApplication)`, and `POST /api/v1/businesses/{businessId}/feedback`.

- [ ] **Step 1: Write failing persistence and wiring tests**

`tests/api/FeedbackPersistenceTests.cs` must cover:

```csharp
[Fact]
public async Task Submit_persists_business_account_kind_and_trimmed_note() { /* seed owner/business; call FeedbackService.SubmitAsync; assert one record */ }

[Fact]
public async Task Opportunity_reference_must_belong_to_the_same_business() { /* other-business opportunity returns null/not-found result and writes nothing */ }

[Fact]
public async Task Unsafe_report_does_not_mutate_opportunity_state_or_version() { /* snapshot before; submit; assert unchanged */ }

[Fact]
public async Task Multiple_submissions_are_append_only() { /* submit twice; assert two rows */ }
```

`tests/api/FeedbackEndpointWiringTests.cs` must read source or use the existing endpoint-test pattern to assert:

```csharp
Assert.Contains("MapFeedbackEndpoints", programSource);
Assert.Contains("/api/v1/businesses/{businessId:guid}/feedback", feedbackSource);
Assert.Contains("RequireAuthorization(\"BusinessOwner\")", feedbackSource);
```

- [ ] **Step 2: Run focused tests and verify RED**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter "FeedbackPersistenceTests|FeedbackEndpointWiringTests"
```

Expected: FAIL because persistence/service/endpoint wiring is absent.

- [ ] **Step 3: Extend AtlasDbContext**

In `apps/api/AtlasDomain.cs` add:

```csharp
public DbSet<FeedbackRecord> FeedbackRecords => Set<FeedbackRecord>();
```

Configure:

```csharp
modelBuilder.Entity<FeedbackRecord>().Property(x => x.Kind).HasMaxLength(40);
modelBuilder.Entity<FeedbackRecord>().Property(x => x.ContextKey).HasMaxLength(120);
modelBuilder.Entity<FeedbackRecord>().Property(x => x.Usefulness).HasMaxLength(20);
modelBuilder.Entity<FeedbackRecord>().Property(x => x.Message).HasMaxLength(1200);
modelBuilder.Entity<FeedbackRecord>().HasIndex(x => new { x.BusinessId, x.CreatedAt });
modelBuilder.Entity<FeedbackRecord>().HasIndex(x => x.OpportunityId);
modelBuilder.Entity<FeedbackRecord>().HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
modelBuilder.Entity<FeedbackRecord>().HasOne<Opportunity>().WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Restrict);
modelBuilder.Entity<FeedbackRecord>().HasOne<UserAccount>().WithMany().HasForeignKey(x => x.SubmittedByAccountId).OnDelete(DeleteBehavior.Restrict);
```

- [ ] **Step 4: Implement the service and endpoint**

In `Feedback.cs` add a focused service:

```csharp
public static async Task<FeedbackReceipt?> SubmitAsync(
    AtlasDbContext db,
    Guid businessId,
    UserAccount account,
    SubmitFeedbackRequest request,
    CancellationToken ct)
```

The service validates first. If `OpportunityId` is supplied, resolve only:

```csharp
await db.Opportunities.SingleOrDefaultAsync(
    x => x.Id == request.OpportunityId && x.BusinessId == businessId, ct);
```

If missing, return null without writing. Otherwise create a new `FeedbackRecord`, add an audit record such as `feedback.submitted:{kind}`, save, and return `FeedbackReceipt`.

Expose:

```csharp
public static void MapFeedbackEndpoints(this WebApplication app)
```

Route:

```csharp
app.MapPost("/api/v1/businesses/{businessId:guid}/feedback", async (...) => { ... })
   .RequireAuthorization("BusinessOwner");
```

The endpoint must derive the account through the same Business-owner membership semantics as existing APIs, return `404` when membership/opportunity resolution fails, return `ValidationProblem` with code `feedback_invalid` for policy violations, and `201 Created` for success.

If `Program.cs` private `OwnerAccount` cannot be reused from the extension module without widening unrelated architecture, keep the endpoint mapping in `Feedback.cs` but perform the same scoped membership query directly there. Do not create a generic repository or auth abstraction for this slice.

Add `app.MapFeedbackEndpoints();` in `Program.cs`.

- [ ] **Step 5: Add the forward-only migration**

Create `apps/api/Migrations/20260812113000_FeedbackSupport.cs` matching existing migration style. `Up` creates `FeedbackRecords` with bounded varchar columns, FK constraints and indexes. `Down` drops only `FeedbackRecords`.

- [ ] **Step 6: Run focused tests and clean PostgreSQL migration replay**

```bash
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release --filter "FeedbackPolicyTests|FeedbackPersistenceTests|FeedbackEndpointWiringTests"
```

Expected: PASS.

Then against a clean PostgreSQL 17 database use the repository CI command:

```bash
dotnet ef database update --project apps/api/Atlas.Api.csproj --context AtlasDbContext
```

Expected: all historical migrations plus `20260812113000_FeedbackSupport` apply successfully.

- [ ] **Step 7: Commit persistence and API**

```bash
git add apps/api/AtlasDomain.cs apps/api/Program.cs apps/api/Feedback.cs apps/api/Migrations/20260812113000_FeedbackSupport.cs tests/api/FeedbackPersistenceTests.cs tests/api/FeedbackEndpointWiringTests.cs
git commit -m "feat(vs32): persist owner feedback"
```

---

### Task 3: Mobile API contract and pure feedback model

**Files:**
- Modify: `apps/mobile/src/api/atlas-client.ts`
- Create: `apps/mobile/src/features/feedback/feedback-model.ts`
- Create: `tests/mobile/vs32-feedback-model.test.mjs`

**Interfaces:**
- Produces TypeScript types `FeedbackKind`, `FeedbackUsefulness`, `FeedbackInput`, `FeedbackReceipt`; API function `submitFeedback`; pure model helpers `feedbackChoices`, `getFeedbackCopy`, `normalizeFeedbackMessage`, `validateFeedbackDraft`, `buildFeedbackInput`.

- [ ] **Step 1: Write failing model/client tests**

Create tests that assert:

```js
assert.deepEqual(feedbackChoices.map(x => x.kind), [
  'incorrect-context', 'unsafe-guidance', 'general-feedback', 'support-request'
]);
assert.equal(normalizeFeedbackMessage('  hello  '), 'hello');
assert.equal(normalizeFeedbackMessage('   '), undefined);
assert.equal(validateFeedbackDraft({ kind: 'general-feedback', message: 'x'.repeat(1201) }).valid, false);
assert.deepEqual(buildFeedbackInput({ kind: 'unsafe-guidance', opportunityId: 'opp-1', message: ' note ' }), {
  kind: 'unsafe-guidance', opportunityId: 'opp-1', message: 'note'
});
```

Source-contract test must assert `atlas-client.ts` POSTs to `/feedback` and carries the five-kind union.

- [ ] **Step 2: Run the mobile focused test and verify RED**

```bash
node --test tests/mobile/vs32-feedback-model.test.mjs
```

Expected: FAIL because model/client contract is absent.

- [ ] **Step 3: Implement the minimal client/model**

Add to `atlas-client.ts`:

```ts
export type FeedbackKind = 'opportunity-rating' | 'incorrect-context' | 'unsafe-guidance' | 'general-feedback' | 'support-request';
export type FeedbackUsefulness = 'useful' | 'not-useful';
export type FeedbackInput = { kind: FeedbackKind; opportunityId?: string; contextKey?: string; usefulness?: FeedbackUsefulness; message?: string };
export type FeedbackReceipt = { id: string; kind: FeedbackKind; createdAt: string };
export function submitFeedback(accessToken: string, businessId: string, input: FeedbackInput): Promise<FeedbackReceipt> {
  return request(`/api/v1/businesses/${businessId}/feedback`, accessToken, { method: 'POST', body: JSON.stringify(input) });
}
```

Implement the pure model with the exact four screen-selectable choices, 1200-character validation, tailored copy, and normalized payload building. `opportunity-rating` is not a general screen choice; it is submitted directly from Opportunity Detail.

- [ ] **Step 4: Verify GREEN**

Run the same Node test. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add apps/mobile/src/api/atlas-client.ts apps/mobile/src/features/feedback/feedback-model.ts tests/mobile/vs32-feedback-model.test.mjs
git commit -m "feat(vs32): add mobile feedback contract"
```

---

### Task 4: Reusable Feedback & Support pushed-detail screen

**Files:**
- Create: `apps/mobile/app/feedback.tsx`
- Create: `apps/mobile/src/features/feedback/FeedbackScreen.tsx`
- Create: `tests/mobile/vs32-feedback-screen.test.mjs`

**Interfaces:**
- Consumes: `useLocalSearchParams`, `loadSession`, `submitFeedback`, pure feedback model.
- Produces: `/feedback` root Stack route with optional query params `kind` and `opportunityId`.

- [ ] **Step 1: Write failing screen contract tests**

Tests must assert source contains:
- `Stack.Screen` with `headerShown: true` and accessible Back-to-Profile fallback;
- `useLocalSearchParams` for `kind` and `opportunityId`;
- four selectable choices when no kind is preselected;
- optional multiline note with `maxLength={1200}`;
- privacy helper containing “customer names” and “contact details”;
- `submitFeedback` call;
- busy/disabled state;
- retryable error copy that does not claim success;
- unsafe success copy containing “recorded” / “review” but no claim of immediate removal;
- no attachment/upload component.

- [ ] **Step 2: Run and verify RED**

```bash
node --test tests/mobile/vs32-feedback-screen.test.mjs
```

Expected: FAIL because the route/screen do not exist.

- [ ] **Step 3: Implement pushed route and screen**

`apps/mobile/app/feedback.tsx` follows the VS-31 Context/Settings Stack pattern. It shows a native header titled `Feedback & support` and a 44pt accessible fallback button returning to Profile when the route has no back history.

`FeedbackScreen.tsx`:
- resolves the preselected kind only if it is one of the four report/support choices;
- keeps an optional Opportunity ID from the route;
- keeps draft message on submit failure;
- blocks duplicate submit while `submitting`;
- obtains current session Business + token;
- calls `submitFeedback` with `buildFeedbackInput`;
- handles missing Business with existing recovery routing;
- after success renders receipt confirmation and a clear return action;
- uses `AtlasScreen`, Atlas tokens, `AtlasPressable`, accessible heading and live-region feedback.

- [ ] **Step 4: Run screen and model tests**

```bash
node --test tests/mobile/vs32-feedback-model.test.mjs tests/mobile/vs32-feedback-screen.test.mjs
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add apps/mobile/app/feedback.tsx apps/mobile/src/features/feedback/FeedbackScreen.tsx tests/mobile/vs32-feedback-screen.test.mjs
git commit -m "feat(vs32): add feedback support screen"
```

---

### Task 5: Opportunity, Context, and Settings entry points

**Files:**
- Modify: `apps/mobile/src/features/opportunity-detail/OpportunityDetailScreen.tsx`
- Modify: `apps/mobile/src/features/context/ContextScreen.tsx`
- Modify: `apps/mobile/src/features/settings/SettingsScreen.tsx`
- Create: `tests/mobile/vs32-feedback-entrypoints.test.mjs`

**Interfaces:**
- Opportunity direct rating uses `submitFeedback(..., { kind: 'opportunity-rating', opportunityId, usefulness })`.
- Unsafe report deep link: `/feedback?kind=unsafe-guidance&opportunityId=<opportunityId>`.
- Context report deep link: `/feedback?kind=incorrect-context`.
- Settings link: `/feedback`.

- [ ] **Step 1: Write failing entry-point tests**

Assert:

```js
assert.match(opportunitySource, /Was this Opportunity useful\?/);
assert.match(opportunitySource, />Useful</);
assert.match(opportunitySource, />Not useful</);
assert.match(opportunitySource, /kind:\s*'opportunity-rating'/);
assert.match(opportunitySource, /kind=unsafe-guidance/);
assert.match(contextSource, /Report incorrect context/);
assert.match(contextSource, /kind=incorrect-context/);
assert.match(settingsSource, /Feedback & support/);
assert.match(settingsSource, /router\.push\('\/feedback'\)/);
assert.doesNotMatch(tabLayoutSource, /name="feedback"/);
```

Also assert rating submission does not call existing Action decision mutation functions.

- [ ] **Step 2: Run and verify RED**

```bash
node --test tests/mobile/vs32-feedback-entrypoints.test.mjs
```

Expected: FAIL only for missing FR-17 entry points.

- [ ] **Step 3: Implement Opportunity rating and unsafe-report entry**

Add local state for `ratingState: 'idle' | 'submitting' | 'success' | 'error'` and selected usefulness. On button tap:
1. prevent overlap;
2. reload session;
3. require Business;
4. call `submitFeedback` with `opportunity-rating` + Opportunity ID + usefulness;
5. show provider-neutral success/error message.

The section remains independent of `ActionDecisionPanel` and `OutcomeCapturePanel` and does not alter Opportunity status.

Add `Report unsafe guidance` linking to the preselected Feedback screen.

- [ ] **Step 4: Implement Context and Settings entry points**

Context: add a secondary `AtlasPressable` near the final actions with `Report incorrect context`; explain owner can edit the context directly and report a problem separately.

Settings: add a normal card `Feedback & support` routed to `/feedback`.

- [ ] **Step 5: Run all VS-32 mobile tests**

```bash
node --test tests/mobile/vs32-feedback-model.test.mjs tests/mobile/vs32-feedback-screen.test.mjs tests/mobile/vs32-feedback-entrypoints.test.mjs
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add apps/mobile/src/features/opportunity-detail/OpportunityDetailScreen.tsx apps/mobile/src/features/context/ContextScreen.tsx apps/mobile/src/features/settings/SettingsScreen.tsx tests/mobile/vs32-feedback-entrypoints.test.mjs
git commit -m "feat(vs32): connect owner feedback entry points"
```

---

### Task 6: Regression, review, exact-SHA certification, and integration

**Files:**
- Modify only if exact failures require scoped fixes: VS-32 files/tests and governance/docs.
- Update: `delivery/current-slice.json`
- Update: `docs/slices/VS-32.md`

**Interfaces:**
- Consumes the complete VS-32 runtime.
- Produces one frozen runtime SHA with deterministic evidence and one governance-only certification commit.

- [ ] **Step 1: Run deterministic repository verification**

```bash
npm run preflight
dotnet build apps/api/Atlas.Api.csproj --configuration Release
dotnet test tests/api/Atlas.Api.Tests.csproj --configuration Release
npm run dashboard:build
```

Expected:
- governance/planning/platform validation pass;
- TypeScript + Expo lint pass;
- all mobile tests/authentic runtime checks pass, including VS-31 four-tab tests;
- API build/tests pass;
- clean Postgres migration replay passes in CI;
- dashboard build passes.

- [ ] **Step 2: Review the changed-file boundary**

Reject any accidental change to:
- `.github/workflows/release.yml`
- `infrastructure/**`
- Payments/Uploads
- recommendation generation/ranking logic
- Action/Outcome lifecycle semantics
- VS-31 tab structure
- external-provider configuration

Confirm unsafe reporting has no Opportunity mutation path.

- [ ] **Step 3: Run exact-head GitHub gates**

Require on the frozen runtime SHA:
- CI success;
- Security baseline success;
- Product Intake success.

If anything fails, invoke `systematic-debugging`, add a regression, fix minimally, and restart exact-head verification.

- [ ] **Step 4: Record certification against the exact runtime SHA**

Set `delivery/current-slice.json` certification status to `passed` and bind the exact 40-character runtime SHA. Record mobile/API counts, migration replay, CI run, Security run, Product Intake run, TDD RED evidence, and changed-file review. Keep release `not-authorized` and production enablement pending.

- [ ] **Step 5: Validate the governance-only certification head**

After the certification metadata commit, require CI/Security/Product Intake green again on that metadata head.

- [ ] **Step 6: Finish branch and merge under standing human approval**

Use `finishing-a-development-branch` / GitHub exact-head merge guard. Merge the certified PR only if it remains mergeable and all post-cert gates are green. Do not deploy or release.
