namespace RvmSharp.Primitives;

using CadRevealComposer;
using CadRevealComposer.Utils;
using CadRevealComposer.Primitives;
using Containers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;

public abstract record RvmPrimitive(
    uint Version,
    RvmPrimitiveKind Kind,
    Matrix4x4 Matrix,
    RvmBoundingBox BoundingBoxLocal,
    PrimitiveAttributes? Attributes = null
) : RvmGroup(Version)
{
    public RvmConnection?[] Connections { get; } = { null, null, null, null, null, null }; // Up to six connections. Connections depend on primitive type.

    public virtual bool Equals(RvmPrimitive? other)
    {
        return this.GetHashCode() == other?.GetHashCode();
    }

    public override int GetHashCode()
    {
        // As connections is an array, we use this for comparison.
        var connectionsHash = (
            Connections[0],
            Connections[1],
            Connections[2],
            Connections[3],
            Connections[4],
            Connections[5]
        ).GetHashCode();
        return (
            base.GetHashCode(),
            connectionsHash,
            SampleStartAngle,
            (int)Kind,
            Matrix,
            BoundingBoxLocal
        ).GetHashCode();
    }

    /// <summary>
    /// Temporary value for the Sample Start Angle.
    /// </summary>
    internal float SampleStartAngle { get; set; }

    /// <summary>
    /// Use the BoundingBox and align with the rotation to make the best fitting axis aligned Bounding Box.
    /// Returns null for RvmLine, as bounding boxes are all over the place for that primitive.
    /// </summary>
    /// <returns>Bounding box in World Space.</returns>
    public RvmBoundingBox? CalculateAxisAlignedBoundingBox()
    {
        if (this is RvmLine)
        {
            return null;
        }

        var box = BoundingBoxLocal.GenerateBoxVertexes();

        var rotatedBox = box.Select(vertex => Vector3.Transform(vertex, this.Matrix)).ToArray();

        var min = rotatedBox.Aggregate(Vector3.Min);
        var max = rotatedBox.Aggregate(Vector3.Max);
        return new RvmBoundingBox(Min: min, Max: max);
    }

    public IEnumerable<APrimitive> CalculateAsRvmBox(ulong treeIndex, Color color, PrimitiveAttributes attributes)
    {
        //if (this is RvmRectangularTorus) { }
        //    var x = this as RvmFacetGroup;
        //    var localBox2 = CalculateAxisAlignedBoundingBox();
        //    if (localBox2 == null)
        //        yield break;
        //    var cadRevealBox2 = new BoundingBox(localBox2.Min, localBox2.Max);

        //    yield return new Box(this.Matrix, treeIndex, color, cadRevealBox2, attributes);
        //}

        if (!this.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + this.Matrix);
        }

        //Need:
        /*
         * InstanceMatrix (matrix4)
         * TreeIndex,
         * Color
         * AxisAlignedBoundingBox
         * Attributes (mystuff)
         */

        var localBox = this.CalculateAxisAlignedBoundingBox();
        if (localBox == null)
            yield break;

        if (this is RvmFacetGroup)
        {
            position = localBox.Center;
        }

        var cadRevealBox = new BoundingBox(localBox.Min * scale, localBox.Max * scale);

        var unitBoxScale = Vector3.Multiply(
            scale,
            new Vector3(
                this.BoundingBoxLocal.Extents.X,
                this.BoundingBoxLocal.Extents.Y,
                this.BoundingBoxLocal.Extents.Z
            )
        );

        var matrix =
            Matrix4x4.CreateScale(unitBoxScale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);

        var theBox = new Box(matrix, treeIndex, color, cadRevealBox, attributes);

        yield return theBox;
    }
}
