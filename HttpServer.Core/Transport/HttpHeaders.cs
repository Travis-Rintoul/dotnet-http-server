using System.Text;

namespace HttpServer.Core.Transport;

public sealed record HttpHeaders(IReadOnlyDictionary<string, string> Cache)
{
    public string? Host => Get("Host");

    public int? ContentLength
    {
        get
        {
            var value = Get("Content-Length");

            if (value is null)
                return null;

            return int.TryParse(value, out var parsed)
                ? parsed
                : null;
        }
    }

    public bool TransferEncodingChunked =>
        Get("Transfer-Encoding")?
            .Contains("chunked", StringComparison.OrdinalIgnoreCase) == true;

    public bool ConnectionKeepAlive =>
        Get("Connection")?
            .Contains("keep-alive", StringComparison.OrdinalIgnoreCase) == true;

    public bool ConnectionClose =>
        Get("Connection")?
            .Contains("close", StringComparison.OrdinalIgnoreCase) == true;

    public bool HasContentLength => ContentLength.HasValue;

    public bool HasBody =>
        TransferEncodingChunked || (ContentLength is > 0);

    public bool IsChunked => TransferEncodingChunked;

    public string? Get(string name) =>
        Cache.GetValueOrDefault(name);

    public bool TryGet(string name, out string value) =>
        Cache.TryGetValue(name, out value!);

    public bool Contains(string name) =>
        Cache.ContainsKey(name);

    public override string ToString()
    {
        var builder = new StringBuilder();

        foreach (var header in Cache)
        {
            builder.Append(header.Key)
                .Append(": ")
                .Append(header.Value)
                .Append("\r\n");
        }

        return builder.ToString();
    }

    public static HttpHeaders Empty { get; } =
        new(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
}
