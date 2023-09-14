namespace CadRevealComposer.Shadow;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using System.Numerics;

public static class TriangleMeshShadowCreator
{
    // Public to be accessible by tests
    public const float SizeTreshold = 1.0f; // Arbitrary number

    public static APrimitive CreateShadow(this TriangleMesh triangleMesh)
    {
        var bb = triangleMesh.AxisAlignedBoundingBox;
        var bbSize = bb.Max - bb.Min;

        // If two sides is greater then threshold, there could be a large diagonal that would look weird as a box
        int largeSizeCounts = 0;

        if (bbSize.X > SizeTreshold)
            largeSizeCounts++;
        if (bbSize.Y > SizeTreshold)
            largeSizeCounts++;
        if (bbSize.Z > SizeTreshold)
            largeSizeCounts++;

        if (largeSizeCounts >= 2)
        {
            return SimplifyTriangleMesh(triangleMesh);
        }

        var scale = bbSize;
        var rotation = Quaternion.Identity;
        var position = bb.Center;

        var matrix =
            Matrix4x4.CreateScale(scale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);

        return new Box(matrix, triangleMesh.TreeIndex, triangleMesh.Color, triangleMesh.AxisAlignedBoundingBox);
    }

    private static TriangleMesh SimplifyTriangleMesh(TriangleMesh triangleMesh)
    {
        var mesh = triangleMesh.Mesh;
        var simplifiedMesh = Simplify.SimplifyMeshLossy(mesh, 1.0f);

        return new TriangleMesh(
            simplifiedMesh,
            triangleMesh.TreeIndex,
            triangleMesh.Color,
            triangleMesh.AxisAlignedBoundingBox
        );
    }
}
