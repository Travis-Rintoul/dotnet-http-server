using System.Buffers;
using System.Net.Sockets;
using System.Text;
using static HttpServer.Core.Constants.HeaderConstants;

namespace HttpServer.Core.Protocol;

public class HttpRequestReader
{
    private const int MaxHeaderSize = 64 * 1024;
    private const int ReadBufferSize = 8 * 1024;
    
    public static async Task<(string startLine, Dictionary<string,string> headers, byte[] body)>  ReadAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);

        try
        {
            var headerEndIndex = -1;
            var received = new List<byte>(ReadBufferSize);

            // Read until terminator
            while (headerEndIndex < 0)
            {
                int result = await stream.ReadAsync(buffer.AsMemory(0, ReadBufferSize), cancellationToken);
                if (result == 0)
                {
                    throw new IOException("Client disconnected while reading headers.");
                }
                
                received.AddRange(buffer.AsSpan(0, result).ToArray());

                if (received.Count >= MaxHeaderSize)
                {\
                    throw new InvalidOperationException($"Headers too large (> {MaxHeaderSize} bytes).");
                }
                
                headerEndIndex = HttpRequestReader.IndexOfHeaderTerminator(received);
            } /* while (headerEndIndex < 0) */

            int headersLength = headerEndIndex + 4;
            byte[] headerBytes = received.GetRange(0, headersLength).ToArray();
            byte[] remainder = received.Count > headersLength
                ? received.GetRange(headersLength, received.Count - headersLength).ToArray()
                : Array.Empty<byte>();

            var headerText = Encoding.ASCII.GetString(headerBytes);
            var lines = headerText.Split("\r\n", StringSplitOptions.None);

            string startLine = lines[0];
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Length == 0)
                {
                    break;
                }

                int colon = line.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }
                
                string name = line[..colon].Trim();
                string value = line[(colon + 1)..].Trim();

                headers[name] = value;
            } /* for (int i = 1; i < lines.Length; i++) */
            
            int contentLength = 0;
            if (headers.TryGetValue(ContentLength, out var contentLenghtString) && 
                int.TryParse(contentLenghtString, out var contentLengthInt))
            {
                contentLength = contentLengthInt;
            }

            if (headers.TryGetValue(TransferEncoding, out var transferEncodingString) &&
                transferEncodingString.Contains("chunked", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("chunked request bodies not supported yet.");
            }
            
            byte[] body = new Byte[contentLength];
            int copied = 0;
            
            int take = Math.Min(remainder.Length, contentLength);
            if (take > 0)
            {
                Buffer.BlockCopy(remainder, 0, body, 0, take);
                copied += take;
            }

            while (copied < contentLength)
            {
                int result = await stream.ReadAsync(body.AsMemory(copied, contentLength - copied), cancellationToken);
                if (result == 0)
                {
                    throw new IOException("Client disconnected while reading body.");
                }
                
                copied += result;
            }
            
            return (startLine, headers, body);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    
    private static int IndexOfHeaderTerminator(List<byte> data)
    {
        // Find \r\n\r\n
        for (int i = 0; i <= data.Count - 4; i++)
        {
            if (data[i] == (byte)'\r' && data[i + 1] == (byte)'\n' &&
                data[i + 2] == (byte)'\r' && data[i + 3] == (byte)'\n')
                return i;
        }
        return -1;
    }
}