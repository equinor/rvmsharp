namespace CadRevealComposer.Primitives
{
    using RvmSharp.Primitives;

    /// <summary>
    /// This mesh will evolve into either InstancedMesh or TriangleMesh depending on multiple factors
    /// like use count, position and sector allocation
    /// </summary>
    /// <param name="CommonPrimitiveProperties"></param>
    public abstract record ProtoMesh(CommonPrimitiveProperties CommonPrimitiveProperties) : APrimitive(CommonPrimitiveProperties);

    public record ProtoMeshFromFacetGroup(CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Ignore)]
        RvmFacetGroup SourceMesh) : APrimitive(CommonPrimitiveProperties);

    public record ProtoMeshFromPyramid(CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Ignore)]
        RvmPyramid SourcePyramid) : APrimitive(CommonPrimitiveProperties);
}