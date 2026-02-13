using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MyHttp.Core.Connection;

namespace MyHttp.Core.Framing;

public sealed class ContentLengthEncodingStream : EncodingStream {
    private long _remaining;
    internal ContentLengthEncodingStream(HttpConnection connection, long contentlength) : base(connection) {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (contentlength < 0) throw new ArgumentOutOfRangeException(nameof(contentlength), "Content length cannot be negative");
        _remaining = contentlength;
    }

    public override void Write(ReadOnlySpan<byte> buffer) {
        throw new NotImplementedException();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (_remaining == 0 || buffer.Length == 0) return;
        if (buffer.Length > _remaining) throw new InvalidOperationException("Attempt to write more bytes than Content-Length allows");
        if (buffer.Length > _connection.FreeOutputBytes) await _connection.FlushOutputAsync(cancellationToken).ConfigureAwait(false);
        _connection.WriteOutput(buffer.Span);
        _remaining -= buffer.Length;
    }
}
