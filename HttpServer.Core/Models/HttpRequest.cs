using System.Net;
using System.Text;
using HttpServer.Core.Transport;

namespace HttpServer.Core.Models;

public sealed record HttpRequestHead(
    string Method,
    string Target,
    string Version,
    HttpHeaders Headers,
    BodyDescriptor Body)
{
    public bool IsKeepAlive =>
        Version == HttpVersion.Version11.ToString()
            ? !Headers.ConnectionClose
            : Headers.ConnectionKeepAlive;

    public bool HasBody =>
        ContentLength > 0 || IsChunked;

    public bool IsChunked =>
        Headers.TransferEncodingChunked;

    public int ContentLength =>
        Headers.ContentLength ?? 0;
}

public sealed record HttpRequestFrame(
    byte[] HeaderBytes,
    byte[] RemainderBytes);
