using System;
using System.Text;

namespace MyHttp.Core.Messages;
public sealed class HttpRequestTarget {
    internal ReadOnlyMemory<byte> _rawUrl;
    private string? _cachedUrl;

    public string RawUrl => _cachedUrl ??= Encoding.ASCII.GetString(_rawUrl.Span);
    internal HttpRequestTarget(ReadOnlyMemory<byte> rawUrl) {
        _rawUrl = rawUrl;
    }
}
