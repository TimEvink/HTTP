using System;
using System.IO;
using System.Collections.Generic;

using MyHttp.Core.Exceptions;
using MyHttp.Core.Messages;
using MyHttp.Core.Framing;
using MyHttp.Core.Utilities;
using System.Runtime.CompilerServices;

namespace MyHttp.Core.Parsing;
public sealed class HttpRequestParser : HttpMessageParser {
    public HttpRequestParser(Stream stream) : base(stream) { }

    public HttpRequestParser(
        Stream stream,
        int maxStartLineLength,
        int maxHeaderLineLength,
        int maxHeaderCount
    ) : base(stream, maxStartLineLength, maxHeaderLineLength, maxHeaderCount) {}

    private (HttpMethod, HttpRequestTarget, HttpVersion) ParseRequestLine() {
        string firstline = ReadLineWithLimit(_maxStartLineLength);
        string[] parts = firstline.Split(' ');
        if (parts.Length != 3) {
            throw new BadRequestException("Incorrect syntax for first request line");
        }
        if (!Enum.TryParse<HttpMethod>(parts[0], ignoreCase: true, out var method)) {
            throw new BadRequestException("Incorrect Http method");
        }
        if (!HttpParseHelpers.TryParseHttpVersion(parts[2], out HttpVersion version)) {
            throw new BadRequestException("Incorrect Http version");
        }
        return (method, new HttpRequestTarget(parts[1]), version);
    }

    public HttpRequest Parse() {
        var (method, target, version) = ParseRequestLine();
        var headers = ParseHeaders();
        Stream body = Helpers.ShouldConsiderBody(headers) ? DetectFraming.GetDecodedStream(headers, _stream) : Stream.Null;
        return new HttpRequest(method, target, version, headers, body);
    }
}