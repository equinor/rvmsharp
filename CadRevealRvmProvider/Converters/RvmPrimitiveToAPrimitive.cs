namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using RvmSharp.Primitives;

public class RvmPrimitiveToAPrimitive
{
    public static IEnumerable<APrimitive> FromRvmPrimitive(ulong treeIndex, RvmPrimitive rvmPrimitive, RvmNode rvmNode, string area)
    {
        if (rvmNode == null)
        {
            Console.WriteLine(
                $"The RvmGroup for Node {treeIndex} was invalid: {rvmNode?.GetType()}. Returning empty array."
            );
            return Array.Empty<APrimitive>();
        }

        switch (rvmPrimitive)
        {
            case RvmBox rvmBox:
                return rvmBox.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), area);
            case RvmCylinder rvmCylinder:
                return rvmCylinder.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), area);
            case RvmEllipticalDish rvmEllipticalDish:
                return rvmEllipticalDish.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), area);
            case RvmFacetGroup rvmFacetGroup:
                return rvmFacetGroup.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), area);
            case RvmLine:
                // Intentionally ignored. Can't draw a 2D line in Cognite Reveal.
                return Array.Empty<APrimitive>();
            case RvmPyramid rvmPyramid:
                return rvmPyramid.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), area);
            case RvmCircularTorus circularTorus:
                return circularTorus.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), area);
            case RvmSphere rvmSphere:
                return rvmSphere.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), area);
            case RvmSphericalDish rvmSphericalDish:
                return rvmSphericalDish.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), area);
            case RvmSnout rvmSnout:
                return rvmSnout.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), area);
            case RvmRectangularTorus rvmRectangularTorus:
                return rvmRectangularTorus.ConvertToRevealPrimitive(treeIndex, rvmNode.GetColor(), area);
            default:
                throw new ArgumentOutOfRangeException(nameof(rvmPrimitive), rvmPrimitive, nameof(rvmPrimitive));
        }
    }
}
