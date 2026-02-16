using System.Net.Sockets;
using FluentResults;
using HttpServer.Core.Models;

namespace HttpServer.Core.Protocol;

public interface IHttpRequestReader
{
    Task<Result<HttpRequestFrame>> ReadHeaderAsync(NetworkStream stream, CancellationToken cancellationToken);

    Task<Result<Stream>> ReadBodyAsync(
        NetworkStream stream,
        BodyDescriptor body,
        byte[] remainder,
        CancellationToken cancellationToken);
}