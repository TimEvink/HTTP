using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MyHttp.Core.Messages;

namespace MyHttp.Server;
public static class HttpResponses {
	public static HttpResponse Ok(string message) {
		HttpVersion version = new(1, 1);
		HttpStatusCode statusCode = new((byte)'2', (byte)'0', (byte)'0');
		HttpReason reason = new("OK");
		Dictionary<ReadOnlyMemory<byte>, List<ReadOnlyMemory<byte>>> headersRaw = new(new ReadOnlyMemoryByteComparer()) {
			{ "Content-Length"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { Encoding.ASCII.GetBytes(message.Length.ToString()) } }
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
			{ "Content-Length"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { Encoding.ASCII.GetBytes(message.Length.ToString()) } }
		};
		HttpHeaders headers = new(headersRaw);
		Stream body = new MemoryStream(Encoding.UTF8.GetBytes(message));

		return new HttpResponse(version, statusCode, reason, headers, body);
	}
}
