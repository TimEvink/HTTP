using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MyHttp.Core.Exceptions;
using MyHttp.Core.Messages;
using MyHttp.Core.Framing;

namespace MyHttp.Core.Connection;
internal sealed class HttpClientConnection : HttpConnection {
	internal HttpClientConnection(Stream stream, HttpConnectionOptions? options = null)
		: base(stream, options ?? HttpConnectionOptions.Default) { }

	//does not flush automatically.
	internal async Task SerializeRequestAsync(HttpRequest request, bool flush = true, CancellationToken cancellationToken = default) {
		await SerializeRequestLineAsync(request.Method, request.Target, request.Version, cancellationToken).ConfigureAwait(false);
		await SerializeHeadersAsync(request.Headers, cancellationToken).ConfigureAwait(false);

		if (request.Body != Stream.Null) {
			FramingInfo info = request.Headers.GetFramingInfo();
			await SerializeBodyAsync(info, request.Body, cancellationToken);
		}
		if (flush)
			await FlushOutputAsync(cancellationToken).ConfigureAwait(false);
	}

	internal async Task<HttpResponse> ParseResponseAsync(CancellationToken cancellationToken = default) {
		var (version, statusCode, reason) = await ParseResponseLineAsync(cancellationToken);
		HttpHeaders headers = await ParseHeadersAsync(cancellationToken).ConfigureAwait(false);
		FramingInfo info = headers.GetFramingInfo();
		Stream body = info.HasBody ? getDecodingStream(info) : Stream.Null;
		return new HttpResponse(version, statusCode, reason, headers, body);
	}

	private async ValueTask SerializeRequestLineAsync(HttpMethod method, HttpRequestTarget target, HttpVersion version, CancellationToken cancellationToken) {
		// "HTTP/1.1\r\n" is 10 bytes, plus 2 spaces makes 12
		int lineLength = MethodBytes[(byte)method].Length + target._rawUrl.Length + 12;
		if (lineLength > FreeOutputBytes) await FlushOutputAsync(cancellationToken).ConfigureAwait(false);

		ReadOnlySpan<byte> methodSpan = MethodBytes[(byte)method].Span;
		WriteOutput(methodSpan);
		WriteOutput(SPACE);
		WriteOutput(target._rawUrl.Span);
		WriteOutput(SPACE);
		WriteOutput(H);
		WriteOutput(T);
		WriteOutput(T);
		WriteOutput(P);
		WriteOutput(SLASH);
		WriteOutput((byte)('0' + version.Major));
		WriteOutput(DOT);
		WriteOutput((byte)('0' + version.Minor));
		WriteOutput(CR);
		WriteOutput(LF);
	}

	private async ValueTask<(HttpVersion, HttpStatusCode, HttpReason)> ParseResponseLineAsync(CancellationToken cancellationToken) {
		byte b;
		//version
		if (_inputCursor + 13 > _inputEnd) {
			if (_inputCursor + 13 - _inputStart >= _maxLineSize) throw new BadResponseException("Response line too long");
			await EnsureInputAvailableAsync(12, cancellationToken).ConfigureAwait(false);
		}
		HttpVersion version = GetHttpVersion(_inputBuffer.AsSpan(_inputCursor, 8));
		_inputCursor += 8;

		//space
		b = _inputBuffer[_inputCursor++];
		if (b != SPACE) throw new BadResponseException("Space character must follow HTTP version");

		//status code
		HttpStatusCode statusCode = new(_inputBuffer[_inputCursor], _inputBuffer[_inputCursor + 1], _inputBuffer[_inputCursor + 2]);
		_inputCursor += 3;

		//space
		b = _inputBuffer[_inputCursor++];
		if (b != SPACE) throw new BadResponseException("Space character must follow HTTP status code");
		int spaceOffset = _inputCursor - 1 - _inputStart;

		//reason + CRLF
		while (true) {
			if (_inputCursor == _inputEnd) {
				if (_inputCursor - _inputStart >= _maxLineSize) throw new BadResponseException("Response line too long");
				await EnsureInputAvailableAsync(1, cancellationToken).ConfigureAwait(false);
			}
			b = _inputBuffer[_inputCursor++];
			if ((CharClass[b] & VALUE_OK) != 0) continue;
			if (b == CR) break;
			if (b == LF) throw new BadResponseException("LF found before CR in response line");
			throw new BadResponseException("Invalid character in reason phrase");
		}
		b = _inputBuffer[_inputCursor++];
		if (b != LF) throw new BadResponseException("Incorrect line separation for response line");

		int reasonStart = _inputStart + spaceOffset + 1;
		int reasonLength = _inputCursor - 1 - reasonStart;
		HttpReason reason = new(_inputBuffer.AsMemory(reasonStart, reasonLength));

		//transfer ownership
		_inputStart = _inputCursor;

		return (version, statusCode, reason);
	}


	private static readonly ReadOnlyMemory<byte>[] MethodBytes = {
		"GET"u8.ToArray(),
		"POST"u8.ToArray(),
		"PUT"u8.ToArray(),
		"DELETE"u8.ToArray(),
		"HEAD"u8.ToArray(),
		"OPTIONS"u8.ToArray(),
		"TRACE"u8.ToArray(),
		"CONNECT"u8.ToArray(),
		"PATCH"u8.ToArray()
	};
}
