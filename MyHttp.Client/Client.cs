using System;
using System.IO;
using System.Text;

using MyHttp.Core.Client;
using System.Threading.Tasks;

namespace MyHttp.Client;
static class Client {
	static async Task Main(string[] args) {
		int port = args.Length != 0 && Int32.TryParse(args[0], out int result) ? result : 8000;

		await using var client = new HttpClient("127.0.0.1", port);

		var request = Requests.Get("/");

		var response = await client.SendRequestAsync(request);

		using StreamReader reader = new(response.Body, Encoding.UTF8);
		string body = await reader.ReadToEndAsync();

		Console.WriteLine($"HTTP Request received: {response.StatusCode} {response.Message}");
		Console.WriteLine($"Body: {body}");
	}
}
