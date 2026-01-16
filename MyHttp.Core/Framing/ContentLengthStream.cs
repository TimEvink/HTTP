using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyHttp.Core.Framing;
public sealed class ContentLengthStream : FramingStream {
    private long _remaining;
    public ContentLengthStream(Stream inner, ReadOnlyMemory<byte> leftover, long contentLength) : base(inner, leftover) {
        if (contentLength < 0) throw new ArgumentOutOfRangeException(nameof(contentLength), "Content length cannot be negative");
        _remaining = contentLength;
    }

    public override void Write(ReadOnlySpan<byte> buffer) {
        if (buffer.Length > _remaining) throw new InvalidOperationException("Writing more bytes than Content-Length allowed");
        _innerStream.Write(buffer);
        _remaining -= buffer.Length;
    }

    public override int Read(Span<byte> buffer) {
        int toRead = (int)Math.Min(buffer.Length, _remaining);
        int read = _innerStream.Read(buffer.Slice(0, toRead));
        _remaining -= read;
        return read;
    }
    // public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
    //     if (buffer == null) throw new ArgumentNullException(nameof(buffer));
    //     if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
    //     if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
    //     if (buffer.Length - offset < count) throw new ArgumentException("Invalid offset/count combination", nameof(count));
    //     if (count == 0) return;
    //     if (count > _remaining) throw new InvalidOperationException("Writing more bytes than Content-Length");
    //     await _innerStream.WriteAsync(
    //         buffer, offset, count, cancellationToken
    //     ).ConfigureAwait(false);
    //     _remaining -= count;
    // }

    // public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationtoken) {
    //     if (buffer == null) throw new ArgumentNullException(nameof(buffer));
    //     if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
    //     if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
    //     if (buffer.Length - offset < count) throw new ArgumentException("Invalid offset/count combination");
    //     if (count == 0 || _remaining <= 0) return 0;
    //     int read = await _innerStream.ReadAsync(
    //         buffer, offset, (int)Math.Min(count, _remaining), cancellationtoken
    //     ).ConfigureAwait(false);
    //     _remaining -= read;
    //     return read;
    // }
}