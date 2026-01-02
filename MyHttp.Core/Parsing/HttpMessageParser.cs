using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using MyHttp.Core.Exceptions;
using MyHttp.Core.Messages;
using MyHttp.Core.Parsing;
using MyHttp.Core.Framing;

namespace MyHttp.Core.Parsing;
public abstract class HttpMessageParser{
    protected readonly Stream _stream;
    protected readonly int _maxStartLineLength;
    protected readonly int _maxHeaderLineLength;
    protected readonly int _maxHeaderCount;
    protected HttpMessageParser(
        Stream stream,
        int maxStartLineLength = 8192,
        int maxHeaderLineLength = 8192,
        int maxHeaderCount = 100
    ) {
        _stream = stream;
        _maxStartLineLength = maxStartLineLength;
        _maxHeaderLineLength = maxHeaderLineLength;
        _maxHeaderCount = maxHeaderCount;
    }

    protected string ReadLineWithLimit(int maxChars) {
        StringBuilder stringbuilder = new();
        while (true) {
            int c = _stream.ReadByte();
            if (c == -1) throw new BadMessageException("Unexpected end of stream");
            if (c == '\n') break;
            if (c != '\r') stringbuilder.Append((char)c);
            if (stringbuilder.Length > maxChars) throw new BadMessageException("Line too long");
        }
        return stringbuilder.ToString();
    }

    protected Dictionary<string, string> ParseHeaders() {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        int headercount = 0;
        while (true) {
            string? headerline = ReadLineWithLimit(_maxHeaderLineLength);
            if (headerline.Length == 0) break;
            headercount++;
            if (headercount > _maxHeaderCount) {
                throw new BadMessageException($"Maximum header count ({_maxHeaderCount}) exceeded.");
            }
            if (!HttpParseHelpers.TryParseHeaderLine(headerline, out string headername, out string headervalue)) {
                throw new BadMessageException($"Incorrect header syntax for line: {headerline}");
            }
            headers.Add(headername, headervalue);
        }
        return headers;
    }
}