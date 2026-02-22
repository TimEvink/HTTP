using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;

using MyHttp.Client;
using MyHttp.Core.Messages;
using MyHttp.Core.Connection;
using System.Threading.Tasks;

namespace MyHttp.Client;
static class Program {
	static async Task Main(string[] args) {
		int port = args.Length != 0 && Int32.TryParse(args[0], out int result) ? result : 8000;

		using TcpClient client = new("127.0.0.1", port);
		using NetworkStream stream = client.GetStream();

		HttpClientConnection connection = new(stream);


		//example good request
		Console.WriteLine("Sending HTTP request...");
		HttpRequest request = Requests.Get();

		//send request
		await connection.SerializeRequestAsync(request);
		await connection.FlushOutputAsync();

		//read response
		HttpResponse response = await connection.ParseResponseAsync();


		using StreamReader reader = new(response.Body, Encoding.UTF8);
		string body = await reader.ReadToEndAsync();

		Console.WriteLine($"HTTP Request recieved: {response.StatusCode} {response.Message}");
		Console.WriteLine($"Body: {body}");
	}
}
