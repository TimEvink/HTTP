using System;
using System.Collections.Generic;
using MyHttp.Core.Exceptions;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Validation;

public static class HttpMessageValidator {
    // static HttpMessageValidator() { }
    public static void ValidateVersion(HttpMessage message) {
        if (!(message.Version.Major == 1 && message.Version.Minor == 1)) {
            throw new BadMessageException("Only HTTP/1.1 is supported");
        }
    }
}
