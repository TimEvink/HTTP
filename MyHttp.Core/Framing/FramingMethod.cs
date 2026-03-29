namespace MyHttp.Core.Framing;
internal enum FramingMethod : byte {
    NONE,
    CONTENTLENGTH,
    CHUNKED
}
