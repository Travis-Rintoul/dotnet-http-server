namespace HttpServer.Core.Models;

public sealed class HttpLimits
{
    public int MaxHeaderBytes { get; init; } = 32 * 1024;
    public int MaxRequestLineBytes { get; init; } = 8 * 1024;
    public int MaxHeaderCount { get; init; } = 100;
    public int MaxBodyBytes { get; init; } = 10 * 1024 * 1024;
    public TimeSpan HeaderReadTimeout { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan BodyReadTimeout { get; init; } = TimeSpan.FromSeconds(15);
}
