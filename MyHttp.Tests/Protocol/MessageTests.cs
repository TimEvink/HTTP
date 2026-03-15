using System.IO;
using System.Threading.Tasks;

using MyHttp.Core.Messages;
using MyHttp.Core.Builders;
using MyHttp.Tests.TcpStreamMock;

namespace MyHttp.Tests.Protocol;
public class MessageTests {
	[Theory]
	[InlineData("/")]
	[InlineData("/index.html")]
	[InlineData("/home?user=Carl%20Friedrich%20Gauss")]
	public async Task HttpGetRequestSerializeParseTest(string path) {
		var (clientConnection, serverConnection) = InMemoryDuplex.GetConnections();

		HttpRequest requestIn = new HttpRequestBuilder(HttpMethod.GET, path)
			.WithHeader("Accept", "text/plain")
			.Build();

		//serialize
		await clientConnection.SerializeRequestAsync(requestIn);

		//parse
		HttpRequest requestOut = await serverConnection.ParseRequestAsync();

		var name = requestOut.Headers.TryGetValue("Accept", out var value);
		Assert.True(name);
		Assert.NotNull(value);
		Assert.Single(value);
		Assert.Equal("text/plain", value[0]);
	}

	[Fact]
	public async Task HttpPostRequestSerializeParseTest() {
		var (clientConnection, serverConnection) = InMemoryDuplex.GetConnections();

		string messageIn = "Hi there Http!";
		HttpRequest requestIn = new HttpRequestBuilder(HttpMethod.POST, "/")
			.WithHeader("Host", "localhost")
			.WithBody(messageIn)
			.Build();

		//serialize
		await clientConnection.SerializeRequestAsync(requestIn);

		//parse
		HttpRequest requestOut = await serverConnection.ParseRequestAsync();

		//consume the body to retrieve message and compare with original.
		using StreamReader reader = new(requestOut.Body);
		string messageOut = await reader.ReadToEndAsync();
		Assert.Equal(messageIn, messageOut);
	}

	[Fact]
	public async Task HttpResponseSerializeParseTest() {
		var (clientConnection, serverConnection) = InMemoryDuplex.GetConnections();

		string messageIn = "Hi there back, Http!";
		HttpResponse responseIn = new HttpResponseBuilder(200, "OK")
			.WithBody(messageIn)
			.Build();

		//serialize
		await serverConnection.SerializeResponseAsync(responseIn);

		//parse
		HttpResponse responseOut = await clientConnection.ParseResponseAsync();

		//consume the body to retrieve messageOut and compare with original (messageIn)
		using StreamReader reader = new(responseOut.Body);
		string messageOut = await reader.ReadToEndAsync();
		Assert.Equal(messageIn, messageOut);
	}

	[Fact]
	public async Task HttpPostRequestSerializeParseMultipleTest() {
		var (clientConnection, serverConnection) = InMemoryDuplex.GetConnections();

		string[] messagesIn = ["Hi from first message", "Hi from second message", ""];

		//serialize
		foreach (var messageIn in messagesIn) {
			HttpRequest requestIn = new HttpRequestBuilder(HttpMethod.POST, "/")
				.WithBody(messageIn)
				.Build();
			await clientConnection.SerializeRequestAsync(requestIn, false);
		}
		await clientConnection.FlushOutputAsync();

		//parse
		foreach (var messageIn in messagesIn) {
			HttpRequest requestOut = await serverConnection.ParseRequestAsync();
			using StreamReader reader = new(requestOut.Body);
			string messageOut = await reader.ReadToEndAsync();
			Assert.Equal(messageIn, messageOut);
		}
	}
	[Fact]
	public async Task HttpResponseSerializeParseMultipleTest() {
		var (clientConnection, serverConnection) = InMemoryDuplex.GetConnections();

		string[] messagesIn = ["Hi back from first message", "Hi back from second message", ""];

		//serialize
		foreach (var messageIn in messagesIn) {
			HttpResponse responseIn = new HttpResponseBuilder(200, "OK")
				.WithBody(messageIn)
				.Build();
			await serverConnection.SerializeResponseAsync(responseIn);
		}

		//parse
		foreach (var messageIn in messagesIn) {
			HttpResponse responseOut = await clientConnection.ParseResponseAsync();
			using StreamReader reader = new(responseOut.Body);
			string messageOut = await reader.ReadToEndAsync();
			Assert.Equal(messageIn, messageOut);
		}
	}
}
