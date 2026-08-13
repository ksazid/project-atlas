using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Atlas.Api;

public static class GoogleDriveOperationalErrorCodes
{
    public const string UnsafeFolderGrant = "unsafe-folder-grant";
    public const string ReauthorizationRequired = "reauthorization-required";
    public const string FolderNotFound = "folder-not-found";
    public const string ProviderUnavailable = "provider-unavailable";
    public const string ConnectorNotConfigured = "connector-not-configured";
}

public sealed class GoogleDriveOperationalException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed record OperationalDriveFolder(string Id, string Name);
public sealed record OperationalSourceFile(
    string Id, string Name, string MimeType, DateTimeOffset ModifiedAt, long Size, string? ProviderChecksum);

public interface IGoogleDriveAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}

public sealed class GoogleDriveConnectorOptions
{
    public string ClientEmail { get; set; } = "";
    public string PrivateKey { get; set; } = "";
    public string TokenUri { get; set; } = "https://oauth2.googleapis.com/token";
}

public sealed class GoogleServiceAccountAccessTokenProvider(
    IHttpClientFactory clients,
    IOptions<GoogleDriveConnectorOptions> configured) : IGoogleDriveAccessTokenProvider
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private string? accessToken;
    private DateTimeOffset expiresAt;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (accessToken is not null && expiresAt > DateTimeOffset.UtcNow.AddMinutes(2)) return accessToken;
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            if (accessToken is not null && expiresAt > DateTimeOffset.UtcNow.AddMinutes(2)) return accessToken;
            var options = configured.Value;
            if (string.IsNullOrWhiteSpace(options.ClientEmail) || string.IsNullOrWhiteSpace(options.PrivateKey))
                throw new GoogleDriveOperationalException(GoogleDriveOperationalErrorCodes.ConnectorNotConfigured,
                    "The Google Drive connector identity is not configured.");

            var now = DateTimeOffset.UtcNow;
            var assertion = CreateAssertion(options, now);
            using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenUri)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    ["assertion"] = assertion
                })
            };
            using var response = await clients.CreateClient("GoogleDriveToken").SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new GoogleDriveOperationalException(GoogleDriveOperationalErrorCodes.ReauthorizationRequired,
                    "Atlas could not authorize its read-only Google Drive connector identity.");
            var token = await response.Content.ReadFromJsonAsync<ServiceAccountTokenResponse>(cancellationToken: cancellationToken);
            if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
                throw new GoogleDriveOperationalException(GoogleDriveOperationalErrorCodes.ReauthorizationRequired,
                    "Atlas received an invalid connector authorization response.");
            accessToken = token.AccessToken;
            expiresAt = now.AddSeconds(Math.Max(60, token.ExpiresIn));
            return accessToken;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static string CreateAssertion(GoogleDriveConnectorOptions options, DateTimeOffset now)
    {
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = options.ClientEmail,
            scope = "https://www.googleapis.com/auth/drive.readonly",
            aud = options.TokenUri,
            iat = now.ToUnixTimeSeconds(),
            exp = now.AddMinutes(55).ToUnixTimeSeconds()
        }));
        var unsigned = $"{header}.{payload}";
        using var rsa = RSA.Create();
        rsa.ImportFromPem(options.PrivateKey.Replace("\\n", "\n", StringComparison.Ordinal));
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{unsigned}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private sealed record ServiceAccountTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

public sealed class GoogleDriveOperationalSource(HttpClient client, IGoogleDriveAccessTokenProvider tokens)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<OperationalDriveFolder> ValidateFolderAsync(string folderId, CancellationToken cancellationToken)
    {
        var path = $"files/{Uri.EscapeDataString(folderId)}?fields=" + Uri.EscapeDataString("id,name,mimeType,capabilities(canEdit,canShare),permissions(type,role,allowFileDiscovery)");
        using var response = await SendAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var folder = await response.Content.ReadFromJsonAsync<DriveFolderResponse>(JsonOptions, cancellationToken)
            ?? throw ProviderUnavailable();
        var isFolder = folder.MimeType == "application/vnd.google-apps.folder";
        var hasReader = folder.Permissions.Any(permission => permission.Type == "user" && permission.Role == "reader");
        var publiclyAccessible = folder.Permissions.Any(permission => permission.Type == "anyone");
        if (!isFolder || folder.Capabilities.CanEdit || folder.Capabilities.CanShare || !hasReader || publiclyAccessible)
            throw new GoogleDriveOperationalException(GoogleDriveOperationalErrorCodes.UnsafeFolderGrant,
                "Share exactly one private Google Drive folder with the Atlas connector identity as Viewer.");
        return new(folder.Id, folder.Name);
    }

    public async Task<IReadOnlyList<OperationalSourceFile>> ListAsync(string folderId, CancellationToken cancellationToken)
    {
        var query = $"'{folderId.Replace("'", "\\'", StringComparison.Ordinal)}' in parents and trashed=false";
        var fields = "files(id,name,mimeType,modifiedTime,size,md5Checksum)";
        var path = $"files?q={Uri.EscapeDataString(query)}&fields={Uri.EscapeDataString(fields)}&pageSize=1000";
        using var response = await SendAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<DriveFileListResponse>(JsonOptions, cancellationToken)
            ?? throw ProviderUnavailable();
        return result.Files
            .Where(file => file.MimeType == "text/csv" && file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .Select(file => new OperationalSourceFile(file.Id, file.Name, file.MimeType,
                DateTimeOffset.Parse(file.ModifiedTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                long.TryParse(file.Size, NumberStyles.None, CultureInfo.InvariantCulture, out var size) ? size : 0,
                file.Md5Checksum))
            .ToArray();
    }

    public async Task<Stream> OpenReadAsync(string fileId, CancellationToken cancellationToken)
    {
        var response = await SendAsync($"files/{Uri.EscapeDataString(fileId)}?alt=media", cancellationToken,
            HttpCompletionOption.ResponseHeadersRead);
        try
        {
            await EnsureSuccessAsync(response, cancellationToken);
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return new ResponseOwnedStream(stream, response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(string relativePath, CancellationToken cancellationToken,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead)
    {
        var token = await tokens.GetAccessTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request, completion, cancellationToken);
    }

    private static Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (response.IsSuccessStatusCode) return Task.CompletedTask;
        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new GoogleDriveOperationalException(GoogleDriveOperationalErrorCodes.ReauthorizationRequired,
                    "Google Drive access must be restored for the selected Atlas folder."),
            HttpStatusCode.NotFound =>
                new GoogleDriveOperationalException(GoogleDriveOperationalErrorCodes.FolderNotFound,
                    "The selected Google Drive folder or file is no longer available."),
            _ => ProviderUnavailable()
        };
    }

    private static GoogleDriveOperationalException ProviderUnavailable() => new(
        GoogleDriveOperationalErrorCodes.ProviderUnavailable, "Google Drive is temporarily unavailable.");

    private sealed class DriveFolderResponse
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string MimeType { get; set; } = "";
        public DriveCapabilities Capabilities { get; set; } = new();
        public List<DrivePermission> Permissions { get; set; } = [];
    }
    private sealed class DriveCapabilities { public bool CanEdit { get; set; } public bool CanShare { get; set; } }
    private sealed class DrivePermission { public string Type { get; set; } = ""; public string Role { get; set; } = ""; }
    private sealed class DriveFileListResponse { public List<DriveFileResponse> Files { get; set; } = []; }
    private sealed class DriveFileResponse
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string MimeType { get; set; } = "";
        public string ModifiedTime { get; set; } = "";
        public string? Size { get; set; }
        public string? Md5Checksum { get; set; }
    }

    private sealed class ResponseOwnedStream(Stream inner, HttpResponseMessage response) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) { inner.Dispose(); response.Dispose(); } base.Dispose(disposing); }
        public override async ValueTask DisposeAsync() { await inner.DisposeAsync(); response.Dispose(); GC.SuppressFinalize(this); }
    }
}
