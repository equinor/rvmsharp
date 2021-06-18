namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System;

    public record InstancedMesh(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.FileId)] [property: JsonProperty("file_id")]
            ulong FileId,
            // TODO textures
            [property: I3df(I3dfAttribute.AttributeType.Null)] [property: JsonProperty("triangle_offset")]
            ulong TriangleOffset,
            [property: I3df(I3dfAttribute.AttributeType.Null)] [property: JsonProperty("triangle_count")]
            ulong TriangleCount,
            [property: I3df(I3dfAttribute.AttributeType.TranslationX)] [property: JsonProperty("translation_x")]
            float TranslationX,
            [property: I3df(I3dfAttribute.AttributeType.TranslationY)] [property: JsonProperty("translation_y")]
            float TranslationY,
            [property: I3df(I3dfAttribute.AttributeType.TranslationZ)] [property: JsonProperty("translation_z")]
            float TranslationZ,
            [property: I3df(I3dfAttribute.AttributeType.Angle)] [property: JsonProperty("rotation_x")]
            float RotationX,
            [property: I3df(I3dfAttribute.AttributeType.Angle)] [property: JsonProperty("rotation_y")]
            float RotationY,
            [property: I3df(I3dfAttribute.AttributeType.Angle)] [property: JsonProperty("rotation_z")]
            float RotationZ,
            [property: I3df(I3dfAttribute.AttributeType.ScaleX)] [property: JsonProperty("scale_x")]
            float ScaleX,
            [property: I3df(I3dfAttribute.AttributeType.ScaleY)] [property: JsonProperty("scale_y")]
            float ScaleY,
            [property: I3df(I3dfAttribute.AttributeType.ScaleZ)] [property: JsonProperty("scale_z")]
            float ScaleZ)
        // TODO remove some common properties 
        : APrimitive(CommonPrimitiveProperties)
    {
        [Obsolete, JsonProperty("diffuse_texture"), I3df(I3dfAttribute.AttributeType.Texture)]  
        public readonly TriangleMesh.Texture DiffuseTexture = new TriangleMesh.Texture();

        [Obsolete, JsonProperty("specular_texture"), I3df(I3dfAttribute.AttributeType.Texture)]  
        public readonly TriangleMesh.Texture SpecularTexture = new TriangleMesh.Texture();

        [Obsolete, JsonProperty("ambient_texture"), I3df(I3dfAttribute.AttributeType.Texture)]  
        public readonly TriangleMesh.Texture AmbientTexture = new TriangleMesh.Texture();

        [Obsolete, JsonProperty("normal_texture"), I3df(I3dfAttribute.AttributeType.Texture)]  
        public readonly TriangleMesh.Texture NormalTexture = new TriangleMesh.Texture();

        [Obsolete, JsonProperty("bump_texture"), I3df(I3dfAttribute.AttributeType.Texture)]  
        public readonly TriangleMesh.Texture BumpTexture = new TriangleMesh.Texture();
    }
}