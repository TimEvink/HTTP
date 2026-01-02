using System;

using MyHttp.Core.Messages;

namespace MyHttp.Core.Parsing;

internal static class HttpParseHelpers {
    internal static bool TryParseHttpVersion(string rawVersion, out HttpVersion version) {
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
    internal static bool TryParseHeaderLine(string rawheaderline, out string headername, out string headervalue) {
        headername = "";
        headervalue = "";
        int colon = rawheaderline.IndexOf(':');
        if (colon <= 0) return false;
        headername = rawheaderline[..colon].Trim();
        headervalue = rawheaderline[(colon + 1)..].Trim();
        return true;
    }
}