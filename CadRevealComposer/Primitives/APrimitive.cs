namespace CadRevealComposer.Primitives;

using Operations;
using Operations.SectorSplitting;
using System.Drawing;
using System.Numerics;
using Tessellation;

// Reveal GLTF model
public sealed record Box(
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public sealed record Circle(
    Matrix4x4 InstanceMatrix,
    Vector3 Normal,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public sealed record Cone(
    Matrix4x4 InstanceMatrix,
    float Angle,
    float ArcAngle,
    Vector3 CenterA,
    Vector3 CenterB,
    Vector3 LocalXAxis,
    float RadiusA,
    float RadiusB,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public sealed record EccentricCone(
    Matrix4x4 InstanceMatrix,
    Vector3 CenterA,
    Vector3 CenterB,
    Vector3 Normal,
    float RadiusA,
    float RadiusB,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public sealed record EllipsoidSegment(
    Matrix4x4 InstanceMatrix,
    float HorizontalRadius,
    float VerticalRadius,
    float Height,
    Vector3 Center,
    Vector3 Normal,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public sealed record GeneralCylinder(
    Matrix4x4 InstanceMatrix,
    float Angle,
    float ArcAngle,
    Vector3 CenterA,
    Vector3 CenterB,
    Vector3 LocalXAxis,
    Vector4 PlaneA,
    Vector4 PlaneB,
    float Radius,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public sealed record GeneralRing(
    float Angle,
    float ArcAngle,
    Matrix4x4 InstanceMatrix,
    Vector3 Normal,
    float Thickness,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public sealed record Nut(
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public sealed record Quad(
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public sealed record TorusSegment(
    float ArcAngle,
    Matrix4x4 InstanceMatrix,
    float Radius,
    float TubeRadius,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public sealed record Trapezium(
    Vector3 Vertex1,
    Vector3 Vertex2,
    Vector3 Vertex3,
    Vector3 Vertex4,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

/// <summary>
/// Defines an "Instance" of a Template. A instance that shares a Geometry representation with other instances of a shared "Template" reference.
/// Every instance reusing a mesh MUST have the same InstanceId AND same TemplateMesh.
/// Only the first TemplateMesh for a given InstanceId is Serialized and used as the template.
/// </summary>
/// <param name="InstanceId">Needs to be the same instanceId for all instances using a given <see cref="TemplateMesh"/>.</param>
/// <param name="TemplateMesh">The Mesh that is used as the Instance Template</param>
/// <param name="InstanceMatrix">The matrix to apply to the TemplateMesh to get the correct transform</param>
/// <param name="TreeIndex">The Instances TreeIndex</param>
/// <param name="Color"></param>
/// <param name="AxisAlignedBoundingBox"></param>
public sealed record InstancedMesh(
    ulong InstanceId,
    Mesh TemplateMesh,
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public sealed record TriangleMesh(
    Mesh Mesh,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, NodePriority);

public abstract record APrimitive(
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    NodePriority NodePriority = NodePriority.Default
);
