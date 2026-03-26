using System;
using System.Threading;
using System.Threading.Tasks;

using MyHttp.Core.Server;
using MyHttp.Core.Messages;

namespace MyHttp.Server;
public static class Server {
	public static async Task Main(string[] args) {
		int port = args.Length != 0 && Int32.TryParse(args[0], out int result) ? result : 8000;

		HttpServer server = new(port, TestHandler, loggingEnabled: false);

		await server.RunAsync();
	}

	private static async Task<HttpResponse> TestHandler(HttpRequest request, CancellationToken cancellationToken = default) {
		await Task.CompletedTask;

		if (request.Target.RawUrl != "/")
			return HttpResponses.NotFound();

		return (request.Method == HttpMethod.GET)
			? HttpResponses.Ok("Hello from server!")
			: HttpResponses.BadRequest("Disappointment from server!");
	}
}
