using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;

using MyHttp.Client;
using MyHttp.Core.Messages;
using MyHttp.Core.Parsing;
using MyHttp.Core.Serialization;

namespace MyHttp.Client;
static class Program {
    static void Main(string[] args) {
        int port = Int32.TryParse(args[0], out int result) ? result : 8000;

        using TcpClient client = new TcpClient("127.0.0.1", port);
        using NetworkStream stream = client.GetStream();

        //example request
        HttpRequest request = Requests.DefaultGet();

        //send request
        new HttpRequestSerializer(stream).Serialize(request);

        //read response
        HttpResponse response = new HttpResponseParser(stream).Parse();

        using StreamReader reader = new(response.Body, Encoding.UTF8);
        string body = reader.ReadToEnd();

        Console.WriteLine($"Http Request recieved: {response.StatusCode} {response.Message}");
        Console.WriteLine($"Body: {body}");
    }
}