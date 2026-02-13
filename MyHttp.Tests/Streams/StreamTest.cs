using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;


using MyHttp.Core.Connection;
using MyHttp.Core.Messages;
using System.Threading;

namespace MyHttp.Tests.Streams;

public class StreamsTest {
    static ReadOnlyMemory<byte> Mem(ReadOnlySpan<byte> span) => new ReadOnlyMemory<byte>(span.ToArray());
    static ReadOnlyMemory<byte> ToAsciiMemory(string s) => Encoding.ASCII.GetBytes(s);

    [Fact]
    public async Task HttpRequestSerializeParseTest() {
        //build a clientside http POST request
        string message = "Hi there Http!";

        HttpVersion version = new(1, 1);
        HttpRequestTarget target = new("/"u8.ToArray());
        Dictionary<ReadOnlyMemory<byte>,List<ReadOnlyMemory<byte>>> headersRaw = new(new ReadOnlyMemoryByteComparer());
        headersRaw.Add(Mem("Host"u8), new List<ReadOnlyMemory<byte>>(1) { Mem("localhost"u8) });
        headersRaw.Add(Mem("Content-Length"u8), new List<ReadOnlyMemory<byte>>(1) { ToAsciiMemory(message.Length.ToString()) });
        HttpHeaders headers = new(headersRaw);
        Stream body = new MemoryStream(Encoding.UTF8.GetBytes(message));
        HttpRequest requestin = new(HttpMethod.POST, target, version, headers, body);

        //mimic a NetworkStream from TCP connection using a MemoryStream.
        MemoryStream wireStream = new(1000);
        var clientConnection = new HttpClientConnection(wireStream);
        var serverConnection = new HttpServerConnection(wireStream);

        //serialize
        await clientConnection.SerializeRequestAsync(requestin, CancellationToken.None);

        //reset 'wireStream' to allow reading by server.
        wireStream.Position = 0;

        //parse
        HttpRequest requestout = await serverConnection.ParseRequestAsync(CancellationToken.None);

        //consume the body to retrieve message and compare with original.
        using (StreamReader reader = new(requestout.Body)) {
            string messageout = reader.ReadToEnd();
            Assert.Equal(message, messageout);
        }
    }
}

    // [Fact]
    // public void HttpResponseSerializeParseTest() {
    //     //build a serverside response.
    //     string messagein = "Hi there back, Http!";

    //     HttpVersion version = new(1, 1);
    //     int statuscode = 200;
    //     string message = "OK";
    //     Dictionary<string, string> headers = new();
    //     headers.Add("Content-Length", messagein.Length.ToString());
    //     Stream body = new MemoryStream(Encoding.UTF8.GetBytes(messagein));
    //     HttpResponse responsein = new(version, statuscode, message, headers, body);

    //     //mimic a NetworkStream from TCP connection using a MemoryStream.
    //     MemoryStream wireStream = new(100);

    //     //serialize
    //     HttpResponseSerializer serializer = new(wireStream);
    //     serializer.Serialize(responsein);

    //     //reset 'wireStream' to allow reading by client.
    //     wireStream.Position = 0;

    //     //parse
    //     HttpResponseParser parser = new(wireStream);
    //     HttpResponse responseout = parser.Parse();

    //     //consume the body to retrieve messageout and compare with original (messagein)
    //     using (StreamReader reader = new(responseout.Body)) {
    //         string messageout = reader.ReadToEnd();
    //         Assert.Equal(messagein, messageout);
    //     }
    // }

