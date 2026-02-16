using System.Net.Sockets;
using System.Text;
using HttpServer.Core.Transport;

namespace HttpServer.Core.Protocol;

public class HttpResponseWriter : IHttpResponseWriter
{
    public async Task WriteAsync(NetworkStream stream, HttpResponse response, CancellationToken cancellationToken)
    {
        Console.WriteLine("Implement HttpResponseWriter.WriteAsync");
        
        var responseString =
             "HTTP/1.1 200 OK\r\n" +
             "Content-Length: 2\r\n" +
             "Connection: close\r\n" +
             "\r\n" +
             "OK";

         var respBytes = Encoding.ASCII.GetBytes(responseString);
         await stream.WriteAsync(respBytes, cancellationToken);
    }
}