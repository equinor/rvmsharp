namespace CadRevealComposer.AlgebraExtensions;

using System.Numerics;

// ReSharper disable once InconsistentNaming -- Common name is lowercase i
// ReSharper disable once UnusedType.Global -- Potentially useful in the future
public readonly record struct Vector3i(int X, int Y, int Z)
{
    public static Vector3i One { get; } = new Vector3i(1, 1, 1);

    public static Vector3 operator *(Vector3i a, float b)
    {
        return new Vector3(a.X * b, a.Y * b, a.Z * b);
    }

    public static Vector3i operator +(Vector3i a, Vector3i b)
    {
        return new Vector3i(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }
}
