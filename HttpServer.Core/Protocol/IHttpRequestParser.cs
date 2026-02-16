using FluentResults;
using HttpServer.Core.Models;

namespace HttpServer.Core.Protocol;

public interface IHttpRequestParser
{
    Result<HttpRequestHead> ParseHeader(ReadOnlySpan<byte> headerBytes);
}