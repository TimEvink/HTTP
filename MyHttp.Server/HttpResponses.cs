using System;

using MyHttp.Core.Messages;
using MyHttp.Core.Builders;

namespace MyHttp.Core.Server;
public static class HttpResponses {
	public static HttpResponse Ok(string message) => new HttpResponseBuilder(200, "OK")
		.WithHeader("Content-Type", "text/html; charset=utf-8")
		.WithHeader("Date", DateTime.UtcNow.ToString("r"))
		.WithHeader("Server", "MyHttpServer/0.2")
		.WithBody(@$"<html><body style=""text-align:center; font-family:sans-serif;""><h1>{message}</h1></body></html>")
		.Build();

	public static HttpResponse BadRequest(string message) => new HttpResponseBuilder(400, "OK")
		.WithHeader("Content-Type", "text/html; charset=utf-8")
		.WithHeader("Date", DateTime.UtcNow.ToString("r"))
		.WithHeader("Server", "MyHttpServer/0.2")
		.WithBody(@$"<html><body style=""text-align:center; font-family:sans-serif;""><h1>{message}</h1></body></html>")
		.Build();

	public static HttpResponse NotFound() => new HttpResponseBuilder(404, "Not Found")
		.WithHeader("Content-Type", "text/html; charset=utf-8")
		.WithBody(@"<html><body style=""text-align:center; font-family:sans-serif;""><h1>404 Not Found</h1></body></html>")
		.Build();
}
