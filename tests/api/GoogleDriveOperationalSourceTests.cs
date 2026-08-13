using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class GoogleDriveOperationalSourceTests
{
    [Fact]
    public async Task Validate_folder_requires_viewer_only_private_folder()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK,
            """{"id":"folder-38","name":"Atlas exports","mimeType":"application/vnd.google-apps.folder","capabilities":{"canEdit":false,"canShare":false},"permissions":[{"type":"user","role":"reader","allowFileDiscovery":false}]}"""));
        var source = Source(handler);

        var folder = await source.ValidateFolderAsync("folder-38", default);

        Assert.Equal("folder-38", folder.Id);
        Assert.Equal("Atlas exports", folder.Name);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("files/folder-38", request.Uri.AbsoluteUri);
        Assert.Contains("capabilities", request.Uri.Query);
        Assert.Equal("Bearer", request.Authorization?.Scheme);
        Assert.Equal("test-access-token", request.Authorization?.Parameter);
    }

    [Theory]
    [InlineData("{\"id\":\"f\",\"name\":\"Atlas\",\"mimeType\":\"application/vnd.google-apps.folder\",\"capabilities\":{\"canEdit\":true,\"canShare\":true},\"permissions\":[{\"type\":\"user\",\"role\":\"writer\"}]}")]
    [InlineData("{\"id\":\"f\",\"name\":\"Atlas\",\"mimeType\":\"application/vnd.google-apps.folder\",\"capabilities\":{\"canEdit\":false,\"canShare\":false},\"permissions\":[{\"type\":\"anyone\",\"role\":\"reader\"}]}")]
    public async Task Validate_folder_rejects_write_or_anyone_access(string response)
    {
        var source = Source(new RecordingHandler(_ => Json(HttpStatusCode.OK, response)));

        var error = await Assert.ThrowsAsync<GoogleDriveOperationalException>(() => source.ValidateFolderAsync("f", default));

        Assert.Equal(GoogleDriveOperationalErrorCodes.UnsafeFolderGrant, error.Code);
    }

    [Fact]
    public async Task List_queries_only_direct_nontrashed_children_and_returns_csv_files()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK,
            """{"files":[
              {"id":"csv-1","name":"sales.csv","mimeType":"text/csv","modifiedTime":"2026-08-13T09:00:00Z","size":"120","md5Checksum":"abc"},
              {"id":"sheet-1","name":"sales","mimeType":"application/vnd.google-apps.spreadsheet","modifiedTime":"2026-08-13T09:00:00Z"},
              {"id":"xlsx-1","name":"sales.xlsx","mimeType":"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","modifiedTime":"2026-08-13T09:00:00Z","size":"300"},
              {"id":"shortcut-1","name":"other.csv","mimeType":"application/vnd.google-apps.shortcut","modifiedTime":"2026-08-13T09:00:00Z"}
            ]}"""));
        var source = Source(handler);

        var files = await source.ListAsync("folder-38", default);

        var file = Assert.Single(files);
        Assert.Equal("csv-1", file.Id);
        Assert.Equal("abc", file.ProviderChecksum);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("%27folder-38%27+in+parents", request.Uri.Query.Replace("%20", "+", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("trashed%3Dfalse", request.Uri.Query.Replace("%20", "", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("files%28id%2Cname%2CmimeType%2CmodifiedTime%2Csize%2Cmd5Checksum%29", request.Uri.Query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Open_read_uses_media_get_and_never_sends_a_write_method()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("Date,Amount\n2026-08-13,10\n"))
        });
        var source = Source(handler);

        await using var stream = await source.OpenReadAsync("csv-1", default);
        using var reader = new StreamReader(stream);
        Assert.Contains("2026-08-13", await reader.ReadToEndAsync());
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Contains("files/csv-1", request.Uri.AbsoluteUri);
        Assert.Contains("alt=media", request.Uri.Query);
        Assert.DoesNotContain(handler.Requests, item => item.Method != HttpMethod.Get);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, GoogleDriveOperationalErrorCodes.ReauthorizationRequired)]
    [InlineData(HttpStatusCode.NotFound, GoogleDriveOperationalErrorCodes.FolderNotFound)]
    public async Task Provider_failures_are_mapped_without_leaking_token_or_response(HttpStatusCode status, string expectedCode)
    {
        const string providerBody = "provider-secret-detail";
        var source = Source(new RecordingHandler(_ => Json(status, $"{{\"error\":\"{providerBody}\"}}")));

        var error = await Assert.ThrowsAsync<GoogleDriveOperationalException>(() => source.ValidateFolderAsync("folder-38", default));

        Assert.Equal(expectedCode, error.Code);
        Assert.DoesNotContain("test-access-token", error.ToString());
        Assert.DoesNotContain(providerBody, error.ToString());
    }

    private static GoogleDriveOperationalSource Source(RecordingHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://www.googleapis.com/drive/v3/") }, new FixedTokenProvider());

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class FixedTokenProvider : IGoogleDriveAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken) => Task.FromResult("test-access-token");
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new(request.Method, request.RequestUri!, request.Headers.Authorization));
            return Task.FromResult(responder(request));
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, Uri Uri, AuthenticationHeaderValue? Authorization);
}
