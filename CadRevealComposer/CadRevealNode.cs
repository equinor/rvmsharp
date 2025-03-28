namespace CadRevealComposer;

using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Primitives;
using ProtoBuf;
using Tessellation;
using Utils;

[ProtoContract(SkipConstructor = true)]
public record BoundingBox([property: ProtoMember(1)] Vector3 Min, [property: ProtoMember(2)] Vector3 Max)
{
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
    /// Extents gives the size in X, Y, and Z dimensions.
    /// Can be used together with <see cref="Center"/>
    /// </summary>
    public Vector3 Extents => (Max - Min);

    /// <summary>
    /// Combine two bounds
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public BoundingBox Encapsulate(BoundingBox other)
    {
        return new BoundingBox(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));
    }

    /// <summary>
    /// Creates an <see cref="Box"/> primitive to visualize the AxisAlignedBoundingBox
    /// Mostly useful as a debug utility.
    /// </summary>
    /// <returns>A Box with the equal Matrix and BoundingBox to this <see cref="BoundingBox"/></returns>
    public Box ToBoxPrimitive(uint treeIndex, Color color)
    {
        var matrix = Matrix4x4.CreateScale(Extents) * Matrix4x4.CreateTranslation(Center);
        return new Box(matrix, treeIndex, color, this);
    }

    /// <summary>
    /// Creates a Mesh <see cref="Mesh"/> representing the bounding box coordinates.
    /// </summary>
    /// <returns>Mesh representing the bounding box.</returns>
    public Mesh ToBoxMesh(float error)
    {
        Vector3[] boundingBoxVertices = new Vector3[8];

        Vector3 d = Max - Min;
        boundingBoxVertices[0] = Min;
        boundingBoxVertices[1] = Min + new Vector3(d.X, 0.0f, 0.0f);
        boundingBoxVertices[2] = Min + new Vector3(0.0f, d.Y, 0.0f);
        boundingBoxVertices[3] = Min + new Vector3(d.X, d.Y, 0.0f);

        boundingBoxVertices[4] = Min + new Vector3(0.0f, 0.0f, d.Z);
        boundingBoxVertices[5] = Min + new Vector3(d.X, 0.0f, d.Z);
        boundingBoxVertices[6] = Min + new Vector3(0.0f, d.Y, d.Z);
        boundingBoxVertices[7] = Min + new Vector3(d.X, d.Y, d.Z);

        var indices = new List<uint>();
        indices.AddRange([0, 2, 3, 0, 3, 1]); // Indices for two triangles along the X-Y-near plane of the bounding box
        indices.AddRange([0, 1, 4, 1, 5, 4]); // Indices for two triangles along the X-Z-near plane of the bounding box
        indices.AddRange([0, 6, 2, 0, 4, 6]); // Indices for two triangles along the Y-Z-near plane of the bounding box
        indices.AddRange([3, 2, 6, 3, 6, 7]); // Indices for two triangles along the X-Z-far plane of the bounding box
        indices.AddRange([3, 5, 1, 3, 7, 1]); // Indices for two triangles along the Y-Z-far plane of the bounding box
        indices.AddRange([4, 5, 7, 4, 7, 6]); // Indices for two triangles along the X-Y-far plane of the bounding box

        return new Mesh(boundingBoxVertices, indices.ToArray(), error);
    }

    /// <summary>
    /// Check if bounding box of this node is equal to other bounding box
    /// </summary>
    /// <param name="other"></param>
    /// <param name="precisionDigits">The number of fractional digits to keep</param>
    /// <returns>True if bounding boxes are equal up to specified precision level</returns>
    public bool EqualTo(BoundingBox other, int precisionDigits = 3)
    {
        return Min.EqualsWithinGridTolerance(other.Min, precisionDigits)
            && Max.EqualsWithinGridTolerance(other.Max, precisionDigits);
    }
};

public class CadRevealNode
{
    public required uint TreeIndex { get; init; }
    public required string Name { get; init; }

    // TODO support Store, Model, File and maybe not RVM
    // public RvmGroup? Group; // PDMS inside, children inside
    public Dictionary<string, string> Attributes = new();
    public required CadRevealNode? Parent;
    public CadRevealNode[]? Children;

    public APrimitive[] Geometries = [];

    /// <summary>
    /// This is a bounding box encapsulating all children bounding boxes.
    /// Some nodes are "Notes", and can validly not have any Bounds
    /// </summary>
    public BoundingBox? BoundingBoxAxisAligned;

    // Depth
    // Subtree size

    /// <summary>
    /// This optional value is exported to the Hierarchy database and can be used for any debug info for the 3D model.
    /// </summary>
    public string? OptionalDiagnosticInfo;

    public static IEnumerable<CadRevealNode> GetAllNodesFlat(CadRevealNode root)
    {
        yield return root;

        if (root.Children == null)
        {
            yield break;
        }

        foreach (CadRevealNode cadRevealNode in root.Children)
        {
            foreach (CadRevealNode revealNode in GetAllNodesFlat(cadRevealNode))
            {
                yield return revealNode;
            }
        }
    }
}
