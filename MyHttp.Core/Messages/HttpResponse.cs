using System.IO;
using System.Collections.Generic;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Messages;
public sealed class HttpResponse : HttpMessage {
    public int StatusCode { get; }
    public string Message { get; }
    public HttpResponse(
        HttpVersion version,
        int statuscode,
        string message,
        Dictionary<string, string> headers,
        Stream body
    ) : base(version, headers, body) {
        StatusCode = statuscode;
        Message = message;
    }
}