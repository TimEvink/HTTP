using System;
using System.IO;
using System.Collections.Generic;

using MyHttp.Core.Messages;
using MyHttp.Core.Exceptions;

namespace MyHttp.Core.Parsing;
public sealed class HttpRequestParser : HttpMessageParser {
    public HttpRequestParser(Stream stream) : base(stream) { }

    private (HttpMethod, HttpRequestTarget, HttpVersion) ParseRequestLine() {
        string? firstline = ReadLineWithLimit(_maxStartLineLength);
        if (firstline == null) {
            throw new BadRequestException("Unexpected end of stream");
        }
        string[] parts = firstline.Split(' ', options: StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) {
            throw new BadRequestException("Incorrect syntax for first request line");
        }
        if (!Enum.TryParse<HttpMethod>(parts[0], ignoreCase: true, out var method)) {
            throw new BadRequestException("Incorrect Http method");
        }

        if (!TryParseHttpVersion(parts[2], out HttpVersion version)) {
            throw new BadRequestException("Incorrect Http version");
        }
        return (method, new HttpRequestTarget(parts[1]), version);
    }

    public HttpRequest Parse() {
        var (method, target, version) = ParseRequestLine();
        var headers = ParseHeaders();
        return new HttpRequest(method, target, version, headers, _reader.BaseStream);
    }
}