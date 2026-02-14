using System.IO;

using MyHttp.Core.Messages;
using MyHttp.Core.Utilities;

namespace MyHttp.Core.Serialization;
public sealed class HttpResponseSerializer : HttpMessageSerializer{
    public HttpResponseSerializer(Stream stream) : base(stream) { }

    private void SerializeResponseLine(HttpVersion version, int statuscode, string message) {
        WriteAscii($"HTTP/{version.Major}.{version.Minor} {statuscode} {message}\r\n");
    }

    public void Serialize(HttpResponse response) {
        SerializeResponseLine(response.Version, response.StatusCode, response.Message);
        SerializeHeaders(response.Headers);
        if (Helpers.ShouldConsiderBody(response.Headers)) {
            SerializeBody(response.Headers, response.Body);            
        }
    }
}