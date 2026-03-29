using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyHttp.Tests.TcpStreamMock;
internal sealed  class DuplexStream : Stream { 
	private readonly Stream _readStream;
	private readonly Stream _writeStream;

	internal DuplexStream(Stream readStream, Stream writeStream) {
		_readStream = readStream;
		_writeStream = writeStream;
	}

	public override bool CanRead => true;
	public override bool CanWrite => true;
	public override bool CanSeek => false;

	public override long Length => throw new NotSupportedException();
	public override long Position {
		get { throw new NotSupportedException(); }
		set { throw new NotSupportedException(); }
	}
	public sealed override void SetLength(long value) => throw new NotSupportedException();
	public sealed override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

	public override int Read(byte[] buffer, int offset, int count)
		=> _readStream.Read(buffer, offset, count);
	public override void Write(byte[] buffer, int offset, int count)
		=> _writeStream.Write(buffer, offset, count);


	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		=> _readStream.ReadAsync(buffer, offset, count, cancellationToken);
	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		=> _readStream.ReadAsync(buffer, cancellationToken);

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		=> _writeStream.WriteAsync(buffer, offset, count, cancellationToken);
	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		=> _writeStream.WriteAsync(buffer, cancellationToken);

	public override void Flush() => _writeStream.Flush();
	public override Task FlushAsync(CancellationToken cancellationToken) => _writeStream.FlushAsync(cancellationToken);

	protected override void Dispose(bool disposing) {
		if (disposing) {
			_writeStream.Dispose();
			_readStream.Dispose();
		}
		base.Dispose(disposing);
	}

}

