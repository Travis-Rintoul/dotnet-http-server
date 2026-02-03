using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HttpServer.Core.Protocol;
using static HttpServer.Core.Constants.HeaderConstants;

var listener = new TcpListener(IPAddress.Loopback, 5001);
listener.Start();
Console.WriteLine($"{IPAddress.Loopback}:5001");

while (true)
{
    var clientListener = await listener.AcceptTcpClientAsync();
    
    Task.Run(async () =>
    {
        using var client = clientListener;
        await using var stream = client.GetStream();
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var (startLine, headers, bodyBytes) = await HttpRequestReader.ReadAsync(stream, cancellationTokenSource.Token);
        
        Console.WriteLine(startLine);
        
        if (headers.TryGetValue(ContentType, out var ct) &&
            ct.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            var bodyText = Encoding.UTF8.GetString(bodyBytes);
            Console.WriteLine("Body: " + bodyText);
        }
        
        Console.WriteLine("Request made to 127.0.0.1:5001");
        
        var response = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK";
        var respBytes = Encoding.ASCII.GetBytes(response);
        await stream.WriteAsync(respBytes);
    });
}
