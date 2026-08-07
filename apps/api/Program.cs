using System.Security.Claims;
using Atlas.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AtlasDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Atlas") ??
        "Host=localhost;Port=5432;Database=atlas;Username=postgres;Password=postgres"));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BusinessOwner", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("InternalOperator", policy => policy.RequireRole("PilotOperator", "PlatformAdministrator"));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (AtlasDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready" })
        : Results.Problem(statusCode: 503, title: "Database unavailable", extensions: new Dictionary<string, object?> { ["code"] = "database_unavailable" }));

static string? Subject(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");

static async Task<UserAccount?> OwnerAccount(Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
{
    var subject = Subject(user);
    if (string.IsNullOrWhiteSpace(subject)) return null;
    var membership = await db.BusinessMemberships.Include(x => x.UserAccount)
        .SingleOrDefaultAsync(x => x.BusinessId == businessId && x.UserAccount.ProviderSubject == subject && x.Role == MembershipRoles.BusinessOwner, ct);
    return membership?.UserAccount;
}

app.MapPost("/api/v1/businesses", async (CreateBusinessRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
{
    var subject = Subject(user);
    if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
    var errors = request.Validate();
    if (errors.Count > 0) return Results.ValidationProblem(errors, extensions: new Dictionary<string, object?> { ["code"] = "business_invalid" });

    var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.ProviderSubject == subject, ct);
    if (account is null)
    {
        account = new UserAccount { Id = Guid.NewGuid(), ProviderSubject = subject, CreatedAt = DateTimeOffset.UtcNow };
        db.UserAccounts.Add(account);
    }

    var existing = await db.BusinessMemberships.AnyAsync(x => x.UserAccountId == account.Id && x.Role == MembershipRoles.BusinessOwner, ct);
    if (existing) return Results.Conflict(new { code = "initial_business_exists", message = "The account already owns a Business." });

    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    var business = Business.Create(request);
    var genericVersion = await db.KnowledgePackVersions
        .Include(x => x.KnowledgePack)
        .Include(x => x.Sections)
        .SingleOrDefaultAsync(x => x.KnowledgePack.Key == KnowledgePackKeys.GenericBusiness &&
            x.VersionNumber == GenericBusinessKnowledgePack.InitialVersion && x.Status == KnowledgePackStatuses.Published, ct);

    KnowledgePack genericPack;
    if (genericVersion is null)
    {
        (genericPack, genericVersion) = GenericBusinessKnowledgePack.Create(account.Id);
        db.KnowledgePacks.Add(genericPack);
    }
    else
    {
        genericPack = genericVersion.KnowledgePack;
    }

    db.Businesses.Add(business);
    db.BusinessMemberships.Add(new BusinessMembership { Id = Guid.NewGuid(), BusinessId = business.Id, UserAccountId = account.Id, Role = MembershipRoles.BusinessOwner, CreatedAt = DateTimeOffset.UtcNow });
    db.BusinessKnowledgeAssignments.Add(BusinessKnowledgeAssignment.Assign(business.Id, genericPack, genericVersion, account.Id));
    db.AuditRecords.Add(AuditRecord.Create(account.Id, business.Id, "business.created"));
    db.AuditRecords.Add(AuditRecord.Create(account.Id, business.Id, $"knowledge-pack.assigned:{genericPack.Key}:{genericVersion.VersionNumber}"));
    await db.SaveChangesAsync(ct);
    await transaction.CommitAsync(ct);
    return Results.Created($"/api/v1/businesses/{business.Id}", BusinessResponse.From(business));
}).RequireAuthorization("BusinessOwner");

app.MapGet("/api/v1/businesses/{businessId:guid}", async (Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
{
    var subject = Subject(user);
    var business = await db.Businesses
        .Where(b => db.BusinessMemberships.Any(m => m.BusinessId == b.Id && m.UserAccount.ProviderSubject == subject && m.Role == MembershipRoles.BusinessOwner))
        .SingleOrDefaultAsync(b => b.Id == businessId, ct);
    return business is null ? Results.NotFound() : Results.Ok(BusinessResponse.From(business));
}).RequireAuthorization("BusinessOwner");

app.MapGet("/api/v1/businesses/{businessId:guid}/profile", async (Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
{
    if (await OwnerAccount(businessId, user, db, ct) is null) return Results.NotFound();
    var profile = await db.BusinessProfiles.SingleOrDefaultAsync(x => x.BusinessId == businessId, ct);
    return Results.Ok(profile);
}).RequireAuthorization("BusinessOwner");

app.MapPut("/api/v1/businesses/{businessId:guid}/profile", async (Guid businessId, UpsertBusinessProfileRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
{
    var account = await OwnerAccount(businessId, user, db, ct);
    if (account is null) return Results.NotFound();
    var errors = request.Validate();
    if (errors.Count > 0) return Results.ValidationProblem(errors, extensions: new Dictionary<string, object?> { ["code"] = "profile_invalid" });

    var profile = await db.BusinessProfiles.SingleOrDefaultAsync(x => x.BusinessId == businessId, ct);
    if (profile is null)
    {
        profile = new BusinessProfile { BusinessId = businessId, Language = request.Language.Trim(), Source = request.Source, OwnerConfirmed = request.OwnerConfirmed, UpdatedAt = DateTimeOffset.UtcNow };
        db.BusinessProfiles.Add(profile);
    }
    profile.Description = request.Description?.Trim();
    profile.Address = request.Address?.Trim();
    profile.Website = request.Website?.Trim();
    profile.Phone = request.Phone?.Trim();
    profile.Email = request.Email?.Trim();
    profile.SocialChannels = request.SocialChannels?.Trim();
    profile.BusinessHours = request.BusinessHours?.Trim();
    profile.Language = request.Language.Trim();
    profile.Source = request.Source;
    profile.OwnerConfirmed = request.OwnerConfirmed;
    profile.UpdatedAt = DateTimeOffset.UtcNow;
    db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, "business.profile.updated"));
    await db.SaveChangesAsync(ct);
    return Results.Ok(profile);
}).RequireAuthorization("BusinessOwner");

app.MapGet("/api/v1/businesses/{businessId:guid}/goals", async (Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
{
    if (await OwnerAccount(businessId, user, db, ct) is null) return Results.NotFound();
    return Results.Ok(await db.BusinessGoals.Where(x => x.BusinessId == businessId).OrderBy(x => x.Priority).ToListAsync(ct));
}).RequireAuthorization("BusinessOwner");

app.MapPut("/api/v1/businesses/{businessId:guid}/goals", async (Guid businessId, UpsertGoalsRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
{
    var account = await OwnerAccount(businessId, user, db, ct);
    if (account is null) return Results.NotFound();
    var errors = request.Validate();
    if (errors.Count > 0) return Results.ValidationProblem(errors, extensions: new Dictionary<string, object?> { ["code"] = "goals_invalid" });

    var existing = await db.BusinessGoals.Where(x => x.BusinessId == businessId).ToListAsync(ct);
    db.BusinessGoals.RemoveRange(existing);
    db.BusinessGoals.AddRange(request.Goals.Select(x => new BusinessGoal
    {
        Id = Guid.NewGuid(), BusinessId = businessId, Type = x.Type.Trim(), Title = x.Title.Trim(), Priority = x.Priority,
        IsCustom = x.IsCustom, UpdatedAt = DateTimeOffset.UtcNow
    }));
    db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, "business.goals.updated"));
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
}).RequireAuthorization("BusinessOwner");

app.MapGet("/api/v1/businesses/{businessId:guid}/context", async (Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
{
    if (await OwnerAccount(businessId, user, db, ct) is null) return Results.NotFound();
    return Results.Ok(await db.BusinessContextEntries.Where(x => x.BusinessId == businessId).OrderBy(x => x.Key).ToListAsync(ct));
}).RequireAuthorization("BusinessOwner");

app.MapPut("/api/v1/businesses/{businessId:guid}/context/{key}", async (Guid businessId, string key, UpsertContextRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
{
    var account = await OwnerAccount(businessId, user, db, ct);
    if (account is null) return Results.NotFound();
    if (!string.Equals(key, request.Key, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(request.Value))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["context"] = ["Context key and value are required and must match the route."] });
    if (request.Source is not FieldSources.Owner and not FieldSources.Public || request.Source == FieldSources.Public && !request.OwnerConfirmed)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["source"] = ["Public context must be owner-confirmed."] });

    var normalizedKey = key.Trim().ToLowerInvariant();
    var entry = await db.BusinessContextEntries.SingleOrDefaultAsync(x => x.BusinessId == businessId && x.Key == normalizedKey, ct);
    if (entry is null)
    {
        entry = new BusinessContextEntry
        {
            Id = Guid.NewGuid(), BusinessId = businessId, Key = normalizedKey, Value = request.Value.Trim(),
            Source = request.Source, OwnerConfirmed = request.OwnerConfirmed, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.BusinessContextEntries.Add(entry);
    }
    else
    {
        entry.Value = request.Value.Trim();
        entry.Source = request.Source;
        entry.OwnerConfirmed = request.OwnerConfirmed;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
    }
    db.AuditRecords.Add(AuditRecord.Create(account.Id, businessId, "business.context.updated"));
    await db.SaveChangesAsync(ct);
    return Results.Ok(entry);
}).RequireAuthorization("BusinessOwner");

app.MapKnowledgePackEndpoints();
app.MapOpportunityEndpoints();
app.MapExecutionKitEndpoints();

app.MapPost("/api/v1/session/logout", (HttpContext context) =>
    Results.Ok(new { status = "signed_out", correlationId = context.TraceIdentifier })).RequireAuthorization();

if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.Run();

public partial class Program;
