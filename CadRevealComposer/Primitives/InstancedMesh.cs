namespace CadRevealComposer.Primitives
{
    using RvmSharp.Tessellation;
    using System;

    public record InstancedMesh(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.FileId)]
            ulong FileId,
            // TODO textures
            [property: I3df(I3dfAttribute.AttributeType.Null)]
            ulong TriangleOffset,
            [property: I3df(I3dfAttribute.AttributeType.Null)]
            ulong TriangleCount,
            [property: I3df(I3dfAttribute.AttributeType.TranslationX)]
            float TranslationX,
            [property: I3df(I3dfAttribute.AttributeType.TranslationY)]
            float TranslationY,
            [property: I3df(I3dfAttribute.AttributeType.TranslationZ)]
            float TranslationZ,
            [property: I3df(I3dfAttribute.AttributeType.Angle)]
            float RotationX,
            [property: I3df(I3dfAttribute.AttributeType.Angle)]
            float RotationY,
            [property: I3df(I3dfAttribute.AttributeType.Angle)]
            float RotationZ,
            [property: I3df(I3dfAttribute.AttributeType.ScaleX)]
            float ScaleX,
            [property: I3df(I3dfAttribute.AttributeType.ScaleY)]
            float ScaleY,
            [property: I3df(I3dfAttribute.AttributeType.ScaleZ)]
            float ScaleZ)
        // TODO remove some common properties
        : APrimitive(CommonPrimitiveProperties)
    {
        [Obsolete,  I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture DiffuseTexture { get; init; } = new Texture();

        [Obsolete,  I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture SpecularTexture { get; init; } = new Texture();

        [Obsolete,  I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture AmbientTexture { get; init; } = new Texture();

        [Obsolete,  I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture NormalTexture { get; init; } = new Texture();

        [Obsolete,  I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture BumpTexture { get; init; } = new Texture();

        public Mesh Mesh { get; init; }
    }
}