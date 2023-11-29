namespace CadRevealComposer.Primitives;

using ProtoBuf;
using System.Drawing;
using System.Numerics;
using Tessellation;

// Reveal GLTF model
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record Box(
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record Circle(
    Matrix4x4 InstanceMatrix,
    Vector3 Normal,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record Cone(
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
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record EccentricCone(
    Vector3 CenterA,
    Vector3 CenterB,
    Vector3 Normal,
    float RadiusA,
    float RadiusB,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record EllipsoidSegment(
    float HorizontalRadius,
    float VerticalRadius,
    float Height,
    Vector3 Center,
    Vector3 Normal,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record GeneralCylinder(
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
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record GeneralRing(
    float Angle,
    float ArcAngle,
    Matrix4x4 InstanceMatrix,
    Vector3 Normal,
    float Thickness,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record Nut(
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record Quad(
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record TorusSegment(
    float ArcAngle,
    Matrix4x4 InstanceMatrix,
    float Radius,
    float TubeRadius,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record Trapezium(
    Vector3 Vertex1,
    Vector3 Vertex2,
    Vector3 Vertex3,
    Vector3 Vertex4,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

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
[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record InstancedMesh(
    ulong InstanceId,
    Mesh TemplateMesh,
    Matrix4x4 InstanceMatrix,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
public sealed record TriangleMesh(
    Mesh Mesh,
    ulong TreeIndex,
    Color Color,
    BoundingBox AxisAlignedBoundingBox,
    string Area
) : APrimitive(TreeIndex, Color, AxisAlignedBoundingBox, Area);

[ProtoContract(SkipConstructor = true, IgnoreUnknownSubTypes = false)]
[ProtoInclude(500, typeof(TriangleMesh))]
[ProtoInclude(501, typeof(InstancedMesh))]
[ProtoInclude(502, typeof(Trapezium))]
[ProtoInclude(503, typeof(TorusSegment))]
[ProtoInclude(504, typeof(Quad))]
[ProtoInclude(505, typeof(Nut))]
[ProtoInclude(506, typeof(GeneralRing))]
[ProtoInclude(507, typeof(GeneralCylinder))]
[ProtoInclude(508, typeof(EllipsoidSegment))]
[ProtoInclude(509, typeof(Cone))]
[ProtoInclude(510, typeof(Circle))]
[ProtoInclude(511, typeof(Box))]
[ProtoInclude(512, typeof(EccentricCone))]
public abstract record APrimitive(
    [property: ProtoMember(1)] ulong TreeIndex,
    [property: ProtoMember(2)] Color Color,
    [property: ProtoMember(3)] BoundingBox AxisAlignedBoundingBox,
    [property: ProtoMember(4)] string Area
);
