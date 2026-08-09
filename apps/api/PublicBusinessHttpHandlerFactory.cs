using System.Net;

namespace Atlas.Api;

public static class PublicBusinessHttpHandlerFactory
{
    public static SocketsHttpHandler Create() => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
        ConnectCallback = PublicBusinessHttpConnector.ConnectAsync,
    };
}
