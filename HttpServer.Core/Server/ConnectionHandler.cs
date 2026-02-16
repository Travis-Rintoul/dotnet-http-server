using System.Net.Sockets;
using HttpServer.Core.Models;
using HttpServer.Core.Protocol;
using HttpServer.Core.Transport;

namespace HttpServer.Core.Server;

public class ConnectionHandler(IHttpRequestReader reader, IHttpRequestParser parser, IHttpResponseWriter writer) : IConnectionHandler
{
    private readonly IHttpRequestReader _reader = reader;
    private readonly IHttpRequestParser _parser = parser;
    private readonly IHttpResponseWriter _writer = writer;
    
    public async Task HandleAsync(Socket socket, CancellationToken token)
    {
        using var stream = new NetworkStream(socket);

        while (!token.IsCancellationRequested)
        {
            // Read headers
            var frameResult = await reader.ReadHeaderAsync(stream, token);
            if (frameResult.IsFailed)
            {
                Console.WriteLine(string.Join(' ', frameResult.Errors));
                break;
            }

            HttpRequestFrame frame = frameResult.Value;

            // Parse headers
            var parseResult = parser.ParseHeader(frame.HeaderBytes);
            if (parseResult.IsFailed)
            {
                Console.WriteLine(string.Join(' ', parseResult.Errors));
                break;
            }

            HttpRequestHead requestHead = parseResult.Value;

            // Read body
            var bodyResult = await reader.ReadBodyAsync(
                stream,
                requestHead.Body,
                frame.RemainderBytes,
                token);

            if (bodyResult.IsFailed)
            {
                Console.WriteLine(string.Join(' ', bodyResult.Errors));
                break;
            }
            
            Console.WriteLine("Request Made");

            await _writer.WriteAsync(stream, new HttpResponse(), token);
            
            Console.WriteLine($"{requestHead.IsKeepAlive}");

            if (!requestHead.IsKeepAlive)
                break;
            
        }
    }
}