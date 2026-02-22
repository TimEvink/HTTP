using System;
using System.IO;
using System.Collections.Generic;

using MyHttp.Core.Messages;

namespace MyHttp.Client;
public static class Requests {
	public static HttpRequest Get(string path = "/") {
		HttpVersion version = new(1, 1);
		HttpRequestTarget target = new(path);
		Dictionary<ReadOnlyMemory<byte>, List<ReadOnlyMemory<byte>>> headersRaw = new(new ReadOnlyMemoryByteComparer()) {
			{ "Host"u8.ToArray(), new List<ReadOnlyMemory<byte>>(1) { "localhost"u8.ToArray() } },
		};
		HttpHeaders headers = new(headersRaw);
		return new HttpRequest(HttpMethod.GET, target, version, headers, Stream.Null);
	}
}
