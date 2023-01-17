namespace CadRevealComposer.Utils.Comparers;

using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Fast comparer for Vector3 equality, with a tolerance parameter in constructor.
/// </summary>
public class XyzVector3EqualityComparer : IEqualityComparer<Vector3>
{
    private readonly float _tolerance;

    public XyzVector3EqualityComparer(float tolerance = 1e-6f)
    {
        _tolerance = tolerance;
    }

    public bool Equals(Vector3 x, Vector3 y)
    {
        return x.X.ApproximatelyEquals(y.X, _tolerance) &&
               x.Y.ApproximatelyEquals(y.Y, _tolerance) &&
               x.Z.ApproximatelyEquals(y.Z, _tolerance);
    }

    public int GetHashCode(Vector3 obj)
    {
        return HashCode.Combine(obj.X, obj.Y, obj.Z);
    }
}