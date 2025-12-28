using System.Collections.Generic;
using System.IO;

using MyHttp.Core.Messages;

namespace MyHttp.Core.Serialization;
public abstract class HttpMessageSerializer {
    protected readonly StreamWriter Writer;
    protected HttpMessageSerializer(Stream stream) {
        Writer = new(stream, System.Text.Encoding.ASCII, leaveOpen: true) {
            NewLine = "\r\n"
        };
    }

    protected void SerializeHeaders(IReadOnlyDictionary<string, string> headers) {
        foreach (var pair in headers) {
            Writer.WriteLine($"{pair.Key}: {pair.Value}");
        }
        Writer.WriteLine();
    }
}