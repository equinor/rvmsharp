namespace CadRevealComposer.Utils.Comparers;

using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Fast comparer for Vector3 exact equality.
/// </summary>
public class XyzVector3EqualityComparer : IEqualityComparer<Vector3>
{
    public bool Equals(Vector3 x, Vector3 y)
    {
        return x.X.Equals(y.X) && x.Y.Equals(y.Y) && x.Z.Equals(y.Z);
    }

    public int GetHashCode(Vector3 obj)
    {
        return HashCode.Combine(obj.X, obj.Y, obj.Z);
    }
}