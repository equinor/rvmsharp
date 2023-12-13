namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using RvmSharp.Primitives;

public static class RvmPrimitiveToAPrimitive
{
    private class FailedPrimitives
    {
        private static Dictionary<FailReason, uint> FailedBoxes = new();
        private static Dictionary<FailReason, uint> FailedCylinders = new();
        private static Dictionary<FailReason, uint> FailedEllipticalDishes = new();
        private static Dictionary<FailReason, uint> FailedCircularToruses = new();
        private static Dictionary<FailReason, uint> FailedSpheres = new();
        private static Dictionary<FailReason, uint> FailedSphericalDish = new();
        private static Dictionary<FailReason, uint> FailedSnout = new();
        private static Dictionary<FailReason, uint> FailedRectangularTorus= new();
    }


    public enum FailReason
    {
        Radius,
        Scale,
        Rotation
    }

    public static IEnumerable<APrimitive> FromRvmPrimitive(ulong treeIndex, RvmPrimitive rvmPrimitive, RvmNode rvmNode)
    {
        FailedPrimitives failedPrimitiveDicts = new FailedPrimitives();
        switch (rvmPrimitive)
        {
            case RvmBox rvmBox:
                return rvmBox.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitiveDicts);
            case RvmCylinder rvmCylinder:
                return rvmCylinder.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitiveDicts);
            case RvmEllipticalDish rvmEllipticalDish:
                return rvmEllipticalDish.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitiveDicts);
            case RvmFacetGroup rvmFacetGroup:
                return rvmFacetGroup.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitiveDicts); // Assuming FailedPrimitives is a placeholder for a correct dictionary
            case RvmLine:
                // Intentionally ignored. Can't draw a 2D line in Cognite Reveal.
                return Array.Empty<APrimitive>();
            case RvmPyramid rvmPyramid:
                return rvmPyramid.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitiveDicts); // Assuming FailedPrimitives is a placeholder for a correct dictionary
            case RvmCircularTorus rvmCircularTorus:
                return rvmCircularTorus.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitiveDicts);
            case RvmSphere rvmSphere:
                return rvmSphere.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitiveDicts);
            case RvmSphericalDish rvmSphericalDish:
                return rvmSphericalDish.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitiveDicts);
            case RvmSnout rvmSnout:
                return rvmSnout.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitiveDicts);
            case RvmRectangularTorus rvmRectangularTorus:
                return rvmRectangularTorus.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), failedPrimitiveDicts);
            default:
                throw new ArgumentOutOfRangeException(nameof(rvmPrimitive), rvmPrimitive, nameof(rvmPrimitive));
        }
    }
}
