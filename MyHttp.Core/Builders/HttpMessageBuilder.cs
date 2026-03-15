using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MyHttp.Core.Messages;

namespace MyHttp.Core.Builders;
//CRTP style generic builder.
public abstract class HttpMessageBuilder<TBuilder> where TBuilder : HttpMessageBuilder<TBuilder> {
	private static readonly ReadOnlyMemoryByteComparer _comparer = new();

	protected HttpVersion _version = new(1, 1);
	protected Dictionary<ReadOnlyMemory<byte>, List<ReadOnlyMemory<byte>>> _rawHeaders = new(_comparer);
	protected Stream _body = Stream.Null;

	public TBuilder WithVersion(HttpVersion version) {
		_version = version;
		return (TBuilder)this;
	}

	public TBuilder WithVersion(int major, int minor) {
		_version = new(major, minor);
		return (TBuilder)this;
	}

	public TBuilder WithHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value) {
		ReadOnlyMemory<byte> nameCopy = name.ToArray();
		ReadOnlyMemory<byte> valueCopy = value.ToArray();
		if (!_rawHeaders.TryGetValue(nameCopy, out var list)) {
			list = [];
			_rawHeaders[nameCopy] = list;
		}
		list.Add(valueCopy);
		return (TBuilder)this;
	}

	public TBuilder WithHeader(string name, string value) {
		ReadOnlyMemory<byte> rawName = Encoding.ASCII.GetBytes(name);
		ReadOnlyMemory<byte> rawValue = Encoding.ASCII.GetBytes(value);
		if (!_rawHeaders.TryGetValue(rawName, out var list)) {
			list = [];
			_rawHeaders[rawName] = list;
		}
		list.Add(rawValue);
		return (TBuilder)this;
	}

	//Also sets content-length appropriately.
	public TBuilder WithBody(string body, Encoding? encoding = null) {
		encoding ??= Encoding.UTF8;
		var bytes = encoding.GetBytes(body);
		WithHeader("Content-Length", bytes.Length.ToString());
		_body = new MemoryStream(bytes, writable: false);
		return (TBuilder)this;
	}

	//Also sets content-length appropriately.
	public TBuilder WithBody(byte[] body) {
		WithHeader("Content-Length", body.Length.ToString());
		_body = new MemoryStream(body);
		return (TBuilder)this;
	}

	public TBuilder WithBody(Stream body) {
		_body = body;
		return (TBuilder)this;
	}
}
