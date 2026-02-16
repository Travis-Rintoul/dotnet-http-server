using System.Net;
using System.Net.Sockets;
using HttpServer.Core.Transport;

namespace HttpServer.Core.Server;

public class HttpServer(IConnectionHandler connectionHandler)
{
    public async Task StartAsync(Port port, CancellationToken cancellationToken)
    {
        Console.WriteLine($"HTTP server listening on http://localhost:{port.Value}");
        
        var listener = new TcpListener(IPAddress.Loopback, port.Value);
        listener.Start();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var socket = await listener.AcceptSocketAsync(cancellationToken);
            await connectionHandler.HandleAsync(socket, cancellationToken);
        }
    }
}