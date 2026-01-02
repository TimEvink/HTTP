using System.Collections.Generic;

namespace MyHttp.Core.Utilities;

internal static class Helpers {
    internal static bool ShouldConsiderBody(IReadOnlyDictionary<string, string> headers) {
        if (TryGetContentLength(headers, out var _)) return true;
        if (headers.TryGetValue("Transfer-Encoding", out string? encoding)) {
            switch (encoding) {
                case "chunked":
                    return true;
                default:
                    return false;
            }
        }
        return false;
    }

    internal static bool TryGetContentLength(IReadOnlyDictionary<string, string> headers, out long length) {
        length = 0;
        if (!headers.TryGetValue("Content-Length", out string? raw)) return false;
        if (!long.TryParse(raw, out long parsed)) return false;
        if (parsed < 0) return false;
        length = parsed;
        return true;
    }
}
