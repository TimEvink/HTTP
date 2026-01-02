using System;
using System.IO;

namespace MyHttp.Core.Framing;
public sealed class ContentLengthDecodingStream : Stream {
    private readonly Stream _innerStream;
    private long _remaining;

    public ContentLengthDecodingStream(Stream inner, long contentlength) {
        if (inner == null) throw new ArgumentNullException(nameof(inner));
        if (contentlength < 0) throw new ArgumentOutOfRangeException(nameof(contentlength), "Content length cannot be negative");
        _innerStream = inner;
        _remaining = contentlength;
    }

    public override int Read(byte[] buffer, int offset, int count) {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (buffer.Length - offset < count) throw new ArgumentException("Invalid offset/count combination");
        if (count == 0 || _remaining <= 0) return 0;
        int read = _innerStream.Read(buffer, offset, (int)Math.Min(count, _remaining));
        _remaining -= read;
        return read;
    }

    //required overrides
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}