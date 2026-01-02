using System.IO;
using System.Collections.Generic;

using MyHttp.Core.Framing;
using MyHttp.Core.Exceptions;
using MyHttp.Core.Utilities;

namespace MyHttp.Core.Framing;
internal static class DetectFraming {
    internal static FramingMethod GetFramingMethod(IReadOnlyDictionary<string, string> headers) {
        if (headers.TryGetValue("Transfer-Encoding", out var encoding)) {
            switch (encoding) {
                case "chunked":
                    return FramingMethod.CHUNKED;
            }
        }
        if (Helpers.TryGetContentLength(headers, out long _)) {
            return FramingMethod.CONTENTLENGTH;
        }
        throw new BadMessageException("Failed to get Framing method.");
    }

    internal static Stream GetEncodedStream(IReadOnlyDictionary<string, string> headers, Stream stream) {
        FramingMethod framingmethod = GetFramingMethod(headers);
        switch (framingmethod) {
            case FramingMethod.CONTENTLENGTH:
                if (!Helpers.TryGetContentLength(headers, out long length)) {
                    throw new BadMessageException("Failed parsing the Content-Length value");
                }
                return new ContentLengthEncodingStream(stream, length);
            case FramingMethod.CHUNKED:
            //to be implemented later
            default:
                throw new BadMessageException($"Framing method ({framingmethod}) not supported");
        }
    }

    internal static Stream GetDecodedStream(IReadOnlyDictionary<string, string> headers, Stream stream) {
        FramingMethod framingmethod = GetFramingMethod(headers);
        switch (framingmethod) {
            case FramingMethod.CONTENTLENGTH:
                if (!Helpers.TryGetContentLength(headers, out long length)) {
                    throw new BadMessageException("Failed parsing the Content-Length value");
                }
                return new ContentLengthDecodingStream(stream, length);
            case FramingMethod.CHUNKED:
            //to be implemented later
            default:
                throw new BadMessageException($"Framing method ({framingmethod}) not supported");
        }
    }
}