using System;

using MyHttp.Core.Exceptions;
using MyHttp.Core.Messages;
using MyHttp.Core.Validation;

namespace MyHttp.Core.Validation;

public static class HttpRequestValidator{

    public static void ValidateHost(HttpRequest message) {
        if (!message.Headers.ContainsKey("Host")) {
            throw new BadRequestException("required header name missing: Host");
        }
    }

    public static void Validate(HttpRequest message) {
    HttpMessageValidator.ValidateVersion(message);
        ValidateHost(message);
    }

}