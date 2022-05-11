namespace CadRevealComposer;

using RvmSharp.Containers;
using RvmSharp.Primitives;
using System;

public class CadRevealNode
{
    public ulong NodeId;

    public ulong TreeIndex;

    // TODO support Store, Model, File and maybe not RVM
    public RvmGroup? Group; // PDMS inside, children inside
    public CadRevealNode? Parent;
    public CadRevealNode[]? Children;

    public RvmPrimitive[] RvmGeometries = Array.Empty<RvmPrimitive>();

    /// <summary>
    /// This is a bounding box encapsulating all childrens bounding boxes.
    /// Some nodes are "Notes", and can validly not have any Bounds
    /// </summary>
    public RvmBoundingBox? BoundingBoxAxisAligned;
    // Depth
    // Subtree size

    /// <summary>
    /// This optional value is exported to the Hierarchy database and can be used for any debug info for the 3D model.
    /// </summary>
    public string? OptionalDiagnosticInfo;
}
