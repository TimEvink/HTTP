using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using MyHttp.Core.Builders;
using MyHttp.Core.Messages;
using MyHttp.Core.Server;

namespace MyHttp.FileHost;
public class FileHost {
	private static readonly Dictionary<string, string> _MimeTypes = new(StringComparer.OrdinalIgnoreCase) {
		[".html"] = "text/html; charset=utf-8",
		[".htm"] = "text/html; charset=utf-8",
		[".css"] = "text/css; charset=utf-8",
		[".js"] = "application/javascript; charset=utf-8",
		[".json"] = "application/json; charset=utf-8",
		[".txt"] = "text/plain; charset=utf-8",

		[".png"] = "image/png",
		[".jpg"] = "image/jpeg",
		[".jpeg"] = "image/jpeg",
		[".gif"] = "image/gif",
		[".svg"] = "image/svg+xml",
		[".ico"] = "image/x-icon",
		[".webp"] = "image/webp",

		[".mp4"] = "video/mp4",
		[".webm"] = "video/webm",

		[".mp3"] = "audio/mpeg",
		[".wav"] = "audio/wav",

		[".woff"] = "font/woff",
		[".woff2"] = "font/woff2",
		[".ttf"] = "font/ttf",

		[".pdf"] = "application/pdf",
		[".zip"] = "application/zip"
	};

	public static async Task Main(string[] args) {
		if (!TryParseArgs(args, out int port, out string rootPath))
			return;

		HttpServer server = new(
			port,
			request => FileHostHandler(request, rootPath),
			loggingEnabled: true
		);

		await server.RunAsync();
	}
	private static bool TryParseArgs(string[] args, out int port, out string rootPath) {
		try {
			if (args.Length != 2)
				throw new ArgumentException("Expected 2 arguments: <port> <absolutePath>");

			// Parse port
			if (!int.TryParse(args[0], out var portRaw) || portRaw <= 0 || portRaw > 65535)
				throw new ArgumentException("Invalid port number.");

			port = portRaw;

			// Parse path
			rootPath = args[1];

			if (!Path.IsPathRooted(args[1]))
				throw new ArgumentException("Path must be absolute.");

			rootPath = Path.GetFullPath(args[1]);
			return true;
		} catch (Exception exception) {
			Console.WriteLine($"Failed to parse arguments: {exception.Message}");
			port = 0;
			rootPath = "";
			return false;
		}
	}

	private static HttpResponse FileHostHandler(HttpRequest request, string rootPath) {
		try {
			if (request.Method != HttpMethod.GET && request.Method != HttpMethod.HEAD)
				return Responses.NotAllowed();

			string url = request.Target.RawUrl.Split('?')[0];
			if (url == "/")
				url = "/index.html";

			string fullPath = Path.GetFullPath(Path.Combine(rootPath, url.TrimStart('/')));

			if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
				throw new UnauthorizedAccessException();

			if (Directory.Exists(fullPath)) {
				fullPath = Path.Combine(fullPath, "index.html");
			}

			if (!File.Exists(fullPath))
				return Responses.NotFound();

			if (!_MimeTypes.TryGetValue(Path.GetExtension(fullPath), out var mimeType))
				mimeType = "application/octet-stream";

			var builder = new HttpResponseBuilder(200, "OK")
				.WithHeader("Content-Type", mimeType)
				.WithHeader("Date", DateTime.UtcNow.ToString("r"))
				.WithHeader("Server", "MyHttpServer/0.2");

			if (request.Method == HttpMethod.HEAD) {
				return builder
					.WithHeader("Content-Length", new FileInfo(fullPath).Length.ToString())
					.Build();
			}

			Stream body = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 8 * 1024, useAsync: true);

			return builder
				.WithHeader("Content-Length", body.Length.ToString())
				.WithBody(body)
				.Build();
		} catch (UnauthorizedAccessException) {
			return new HttpResponseBuilder(403, "Forbidden")
				.WithHeader("Content-Type", "text/html; charset=utf-8")
				.WithHeader("Content-Length", "0")
				.Build();
		} catch {
			return Responses.InternalServerError();
		}
	}
}
