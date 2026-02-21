using System;

using MyHttp.Core.Exceptions;

namespace MyHttp.Core.Messages {
	public readonly struct HttpStatusCode {
		internal readonly byte b0;
		internal readonly byte b1;
		internal readonly byte b2;

		internal HttpStatusCode(byte b0, byte b1, byte b2) {
			if ((uint)(b0 - (byte)'2') > 3)
				throw new BadResponseException("First status code digit must be in range 2-5");

			if ((uint)(b1 - (byte)'0') > 9)
				throw new BadResponseException("Second status code digit must be in range 0-9");

			if ((uint)(b2 - (byte)'0') > 9)
				throw new BadResponseException("Third status code digit must be in range 0-9");

			this.b0 = b0;
			this.b1 = b1;
			this.b2 = b2;
		}

		//public override string ToString() => string.Create(3, this, static (span, state) => {
		//	span[0] = (char)state.b0;
		//	span[1] = (char)state.b1;
		//	span[2] = (char)state.b2;
		//});

		public ushort ToUInt16() => (ushort)(
			(b0 - (byte)'0') * 100 +
			(b1 - (byte)'0') * 10 +
			(b2 - (byte)'0')
		);

		public void WriteTo(Span<byte> destination) {
			destination[0] = b0;
			destination[1] = b1;
			destination[2] = b2;
		}
	}
}
