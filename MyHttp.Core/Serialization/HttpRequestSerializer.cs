using System;
using System.IO;
using System.Collections.Generic;

using MyHttp.Core.Messages;
using MyHttp.Core.Utilities;

namespace MyHttp.Core.Serialization;
public sealed class HttpRequestSerializer : HttpMessageSerializer{
    public HttpRequestSerializer(Stream stream) : base(stream) { }

    private void SerializeRequestLine(HttpMethod method, HttpRequestTarget target, HttpVersion version) {
        WriteAscii($"{method.ToString()} {target.RawPath} HTTP/{version.Major}.{version.Minor}\r\n");
    }

    public void Serialize(HttpRequest request) {
        SerializeRequestLine(request.Method, request.Target, request.Version);
        SerializeHeaders(request.Headers);
        if (Helpers.ShouldConsiderBody(request.Headers)) {
            SerializeBody(request.Headers, request.Body);            
        }
    }
}