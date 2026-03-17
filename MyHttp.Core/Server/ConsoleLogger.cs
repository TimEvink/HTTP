using System;

using MyHttp.Core.Messages;

namespace MyHttp.Core.Server;
internal static class ConsoleLogger {
	private const string Reset = "\x1b[0m";
	private const string Green = "\x1b[32m";
	private const string Red = "\x1b[31m";
	private const string Cyan = "\x1b[36m";

	private static string Timestamp() =>
		DateTime.Now.ToString("HH:mm:ss");

	internal static void LogInfo(string message) {
		Console.WriteLine($"{Green}[{Timestamp()}] {message}{Reset}");
	}

	internal static void LogRequest(HttpRequest request, HttpResponse response) {
		string method = request.Method.ToString();
		string url = request.Target.RawUrl;
		ushort status = response.StatusCode.ToUInt16();

		string color = status >= 400
			? Red
			: Cyan;

		Console.WriteLine(
			$"{color}[{Timestamp()}] {method} {url} -> {status}{Reset}"
		);
	}
}

