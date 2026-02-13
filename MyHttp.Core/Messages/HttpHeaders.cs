using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using MyHttp.Core.Exceptions;
using MyHttp.Core.Framing;

namespace MyHttp.Core.Messages;
public sealed class HttpHeaders : IReadOnlyDictionary<string, string[]> {
    internal readonly Dictionary<ReadOnlyMemory<byte>, List<ReadOnlyMemory<byte>>> _raw;
    private readonly Dictionary<ReadOnlyMemory<byte>, string> _keyCache = new(new ReadOnlyMemoryByteComparer());
    private readonly Dictionary<string, string[]> _stringCache = new(StringComparer.OrdinalIgnoreCase);

    // input to constructor has to use the internal ReadOnlyMemoryByteComparer as comparer!
    internal HttpHeaders(Dictionary<ReadOnlyMemory<byte>, List<ReadOnlyMemory<byte>>> inner) {
        _raw = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public string[] this[string key] {
        get {
            if (_stringCache.TryGetValue(key, out var cached)) return cached;
            if (_raw.TryGetValue(Encoding.ASCII.GetBytes(key), out var values)) {
                var strings = new string[values.Count];
                for (int i = 0; i < values.Count; i++)
                    strings[i] = Encoding.ASCII.GetString(values[i].Span);
                _stringCache[key] = strings;
                return strings;
            }
            throw new KeyNotFoundException(key);
        }
    }

    public IEnumerable<string> Keys {
        get {
            foreach (var key in _raw.Keys) {
                if (!_keyCache.TryGetValue(key, out var str)) {
                    str = Encoding.ASCII.GetString(key.Span);
                    _keyCache[key] = str;
                }
                yield return str;
            }
        }
    }

    public IEnumerable<string[]> Values {
        get { foreach (var key in Keys) yield return this[key]; }
    }

    public bool ContainsKey(string key) {
        if (key is null) throw new ArgumentNullException(nameof(key));
        return _raw.ContainsKey(Encoding.ASCII.GetBytes(key));
    }
    public bool TryGetValue(string key, [NotNullWhen(true)] out string[]? value) {
        if (_stringCache.TryGetValue(key, out value)) return true;
        if (_raw.TryGetValue(Encoding.ASCII.GetBytes(key), out var values)) {
            value = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
                value[i] = Encoding.ASCII.GetString(values[i].Span);
            _stringCache[key] = value;
            return true;
        }
        value = null;
        return false;
    }

    public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator() {
        foreach (var key in Keys) yield return new KeyValuePair<string, string[]>(key, this[key]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _raw.Count;

    internal FramingInfo GetFramingInfo() {
        if (this.TryGetValue("Transfer-Encoding", out string[]? encodingraw)) {
            string encoding = encodingraw[0];
            switch (encoding) {
                case "chunked":
                    return FramingInfo.FromChunked();
                default:
                    throw new BadMessageException("Unsupported Transfer-Encoding value found");
            }
        }
        if (this.TryGetValue("Content-Length", out string[]? listraw)) {
            if (long.TryParse(listraw[0], out long length)) return FramingInfo.FromContentLength(length);
        }
        var test = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return FramingInfo.FromNone();
    }
}

// for internal header storage.
// compares case insensitively
internal sealed class ReadOnlyMemoryByteComparer : IEqualityComparer<ReadOnlyMemory<byte>> {
    public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y) {
        if (x.Equals(y)) return true;
        if (x.Length != y.Length) return false;
        var xspan = x.Span;
        var yspan = y.Span;
        if (FoldAscii(xspan[0]) != FoldAscii(yspan[0])) return false;
        for (int i = 1; i < xspan.Length; i++) {
            if (FoldAscii(xspan[i]) != FoldAscii(yspan[i])) return false;
        }
        return true;
    }

    public int GetHashCode(ReadOnlyMemory<byte> obj) {
        // FNV-1a hash over normalized bytes.
        unchecked {
            uint hash = 2166136261;
            foreach (byte b in obj.Span)
                hash = (hash ^ FoldAscii(b)) * 16777619;
            return (int)hash;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte FoldAscii(byte b) {
        // maps A-Z to a-z resp, leaves other asciivalues constant.
        return (byte)(b | ((byte)(b - 'A') <= ('Z' - 'A') ? 0x20 : 0));
    }
}
