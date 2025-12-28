using System.Collections.Generic;
using System.IO;

using MyHttp.Core.Messages;

namespace MyHttp.Core.Serialization;
public abstract class HttpMessageSerializer {
    protected readonly StreamWriter _writer;
    protected HttpMessageSerializer(Stream stream) {
        _writer = new(stream, System.Text.Encoding.ASCII, leaveOpen: true) {
            NewLine = "\r\n"
        };
    }

    protected void SerializeHeaders(IReadOnlyDictionary<string, string> headers) {
        foreach (var pair in headers) {
            _writer.WriteLine($"{pair.Key}: {pair.Value}");
        }
        _writer.WriteLine();
    }
}