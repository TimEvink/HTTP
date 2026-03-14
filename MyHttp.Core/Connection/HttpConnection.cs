using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using MyHttp.Core.Exceptions;
using MyHttp.Core.Framing;
using MyHttp.Core.Messages;

namespace MyHttp.Core.Connection;
//owns _stream.
internal abstract class HttpConnection : IAsyncDisposable {
    internal readonly Stream _stream;
    private static readonly ReadOnlyMemoryByteComparer _comparer = new();

    //input = bytes coming in from _stream reads.
    internal byte[] _inputBuffer;
	internal int _inputStart = 0;
    protected int _inputCursor = 0;
    protected int _inputEnd = 0;

    //state invariants for _inputBuffer: _inputStart <= _inputCursor <= _inputEnd
    //_inputStart is the index of oldest buffered byte of relevence; bytes to the left may be discarded.
    //_inputCursor indicates the current parse scanning position on _inputBuffer.
    //_inputEnd is the first free index available for incoming bytes; it is the offset used for writing to the buffer from the network stream.

    private int TotalInputBytes => _inputEnd - _inputStart;
    private int FreeInputBytes => _inputBuffer.Length - _inputEnd;

    //output = bytes going out to _stream writes.
    protected byte[] _outputBuffer;
    protected int _outputEnd = 0;

    internal int FreeOutputBytes => _outputBuffer.Length - _outputEnd;

    protected readonly int _maxLineSize;
    private readonly int _maxHeaderSize;
    private int _headerBytesRead;

    protected static readonly byte[] CharClass = new byte[256];

    //bit masks for faster parsing.
    protected const byte TOKEN = 0x01; // tokens used for fast path.
    protected const byte VALUE_OK = 0x02; // headervalue safe chars. Also used for valid reason messages in response line.
    protected const byte READABLE = 0x04; 
	protected const byte URI = 0x08; // uri chars used for fast path.
	protected const byte HEX = 0x10; // used for (case-insenstive) hex chars for uri % encoding.

	//const bytes.
	protected const byte LF = 0xa; // '\n'
	protected const byte CR = 0xd; // '\r'
	protected const byte SPACE = 0x20; // ' '
	protected const byte PERCENT = 0x25; // '%'
	protected const byte COMMA = 0x2c; // ','
	protected const byte DOT = 0x2e; // '.'
	protected const byte SLASH = 0x2f; // '/'
	protected const byte COLON = 0x3a; // ':'
	protected const byte H = 0x48; // 'H'
	protected const byte P = 0x50; // 'P'
	protected const byte T = 0x54; // 'T'

	//private static readonly ReadOnlyMemory<byte> HTTP = "HTTP/"u8.ToArray();
	private static readonly ReadOnlyMemory<byte> SetCookie = "Set-Cookie"u8.ToArray();

	//initialize byte array for bit masking.
    static HttpConnection() {
        for (char c = '!'; c <= '~'; c++) {
            if (!":()<>@,;\\\"/[]?={}".Contains(c))
				CharClass[c] = TOKEN;
			if (!"\"%<>\\^`{|}".Contains(c))
				CharClass[c] |= URI;
            CharClass[c] |= VALUE_OK;
            CharClass[c] |= READABLE;
        }

		for (char c = '0'; c <= '9'; c++) CharClass[c] |= HEX;
		for (char c = 'A'; c <= 'F'; c++) CharClass[c] |= HEX;
		for (char c = 'a'; c <= 'f'; c++) CharClass[c] |= HEX;

		CharClass['\t'] |= VALUE_OK;
        CharClass[' '] |= VALUE_OK;
	}

    protected HttpConnection(Stream stream, HttpConnectionOptions options) {
		ArgumentNullException.ThrowIfNull(stream);
		if (!stream.CanRead || !stream.CanWrite) throw new ArgumentException("Stream must be readable and writable", nameof(stream));
        _stream = stream;
		_inputBuffer = new byte[options.InputBufferSize];
		_outputBuffer = new byte[options.OutputBufferSize];
		_maxLineSize = options.MaxLineSize;
		_maxHeaderSize = options.MaxHeaderSize;
    }

    //removes consumed bytes from inputbuffer to make room for more input, shifting unread bytes & empty slots to the left.
    private void CompactifyInput() {
        Buffer.BlockCopy(_inputBuffer, _inputStart, _inputBuffer, 0, TotalInputBytes);
        _inputEnd -= _inputStart;
        _inputCursor -= _inputStart;
        _inputStart = 0;
    }

	//ensures there are at least minBytes bytes available in the inputbuffer.
	//the networkstream reads try to get as many bytes as possible without triggereing a CompactifyInput call.
	internal async ValueTask EnsureInputAvailableAsync(int minBytes, CancellationToken cancellationToken) {
		while (TotalInputBytes < minBytes) {
			if (FreeInputBytes == 0) {
				if (_inputStart > 0) {
					CompactifyInput();
				} else {
					throw new InvalidOperationException("Inputvuffer too small for incoming data");
				}
			}
			int read = await _stream.ReadAsync(_inputBuffer.AsMemory(_inputEnd, FreeInputBytes), cancellationToken).ConfigureAwait(false);
			if (read == 0) throw new EndOfStreamException();
			_inputEnd += read;
		}
	}

	public async ValueTask FlushOutputAsync(CancellationToken cancellationToken = default) {
        if (_outputEnd == 0) return;
        await _stream.WriteAsync(_outputBuffer.AsMemory(0, _outputEnd), cancellationToken).ConfigureAwait(false);
        _outputEnd = 0;
    }

    public async ValueTask DisposeAsync() {
        if (_outputEnd > 0) await FlushOutputAsync(CancellationToken.None).ConfigureAwait(false);
        await _stream.DisposeAsync().ConfigureAwait(false);
    }

    internal async ValueTask WriteOutputAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken) {
        if (data.Length > FreeOutputBytes) await FlushOutputAsync(cancellationToken);
        if (data.Length > _outputBuffer.Length) {
            await _stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            return;
        }
        WriteOutput(data.Span);
    }

    //caller is responsible for ensuring that data.Length <= FreeOutputBytes.
    internal void WriteOutput(ReadOnlySpan<byte> data) {
        data.CopyTo(_outputBuffer.AsSpan(_outputEnd));
        _outputEnd += data.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void WriteOutput(byte b)
    {
        _outputBuffer[_outputEnd++] = b;
    }

    protected static HttpVersion GetHttpVersion(ReadOnlySpan<byte> span) {
        //Span must represent exactly "HTTP/X.Y". Throws otherwise.
        if (span.Length != 8) throw new BadMessageException("Incorrect HTTP version syntax");
        if (span[0] != (byte)'H' || span[1] != (byte)'T' || span[2] != (byte)'T' ||
            span[3] != (byte)'P' || span[4] != (byte)'/' || span[6] != (byte)'.') {
            throw new BadMessageException("Incorrect HTTP version syntax");
        }
        byte majorChar = span[5];
        byte minorChar = span[7];
        if ((uint)(majorChar - (byte)'0') > 9 || (uint)(minorChar - (byte)'0') > 9) {
            throw new BadMessageException("HTTP version numbers must be digits");
        }
        return new HttpVersion(majorChar - (byte)'0', minorChar - (byte)'0');
    }

    protected DecodingStream getDecodingStream(FramingInfo info) {
        return info.Method switch {
            FramingMethod.CONTENTLENGTH => new ContentLengthDecodingStream(this, info.ContentLength),
            FramingMethod.NONE => throw new ArgumentException("No decoding stream exists for an empty body"),
            _ => throw new NotSupportedException($"Framing method {info.Method} not supported")
        };
    }

    protected async ValueTask SerializeHeadersAsync(HttpHeaders headers, CancellationToken cancellationToken) {
        foreach (var (name, parts) in headers._raw) {
            int lineLength = name.Length + 2 * (parts.Count + 1);
            for (int i = 0; i < parts.Count; i++)
                lineLength += parts[i].Length;
            if (lineLength > FreeOutputBytes) await FlushOutputAsync(cancellationToken).ConfigureAwait(false);
            WriteOutput(name.Span);
            WriteOutput(": "u8);
            WriteOutput(parts[0].Span);
            for (int i = 1; i < parts.Count; i++) {
                WriteOutput(", "u8);
                WriteOutput(parts[i].Span);
            }
            if (2 > FreeOutputBytes) await FlushOutputAsync(cancellationToken).ConfigureAwait(false);
            WriteOutput("\r\n"u8);
        }
		WriteOutput("\r\n"u8);
	}

    protected async ValueTask SerializeBodyAsync(FramingInfo info, Stream body, CancellationToken cancellationToken) {
        switch (info.Method) {
            case FramingMethod.CONTENTLENGTH:
                long remaining = info.ContentLength;
                while (remaining > 0) {
                    if (FreeOutputBytes == 0) await FlushOutputAsync(cancellationToken);
					int maxToRead = (int)Math.Min(FreeOutputBytes, remaining);
					int read = await body.ReadAsync(_outputBuffer.AsMemory(_outputEnd, maxToRead));
					_outputEnd += read;
                    if (read == 0) throw new EndOfStreamException("Message body too small");
                    remaining -= read;
                }
                break;
            default:
                throw new BadMessageException("Framing method not supported");
        }
    }

    //owns _inputCursor
    private async ValueTask<(int, bool)?> ReadHeaderLineAsync(CancellationToken cancellationToken) {
        int colonOffset = -1;
        bool hasComma = false;
        byte b;
        while (true) {
            if (_inputCursor == _inputEnd) {
                if (_inputCursor - _inputStart >= _maxLineSize) throw new BadMessageException("Header line too long");
                await EnsureInputAvailableAsync(1, cancellationToken).ConfigureAwait(false);
            }
            b = _inputBuffer[_inputCursor++];
            byte flags = CharClass[b];
            //hot path
            if ((flags & TOKEN) != 0) continue;
            if (b == COLON) {
                if (colonOffset == -1) {
                    colonOffset = (_inputCursor - 1) - _inputStart;
                    if (colonOffset == 0) throw new BadMessageException("Empty header name found; ':' first character on header line");
                }
                continue;
            }
            if (b == CR) break;
            if (b == LF) throw new BadMessageException(@"LF not immediately following a CR in header line");
            //remaining bytes either part of header name or value
            if (colonOffset == -1) throw new BadMessageException("Invalid character in header name");
            if ((flags & VALUE_OK) == 0) throw new BadMessageException("Invalid control character in header value");
            if (b == COMMA) hasComma = true;
        }
        //being here means we just saw a CR, so next byte has to be LF.
        if (_inputCursor == _inputEnd) {
            if (_inputCursor - _inputStart >= _maxLineSize) throw new BadMessageException("Header line too long");
            await EnsureInputAvailableAsync(1, cancellationToken).ConfigureAwait(false);
        }
        b = _inputBuffer[_inputCursor++];
        if (b != LF) throw new BadMessageException(@"No LF following CR on header line");

        _headerBytesRead += _inputCursor - _inputStart;
        if (_headerBytesRead > _maxHeaderSize) throw new BadMessageException("Total HTTP header size exceeded");
        return _inputCursor == _inputStart + 2 ? null : (colonOffset, hasComma);
    }

    private static ReadOnlySpan<byte> Trim(ReadOnlySpan<byte> span) {
        int start = 0;
        int end = span.Length - 1;
        while (start <= end && (span[start] == (byte)' ' || span[start] == (byte)'\t')) start++;
        while (start <= end && (span[end] == (byte)' ' || span[end] == (byte)'\t')) end--;
        return span.Slice(start, end - start + 1);
    }

    private static bool NeverSplitOnComma(ReadOnlyMemory<byte> headername)
        => _comparer.Equals(headername, SetCookie);

    //for splitting a header value as span.
    //separates on ',' and trims the parts.
    //callback based as spans are incompatible with generators.
    private static void SplitOnCommas(ReadOnlySpan<byte> span, Action<ReadOnlySpan<byte>> onElement) {
        int tokenStart = -1;
        int lastNonWhiteSpace = -1;
        for (int i = 0; i < span.Length; i++) {
            byte b = span[i];
            if (b == (byte)',') {
                if (lastNonWhiteSpace >= tokenStart) {
                    onElement(span.Slice(tokenStart, lastNonWhiteSpace - tokenStart + 1));
                }
                //reset
                tokenStart = i + 1;
                lastNonWhiteSpace = tokenStart - 1;
            } else {
                if (b != (byte)' ' && b != (byte)'\t') lastNonWhiteSpace = i;
            }
        }
        // emit last token
        if (lastNonWhiteSpace >= tokenStart) {
            onElement(span.Slice(tokenStart, lastNonWhiteSpace - tokenStart + 1));
        }
    }

    //owns _inputStart and _headerBytesRead reset.
    //iteration usage will automatically have the colonOffset relative to the correct _linestart
    private async IAsyncEnumerable<(int, bool)> ReadHeadersAsync([EnumeratorCancellation] CancellationToken cancellationToken) {
        while (true) {
            (int, bool)? lineData = await ReadHeaderLineAsync(cancellationToken).ConfigureAwait(false);
            if (lineData == null) {
                _inputStart = _inputCursor;
                _headerBytesRead = 0;
                break;
            }
            yield return lineData.Value;
            _inputStart = _inputCursor;
        }
    }

	// NOTE: Quoted-string parsing intentionally not supported.
	// Consequently, if a header name allows comma separated values, the parser will separate the corresponding raw header value regardless of quotes surrounding commas.
	protected async ValueTask<HttpHeaders> ParseHeadersAsync(CancellationToken cancellationToken) {
        var headers = new Dictionary<ReadOnlyMemory<byte>, List<ReadOnlyMemory<byte>>>(_comparer);
        await foreach ((int colonOffset, bool hasComma) in ReadHeadersAsync(cancellationToken).ConfigureAwait(false)) {
            Memory<byte> name = _inputBuffer.AsMemory(_inputStart, colonOffset);
            ReadOnlySpan<byte> valueTrimmed = Trim(_inputBuffer.AsSpan(_inputStart + colonOffset + 1, _inputCursor - _inputStart - 3 - colonOffset));
            if (!headers.TryGetValue(name, out var list)) {
                list = [];
                headers[name] = list;
            }
            if (!hasComma || NeverSplitOnComma(name)) {
                list.Add(valueTrimmed.ToArray());
            } else {
                SplitOnCommas(valueTrimmed, part => list.Add(part.ToArray()));
            }
        }
        return new HttpHeaders(headers);
    }

    //advances _inputCursor for body reading.
    internal async ValueTask<int> ReadBodyAsync(int maxBytes, CancellationToken cancellationToken) {
        if (_inputCursor == _inputEnd) {
            await EnsureInputAvailableAsync(1, cancellationToken).ConfigureAwait(false);
        }
        int available = _inputEnd - _inputCursor;
        int toRead = Math.Min(available, maxBytes);
        _inputCursor += toRead;
        return toRead;
    }

    internal void UpdateInputStart() {
        _inputStart = _inputCursor;
    }
}
