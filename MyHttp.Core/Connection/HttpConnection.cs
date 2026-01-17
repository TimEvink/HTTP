using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using MyHttp.Core.Exceptions;

// NOTE: Quoted-string parsing intentionally not supported yet

namespace MyHttp.Core.Connection;

internal abstract class HttpConnectionBase {
    private readonly Stream _stream;
    private byte[] _buffer;
    private int _lineStart = 0;
    private int _readPosition = 0;
    private int _writePosition = 0;
    private readonly int _maxHeaderLineSize;
    private readonly int _maxHeaderBytes;
    private int _headerBytesRead;
    private static readonly byte[] CharClass = new byte[256];

    //bit masks for faster parsing.
    const byte TOKEN = 0x01; // tokens used for fast path.
    const byte VALUE_OK = 0x02; // headervalue safe chars.
    const byte CR = 0x04; // '\r'
    const byte LF = 0x08; // '\n'
    const byte COLON = 0x10; // ':'
    const byte COMMA = 0x20; // ','

    // number of bytes of importance currently in the buffer.
    private int ReadableBytes => _writePosition - _lineStart;

    // room in buffer for writing bytes read from the network stream.
    private int FreeBytes => _buffer.Length - _writePosition;

    static HttpConnectionBase() {
        for (char i = '!'; i <= '~'; i++) {
            if (":()<>@,;\\\"/[]?={}".IndexOf(i) == -1) CharClass[i] = TOKEN;
            CharClass[i] |= VALUE_OK;
        }
        CharClass['\r'] = CR;
        CharClass['\n'] = LF;
        CharClass['\t'] |= VALUE_OK;
        CharClass[' '] |= VALUE_OK;
        CharClass[':'] |= COLON;
        CharClass[','] |= COMMA;
    }

    protected HttpConnectionBase(Stream stream, int buffersize = 16384, int maxheadersize = 4096, int maxheaderbytes = 32768) {
        _stream = stream;
        _buffer = new byte[buffersize];
        _maxHeaderLineSize = maxheadersize;
        _maxHeaderBytes = maxheaderbytes;
    }

    //removes consumed bytes from buffer to make room for another read, shifting unread bytes & empty slots to the left.
    private void Compactify() {
        Buffer.BlockCopy(_buffer, _lineStart, _buffer, 0, ReadableBytes);
        _writePosition -= _lineStart;
        _readPosition -= _lineStart;
        _lineStart = 0;
    }

    //ensures there are at least minBytes bytes available in the buffer to be read.
    private async ValueTask EnsureBufferedAsync(int minBytes) {
        if (minBytes <= 0) return;
        while (ReadableBytes < minBytes) {
            if (FreeBytes == 0) {
                if (_lineStart > 0) {
                    Compactify();
                }
                else {
                    throw new InvalidOperationException("Buffer too small for incoming data");
                }
            }
            int read = await _stream.ReadAsync(_buffer, _writePosition, FreeBytes).ConfigureAwait(false);
            if (read == 0) throw new EndOfStreamException();
            _writePosition += read;
        }
    }

    protected async ValueTask<(int, bool)> ReadLineAsync() {
        int colonOffset = -1;
        bool hasComma = false;
        byte b, flags;
        while (true) {
            if (_readPosition == _writePosition) {
                if (_readPosition - _lineStart >= _maxHeaderLineSize) throw new BadMessageException("Header line too long");
                await EnsureBufferedAsync(1).ConfigureAwait(false);
            }
            b = _buffer[_readPosition++];
            flags = CharClass[b];
            //hot path
            if ((flags & TOKEN) != 0) continue;
            if ((flags & COLON) != 0) {
                if (colonOffset == -1) {
                    colonOffset = (_readPosition - 1) - _lineStart;
                    if (colonOffset == 0) throw new BadMessageException("Empty header name found; ':' first character on header line.");
                }
                continue;
            }
            if ((flags & CR) != 0) break;
            if ((flags & LF) != 0) throw new BadMessageException(@"LF not immediately following a CR.");
            //remaining bytes either part of header name or value
            if (colonOffset == -1) throw new BadMessageException("Invalid character in header name");
            if ((flags & VALUE_OK) == 0) throw new BadMessageException("Invalid control character in header value");
            if ((flags & COMMA) != 0) hasComma = true;
        }
        //being here means we just saw a CR, so next byte has to be LF.
        if (_readPosition == _writePosition) {
            if (_readPosition - _lineStart >= _maxHeaderLineSize) throw new BadMessageException("Header line too long");
            await EnsureBufferedAsync(1).ConfigureAwait(false);
        }
        b = _buffer[_readPosition++];
        if ((CharClass[b] & LF) == 0) throw new BadMessageException(@"No LF following CR");

        _headerBytesRead += _readPosition - _lineStart;
        if (_headerBytesRead > _maxHeaderBytes) throw new BadMessageException("Total HTTP header size exceeded");
        // return new LineInfo(_readPosition - 2 - _lineStart, colonOffset, hasComma);
        return (colonOffset, hasComma);
    }

    private static string NormalizeHeaderValue(ReadOnlySpan<byte> span) {
        int len = span.Length;
        int i = 0;
        while (i < len && (span[i] == (byte)' ' || span[i] == (byte)'\t')) i++;
        if (i == len) return string.Empty;
        Span<byte> normalized = stackalloc byte[len];
        int j = 0;
        bool inWhitespace = false;
        for (; i < len; i++) {
            byte b = span[i];
            if (b == (byte)' ' || b == (byte)'\t') {
                inWhitespace = true;
            }
            else {
                if (inWhitespace && j > 0) normalized[j++] = (byte)' ';
                normalized[j++] = b;
                inWhitespace = false;
            }
        }
        return Encoding.ASCII.GetString(normalized.Slice(0, j));
    }

    //normalizes OWS of headervalue.
    private async Task<(string, string, bool)?> ParseHeaderAsync() {
        // LineInfo line = await ReadLineAsync().ConfigureAwait(false);
        var (colonOffset, hasComma) = await ReadLineAsync().ConfigureAwait(false);
        int lineLength = _readPosition - 2 - _lineStart;
        if (lineLength == 0) {
            //ownership transfer
            _lineStart = _readPosition;
            return null;
        }
        if (colonOffset == -1) throw new BadMessageException("Header line missing ':'");
        string name = Encoding.ASCII.GetString(_buffer, _lineStart, colonOffset);
        string value = NormalizeHeaderValue(_buffer.AsSpan(_lineStart + colonOffset + 1, lineLength - colonOffset - 1));
        //ownership transfer
        _lineStart = _readPosition;
        return (name, value, hasComma);
    }

    //generates header name: value pairs (with normalized OWS for value) and also a flag indicating if a comma was found in the value.
    private async IAsyncEnumerable<(string, string, bool)> ParseHeadersAsync() {
        while (true) {
            (string, string, bool)? pair = await ParseHeaderAsync().ConfigureAwait(false);
            if (pair == null) {
                _headerBytesRead = 0;
                break;
            }
            yield return pair.Value;
        }
    }

    private static bool NeverSplitOnComma(string headername) {
        return headername.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase);
    }

    //consumes the ParseHeadersAsync generator
    protected async Task<Dictionary<string, List<string>>> ReadHeadersAsync() {
        var headers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        await foreach (var (name, value, hasComma) in ParseHeadersAsync().ConfigureAwait(false)) {
            if (!headers.TryGetValue(name, out var list)) {
                list = new List<string>();
                headers[name] = list;
            }
            if (!hasComma || NeverSplitOnComma(name)) {
                list.Add(value);
            }
            else {
                foreach (var part in value.Split(',')) {
                    string trimmed = part.Trim();
                    if (trimmed.Length != 0) list.Add(trimmed);
                }
            }
        }
        return headers;
    }
}



//        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
