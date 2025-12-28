using System;
using System.IO;
using System.Collections.Generic;

using MyHttp.Core.Messages;
using MyHttp.Core.Exceptions;

namespace MyHttp.Core.Parsing;
public sealed class HttpResponseParser : HttpMessageParser {
    public HttpResponseParser(Stream stream) : base(stream) { }

    public HttpResponseParser(
        Stream stream,
        int maxStartLineLength,
        int maxHeaderLineLength,
        int maxHeaderCount
    ) : base(stream, maxStartLineLength, maxHeaderLineLength, maxHeaderCount) {}

    private (HttpVersion, int, string) ParseResponseLine() {
        string? firstline = ReadLineWithLimit(_maxStartLineLength);
        if (firstline == null) {
            throw new BadResponseException("Unexpected end of stream.");
        }
        string[] parts = firstline.Split(' ', 3, options: StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) {
            throw new BadResponseException("Incorrect syntax for first response line.");
        }
        if (!TryParseHttpVersion(parts[0], out HttpVersion version)) {
            throw new BadResponseException("Incorrect Http version.");
        }
        if (!int.TryParse(parts[1], out int statuscode) || statuscode < 100 || statuscode > 599) {
            throw new BadResponseException("Invalid Http status code");
        }
        return (version, statuscode, parts[2]);
    }

    public HttpResponse Parse() {
        var (version, statuscode, message) = ParseResponseLine();
        var headers = ParseHeaders();
        return new HttpResponse(version, statuscode, message, headers, _reader.BaseStream);
    }
}