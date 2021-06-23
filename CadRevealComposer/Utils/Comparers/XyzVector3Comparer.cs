namespace CadRevealComposer.Utils.Comparers
{
    using System.Collections.Generic;
    using System.Numerics;

    /// <summary>
    /// Used to have a repeatable comparison between two Vector3 when sorting.
    /// Compares on X, then Y, then Z.
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
}