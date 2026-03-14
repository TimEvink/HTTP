using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MyHttp.Core.Connection;
using MyHttp.Core.Exceptions;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Server;
internal static class HttpServerConnectionHandler {
	internal static async Task HandleRequests(
		this Stream stream,
		Func<HttpRequest, CancellationToken, Task<HttpResponse>> handler,
		Func<Exception, HttpResponse>? errorHandler = null,
		HttpConnectionOptions? options = null,
		CancellationToken cancellationToken = default
	) {
		await using HttpServerConnection connection = new(stream, options);
		errorHandler ??= exception => Responses.GetDefault500Response(exception);

		while (!cancellationToken.IsCancellationRequested) {
			HttpRequest request;
			HttpResponse response;

			try {
				request = await connection.ParseRequestAsync(cancellationToken);
				LogRequest(request);
			} catch (BadMessageException requestException) {
				Console.Error.WriteLine(requestException);
				return;
			} catch (EndOfStreamException) {
				return;
			}

			try {
				response = await handler(request, cancellationToken);
			} catch (Exception exception) {
				Console.Error.WriteLine(exception);
				try {
					response = errorHandler(exception);
				} catch {
					return;
				}
			}

			try {
				await connection.SerializeResponseAsync(response, cancellationToken);
			} catch {
				return;
			} finally {
				try {
					await response.Body.DisposeAsync();
				} catch { }
			}

			if (ShouldClose(request) || ShouldClose(response))
				return;
		}
	}

	private static bool ShouldClose(HttpMessage message) {
		if (!message.Headers.TryGetValue("Connection", out var list) || list.Length == 0)
			return false;
		for (int i = 0; i < list.Length; i++) {
			if (string.Equals(list[i], "close", StringComparison.OrdinalIgnoreCase))
				return true;
		}
		return false;
	}

	private static void LogRequest(HttpRequest request) {
		// Basic info: timestamp, client info if available, method, and target
		string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
		string method = request.Method.ToString();
		string url = request.Target.RawUrl;

		Console.WriteLine($"[{timestamp}] {method} {url}");
	}
}
