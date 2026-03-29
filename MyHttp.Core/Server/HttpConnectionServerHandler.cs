using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using MyHttp.Core.Connection;
using MyHttp.Core.Exceptions;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Server;
internal static class HttpServerConnectionHandler {
	internal static async Task HandleRequests(
		this HttpServerConnection serverConnection,
		Func<HttpRequest, HttpResponse> handler,
		Func<Exception, HttpResponse>? errorHandler = null,
		bool loggingEnabled = true,
		CancellationToken cancellationToken = default
	) {
		errorHandler ??= _ => Responses.GetDefault500Response();

		while (!cancellationToken.IsCancellationRequested) {
			HttpRequest request;
			HttpResponse response;

			try {
				request = await serverConnection.ParseRequestAsync(cancellationToken);
			} catch (BadMessageException requestException) {
				if (loggingEnabled)
					Console.Error.WriteLine(requestException);
				return;
			} catch (EndOfStreamException) {
				return;
			}

			try {
				if (loggingEnabled) {
					var stopwatch = Stopwatch.StartNew();
					response = handler(request);
					stopwatch.Stop();
					ConsoleLogger.LogRequest(request, response, stopwatch.Elapsed);
				} else {
					response = handler(request);
				}
			} catch (Exception exception) {
				if (loggingEnabled)
					Console.Error.WriteLine(exception);
				try {
					response = errorHandler(exception);
				} catch {
					return;
				}
			}

			try {
				await serverConnection.SerializeResponseAsync(response, cancellationToken);
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
	internal static async Task HandleRequests(
		this HttpServerConnection serverConnection,
		Func<HttpRequest, CancellationToken, Task<HttpResponse>> handlerAsync,
		Func<Exception, HttpResponse>? errorHandler = null,
		bool loggingEnabled = true,
		CancellationToken cancellationToken = default
	) {
		errorHandler ??= _ => Responses.GetDefault500Response();

		while (!cancellationToken.IsCancellationRequested) {
			HttpRequest request;
			HttpResponse response;

			try {
				
				request = await serverConnection.ParseRequestAsync(cancellationToken);
			} catch (BadMessageException requestException) {
				if (loggingEnabled)
					Console.Error.WriteLine(requestException);
				return;
			} catch (EndOfStreamException) {
				return;
			}

			try {
				if (loggingEnabled) {
					var stopwatch = Stopwatch.StartNew();
					response = await handlerAsync(request, cancellationToken);
					stopwatch.Stop();
					ConsoleLogger.LogRequest(request, response, stopwatch.Elapsed);
				} else {
					response = await handlerAsync(request, cancellationToken);
				}
			} catch (Exception exception) {
				if (loggingEnabled)
					Console.Error.WriteLine(exception);
				try {
					response = errorHandler(exception);
				} catch {
					return;
				}
			}

			try {
				await serverConnection.SerializeResponseAsync(response, cancellationToken);
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
}
