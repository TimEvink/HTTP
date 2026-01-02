using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

using MyHttp.Core.Messages;
using MyHttp.Core.Parsing;
using MyHttp.Core.Serialization;
using MyHttp.Server;


namespace MyHttp.Server;
static class Program {
    static void Main(string[] args) {
        int port = args.Length != 0 && Int32.TryParse(args[0], out int result) ? result : 8000; 
        TcpListener server = new(IPAddress.Loopback, port);
        server.Start();
        Console.WriteLine("Starting server.");

        while (true) {
            using TcpClient client = server.AcceptTcpClient();
            Console.WriteLine($"connected with {client.Client}");
            using NetworkStream stream = client.GetStream();

            HttpRequest request = new HttpRequestParser(stream).Parse();
            HttpResponse response = Handler.Handle(request);

            Console.WriteLine("Sending response");
            new HttpResponseSerializer(stream).Serialize(response);
        }
    }
}
