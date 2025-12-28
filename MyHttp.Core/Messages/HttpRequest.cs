using System.IO;
using System.Collections.Generic;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Messages;
public sealed class HttpRequest : HttpMessage {
    public HttpMethod Method { get; }
    public HttpRequestTarget Target { get; }

    public HttpRequest(
        HttpMethod method,
        HttpRequestTarget target,
        HttpVersion version,
        Dictionary<string, string> headers,
        Stream body
    ) : base(version, headers, body){
        Method = method;
        Target = target;
    }
}