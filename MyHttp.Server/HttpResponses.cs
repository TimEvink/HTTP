using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MyHttp.Core.Messages;
using MyHttp.Core.Builders;

namespace MyHttp.Core.Server;
public static class HttpResponses {
	public static HttpResponse Ok(string message) {
		HttpVersion version = new(1, 1);
		HttpStatusCode statusCode = new((byte)'2', (byte)'0', (byte)'0');
		HttpReason reason = new("OK");
		Dictionary<ReadOnlyMemory<byte>, List<ReadOnlyMemory<byte>>> headersRaw = new(new ReadOnlyMemoryByteComparer()) {
			{ "Content-Length"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { Encoding.ASCII.GetBytes(message.Length.ToString()) } },
			{ "Content-Type"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { "text/html; charset=utf-8"u8.ToArray() } },
			//{ "Connection"u8.ToArray(), new List<ReadOnlyMemory<byte>> { "close"u8.ToArray() } },
			{ "Date"u8.ToArray(), new List<ReadOnlyMemory<byte>> { Encoding.ASCII.GetBytes(DateTime.UtcNow.ToString("r")) } }, // RFC1123 format
			{ "Server"u8.ToArray(), new List<ReadOnlyMemory<byte>> { "MyHttpServer/0.1"u8.ToArray() } }
		};
		HttpHeaders headers = new(headersRaw);
		Stream body = new MemoryStream(Encoding.UTF8.GetBytes(message));

		return new HttpResponse(version, statusCode, reason, headers, body);
	}

	public static HttpResponse BadRequest(string message) {
		HttpVersion version = new(1, 1);
		HttpStatusCode statusCode = new((byte)'4', (byte)'0', (byte)'0');
		HttpReason reason = new("OK");
		Dictionary<ReadOnlyMemory<byte>, List<ReadOnlyMemory<byte>>> headersRaw = new(new ReadOnlyMemoryByteComparer()) {
			{ "Content-Length"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { Encoding.ASCII.GetBytes(message.Length.ToString()) } },
			{ "Content-Type"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { "text/html; charset=utf-8"u8.ToArray() } },
			//{ "Connection"u8.ToArray(), new List<ReadOnlyMemory<byte>> { "close"u8.ToArray() } },
			{ "Date"u8.ToArray(), new List<ReadOnlyMemory<byte>> { Encoding.ASCII.GetBytes(DateTime.UtcNow.ToString("r")) } }, // RFC1123 format
			{ "Server"u8.ToArray(), new List<ReadOnlyMemory<byte>> { "MyHttpServer/0.1"u8.ToArray() } }
		};
		HttpHeaders headers = new(headersRaw);
		Stream body = new MemoryStream(Encoding.UTF8.GetBytes(message));

		return new HttpResponse(version, statusCode, reason, headers, body);
	}

	public static HttpResponse NotFound()
		=> new HttpResponseBuilder(404, "Not Found")
			.WithHeader("Content-Type", "text/html; charset=utf-8")
			.WithBody(@"<html><body><h1>404 Not Found</h1></body></html>")
			.Build();
}
