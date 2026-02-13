using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MyHttp.Core.Exceptions;
using MyHttp.Core.Framing;
using MyHttp.Core.Messages;


namespace MyHttp.Core.Connection;

internal sealed class HttpClientConnection : HttpConnection {
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

    internal HttpClientConnection(Stream stream, int inputBufferSize = 16384, int outputBufferSize = 16384, int maxLineSize = 4096, int maxHeaderSize = 32768)
        : base(stream, inputBufferSize, outputBufferSize, maxLineSize, maxHeaderSize) { }

    private async ValueTask SerializeRequestLineAsync(HttpMethod method, HttpRequestTarget target, HttpVersion version, CancellationToken cancellationToken) {
        // "HTTP/1.1\r\n" is 10 bytes, plus 2 spaces makes 12
        int lineLength = MethodBytes[(byte)method].Length + target._rawUrl.Length + 12;
        if (lineLength > FreeOutputBytes) await FlushOutputAsync(cancellationToken).ConfigureAwait(false);

        ReadOnlySpan<byte> methodSpan = MethodBytes[(byte)method].Span;
        WriteOutput(methodSpan);
        WriteOutput((byte)' ');
        WriteOutput(target._rawUrl.Span);
        WriteOutput(HTTP.Span);
        WriteOutput((byte)('0' + version.Major));
        WriteOutput((byte)'.');
        WriteOutput((byte)('0' + version.Minor));
        WriteOutput(CRLF.Span);
    }

    //does not flush automatically.
    internal async ValueTask SerializeRequestAsync(HttpRequest request, CancellationToken cancellationToken) {
        await SerializeRequestLineAsync(request.Method, request.Target, request.Version, cancellationToken).ConfigureAwait(false);
        await SerializeHeadersAsync(request.Headers, cancellationToken).ConfigureAwait(false);
        if (request.Body == Stream.Null) return;
        FramingInfo info = request.Headers.GetFramingInfo();
        await SerializeBodyAsync(info, request.Body, cancellationToken);
    }
}
