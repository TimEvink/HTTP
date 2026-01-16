using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyHttp.Core.Framing;
public abstract class FramingStream : Stream {
    protected readonly Stream _innerStream;
    protected ReadOnlyMemory<byte> _leftover;
    protected long _consumed = 0;

    public FramingStream(Stream inner, ReadOnlyMemory<byte> leftover) {
        if (inner == null) throw new ArgumentNullException(nameof(inner));
        _innerStream = inner;
        _leftover = leftover;
    }

    //Must be called after the framing stream completed to get leftover bytes.
    public ReadOnlyMemory<byte> Finish() => _leftover;

    //invariants
    public sealed override bool CanWrite => true;
    public sealed override bool CanRead => true;
    public sealed override bool CanSeek => false;
    public sealed override long Length => throw new NotSupportedException();
    public sealed override long Position {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    public sealed override void Flush() => _innerStream.Flush();
    public sealed override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public sealed override void SetLength(long value) => throw new NotSupportedException();

    public sealed override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
    public sealed override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public abstract override void Write(ReadOnlySpan<byte> buffer);
    public abstract override int Read(Span<byte> buffer);

    // public abstract Task WriteAsync(ReadOnlySpan<byte> buffer, int offset, int count, CancellationToken cancellationToken);
    // public abstract Task<int> ReadAsync(ReadOnlySpan<byte> buffer, int offset, int count, CancellationToken cancellationToken);
}