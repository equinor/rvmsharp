namespace CadRevealComposer;

using Primitives;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Numerics;

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
};

public class CadRevealNode
{
    public required ulong TreeIndex { get; init; }
    public required string Name { get; init; }

    // TODO support Store, Model, File and maybe not RVM
    // public RvmGroup? Group; // PDMS inside, children inside
    public Dictionary<string, string> Attributes = new Dictionary<string, string>();
    public required CadRevealNode? Parent;
    public CadRevealNode[]? Children;

    public APrimitive[] Geometries = Array.Empty<APrimitive>();

    /// <summary>
    /// This is a bounding box encapsulating all childrens bounding boxes.
    /// Some nodes are "Notes", and can validly not have any Bounds
    /// </summary>
    public BoundingBox? BoundingBoxAxisAligned;

    // Depth
    // Subtree size

    /// <summary>
    /// This optional value is exported to the Hierarchy database and can be used for any debug info for the 3D model.
    /// </summary>
    public string? OptionalDiagnosticInfo;

    public string? Area;

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
