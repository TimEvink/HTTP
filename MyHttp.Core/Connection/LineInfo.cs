namespace MyHttp.Core.Connection;
internal readonly struct LineInfo {
    internal readonly int Start;
    internal readonly int Length;
    internal readonly int ColonOffset; // -1 if none

    internal LineInfo(int start, int length, int colonOffset) {
        Start = start;
        Length = length;
        ColonOffset = colonOffset;
    }
}
