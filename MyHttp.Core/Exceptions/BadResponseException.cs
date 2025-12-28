using System;

namespace MyHttp.Core.Exceptions;
public sealed class BadResponseException : Exception {
    public BadResponseException(string? message = null) : base(message ?? "Malformed HTTP response") { }
}