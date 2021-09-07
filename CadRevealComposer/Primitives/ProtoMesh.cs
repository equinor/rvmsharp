namespace CadRevealComposer.Primitives
{
    using RvmSharp.Primitives;

    /// <summary>
    /// This mesh will evolve into either InstancedMesh or TriangleMesh depending on multiple factors
    /// like use count, position and sector allocation
    /// </summary>
    /// <param name="CommonPrimitiveProperties"></param>
    /// <param name="SourceMesh">Facet group the mesh is based on</param>
    public record ProtoMesh(CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Ignore)]
        RvmFacetGroup SourceMesh
        ) : APrimitive(CommonPrimitiveProperties);
}