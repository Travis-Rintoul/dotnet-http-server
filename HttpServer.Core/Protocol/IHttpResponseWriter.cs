using System.Net.Sockets;
using HttpServer.Core.Transport;

namespace HttpServer.Core.Protocol;

public interface IHttpResponseWriter
{
    Task WriteAsync(NetworkStream stream, HttpResponse response, CancellationToken cancellationToken);
}