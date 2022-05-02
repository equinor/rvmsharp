namespace CadRevealComposer.Primitives;

using RvmSharp.Primitives;

/// <summary>
/// This mesh will evolve into either InstancedMesh or TriangleMesh depending on multiple factors
/// like use count, position and sector allocation
/// </summary>
/// <param name="CommonPrimitiveProperties"></param>
public abstract record ProtoMesh(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Ignore)] RvmPrimitive ProtoPrimitive)
    : APrimitive(CommonPrimitiveProperties);

public record ProtoMeshFromFacetGroup(CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Ignore)] RvmFacetGroup FacetGroup)
    : ProtoMesh(CommonPrimitiveProperties, FacetGroup);

public record ProtoMeshFromPyramid(CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Ignore)] RvmPyramid Pyramid)
    : ProtoMesh(CommonPrimitiveProperties, Pyramid);

// Workaround for Reveal not rendering ClosedGeneralCylinder and ClosedGeneralCone correctly
public record ProtoMeshFromSnout(CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Ignore)] RvmSnout Snout)
    : ProtoMesh(CommonPrimitiveProperties, Snout);