using System;

using MyHttp.Core.Messages;

namespace MyHttp.Core.Builders;
public class HttpRequestBuilder : HttpMessageBuilder<HttpRequestBuilder> {
	private readonly HttpMethod _method;
	private readonly HttpRequestTarget _target;

	public HttpRequestBuilder(HttpMethod method, HttpRequestTarget uri) {
		_method = method;
		_target = uri;
	}

	public HttpRequestBuilder(HttpMethod method, string uri) {
		_method = method;
		_target = new(uri);
	}

	public HttpRequestBuilder(HttpMethod method, ReadOnlyMemory<byte> uri) {
		_method = method;
		_target = new(uri);
	}

	public HttpRequest Build() {
		return new(_method, _target, _version, new(_rawHeaders), _body);
	}
}
