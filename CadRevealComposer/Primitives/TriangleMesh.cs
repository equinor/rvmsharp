namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System;

    public record TriangleMesh(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.FileId), JsonProperty("file_id")] 
            ulong FileId,
            [property: I3df(I3dfAttribute.AttributeType.Null), JsonProperty("triangle_count")] 
            ulong TriangleCount
        )
        : APrimitive(CommonPrimitiveProperties)
    {
        /// <summary>
        /// A Texture. This is currently Unused in the Reveal format, but it is required in the exported JSON.
        /// </summary>
        public record Texture([property: JsonProperty("file_id")] ulong FileId = 0,
            [property: JsonProperty("width")] ushort Width = 0, [property: JsonProperty("height")] ushort Height = 0);
        
        [Obsolete, JsonProperty("diffuse_texture"), I3df(I3dfAttribute.AttributeType.Texture)]  
        public readonly Texture DiffuseTexture = new Texture();

        [Obsolete, JsonProperty("specular_texture"), I3df(I3dfAttribute.AttributeType.Texture)]  
        public readonly Texture SpecularTexture = new Texture();

        [Obsolete, JsonProperty("ambient_texture"), I3df(I3dfAttribute.AttributeType.Texture)]  
        public readonly Texture AmbientTexture = new Texture();

        [Obsolete, JsonProperty("normal_texture"), I3df(I3dfAttribute.AttributeType.Texture)]  
        public readonly Texture NormalTexture = new Texture();

        [Obsolete, JsonProperty("bump_texture"), I3df(I3dfAttribute.AttributeType.Texture)]  
        public readonly Texture BumpTexture = new Texture();
    };

    
}