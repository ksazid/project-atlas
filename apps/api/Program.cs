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

app.MapPost("/api/v1/businesses", async (CreateBusinessRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
{
    var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
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
    db.Businesses.Add(business);
    db.BusinessMemberships.Add(new BusinessMembership
    {
        Id = Guid.NewGuid(), BusinessId = business.Id, UserAccountId = account.Id,
        Role = MembershipRoles.BusinessOwner, CreatedAt = DateTimeOffset.UtcNow
    });
    db.AuditRecords.Add(AuditRecord.Create(account.Id, business.Id, "business.created"));
    await db.SaveChangesAsync(ct);
    await transaction.CommitAsync(ct);

    return Results.Created($"/api/v1/businesses/{business.Id}", BusinessResponse.From(business));
}).RequireAuthorization("BusinessOwner");

app.MapGet("/api/v1/businesses/{businessId:guid}", async (Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct) =>
{
    var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    var business = await db.Businesses
        .Where(b => db.BusinessMemberships.Any(m => m.BusinessId == b.Id && m.UserAccount.ProviderSubject == subject && m.Role == MembershipRoles.BusinessOwner))
        .SingleOrDefaultAsync(b => b.Id == businessId, ct);
    return business is null ? Results.NotFound() : Results.Ok(BusinessResponse.From(business));
}).RequireAuthorization("BusinessOwner");

app.MapPost("/api/v1/session/logout", (HttpContext context) =>
    Results.Ok(new { status = "signed_out", correlationId = context.TraceIdentifier })).RequireAuthorization();

if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.Run();

public partial class Program;
