using System;
using System.Threading;
using System.Threading.Tasks;

using MyHttp.Core.Server;
using MyHttp.Core.Messages;

namespace MyHttp.Server;
static class Server {
	static async Task Main(string[] args) {
		int port = args.Length != 0 && Int32.TryParse(args[0], out int result) ? result : 8000;

		HttpServer server = new(port, TestHandler);

		server.Start();
		await server.ServeClients();
	}

	private static async Task<HttpResponse> TestHandler(HttpRequest request, CancellationToken cancellationToken = default) {
		await ValueTask.CompletedTask;
		return (request.Method == HttpMethod.GET && request.Target.RawUrl == "/")
			? HttpResponses.Ok("Hello from server!")
			: HttpResponses.BadRequest("Disappointment from server!");
	}
}
