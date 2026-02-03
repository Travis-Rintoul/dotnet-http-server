using FluentResults;

namespace HttpServer.Core.Errors;

public abstract class HttpReadError(string message) : Error(message);

public sealed class ClientDisconnectedError() : HttpReadError("Client disconnected.");

public sealed class HeaderTooLargeError(int maxBytes) : HttpReadError($"Request headers exceed {maxBytes} bytes.");

public sealed class InvalidRequestError(string detail) : HttpReadError(detail);