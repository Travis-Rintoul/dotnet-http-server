namespace HttpServer.Core.Models;

public sealed record HttpRequestHead(
    string Method,
    string Target,
    string Version,
    IReadOnlyDictionary<string, string> Headers,
    BodyDescriptor Body);

public sealed record HttpRequestFrame(
    byte[] HeaderBytes,
    byte[] RemainderBytes);
