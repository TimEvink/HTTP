using System.IO;
using System.IO.Pipelines;

namespace MyHttp.Tests.TcpStreamMock;
internal static class InMemoryDuplex {
	internal static (Stream client, Stream server) Create() {
		var serverToClient = new Pipe();
		var clientToServer = new Pipe();

		var clientStream = new DuplexStream(
			serverToClient.Reader.AsStream(),
			clientToServer.Writer.AsStream()
		);
		var serverStream = new DuplexStream(
			clientToServer.Reader.AsStream(),
			serverToClient.Writer.AsStream()
		);

		return (clientStream, serverStream);
	}
}

