using System;

using MyHttp.Core.Exceptions;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Validation;

public abstract class HttpRequestValidator : HttpMessageValidator {
    public HttpRequestValidator() {}

    public void ValidateHost(HttpRequest message) {
        if (!message.Headers.ContainsKey("Host")) {
            throw new BadRequestException("required header name missing: Host");
        }
    }

    public void Validate(HttpRequest message) {
        ValidateVersion(message);
        ValidateHost(message);
    }

}