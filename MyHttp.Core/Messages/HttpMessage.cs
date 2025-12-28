using System.IO;
using System.Collections.Generic;

namespace MyHttp.Core.Messages;
//base class for requests & responses
public abstract class HttpMessage {
    public HttpVersion Version { get; }
    public IReadOnlyDictionary<string, string> Headers { get; }
    public Stream Body { get; }

    internal HttpMessage(HttpVersion version, Dictionary<string, string> headers, Stream body) {
        Version = version;
        //copy dictionary for immutability
        Headers = new Dictionary<string, string>(headers);
        Body = body;
    }
}