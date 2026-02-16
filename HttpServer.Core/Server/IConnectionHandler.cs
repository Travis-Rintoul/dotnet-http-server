using System.Net.Sockets;

namespace HttpServer.Core.Server;

public interface IConnectionHandler
{
    Task HandleAsync(Socket socket, CancellationToken token);
}