namespace CadRevealComposer.AlgebraExtensions;

using System.Numerics;

public readonly record struct Triangle(Vector3 V1, Vector3 V2, Vector3 V3)
{
    public Bounds Bounds => new(Vector3.Min(V1, Vector3.Min(V2, V3)), Vector3.Max(V1, Vector3.Max(V2, V3)));
}