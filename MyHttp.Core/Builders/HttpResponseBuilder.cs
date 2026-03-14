using MyHttp.Core.Messages;

namespace MyHttp.Core.Builders;
internal class HttpResponseBuilder : HttpMessageBuilder<HttpResponseBuilder> {
	private readonly HttpStatusCode _statusCode;
	private readonly HttpReason _reason;

	public HttpResponseBuilder(HttpStatusCode statusCode, HttpReason reason) {
		_statusCode = statusCode;
		_reason = reason;
	}

	public HttpResponseBuilder(ushort statusCode, string reason) {
		_statusCode = new(
			(byte)('0' + statusCode / 100),
			(byte)('0' + statusCode / 10 % 10),
			(byte)('0' + statusCode % 10)
		);
		_reason = new(reason);
	}

	public HttpResponse Build() {
		return new(_version, _statusCode, _reason, new(_rawHeaders), _body);
	}
}
