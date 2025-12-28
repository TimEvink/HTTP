using System.IO;

using MyHttp.Core.Messages;

namespace MyHttp.Core.Serialization;
public abstract class HttpResponseSerializer : HttpMessageSerializer{
    protected HttpResponseSerializer(Stream stream) : base(stream) { }

    protected void SerializeResponseLine(
        HttpVersion version,
        int statuscode,
        string message
    ) {
        _writer.WriteLine($"HTTP/{version.Major}.{version.Minor} {statuscode} {message}");
    }

    public void Serialize(HttpResponse response) {
        SerializeResponseLine(response.Version, response.StatusCode, response.Message);
        SerializeHeaders(response.Headers);
        response.Body?.CopyTo(_writer.BaseStream);
        _writer.Flush();
    }
}