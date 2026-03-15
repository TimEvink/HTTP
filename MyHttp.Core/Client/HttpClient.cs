using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using MyHttp.Core.Connection;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Client;
public sealed class HttpClient : IAsyncDisposable {
	private readonly HttpClientConnection _connection;

	public HttpClient(string hostname, int port, HttpConnectionOptions? options = null) {
		TcpClient client = new(hostname, port);
		_connection = new(client.GetStream(), options ?? HttpConnectionOptions.Default);
	}

	public async Task<HttpResponse> SendRequestAsync(HttpRequest request, CancellationToken cancellationToken) {
		await _connection.SerializeRequestAsync(request, true, cancellationToken).ConfigureAwait(false);
		return await _connection.ParseResponseAsync(cancellationToken).ConfigureAwait(false);
	}

	public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
