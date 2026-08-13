using Xunit;

namespace Atlas.Api.Tests;

public sealed class OperationalUploadEndpointTests
{
    [Fact]
    public void Device_upload_routes_are_owner_only_bounded_and_use_common_ingestion()
    {
        var source = Read("OperationalUploadEndpoints.cs");
        var program = Read("Program.cs");

        Assert.Contains("app.MapOperationalUploadEndpoints();", program, StringComparison.Ordinal);
        Assert.Contains("/api/v1/businesses/{businessId:guid}/operational-upload/preview", source, StringComparison.Ordinal);
        Assert.Contains("/api/v1/businesses/{businessId:guid}/operational-upload/confirm", source, StringComparison.Ordinal);
        Assert.True(Count(source, "RequireAuthorization(\"BusinessOwner\")") >= 2);
        Assert.Contains("OperationalCsvReader.MaximumFileBytes", source, StringComparison.Ordinal);
        Assert.Contains("OperationalCsvReader.PreviewAsync", source, StringComparison.Ordinal);
        Assert.Contains("OperationalCsvReader.NormalizeAsync", source, StringComparison.Ordinal);
        Assert.Contains("OperationalIngestionService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InternalOperator", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirmation_is_fingerprint_bound_and_raw_upload_is_never_durably_stored()
    {
        var source = Read("OperationalUploadEndpoints.cs");

        Assert.Contains("PreviewFingerprint", source, StringComparison.Ordinal);
        Assert.Contains("fingerprint-mismatch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("File.Write", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CopyToAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MemoryCache", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RawCsv", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RawFile", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Multipart_validation_requires_one_csv_file_and_rejects_oversize_content_length()
    {
        var source = Read("OperationalUploadEndpoints.cs");

        Assert.Contains("multipart/form-data", source, StringComparison.Ordinal);
        Assert.Contains(".csv", source, StringComparison.Ordinal);
        Assert.Contains("text/csv", source, StringComparison.Ordinal);
        Assert.Contains("file-too-large", source, StringComparison.Ordinal);
        Assert.Contains("upload-invalid", source, StringComparison.Ordinal);
    }

    private static int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;

    private static string Read(string file)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "apps", "api", file);
            if (File.Exists(path)) return File.ReadAllText(path);
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException();
    }
}
