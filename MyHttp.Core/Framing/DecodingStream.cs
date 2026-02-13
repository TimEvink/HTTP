using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MyHttp.Core.Connection;

namespace MyHttp.Core.Framing;
public abstract class DecodingStream : Stream {
    internal HttpConnection _connection;
    internal DecodingStream(HttpConnection connection) {
        _connection = connection;
    }
    public sealed override bool CanWrite => false;
    public sealed override bool CanRead => true;
    public sealed override bool CanSeek => false;
    public sealed override long Length => throw new NotSupportedException();
    public sealed override long Position {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    public sealed override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public sealed override void SetLength(long value) => throw new NotSupportedException();

    public sealed override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public sealed override void Flush() { }
    public sealed override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public sealed override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public abstract override int Read(Span<byte> buffer);
    public abstract override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);
}
