using System;
using System.Text;

namespace MyHttp.Core.Messages;
public sealed class HttpReason {
	internal ReadOnlyMemory<byte> _rawMessage;
	private string? _cachedMessage;

	public string Message => _cachedMessage??= Encoding.ASCII.GetString(_rawMessage.Span);
	public HttpReason(ReadOnlyMemory<byte> rawMessage) {
		_rawMessage = rawMessage;
	}

	public HttpReason(string message) {
		_rawMessage = Encoding.ASCII.GetBytes(message);
	}
}
