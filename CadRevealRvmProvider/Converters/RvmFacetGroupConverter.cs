namespace CadRevealRvmProvider.Converters;

using CadRevealComposer;
using CadRevealComposer.Primitives;
using MathNet.Numerics.LinearAlgebra.Complex;
using RvmSharp.Primitives;
using System.Drawing;
using System.Runtime.CompilerServices;

public static class RvmFacetGroupConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmFacetGroup rvmFacetGroup,
        ulong treeIndex,
        Color color,
        PrimitiveAttributes? attr = null
    )
    {
        //if (color == Color.Yellow)
        //{
        //    //Console.WriteLine($"We are here {rvmFacetGroup.BoundingBoxLocal}");
        //    var boundingBox = new BoundingBox(rvmFacetGroup.BoundingBoxLocal.Min, rvmFacetGroup.BoundingBoxLocal.Max);
        //    yield return new Box(rvmFacetGroup.Matrix, treeIndex, color, boundingBox, attr);
        //}
        yield return new ProtoMeshFromFacetGroup(
            rvmFacetGroup,
            treeIndex,
            color,
            rvmFacetGroup.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox(),
            attr
        );
    }
}
