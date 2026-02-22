using System;
using System.IO;
using System.Threading.Tasks;

using MyHttp.Core.Messages;
using MyHttp.Core.Exceptions;
using MyHttp.Core.Framing;
using System.Threading;

namespace MyHttp.Core.Connection;

public sealed class HttpServerConnection : HttpConnection {
    public HttpServerConnection(Stream stream, int inputBufferSize = 16384, int outputBufferSize = 16384, int maxLineSize = 4096, int maxHeaderSize = 32768)
        : base(stream, inputBufferSize, outputBufferSize, maxLineSize, maxHeaderSize) { }

	public async Task SerializeResponseAsync(HttpResponse response, CancellationToken cancellationToken = default) {
		await SerializeResponseLineAsync(response.Version, response.StatusCode, response.Message, cancellationToken).ConfigureAwait(false);
		await SerializeHeadersAsync(response.Headers, cancellationToken).ConfigureAwait(false);
		if (response.Body == Stream.Null) return;
		FramingInfo info = response.Headers.GetFramingInfo();
		await SerializeBodyAsync(info, response.Body, cancellationToken);
	}

	public async Task<HttpRequest> ParseRequestAsync(CancellationToken cancellationToken = default) {
		var (method, target, version) = await ParseRequestLineAsync(cancellationToken).ConfigureAwait(false);
		HttpHeaders headers = await ParseHeadersAsync(cancellationToken).ConfigureAwait(false);
		FramingInfo info = headers.GetFramingInfo();
		Stream body = info.HasBody ? getDecodingStream(info) : Stream.Null;
		return new HttpRequest(method, target, version, headers, body);
	}

	private async ValueTask SerializeResponseLineAsync(HttpVersion version, HttpStatusCode statusCode, HttpReason message, CancellationToken cancellationToken) {
		//HTTP/1.1 200 \r\n is 15 bytes
		int lineLength = message._rawMessage.Length + 15;
		if (lineLength > FreeOutputBytes) await FlushOutputAsync(cancellationToken).ConfigureAwait(false);
		WriteOutput(H);
		WriteOutput(T);
		WriteOutput(T);
		WriteOutput(P);
		WriteOutput(SLASH);
		WriteOutput((byte)('0' + version.Major));
		WriteOutput(DOT);
		WriteOutput((byte)('0' + version.Minor));
		WriteOutput(SPACE);
		WriteOutput(statusCode.b0);
		WriteOutput(statusCode.b1);
		WriteOutput(statusCode.b2);
		WriteOutput(SPACE);
		WriteOutput(message._rawMessage.Span);
		WriteOutput(CR);
		WriteOutput(LF);
	}

    private async ValueTask<(HttpMethod, HttpRequestTarget, HttpVersion)> ParseRequestLineAsync(CancellationToken cancellationToken) {
        byte b;
        int spaceOffset;

        //method + space
        while (true) {
            if (_inputCursor == _inputEnd) {
                if (_inputCursor - _inputStart >= _maxLineSize) throw new BadRequestException("Request line too long");
                await EnsureInputAvailableAsync(1, cancellationToken).ConfigureAwait(false);
            }
            b = _inputBuffer[_inputCursor++];
            if ((CharClass[b] & READABLE) != 0) continue;
            if (b == SPACE) {
                spaceOffset = _inputCursor - 1 - _inputStart;
                break;
            }
            throw new BadRequestException("Method characters must be readable ASCII");
        }
        HttpMethod method = GetHttpMethod(_inputBuffer.AsSpan(_inputStart, spaceOffset));

		//request target
		while (true) {
            if (_inputCursor == _inputEnd) {
                if (_inputCursor - _inputStart >= _maxLineSize) throw new BadRequestException("Request line too long");
                await EnsureInputAvailableAsync(1, cancellationToken).ConfigureAwait(false);
            }
            b = _inputBuffer[_inputCursor++];
            if ((CharClass[b] & URI) != 0) continue;
			if (b == PERCENT) {
				if (_inputCursor + 2 > _inputEnd) {
					if (_inputCursor + 2 - _inputStart >= _maxLineSize) throw new BadRequestException("Request line too long");
					await EnsureInputAvailableAsync(2, cancellationToken).ConfigureAwait(false);
				}
				if ((CharClass[_inputBuffer[_inputCursor]] & HEX) == 0 || (CharClass[_inputBuffer[_inputCursor + 1]] & HEX) == 0)
					throw new BadRequestException("Uri contains invalid % encoding");
				_inputCursor += 2;
				continue;
			}
			if (b == SPACE) break;
            throw new BadRequestException("Request target contains invalid character");
        }
        int targetStart = _inputStart + spaceOffset + 1;
        int targetLength = _inputCursor - 1 - targetStart;
        HttpRequestTarget target = new(_inputBuffer.AsMemory(targetStart, targetLength));
        spaceOffset = _inputCursor - 1 - _inputStart;
        //parse version
        //"HTTP/1.1\r\n" is 10 bytes.
        if (_inputCursor + 10 > _inputEnd) {
            if (_inputCursor + 10 - _inputStart >= _maxLineSize) throw new BadRequestException("Request line too long");
            await EnsureInputAvailableAsync(10, cancellationToken).ConfigureAwait(false);
        }
        HttpVersion version = GetHttpVersion(_inputBuffer.AsSpan(_inputCursor, 8));
        _inputCursor += 8;
        //verify correct line ending
        b = _inputBuffer[_inputCursor++];
        if (b != CR) throw new BadRequestException("Incorrect line separation for request line");
        b = _inputBuffer[_inputCursor++];
        if (b != LF) throw new BadRequestException("Incorrect line separation for request line");
        //transfer ownership
        _inputStart = _inputCursor;
        return (method, target, version);
    }

	private static HttpMethod GetHttpMethod(ReadOnlySpan<byte> span) => span.Length switch {
		3 => span[0] == 'G' && span[1] == 'E' && span[2] == 'T'
			? HttpMethod.GET
			: span[0] == 'P' && span[1] == 'U' && span[2] == 'T'
				? HttpMethod.PUT
				: throw new BadMessageException("Unknown HTTP method"),
		4 => span[0] == 'P' && span[1] == 'O' && span[2] == 'S' && span[3] == 'T'
			? HttpMethod.POST
			: span[0] == 'H' && span[1] == 'E' && span[2] == 'A' && span[3] == 'D'
				? HttpMethod.HEAD
				: throw new BadMessageException("Unknown HTTP method"),
		5 => span[0] == 'T' && span[1] == 'R' && span[2] == 'A' && span[3] == 'C' && span[4] == 'E'
			? HttpMethod.TRACE
			: throw new BadMessageException("Unknown HTTP method"),
		6 => span[0] == 'D' && span[1] == 'E' && span[2] == 'L' && span[3] == 'E' && span[4] == 'T' && span[5] == 'E'
			? HttpMethod.DELETE
			: span[0] == 'P' && span[1] == 'A' && span[2] == 'T' && span[3] == 'C' && span[4] == 'H'
				? HttpMethod.PATCH
				: throw new BadMessageException("Unknown HTTP method"),
		7 => span[0] == 'O' && span[1] == 'P' && span[2] == 'T' && span[3] == 'I' && span[4] == 'O' && span[5] == 'N' && span[6] == 'S'
			? HttpMethod.OPTIONS
			: span[0] == 'C' && span[1] == 'O' && span[2] == 'N' && span[3] == 'N' && span[4] == 'E' && span[5] == 'C' && span[6] == 'T'
				? HttpMethod.CONNECT
				: throw new BadMessageException("Unknown HTTP method"),
		_ => throw new BadMessageException("Unknown HTTP method")
	};
}
