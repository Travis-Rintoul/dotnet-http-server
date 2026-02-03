using System.Buffers;
using System.Net.Sockets;
using System.Text;
using FluentResults;
using HttpServer.Core.Constants;
using HttpServer.Core.Errors;
using HttpServer.Core.Models;

namespace HttpServer.Core.Protocol;

public class HttpRequestParser(HttpLimits limits)
{
    public Result<HttpRequestHead> ParseHeader(ReadOnlySpan<byte> headerBytes)
    {
        string text = Encoding.ASCII.GetString(headerBytes);
        var lines = text.Split("\r\n", StringSplitOptions.None);

        if (lines.Length == 0)
            return Result.Fail(new InvalidRequestError("Empty request."));

        // Request line
        string requestLine = lines[0];

        if (requestLine.Length > limits.MaxRequestLineBytes)
            return Result.Fail(new InvalidRequestError("Request line too long."));

        var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return Result.Fail(new InvalidRequestError("Invalid request line."));

        string method = parts[0];
        string target = parts[1];
        string version = parts[2];

        if (!version.Equals("HTTP/1.1", StringComparison.Ordinal))
            return Result.Fail(new InvalidRequestError("Unsupported HTTP version."));

        // Headers
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int headerCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.Length == 0)
                break;

            headerCount++;
            if (headerCount > limits.MaxHeaderCount)
                return Result.Fail(new InvalidRequestError("Too many headers."));

            int colon = line.IndexOf(':');
            if (colon <= 0)
                return Result.Fail(new InvalidRequestError($"Invalid header: '{line}'"));

            string name = line[..colon].Trim();
            string value = line[(colon + 1)..].Trim();

            headers[name] = value;
        }

        // Body semantics
        BodyDescriptor body;

        if (headers.TryGetValue(HeaderConstants.TransferEncoding, out var transferEncodingValue))
        {
            if (transferEncodingValue.Contains("chunked", StringComparison.OrdinalIgnoreCase))
                return Result.Fail(new InvalidRequestError("Chunked bodies not supported."));
        }

        if (headers.TryGetValue(HeaderConstants.ContentLength, out var contentLengthValue))
        {
            if (!long.TryParse(contentLengthValue, out var length) || length < 0)
                return Result.Fail(new InvalidRequestError("Invalid Content-Length."));

            if (length > limits.MaxBodyBytes)
                return Result.Fail(new InvalidRequestError("Request body too large."));

            body = length == 0
                ? new NoBody()
                : new ContentLengthBody(length);
        }
        else
        {
            body = new NoBody();
        }

        return Result.Ok(
            new HttpRequestHead(
                method,
                target,
                version,
                headers,
                body));
    }
}