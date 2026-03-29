using System;
using System.Threading;
using System.Threading.Tasks;
using MyHttp.Core.Connection;

namespace MyHttp.Core.Framing;
public sealed class ContentLengthDecodingStream : DecodingStream {
    private long _remaining;
    internal ContentLengthDecodingStream(HttpConnection connection, long contentlength) : base(connection) {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
		if (contentlength < 0) throw new ArgumentOutOfRangeException(nameof(contentlength), "Content length cannot be negative");
        _remaining = contentlength;
    }

    public override int Read(Span<byte> buffer) {
        throw new NotImplementedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        if (_remaining == 0 || buffer.Length == 0) return 0;
        int toReadmax = Math.Min((int)_remaining, buffer.Length);
        int read = await _connection.ReadBodyAsync(toReadmax, cancellationToken);
        _connection._inputBuffer
            .AsSpan(_connection._inputStart, read)
            .CopyTo(buffer.Span);
        _remaining -= read;
        _connection.UpdateInputStart();
        return read;
    }
}
