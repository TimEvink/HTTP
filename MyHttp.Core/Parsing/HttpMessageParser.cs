using System;
using System.IO;
using System.Collections.Generic;

using MyHttp.Core.Messages;
using MyHttp.Core.Exceptions;

namespace MyHttp.Core.Parsing;
public abstract class HttpMessageParser {
    protected readonly StreamReader Reader;
    protected HttpMessageParser(Stream stream) {
        Reader = new StreamReader(stream, leaveOpen: true);
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
        string? headerline;
        while ((headerline = Reader.ReadLine()) != null) {
            if (headerline.Length == 0) break;
            if (!TryParseHeaderLine(headerline, out string headername, out string headervalue)) {
                throw new BadRequestException($"Incorrect header syntax for line: {headerline}");
            }
            headers.Add(headername, headervalue);
        }
        return headers;
    }

}