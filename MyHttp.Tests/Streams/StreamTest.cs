using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MyHttp.Core.Messages;
using MyHttp.Core.Parsing;
using MyHttp.Core.Serialization;

namespace MyHttp.Tests.Streams;

public class StreamsTest {
    [Fact]
    public void HttpRequestSerializeParseTest() {
        //build a clientside http POST request
        string message = "Hi there Http!";

        HttpVersion version = new(1, 1);
        HttpRequestTarget target = new("/");
        Dictionary<string, string> headers = new();
        headers.Add("Host", "localhost");
        headers.Add("Content-Length", message.Length.ToString());
        Stream body = new MemoryStream(Encoding.UTF8.GetBytes(message));
        HttpRequest requestin = new(HttpMethod.POST, target, version, headers, body);

        //mimic a NetworkStream from TCP connection using a MemoryStream.
        MemoryStream wireStream = new(100);

        //serialize
        HttpRequestSerializer serializer = new(wireStream);
        serializer.Serialize(requestin);

        //reset 'wireStream' to allow reading by server.
        wireStream.Position = 0;

        //parse
        HttpRequestParser parser = new(wireStream);
        HttpRequest requestout = parser.Parse();

        //consume the body to retrieve message and compare with original.
        using (StreamReader reader = new(requestout.Body)) {
            string messageout = reader.ReadToEnd();
            Assert.Equal(message, messageout);
        }
    }

    [Fact]
    public void HttpResponseSerializeParseTest() {
        //build a serverside response.
        string messagein = "Hi there back, Http!";

        HttpVersion version = new(1, 1);
        int statuscode = 200;
        string message = "OK";
        Dictionary<string, string> headers = new();
        headers.Add("Content-Length", messagein.Length.ToString());
        Stream body = new MemoryStream(Encoding.UTF8.GetBytes(messagein));
        HttpResponse responsein = new(version, statuscode, message, headers, body);

        //mimic a NetworkStream from TCP connection using a MemoryStream.
        MemoryStream wireStream = new(100);

        //serialize
        HttpResponseSerializer serializer = new(wireStream);
        serializer.Serialize(responsein);

        //reset 'wireStream' to allow reading by client.
        wireStream.Position = 0;

        //parse
        HttpResponseParser parser = new(wireStream);
        HttpResponse responseout = parser.Parse();

        //consume the body to retrieve messageout and compare with original (messagein)
        using (StreamReader reader = new(responseout.Body)) {
            string messageout = reader.ReadToEnd();
            Assert.Equal(messagein, messageout);
        }
    }
}
