using System.Net;
using System.Net.Sockets;
using System.Text;
using HttpServer.Core.Constants;
using HttpServer.Core.Models;
using HttpServer.Core.Protocol;

var listener = new TcpListener(IPAddress.Loopback, 5001);
listener.Start();

while (true)
{
    var clientListener = await listener.AcceptTcpClientAsync();
    
    Task.Run(async () =>
    {
        using var client = clientListener;
        await using var stream = client.GetStream();

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var limits = new HttpLimits();
        var reader = new HttpRequestReader(limits);
        var parser = new HttpRequestParser(limits);

        // Read headers
        var frameResult = await reader.ReadHeaderAsync(stream, cancellationTokenSource.Token);
        if (frameResult.IsFailed)
            return;

        HttpRequestFrame frame = frameResult.Value;

        // Parse headers
        var parseResult = parser.ParseHeader(frame.HeaderBytes);
        if (parseResult.IsFailed)
            return;

        HttpRequestHead requestHead = parseResult.Value;

        // Read body
        var bodyResult = await reader.ReadBodyAsync(
            stream,
            requestHead.Body,
            frame.RemainderBytes,
            cancellationTokenSource.Token);

        if (bodyResult.IsFailed)
            return;

        await using Stream bodyStream = bodyResult.Value;

        Console.WriteLine($"{requestHead.Method} {requestHead.Target} {requestHead.Version}");

        if (requestHead.Headers.TryGetValue(HeaderConstants.ContentType, out var contentType) &&
            contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            // For now, buffer intentionally (application decision)
            using var ms = new MemoryStream();
            await bodyStream.CopyToAsync(ms, cancellationTokenSource.Token);

            var bodyText = Encoding.UTF8.GetString(ms.ToArray());
            Console.WriteLine("Body:");
            Console.WriteLine(bodyText);
        }

        Console.WriteLine("Request made to 127.0.0.1:5001");

        var response =
            "HTTP/1.1 200 OK\r\n" +
            "Content-Length: 2\r\n" +
            "Connection: close\r\n" +
            "\r\n" +
            "OK";

        var respBytes = Encoding.ASCII.GetBytes(response);
        await stream.WriteAsync(respBytes, cancellationTokenSource.Token);
    });
}
