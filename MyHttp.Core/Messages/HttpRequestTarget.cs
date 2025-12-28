namespace MyHttp.Core.Messages;

public sealed class HttpRequestTarget {
    public string Raw { get; }
    public string Path { get; }
    public string? Query { get; }

    public HttpRequestTarget(string raw) {
        Raw = raw;
        int q = Raw.IndexOf('?');
        if (q >= 0) {
            Path = Raw[..q];
            Query = Raw[(q + 1)..];
        } else {
            Path = Raw;
            Query = null;
        }
    }
}