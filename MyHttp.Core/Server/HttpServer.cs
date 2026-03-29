using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MyHttp.Core.Connection;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Server;
public sealed class HttpServer : IDisposable {
	private readonly int _port;
	private readonly TcpListener _server;
	private readonly Func<HttpRequest, HttpResponse>? _requestHandler = null;
	private readonly Func<HttpRequest, CancellationToken, Task<HttpResponse>>? _requestHandlerAsync = null;
	private readonly Func<Exception, HttpResponse> _errorHandler;
	private readonly HttpConnectionOptions _options;
	private readonly bool _loggingEnabled;

	private readonly HashSet<Task> _connections = [];
	private readonly Lock _lock = new();

	public HttpServer(
		int port,
		Func<HttpRequest, HttpResponse> requestHandler,
		Func<Exception, HttpResponse>? errorHandler = null,
		bool loggingEnabled = true,
		HttpConnectionOptions? options = null
	) {
		//TODO: validate port;
		_port = port;
		_server = new(IPAddress.Any, port);
		_requestHandler = requestHandler;
		_errorHandler = errorHandler ??= _ => Responses.GetDefault500Response();
		_options = options ?? HttpConnectionOptions.Default;
		_loggingEnabled = loggingEnabled;
	}

	public HttpServer(
		int port,
		Func<HttpRequest, CancellationToken, Task<HttpResponse>> requestHandlerAsync,
		Func<Exception, HttpResponse>? errorHandler = null,
		bool loggingEnabled = true,
		HttpConnectionOptions? options = null
	) {
		//TODO: validate port;
		_port = port;
		_server = new(IPAddress.Loopback, port);
		_requestHandlerAsync = requestHandlerAsync;
		_errorHandler = errorHandler ??= _ => Responses.GetDefault500Response();
		_options = options ?? HttpConnectionOptions.Default;
		_loggingEnabled = loggingEnabled;
	}

	public async Task RunAsync(CancellationToken cancellationToken = default) {
		_server.Start();
		ConsoleLogger.LogInfo($"Listening on http://localhost:{_port}");

		while (true) {
			TcpClient client;
			try {
				client = await _server.AcceptTcpClientAsync(cancellationToken);
			} catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException) {
				break;
			}

			Task task;
			try {
				task = HandleTcpClient(client, cancellationToken);
			} catch (Exception exception) {
				if (_loggingEnabled)
					Console.Error.WriteLine($"Client error: {exception}");
				continue;
			}

			_ = task.ContinueWith(t => {
				if (t.IsFaulted && _loggingEnabled)
					Console.Error.WriteLine(t.Exception);
				lock (_lock) {
					_connections.Remove(t);
				}
			}, TaskContinuationOptions.ExecuteSynchronously);

			lock (_lock) {
				_connections.Add(task);
			}
		}
	}

	public void Stop() {
		_server.Stop();
	}

	public void Dispose() {
		_server.Stop();
	}

	private async Task HandleTcpClient(TcpClient client, CancellationToken cancellationToken) {
		using (client)
		using (NetworkStream stream = client.GetStream()) {
			if (_loggingEnabled && client.Client.RemoteEndPoint is IPEndPoint remoteEndPoint) 
				Console.WriteLine($"Connected to: {remoteEndPoint.Address}:{remoteEndPoint.Port}");
			
			var serverConnection = new HttpServerConnection(stream, _options);
			if (_requestHandler is not null) {
				await serverConnection.HandleRequests(_requestHandler, _errorHandler, _loggingEnabled, cancellationToken);

			}
			await serverConnection.HandleRequests(_requestHandlerAsync!, _errorHandler, _loggingEnabled, cancellationToken);
		}
	}
}
