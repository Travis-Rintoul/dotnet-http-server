using System.Buffers;
using System.Net.Sockets;
using System.Text;
using FluentResults;
using HttpServer.Core.Errors;
using HttpServer.Core.Models;
using static HttpServer.Core.Constants.HeaderConstants;

namespace HttpServer.Core.Protocol;

public class HttpRequestReader(HttpLimits limits) : IHttpRequestReader
{
    private const int ReadBufferSize = 8 * 1024;

    public async Task<Result<HttpRequestFrame>> ReadHeaderAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        byte[] readBuffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
        byte[] headerBuffer = ArrayPool<byte>.Shared.Rent(limits.MaxHeaderBytes);

        int headerCount = 0;
        int matched = 0;

        try
        {
            while (true)
            {
                int read = await stream.ReadAsync(readBuffer.AsMemory(0, ReadBufferSize), cancellationToken);
                if (read == 0)
                    Result.Fail(new ClientDisconnectedError());

                for (int i = 0; i < read; i++)
                {
                    if (headerCount >= limits.MaxBodyBytes)
                        return Result.Fail(new HeaderTooLargeError(limits.MaxHeaderBytes));
                    
                    byte readByte = readBuffer[i];
                    headerBuffer[headerCount++] = readByte;
                    
                    matched = readByte switch
                    {
                        (byte)'\r' when matched is 0 or 2 => matched + 1,
                        (byte)'\n' when matched is 1 or 3 => matched + 1,
                        _ => 0
                    };

                    if (matched == 4)
                    {
                        var headers = headerBuffer[..headerCount].ToArray();
                        var remainder = read > i + 1
                            ? readBuffer[(i + 1)..read].ToArray()
                            : [];

                        return Result.Ok(new HttpRequestFrame(headers, remainder));
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
            ArrayPool<byte>.Shared.Return(headerBuffer);
        }
    }

    public async Task<Result<Stream>> ReadBodyAsync(
        NetworkStream stream,
        BodyDescriptor body,
        byte[] remainder,
        CancellationToken cancellationToken)
    {
        if (body is NoBody)
            return Result.Ok(Stream.Null);

        if (body is not ContentLengthBody contentLengthValue)
            return Result.Fail("Unsupported body type.");
        
        if (contentLengthValue.Length > limits.MaxBodyBytes)
            return Result.Fail("Request body is too large");
        
        var memoryStream = new MemoryStream((int)contentLengthValue.Length);

        int taken = Math.Min(remainder.Length, (int)contentLengthValue.Length);
        if (taken > 0)
            await memoryStream.WriteAsync(remainder.AsMemory(0, taken), cancellationToken);

        long remaining = contentLengthValue.Length - taken;
        while (remaining > 0)
        {
            int read = await stream.ReadAsync(memoryStream.GetBuffer()
                .AsMemory((int)memoryStream.Position, (int)remaining), cancellationToken);
            
            if (read == 0)
                return Result.Fail(new ClientDisconnectedError());
            
            memoryStream.Position += read;
            remaining -= read;
        }
        
        memoryStream.Position = 0;
        return Result.Ok<Stream>(memoryStream);
    }
}