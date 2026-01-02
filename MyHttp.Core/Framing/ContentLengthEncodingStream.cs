using System;
using System.IO;

namespace MyHttp.Core.Framing;
public sealed class ContentLengthEncodingStream : Stream {
    private readonly Stream _innerStream;
    private long _remaining;
    public ContentLengthEncodingStream(Stream inner, long contentLength) {
        if (inner == null) throw new ArgumentNullException(nameof(inner));
        if (contentLength < 0) throw new ArgumentOutOfRangeException(nameof(contentLength), "Content length cannot be negative");
        _innerStream = inner;
        _remaining = contentLength;
    }

    // for serialization
    public override void Write(byte[] buffer, int offset, int count) {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (buffer.Length - offset < count) throw new ArgumentException("Invalid offset/count combination", nameof(count));
        if (count == 0) return;
        if (count > _remaining) throw new InvalidOperationException("Writing more bytes than Content-Length allowed");
        _innerStream.Write(buffer, offset, count);
        _remaining -= count;
    }

    public override int Read(byte[] buffer, int offset, int count) {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (buffer.Length - offset < count) throw new ArgumentException("Invalid offset/count combination");
        if (count == 0 || _remaining <= 0) return 0;
        int read = _innerStream.Read(buffer, offset, (int)Math.Min(count, _remaining));
        if (read == 0) throw new EndOfStreamException("Unexpected end of stream while reading HTTP body");
        _remaining -= read;
        return read;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    public override void Flush() => _innerStream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}