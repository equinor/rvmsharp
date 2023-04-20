namespace CadRevealComposer.Utils.Comparers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;

/// <summary>
/// Used to have a repeatable comparison between Vector3s when sorting.
/// Compares on X, then Y, then Z.
/// The intended usage is to be able to use Vector3 in a <see cref="ImmutableSortedSet{T}"/>
/// </summary>
public class XyzVector3Comparer : IComparer<Vector3>
{
    public int Compare(Vector3 x, Vector3 y)
    {
        int xComparison = x.X.CompareTo(y.X);
        if (xComparison != 0)
        {
            return xComparison;
        }

        int yComparison = x.Y.CompareTo(y.Y);
        if (yComparison != 0)
        {
            return yComparison;
        }

        return x.Z.CompareTo(y.Z);
    }
}
