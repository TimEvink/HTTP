using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using MyHttp.Core.Messages;
using MyHttp.Core.Exceptions;

namespace MyHttp.Core.Parsing;
public abstract class HttpMessageParser {
    //reader is not allowed to read message bodies.
    protected readonly StreamReader _reader;
    protected readonly int _maxStartLineLength;
    protected readonly int _maxHeaderLineLength;
    protected readonly int _maxHeaderCount;
    protected HttpMessageParser(
        Stream stream,
        int maxStartLineLength = 8192,
        int maxHeaderLineLength = 8192,
        int maxHeaderCount = 100
    ) {
        _reader = new(stream, leaveOpen: true);
        _maxStartLineLength = maxStartLineLength;
        _maxHeaderLineLength = maxHeaderLineLength;
        _maxHeaderCount = maxHeaderCount;
    }

    protected string ReadLineWithLimit(int maxChars) {
        StringBuilder stringbuilder = new();
        while (true) {
            int c = _reader.Read();
            if (c == -1) throw new BadMessageException("Unexpected end of stream");
            if (c == '\n') break;
            if (c != '\r') stringbuilder.Append((char)c);
            if (stringbuilder.Length > maxChars) throw new BadMessageException("Line too long");
        }
        return stringbuilder.ToString();
    }

    protected static bool TryParseHttpVersion(string rawVersion, out HttpVersion version) {
        version = default;
        if (!rawVersion.StartsWith("HTTP/", StringComparison.Ordinal)) return false;
        var numbers = rawVersion.Substring(5);
        int dot = numbers.IndexOf('.');
        if (dot < 0) return false;
        if (!int.TryParse(numbers[..dot], out int major)) return false;
        if (!int.TryParse(numbers[(dot + 1)..], out int minor)) return false;
        version = new HttpVersion(major, minor);
        return true;
    }
    protected static bool TryParseHeaderLine(string rawheaderline, out string headername, out string headervalue) {
        headername = "";
        headervalue = "";
        int colon = rawheaderline.IndexOf(':');
        if (colon <= 0) return false;
        headername  = rawheaderline[..colon].Trim();
        headervalue = rawheaderline[(colon + 1)..].Trim();
        return true;
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
            if (!TryParseHeaderLine(headerline, out string headername, out string headervalue)) {
                throw new BadMessageException($"Incorrect header syntax for line: {headerline}");
            }
            headers.Add(headername, headervalue);
        }
        return headers;
    }
}