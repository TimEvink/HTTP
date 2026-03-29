using MyHttp.Core.Builders;
using MyHttp.Core.Messages;

namespace MyHttp.FileHost;
internal static class Responses {
	internal static HttpResponse NotAllowed() => new HttpResponseBuilder(405, "Method Not Allowed")
		.WithHeader("Allow", "GET, HEAD")
		.WithHeader("Content-Length", "0")
		.Build();

	internal static HttpResponse NotFound() => new HttpResponseBuilder(404, "Not Found")
		.WithHeader("Content-Type", "text/html; charset=utf-8")
		.WithBody(@"<html><body style=""text-align:center; font-family:sans-serif;""><h1>404 Not Found</h1></body></html>")
		.Build();

	internal static HttpResponse InternalServerError() => new HttpResponseBuilder(500, "Internal Server Error")
		.WithHeader("Content-Type", "text/html; charset=utf-8")
		.WithBody(@"<html><body style=""text-align:center; font-family:sans-serif;""><h1>500 Internal Server Error</h1></body></html>")
		.Build();
}
