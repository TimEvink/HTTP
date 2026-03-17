using System;

namespace MyHttp.Core.Parsing;
internal ref struct CommaSplitEnumerator {
	private readonly ReadOnlySpan<byte> _span;
	private int _index;
	private int _start;
	private int _end;

	private const byte HTAB = 0x09; // '\t'
	private const byte SPACE = 0x20; // ' '
	private const byte COMMA = 0x2c; // ','

	internal ReadOnlySpan<byte> Current { get; private set; }

	internal CommaSplitEnumerator(ReadOnlySpan<byte> span) {
		_span = span;
		_index = 0;
		_start = -1;
		_end = -1;
		Current = default;
	}

	internal bool MoveNext() {
		while (_index < _span.Length) {
			byte b = _span[_index];
			if (b == COMMA) {
				if (_start == -1) {
					_index++;
					continue;
				}
				Current = _span[_start..(_end + 1)];
				_start = -1;
				_index++;
				return true;
			}
			if (b == SPACE || b == HTAB) {
				_index++;
				continue;
			}
			if (_start == -1) {
				_start = _index;
			}
			_end = _index++;
		}
		if (_start == -1) return false;
		Current = _span[_start..(_end + 1)];
		_start = -1;
		return true;
	}
}
