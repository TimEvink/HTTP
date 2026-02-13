using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using MyHttp.Core.Messages;
using MyHttp.Server;


namespace MyHttp.Server;
static class Program {
    static void Main(string[] args) {
        int port = args.Length != 0 && Int32.TryParse(args[0], out int result) ? result : 8000;
        Console.WriteLine("wip: closing server.");
    }
}       
        // TcpListener server = new(IPAddress.Loopback, port);
        // server.Start();
        // Console.WriteLine("Starting server.");

        // while (true) {
        //     using TcpClient client = server.AcceptTcpClient();
        //     IPEndPoint? remoteEndPoint = (IPEndPoint?)client.Client.RemoteEndPoint;
        //     if (remoteEndPoint == null) {
        //         Console.WriteLine($"Connected client has no endpoint, breaking connection");
        //         continue;
        //     }
        //     Console.WriteLine($"Connected to: {remoteEndPoint.Address}:{remoteEndPoint.Port}");
        //     using NetworkStream stream = client.GetStream();

        //     //parse request
        //     HttpRequest request = new HttpRequestParser(stream).Parse();
        //     HttpResponse response = Handler.Handle(request);

        //     //serialize response
        //     Console.WriteLine("Sending response");
        //     new HttpResponseSerializer(stream).Serialize(response);
        // }
