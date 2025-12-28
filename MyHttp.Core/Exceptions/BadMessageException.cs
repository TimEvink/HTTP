using System;

namespace MyHttp.Core.Exceptions;
public class BadMessageException : Exception {
    public BadMessageException(string? message = null) : base(message ?? "Malformed HTTP message.") { }
}