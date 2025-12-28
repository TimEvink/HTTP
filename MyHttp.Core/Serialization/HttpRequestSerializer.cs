using System.IO;

using MyHttp.Core.Messages;

namespace MyHttp.Core.Serialization;
public abstract class HttpRequestSerializer : HttpMessageSerializer{
    protected HttpRequestSerializer(Stream stream) : base(stream) { }

    protected void SerializeRequestLine(
        HttpMethod method,
        HttpRequestTarget target,
        HttpVersion version
    ) {
        Writer.WriteLine($"{method.ToString()} {target.Raw} HTTP/{version.Major}.{version.Minor}");
    }

    public void Serialize(HttpRequest request) {
        SerializeRequestLine(request.Method, request.Target, request.Version);
        SerializeHeaders(request.Headers);
        request.Body?.CopyTo(Writer.BaseStream);
        Writer.Flush();
    }
}