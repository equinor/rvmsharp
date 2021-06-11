namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record TriangleMesh(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.FileId)]
            [property: JsonProperty("file_id")] ulong FileId,
            // TODO: textures
            [property: I3df(I3dfAttribute.AttributeType.Null)]
            [property: JsonProperty("triangle_count")] ulong TriangleCount)
        // TODO remove some common properties
        : APrimitive(CommonPrimitiveProperties);
}