namespace CadRevealComposer.Utils.Comparers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;

/// <summary>
/// Lets you sort colors repeatable.
/// Compares by R, then G, B, A.
/// The intended usage is to be able to use Vector3 in a <see cref="ImmutableSortedSet{T}"/>
/// </summary>
public class RgbaColorComparer : IComparer<Color>
{
    public int Compare(Color x, Color y)
    {
        int rComparison = x.R.CompareTo(y.R);
        if (rComparison != 0)
        {
            return rComparison;
        }

        int gComparison = x.G.CompareTo(y.G);
        if (gComparison != 0)
        {
            return gComparison;
        }

        int bComparison = x.B.CompareTo(y.B);
        if (bComparison != 0)
        {
            return bComparison;
        }

        return x.A.CompareTo(y.A);
    }
}
