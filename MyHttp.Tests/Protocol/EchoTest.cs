using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MyHttp.Core.Builders;
using MyHttp.Core.Messages;
using MyHttp.Core.Server;
using MyHttp.Tests.TcpStreamMock;

namespace MyHttp.Tests.Protocol;
public class EchoTest {
	[Fact]
	public async Task ServerEchoTest() {
		var (clientConnection, serverConnection) = InMemoryDuplex.GetConnections();

		//set up 'server'
		var cts = new CancellationTokenSource();
		var serverTask = serverConnection.HandleRequests(
			async (request, cancellationToken) => {
				using var reader = new StreamReader(request.Body);
				var requestBody = await reader.ReadToEndAsync(cancellationToken);
				return new HttpResponseBuilder(200, "OK")
					.WithHeader("Content-Type", "text/plain; charset=utf-8")
					.WithBody(requestBody)
					.Build();
			},
			null,
			cts.Token
		);

		//ensure the serverTask code awaits a request parse
		await Task.Yield();

		//send request from client
		string body = "Hello server!";

		var request = new HttpRequestBuilder(HttpMethod.POST, "/")
			.WithHeader("Content-Type", "text/plain; charset=utf-8")
			.WithBody(body)
			.Build();

		await clientConnection.SerializeRequestAsync(request);
		var response = await clientConnection.ParseResponseAsync();

		var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
		Assert.Equal("Hello server!", responseBody);
	}
}
