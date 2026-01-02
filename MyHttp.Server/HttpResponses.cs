using System.Collections.Generic;
using System.IO;
using System.Text;

using MyHttp.Core.Messages;

namespace MyHttp.Server;
public static class HttpResponses {
    public static HttpResponse Ok(string message) {
        HttpVersion version = new(1, 1);
        int statuscode = 200;
        string statusmessage = "OK";
        Dictionary<string, string> headers = new();
        headers.Add("Content-Length", message.Length.ToString());
        Stream body = new MemoryStream(Encoding.UTF8.GetBytes(message));
        return new HttpResponse(version, statuscode, statusmessage, headers, body);
    }

    public static HttpResponse BadRequest(string message) {
        HttpVersion version = new(1, 1);
        int statuscode = 400;
        string statusmessage = "BAD REQUEST";
        Dictionary<string, string> headers = new();
        headers.Add("Content-Length", message.Length.ToString());
        Stream body = new MemoryStream(Encoding.UTF8.GetBytes(message));
        return new HttpResponse(version, statuscode, statusmessage, headers, body);
    }
}