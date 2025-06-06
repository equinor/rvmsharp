namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Numerics;
using CadRevealComposer;

public class SortedBoundingBoxExtent
{
    public enum DisplacementOrigin
    {
        BeamTop = 0,
        BeamBottom,
    }

    public float ValueOfLargest { get; }
    public float ValueOfMiddle { get; }
    public float ValueOfSmallest { get; }
    public int AxisIndexOfLargest { get; }
    public int AxisIndexOfMiddle { get; }
    public int AxisIndexOfSmallest { get; }

    private readonly BoundingBox _originalBoundingBox;

    public SortedBoundingBoxExtent(BoundingBox boundingBox)
    {
        _originalBoundingBox = boundingBox;

        // Calculate the extents
        float lx = boundingBox.Extents.X;
        float ly = boundingBox.Extents.Y;
        float lz = boundingBox.Extents.Z;

        // Find largest, smallest, and the middle side lengths
        (ValueOfLargest, AxisIndexOfLargest) =
            (lx > ly) ? ((lx > lz) ? (lx, 0) : (lz, 2)) : ((ly > lz) ? (ly, 1) : (lz, 2));
        (ValueOfSmallest, AxisIndexOfSmallest) =
            (lx < ly) ? ((lx < lz) ? (lx, 0) : (lz, 2)) : ((ly < lz) ? (ly, 1) : (lz, 2));
        (ValueOfMiddle, AxisIndexOfMiddle) =
            (AxisIndexOfSmallest == 0 && AxisIndexOfLargest == 1)
                ? (lz, 2)
                : (
                    (AxisIndexOfSmallest == 0 && AxisIndexOfLargest == 2)
                        ? (ly, 1)
                        : (
                            (AxisIndexOfSmallest == 1 && AxisIndexOfLargest == 0)
                                ? (lz, 2)
                                : (
                                    (AxisIndexOfSmallest == 1 && AxisIndexOfLargest == 2)
                                        ? (lx, 0)
                                        : ((AxisIndexOfSmallest == 2 && AxisIndexOfLargest == 0) ? (ly, 1) : (lx, 0))
                                )
                        )
                );
    }

    public (Vector3 p1, Vector3 p2) CalcPointsAtEndOfABeamShapedBox(
        DisplacementOrigin displacementOrigin,
        float displacementAlongBeamHeight
    )
    {
        // Find the start vector of the upper cylinder
        var p1 = new Vector3();
        p1[AxisIndexOfSmallest] =
            (_originalBoundingBox.Max[AxisIndexOfSmallest] + _originalBoundingBox.Min[AxisIndexOfSmallest]) / 2.0f;
        p1[AxisIndexOfMiddle] =
            displacementOrigin == DisplacementOrigin.BeamTop
                ? _originalBoundingBox.Max[AxisIndexOfMiddle] - displacementAlongBeamHeight
                : _originalBoundingBox.Min[AxisIndexOfMiddle] + displacementAlongBeamHeight;
        p1[AxisIndexOfLargest] = _originalBoundingBox.Min[AxisIndexOfLargest];

        // Find the end vector of the upper cylinder
        var p2 = new Vector3();
        p2[AxisIndexOfSmallest] = p1[AxisIndexOfSmallest];
        p2[AxisIndexOfMiddle] = p1[AxisIndexOfMiddle];
        p2[AxisIndexOfLargest] = _originalBoundingBox.Max[AxisIndexOfLargest];

        return (p1, p2);
    }
}
