using System.IO;
using System.Collections.Generic;

namespace MyHttp.Core.Messages;
//base class for requests & responses
public abstract class HttpMessage {
    public HttpVersion Version { get; }
    public HttpHeaders Headers { get; }
    public Stream Body { get; }

    internal HttpMessage(HttpVersion version, HttpHeaders headers, Stream body) {
        Version = version;
        //copy dictionary for immutability
        Headers = headers;
        Body = body;
    }
}
