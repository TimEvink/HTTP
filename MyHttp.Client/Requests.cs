using MyHttp.Core.Messages;
using MyHttp.Core.Builders;

namespace MyHttp.Client;
public static class Requests {
	public static HttpRequest Get(string path = "/") => new HttpRequestBuilder(HttpMethod.GET, path)
		.WithHeader("Accept", "text/html")
		.WithHeader("Host", "localhost")
		.WithHeader("Connection", "close")
		.WithHeader("Accept-Language", "en-US")
		.WithHeader("Content-Length", "0")
		.Build();
}
