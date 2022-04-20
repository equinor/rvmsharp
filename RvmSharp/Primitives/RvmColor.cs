namespace RvmSharp.Primitives;

public record RvmColor(uint ColorKind, uint ColorIndex, (byte Red, byte Green, byte Blue) Color);