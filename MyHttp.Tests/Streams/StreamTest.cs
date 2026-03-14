using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

using MyHttp.Core.Connection;
using MyHttp.Core.Messages;

namespace MyHttp.Tests.Streams;
public class StreamsTest {

	private readonly ReadOnlyMemoryByteComparer _comparer = new();

	[Fact]
	public async Task HttpRequestSerializeParseTest() {
		//build a simple clientside POST request
		string messageIn = "Hi there Http!";

		HttpMethod method = HttpMethod.POST;
		HttpRequestTarget target = new("/"u8.ToArray());
		HttpVersion version = new(1, 1);
		Dictionary<ReadOnlyMemory<byte>, List<ReadOnlyMemory<byte>>> headersRaw = new(_comparer) {
			{ "Host"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { "localhost"u8.ToArray() } },
			{ "Content-Length"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { Encoding.ASCII.GetBytes(messageIn.Length.ToString()) } }
		};
		HttpHeaders headers = new(headersRaw);
		Stream body = new MemoryStream(Encoding.ASCII.GetBytes(messageIn));

		HttpRequest requestIn = new(method, target, version, headers, body);

		//mimic a NetworkStream from TCP connection using a MemoryStream.
		MemoryStream wireStream = new(1000);
		var clientConnection = new HttpClientConnection(wireStream, HttpConnectionOptions.Default);
		var serverConnection = new HttpServerConnection(wireStream, HttpConnectionOptions.Default);

		//serialize
		await clientConnection.SerializeRequestAsync(requestIn);
		await clientConnection.FlushOutputAsync();

		//reset wireStream to allow reading by server.
		wireStream.Position = 0;

		//parse
		HttpRequest requestOut = await serverConnection.ParseRequestAsync();

		//consume the body to retrieve message and compare with original.
		using StreamReader reader = new(requestOut.Body);
		string messageOut = await reader.ReadToEndAsync();
		Assert.Equal(messageIn, messageOut);
	}


	[Fact]
	public async Task HttpResponseSerializeParseTest() {
		//build a serverside response.
		string messageIn = "Hi there back, Http!";

		HttpVersion version = new(1, 1);
		HttpStatusCode statusCode = new((byte)'2', (byte)'0', (byte)'0');
		HttpReason reason = new("OK");
		Dictionary<ReadOnlyMemory<byte>, List<ReadOnlyMemory<byte>>> headersRaw = new(_comparer) {
			{ "Content-Length"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { Encoding.ASCII.GetBytes(messageIn.Length.ToString()) } }
		};
		HttpHeaders headers = new(headersRaw);
		Stream body = new MemoryStream(Encoding.ASCII.GetBytes(messageIn));

		HttpResponse responseIn = new(version, statusCode, reason, headers, body);

		//mimic a NetworkStream from TCP connection using a MemoryStream.
		MemoryStream wireStream = new(1000);
		var clientConnection = new HttpClientConnection(wireStream, HttpConnectionOptions.Default);
		var serverConnection = new HttpServerConnection(wireStream, HttpConnectionOptions.Default);

		//serialize
		await serverConnection.SerializeResponseAsync(responseIn);
		await serverConnection.FlushOutputAsync();

		//reset wireStream to allow reading by server.
		wireStream.Position = 0;

		//parse
		HttpResponse responseOut = await clientConnection.ParseResponseAsync();

		//consume the body to retrieve messageOut and compare with original (messageIn)
		using StreamReader reader = new(responseOut.Body);
		string messageOut = await reader.ReadToEndAsync();
		Assert.Equal(messageIn, messageOut);
	}
}
