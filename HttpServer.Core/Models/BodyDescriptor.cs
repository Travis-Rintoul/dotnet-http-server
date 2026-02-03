namespace HttpServer.Core.Models;

public abstract record BodyDescriptor;

public sealed record NoBody() : BodyDescriptor;

public sealed record ContentLengthBody(long Length) : BodyDescriptor;

public sealed record ChunkedBody() : BodyDescriptor;
