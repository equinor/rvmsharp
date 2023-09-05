namespace RvmSharp.Primitives;

using System.Linq;
using System.Numerics;

public record RvmBoundingBox(Vector3 Min, Vector3 Max)
{
    /// <summary>
    /// Generate all 8 corners of the bounding box.
    /// Remark: This can be "Flat" (Zero width) in one or more dimensions.
    /// </summary>
    public Vector3[] GenerateBoxVertexes()
    {
        var cube = new[]
        {
            Max,
            Min,
            new Vector3(Min.X, Min.Y, Max.Z),
            new Vector3(Min.X, Max.Y, Min.Z),
            new Vector3(Max.X, Min.Y, Min.Z),
            new Vector3(Max.X, Max.Y, Min.Z),
            new Vector3(Max.X, Min.Y, Max.Z),
            new Vector3(Min.X, Max.Y, Max.Z)
        };

        return cube;
    }

    /// <summary>
    /// Calculate the diagonal size (distance between "min" and "max")
    /// </summary>
    public float Diagonal => Vector3.Distance(Min, Max);

    /// <summary>
    /// Helper method to calculate the Center of the bounding box.
    /// Can be used together with <see cref="Extents"/>
    /// </summary>
    public Vector3 Center => (Max + Min) / 2;

    /// <summary>
    /// Helper method to calculate the Extent of the bounding box.
    /// Can be used together with <see cref="Center"/>
    /// </summary>
    public Vector3 Extents => (Max - Min);

    /// <summary>
    /// Combine two bounds
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public RvmBoundingBox Encapsulate(RvmBoundingBox other)
    {
        return new RvmBoundingBox(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));
    }

    /// <summary>
    /// Transforms a local axis aligned bounding box to a world space axis aligned bounding box
    /// </summary>
    /// <param name="localBoundingBox">An axis aligned bounding box in the primitive's local space</param>
    /// <param name="matrix"></param>
    /// <returns>An axis aligned bounding box in world space</returns>
    public static RvmBoundingBox CalculateAxisAlignedBoundingBox(RvmBoundingBox localBoundingBox, Matrix4x4 matrix)
    {
        var box = localBoundingBox.GenerateBoxVertexes();

        var rotatedBox = box.Select(vertex => Vector3.Transform(vertex, matrix)).ToArray();

        var min = rotatedBox.Aggregate(Vector3.Min);
        var max = rotatedBox.Aggregate(Vector3.Max);
        return new RvmBoundingBox(Min: min, Max: max);
    }
};
