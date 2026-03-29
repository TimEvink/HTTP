using System.IO;

namespace MyHttp.Core.Messages;
public sealed class HttpResponse : HttpMessage {
    public HttpStatusCode StatusCode { get; }
    public HttpReason Message { get; }
    public HttpResponse(
        HttpVersion version,
        HttpStatusCode statuscode,
        HttpReason message,
        HttpHeaders headers,
        Stream body
    ) : base(version, headers, body) {
        StatusCode = statuscode;
        Message = message;
    }
}
