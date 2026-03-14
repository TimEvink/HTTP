using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MyHttp.Core.Builders;
using MyHttp.Core.Messages;
using MyHttp.Core.Connection;
using MyHttp.Core.Server;
using MyHttp.Tests.TcpStreamMock;

namespace MyHttp.Tests.RoundTripTests;
public class EchoTest {
	
	[Fact]
	public async Task ServerEchoTest() {
		var (clientStream, serverStream) = InMemoryDuplex.Create();

		//set up 'server'
		var cts = new CancellationTokenSource();
		var serverTask = HttpServerConnectionHandler.HandleRequests(
			serverStream,
			async (request, cancellationToken) => {
				using var reader = new StreamReader(request.Body);
				var body = await reader.ReadToEndAsync();
				return new HttpResponseBuilder(200, "OK")
					.WithHeader("Content-Type", "text/html; charset=utf-8")
					.WithHeader("Content-Length", body.Length.ToString())
					//.WithHeader("Date", DateTime.UtcNow.ToString("r"))
					.WithBody(body)
					.Build();
			},
			null,
			null,
			cts.Token
		);

		await Task.Yield();

		//set up client and send request
		HttpClientConnection clientConnection = new(clientStream);

		string body = "Hello server!";

		var request = new HttpRequestBuilder(HttpMethod.POST, "/")
			.WithHeader("Content-Type", "text/html; charset=utf-8")
			.WithHeader("Content-Length", body.Length.ToString())
			.WithBody(body)
			.Build();

		await clientConnection.SerializeRequestAsync(request);
		await clientConnection.FlushOutputAsync();
		var response = await clientConnection.ParseResponseAsync();

		var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
		Assert.Equal("Hello server!", responseBody);

		await clientConnection.DisposeAsync();
		await serverTask;
	}
}
