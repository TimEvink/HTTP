using System;

namespace MyHttp.Core.Connection;
public sealed class HttpConnectionOptions {
	public int InputBufferSize { get; init; } = 16 * 1024;
	public int OutputBufferSize { get; init; } = 16 * 1024;
	public int MaxLineSize { get; init; } = 4 * 1024;
	public int MaxHeaderSize { get; init; } = 32 * 1024;
	public static HttpConnectionOptions Default { get; } = new();

	private void Validate() {
		if (InputBufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(InputBufferSize));
		if (OutputBufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(OutputBufferSize));
		if (MaxLineSize <= 0) throw new ArgumentOutOfRangeException(nameof(MaxLineSize));
		if (MaxLineSize > InputBufferSize) throw new ArgumentOutOfRangeException(nameof(MaxLineSize), MaxLineSize, "inputBufferSize must be at least MaxLineSize");
		if (MaxLineSize > OutputBufferSize) throw new ArgumentOutOfRangeException(nameof(MaxLineSize), MaxLineSize, "outputBufferSize must be at least MaxLineSize");
		if (MaxHeaderSize <= 0) throw new ArgumentOutOfRangeException(nameof(MaxHeaderSize));
	}

	public HttpConnectionOptions() {
		Validate();
	}
}
