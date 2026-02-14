
using System;
using System.Collections.Generic;
using MyHttp.Core.Exceptions;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Validation;

public static class HttpMessageValidator {
    // static HttpMessageValidator() { }
    public static void ValidateVersion(HttpMessage message) {
        if (message.Version != new HttpVersion(1, 1)) {
            throw new BadMessageException("Only HTTP/1.1 is supported");
        }
    }

    //must only be called after another Validation step has verified that a body is present.
    public static void ValidateBodyIndicators(IReadOnlyDictionary<string,string> headers) {
        bool hasTransferEncoding = headers.TryGetValue("transfer-encoding", out string? transferValue);
        bool hasContentLength = headers.TryGetValue("content-length", out string? contentValue);
        if (hasTransferEncoding && hasContentLength) {
            throw new BadMessageException("Both Transfer-Encoding and Content-Length detected");
        } else if (hasTransferEncoding) {
            switch (transferValue?.ToLowerInvariant()) {
                case "chunked":
                    break;
                default:
                    throw new BadMessageException($"Transfer-Encoding: {transferValue} is not supported");
            }
        } else if (hasContentLength) {
            if (!Int64.TryParse(contentValue, out var contentlength)) {
                throw new BadMessageException($"Content-Length value ({contentValue}) is not a valid 64-bit integer");
            }
            if (contentlength < 0) {
                throw new BadMessageException($"Content-Length ({contentlength}) cannot be negative");
            }
        } else {
            throw new BadMessageException("No body indicators (Content-Length or Transfer-Encoding) found");
        }
    }
}