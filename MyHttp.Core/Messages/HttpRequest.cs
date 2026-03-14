using System.IO;

namespace MyHttp.Core.Messages;
public sealed class HttpRequest : HttpMessage {
    public HttpMethod Method { get; }
    public HttpRequestTarget Target { get; }

    public HttpRequest(
        HttpMethod method,
        HttpRequestTarget target,
        HttpVersion version,
        HttpHeaders headers,
        Stream body
    ) : base(version, headers, body) {
        Method = method;
        Target = target;
    }
}
