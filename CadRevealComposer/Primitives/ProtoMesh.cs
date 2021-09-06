namespace CadRevealComposer.Primitives
{
    using RvmSharp.Primitives;

    public record ProtoMesh(CommonPrimitiveProperties CommonPrimitiveProperties,
        RvmFacetGroup SourceMesh
        ) : APrimitive(CommonPrimitiveProperties)
    {
    }
}