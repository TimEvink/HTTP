using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MyHttp.Core.Messages;

namespace MyHttp.Core.Server;
public static class Responses {
	private static readonly HttpVersion _version = new(1, 1);
	private static readonly HttpStatusCode _statusCode = new((byte)'5', (byte)'0', (byte)'0');
	private static readonly HttpReason _reason = new("Internal Server Error"u8.ToArray());
	private static readonly byte[] _bodyBytes = """
		<html>
			<head><title>500 Internal Server Error</title></head>
			<body>
				<h1>500 Internal Server Error</h1>
				<p>An unexpected error occurred.</p>
			</body>
		</html>
		"""u8.ToArray();
	private static readonly HttpHeaders _headers = new(new(new ReadOnlyMemoryByteComparer()) {
		{ "Content-Type"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { "text/html; charset=utf-8"u8.ToArray() } },
		{ "Content-Length"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { Encoding.ASCII.GetBytes(_bodyBytes.Length.ToString()) } }
	});

	public static HttpResponse GetDefault500Response(Exception exception) {
		Console.Error.WriteLine(exception);
		return new HttpResponse(_version, _statusCode, _reason, _headers, new MemoryStream(_bodyBytes, writable: false));
	}
}
