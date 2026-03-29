using MyHttp.Core.Messages;
using MyHttp.Core.Builders;

namespace MyHttp.Client;
internal static class Requests {
	internal static HttpRequest Get(string path = "/") => new HttpRequestBuilder(HttpMethod.GET, path)
		.WithHeader("Accept", "text/html")
		.WithHeader("Host", "localhost")
		.WithHeader("Connection", "close")
		.WithHeader("Accept-Language", "en-US")
		.WithHeader("Content-Length", "0")
		.Build();
}
