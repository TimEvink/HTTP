using System;
using System.Collections.Generic;
using System.Linq;

namespace MyHttp.Core.Messages;
internal sealed class HttpHeaders {
    private readonly Dictionary<string, List<string>> _headers;

    internal HttpHeaders(Dictionary<string, IEnumerable<string>> headers) {
        _headers = new Dictionary<string, List<string>>(
            headers.Count,
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var (key, values) in headers) {
            _headers[key] = values.ToList();
        }
    }

    internal HttpHeaders(Dictionary<string, string> headers) : this(headers.ToDictionary(
        pair => pair.Key,
        pair => (IEnumerable<string>)new[] { pair.Value },
        StringComparer.OrdinalIgnoreCase
    )) {}

    internal void Add(string name, string value) {
        if (string.IsNullOrEmpty(name) || value == null) return;
        if (_headers.TryGetValue(name, out var list)) {
            list.Add(value);
        } else {
            _headers[name] = new List<string> { value };
        }
    }
}