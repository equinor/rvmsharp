namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using RvmSharp.Primitives;

public static class RvmPrimitiveToAPrimitive
{
    public static IEnumerable<APrimitive> FromRvmPrimitive(
        ulong treeIndex,
        RvmPrimitive rvmPrimitive,
        RvmNode rvmNode,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        switch (rvmPrimitive)
        {
            case RvmBox rvmBox:
                return rvmBox.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitivesLogObject);
            case RvmCylinder rvmCylinder:
                return rvmCylinder.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitivesLogObject);
            case RvmEllipticalDish rvmEllipticalDish:
                return rvmEllipticalDish.ConvertToRevealPrimitive(
                    treeIndex,
                    rvmNode.GetColor(),
                    failedPrimitivesLogObject
                );
            case RvmFacetGroup rvmFacetGroup:
                return rvmFacetGroup.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitivesLogObject);
            case RvmLine:
                // Intentionally ignored. Can't draw a 2D line in Cognite Reveal.
                return Array.Empty<APrimitive>();
            case RvmPyramid rvmPyramid:
                return rvmPyramid.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitivesLogObject);
            case RvmCircularTorus rvmCircularTorus:
                return rvmCircularTorus.ConvertToRevealPrimitive(
                    treeIndex,
                    rvmNode.GetColor(),
                    failedPrimitivesLogObject
                );
            case RvmSphere rvmSphere:
                return rvmSphere.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitivesLogObject);
            case RvmSphericalDish rvmSphericalDish:
                return rvmSphericalDish.ConvertToRevealPrimitive(
                    treeIndex,
                    rvmNode.GetColor(),
                    failedPrimitivesLogObject
                );
            case RvmSnout rvmSnout:
                return rvmSnout.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitivesLogObject);
            case RvmRectangularTorus rvmRectangularTorus:
                return rvmRectangularTorus.ConvertToRevealPrimitive(
                    treeIndex,
                    rvmNode.GetColor(),
                    failedPrimitivesLogObject
                );
            default:
                throw new ArgumentOutOfRangeException(nameof(rvmPrimitive), rvmPrimitive, nameof(rvmPrimitive));
        }
    }
}
