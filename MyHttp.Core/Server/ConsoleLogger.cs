using System;

using MyHttp.Core.Messages;

namespace MyHttp.Core.Server;
internal static class ConsoleLogger {
	private const string Reset = "\x1b[0m";
	private const string Green = "\x1b[32m";
	private const string Yellow = "\x1b[33m";
	private const string Red = "\x1b[31m";
	private const string Cyan = "\x1b[36m";

	private static string Timestamp() =>
		DateTime.Now.ToString("HH:mm:ss");

	internal static void LogInfo(string message) {
		Console.WriteLine($"{Green}[{Timestamp()}] {message}{Reset}");
	}

	internal static void LogRequest(HttpRequest request, HttpResponse response, TimeSpan duration) {
		string methodPadded = request.Method.ToString().PadRight(6);
		string url = request.Target.RawUrl;
		string urlPadded = url.Length > 20
			? string.Concat(url.AsSpan(0, 17), "...")
			: url.PadRight(20);
		ushort status = response.StatusCode.ToUInt16();
		string time = duration.TotalMilliseconds >= 1
			? $"{duration.TotalMilliseconds:0.0} ms"
			: $"{duration.TotalMilliseconds * 1000:0} µs";

		string color = status switch {
			>= 500 => Red,
			>= 400 => Yellow,
			>= 300 => Cyan,
			_ => Green
		};

		Console.WriteLine(
			$"{color}[{Timestamp()}] {methodPadded} {urlPadded} {status,3} {time, 7}{Reset}"
		);
	}
}

