using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api;

public sealed record OperationalUploadPreviewResponse(
    string PreviewFingerprint,
    int RowCount,
    int OrderCount,
    DateOnly EarliestBusinessDate,
    DateOnly LatestBusinessDate,
    IReadOnlyList<string> RecognizedColumns,
    IReadOnlyList<string> IgnoredSensitiveColumns,
    IReadOnlyList<string> MetricKeys);

public static class OperationalUploadEndpoints
{
    private const string BasePath = "/api/v1/businesses/{businessId:guid}/operational-upload";

    public static void MapOperationalUploadEndpoints(this WebApplication app)
    {
        app.MapPost(BasePath + "/preview", PreviewAsync)
            .RequireAuthorization("BusinessOwner")
            .DisableAntiforgery();
        app.MapPost(BasePath + "/confirm", ConfirmAsync)
            .RequireAuthorization("BusinessOwner")
            .DisableAntiforgery();
    }

    private static async Task<IResult> PreviewAsync(
        Guid businessId, HttpRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
    {
        if (!await IsOwnerAsync(businessId, user, db, ct)) return Results.NotFound();
        var validation = await ValidateUploadAsync(request, ct);
        if (validation.Error is not null) return validation.Error;
        var business = await db.Businesses.SingleAsync(item => item.Id == businessId, ct);
        try
        {
            await using var stream = validation.File!.OpenReadStream();
            var preview = await OperationalCsvReader.PreviewAsync(stream, business, ct);
            return Results.Ok(new OperationalUploadPreviewResponse(preview.Fingerprint, preview.RowCount, preview.OrderCount,
                preview.EarliestBusinessDate, preview.LatestBusinessDate, preview.RecognizedColumns,
                preview.IgnoredSensitiveColumns, preview.MetricKeys));
        }
        catch (OperationalCsvException error) { return CsvProblem(error); }
    }

    private static async Task<IResult> ConfirmAsync(
        Guid businessId, HttpRequest request, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
    {
        if (!await IsOwnerAsync(businessId, user, db, ct)) return Results.NotFound();
        var validation = await ValidateUploadAsync(request, ct);
        if (validation.Error is not null) return validation.Error;
        var form = validation.Form!;
        var suppliedFingerprint = form["PreviewFingerprint"].ToString();
        if (string.IsNullOrWhiteSpace(suppliedFingerprint))
            return Problem(400, "upload-invalid", "PreviewFingerprint is required.");
        var business = await db.Businesses.SingleAsync(item => item.Id == businessId, ct);
        try
        {
            OperationalNormalizationResult normalized;
            await using (var stream = validation.File!.OpenReadStream())
                normalized = await OperationalCsvReader.NormalizeAsync(stream, business, ct);
            if (!string.Equals(suppliedFingerprint, normalized.Preview.Fingerprint, StringComparison.Ordinal))
                return Problem(409, "fingerprint-mismatch", "The CSV changed after preview. Preview it again before confirming.");
            var ingestion = await new OperationalIngestionService(db).IngestAsync(
                businessId, "device-upload", "owner-device", normalized.Preview.Fingerprint,
                normalized, DateTimeOffset.UtcNow, ct);
            return Results.Ok(ingestion);
        }
        catch (OperationalCsvException error) { return CsvProblem(error); }
    }

    private static async Task<UploadValidation> ValidateUploadAsync(HttpRequest request, CancellationToken ct)
    {
        if (!request.HasFormContentType || !request.ContentType!.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return new(null, null, Problem(400, "upload-invalid", "Upload one CSV file using multipart/form-data."));
        if (request.ContentLength is > OperationalCsvReader.MaximumFileBytes)
            return new(null, null, Problem(413, "file-too-large", "CSV files must be 10 MiB or smaller."));
        var form = await request.ReadFormAsync(new FormOptions
        {
            MultipartBodyLengthLimit = OperationalCsvReader.MaximumFileBytes,
            MemoryBufferThreshold = 64 * 1024
        }, ct);
        if (form.Files.Count != 1)
            return new(form, null, Problem(400, "upload-invalid", "Upload exactly one CSV file."));
        var file = form.Files[0];
        if (file.Length > OperationalCsvReader.MaximumFileBytes)
            return new(form, null, Problem(413, "file-too-large", "CSV files must be 10 MiB or smaller."));
        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(file.ContentType, "text/csv", StringComparison.OrdinalIgnoreCase))
            return new(form, null, Problem(400, "upload-invalid", "Choose a .csv file with content type text/csv."));
        return new(form, file, null);
    }

    private static async Task<bool> IsOwnerAsync(Guid businessId, ClaimsPrincipal user, AtlasDbContext db, CancellationToken ct)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return subject is not null && await db.BusinessMemberships.AnyAsync(item => item.BusinessId == businessId &&
            item.Role == MembershipRoles.BusinessOwner && item.UserAccount.ProviderSubject == subject, ct);
    }

    private static IResult CsvProblem(OperationalCsvException error) =>
        Problem(error.Code is OperationalCsvErrorCodes.FileTooLarge ? 413 : 400, error.Code, error.Message);

    private static IResult Problem(int status, string code, string detail) => Results.Problem(
        statusCode: status, title: "Operational CSV upload could not be processed", detail: detail,
        extensions: new Dictionary<string, object?> { ["code"] = code });

    private sealed record UploadValidation(IFormCollection? Form, IFormFile? File, IResult? Error);
}
