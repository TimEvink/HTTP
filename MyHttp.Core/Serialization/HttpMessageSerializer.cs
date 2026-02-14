using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using MyHttp.Core.Messages;
using MyHttp.Core.Framing;

namespace MyHttp.Core.Serialization;
public abstract class HttpMessageSerializer {
    protected readonly Stream _stream;
    protected HttpMessageSerializer(Stream stream, Memory<byte> leftover) {
        _stream = stream;
    }

    protected void WriteAscii(string text) {
        ReadOnlySpan<byte> bytes = Encoding.ASCII.GetBytes(text);
        _stream.Write(bytes);

    }

    protected void SerializeHeaders(IReadOnlyDictionary<string, string> headers) {
        foreach (var pair in headers) {
            WriteAscii($"{pair.Key}: {pair.Value}\r\n");
        }
        WriteAscii("\r\n");
    }
    protected void SerializeBody(IReadOnlyDictionary<string, string> headers, Stream body) {
        Stream encodedstream = DetectFraming.GetEncodedStream(headers, body);
        encodedstream.CopyTo(_stream);
    }
}