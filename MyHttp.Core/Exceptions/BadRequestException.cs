using System;

namespace MyHttp.Core.Exceptions;
public sealed class BadRequestException : BadMessageException {
    public BadRequestException(string? message = null) : base(message ?? "Malformed HTTP request") { }
}