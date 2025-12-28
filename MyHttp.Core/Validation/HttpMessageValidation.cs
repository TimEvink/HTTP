
using MyHttp.Core.Exceptions;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Validation;

public abstract class HttpMessageValidator {
    public HttpMessageValidator() { }

    public void ValidateVersion(HttpMessage message) {
        if (message.Version != new HttpVersion(1, 1)) {
            throw new BadRequestException("Only Http version 1.1 is supported");
        }
    }

}




    // public void ValidateBodyIndicators(HttpMessage message) {
    //     bool hascontentlength = message.Headers.ContainsKey("Content-Length");
    //     bool haschunkencoding = message.Headers.ContainsKey("Transfer-Encoding");
    //     if (message.)
    //     try {
    //         int n = int.Parse(message.Headers["Content-Length"]);
    //         if (n <= 0) {
    //             throw new BadRequestException("");
    //         }
    //     } catch {
    //         throw new BadRequestException("Error parsing Content-Length value");
    //     }
    // }