using System.IO;
using System.Collections.Generic;

using MyHttp.Core.Messages;

namespace MyHttp.Client;
public static class Requests {
    public static HttpRequest DefaultGet() {
        HttpVersion version = new(1, 1);
        HttpRequestTarget target = new("/");
        Dictionary<string, string> headers = new();
        headers.Add("Host", "localhost");
        return new HttpRequest(HttpMethod.GET, target, version, headers, Stream.Null);
    }
}