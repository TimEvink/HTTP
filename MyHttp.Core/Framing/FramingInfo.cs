namespace MyHttp.Core.Framing;
internal readonly struct FramingInfo {
    internal FramingMethod Method { get; }
    internal long ContentLength { get; } //only meaningful if Method == FramingMethod.CONTENTLENGTH, set to 0 otherwise.
    internal bool HasBody => Method != FramingMethod.NONE;

    private FramingInfo(FramingMethod method, long contentLength) {
        Method = method;
        ContentLength = contentLength;
    }

    internal static FramingInfo FromNone() => new(FramingMethod.NONE, 0);
    internal static FramingInfo FromContentLength(long length) => length <= 0 ? new(FramingMethod.NONE, 0) : new(FramingMethod.CONTENTLENGTH, length);
    internal static FramingInfo FromChunked() => new(FramingMethod.CHUNKED, 0);
}
