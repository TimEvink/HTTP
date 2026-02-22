using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using MyHttp.Core.Connection;
using MyHttp.Core.Messages;


namespace MyHttp.Server;
static class Program {
    static async Task Main(string[] args) {
        int port = args.Length != 0 && Int32.TryParse(args[0], out int result) ? result : 8000;

		TcpListener server = new(IPAddress.Loopback, port);
		server.Start();
		Console.WriteLine("Starting server.");

		while (true) {
			TcpClient client = await server.AcceptTcpClientAsync();
			_ = HandleClientAsync(client);
		}
	}

	static async Task HandleClientAsync(TcpClient client) {
		try {
			using (client)
			using (NetworkStream stream = client.GetStream()) {
				IPEndPoint? remoteEndPoint = (IPEndPoint?)client.Client.RemoteEndPoint;
				if (remoteEndPoint == null) {
					Console.WriteLine($"Connected client has no endpoint, breaking connection");
					return;
				}
				Console.WriteLine($"Connected to: {remoteEndPoint.Address}:{remoteEndPoint.Port}");

				HttpServerConnection connection = new(stream);

				//parse request
				HttpRequest request = await connection.ParseRequestAsync();

				HttpResponse response = Handler.Handle(request);

				//serialize response
				Console.WriteLine("Sending response");
				await connection.SerializeResponseAsync(response);
				await connection.FlushOutputAsync();

				//close connection
			}
		} catch (Exception exception) {
			Console.WriteLine($"Client error: {exception}");
		}
	}
}       
