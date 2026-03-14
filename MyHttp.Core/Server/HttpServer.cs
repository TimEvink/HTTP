using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MyHttp.Core.Connection;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Server;
public class HttpServer : IDisposable {
	private readonly int _port;
	private readonly TcpListener _server;
	private readonly Func<HttpRequest, CancellationToken, Task<HttpResponse>> _requestHandler;
	private readonly Func<Exception, HttpResponse> _errorHandler;
	private readonly HttpConnectionOptions _options;

	public HttpServer(
		int port,
		Func<HttpRequest, CancellationToken, Task<HttpResponse>> requestHandler,
		Func<Exception, HttpResponse>? errorHandler = null,
		HttpConnectionOptions? options = null
	) {
		//TODO: validate port;
		_port = port;
		_server = new(IPAddress.Loopback, port);
		_requestHandler = requestHandler;
		_errorHandler = errorHandler ?? Responses.GetDefault500Response;
		_options = options ?? HttpConnectionOptions.Default;
	}

	public void Start() {
		_server.Start();
		Console.WriteLine("Starting server.");
		Console.WriteLine($"Listening on localhost:{_port}");
	}

	public async Task ServeClients(CancellationToken cancellationToken = default) {
		while (!cancellationToken.IsCancellationRequested) {
			try {
				TcpClient client = await AcceptTcpClientAsync(cancellationToken);
				_ = HandleTcpClient(client, cancellationToken);
			} catch (OperationCanceledException) {
				break;
			}
		}
	}

	public void Stop() {
		_server.Stop();
	}

	public void Dispose() {
		_server.Stop();
	}

	public ValueTask<TcpClient> AcceptTcpClientAsync(CancellationToken cancellationToken) {
		return _server.AcceptTcpClientAsync(cancellationToken);
	}

	private async Task HandleTcpClient(TcpClient client, CancellationToken cancellationToken) {
		try {
			using (client)
			using (NetworkStream stream = client.GetStream()) {
				if (client.Client.RemoteEndPoint is not IPEndPoint remoteEndPoint) {
					Console.WriteLine($"Connected client has no endpoint, breaking connection");
					return;
				}
				Console.WriteLine($"Connected to: {remoteEndPoint.Address}:{remoteEndPoint.Port}");

				await stream.HandleRequests(_requestHandler, _errorHandler, _options, cancellationToken);
			}
		} catch (Exception exception) {
			Console.Error.WriteLine($"Client error: {exception}");
		}
	}
}
