namespace MyHttp.Core.Messages;

public sealed class HttpRequestTarget {
    private bool _parsed = false;
    private string _rawPath;
    private string? _path;
    private string? _query;
    public string RawPath => _rawPath;
    public string Path {
        get {
            if (!_parsed) ParseRaw();
            return _path!;
        }
    }
    public string? Query {
        get {
            if (!_parsed) ParseRaw();
            return _query;
        }
    }

    private void ParseRaw() {
        int q = _rawPath.IndexOf('?');
        if (q >= 0) {
            _path = RawPath[..q];
            _query = RawPath[(q + 1)..];
        }
        else {
            _path = RawPath;
            _query = null;
        }
        _parsed = true;
    }

    public HttpRequestTarget(string rawPath) {
        _rawPath = rawPath;
    }
}