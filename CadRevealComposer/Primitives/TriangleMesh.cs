namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using RvmSharp.Tessellation;
    using System;

    /// <summary>
    /// The TriangleMesh stores the indices
    /// </summary>
    /// <param name="CommonPrimitiveProperties"></param>
    /// <param name="FileId">The MeshFile Id</param>
    /// <param name="TriangleCount">This should be (Mesh.Indices.Count / 3)</param>
    /// <param name="TempTessellatedMesh">Temporary reference to the Mesh data (Not Serialized)</param>
    public record TriangleMesh(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.FileId), JsonProperty("file_id")]
            ulong FileId,
            [property: I3df(I3dfAttribute.AttributeType.Null), JsonProperty("triangle_count")]
            ulong TriangleCount,
            [property: JsonIgnore] Mesh? TempTessellatedMesh =
                null // Store the mesh here, for serializing to file later
        )
        : APrimitive(CommonPrimitiveProperties)
    {

        [Obsolete, JsonProperty("diffuse_texture"), I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture DiffuseTexture { get; init; } = new Texture();

        [Obsolete, JsonProperty("specular_texture"), I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture SpecularTexture { get; init; } = new Texture();

        [Obsolete, JsonProperty("ambient_texture"), I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture AmbientTexture { get; init; } = new Texture();

        [Obsolete, JsonProperty("normal_texture"), I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture NormalTexture { get; init; } = new Texture();

        [Obsolete, JsonProperty("bump_texture"), I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture BumpTexture { get; init; } = new Texture();
    };
}