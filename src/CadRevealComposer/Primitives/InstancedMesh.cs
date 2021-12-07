namespace CadRevealComposer.Primitives
{
    using RvmSharp.Tessellation;
    using System;

    /// <summary>
    /// Create an instanced mesh.
    /// </summary>
    /// <param name="CommonPrimitiveProperties"></param>
    /// <param name="FileId"></param>
    /// <param name="TriangleOffset"></param>
    /// <param name="TriangleCount"></param>
    /// <param name="TranslationX"></param>
    /// <param name="TranslationY"></param>
    /// <param name="TranslationZ"></param>
    /// <param name="RotationX">RollX</param>
    /// <param name="RotationY">PitchY</param>
    /// <param name="RotationZ">YawZ</param>
    /// <param name="ScaleX"></param>
    /// <param name="ScaleY"></param>
    /// <param name="ScaleZ"></param>
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
        private const string ObsoleteMessage =
            "Property is never used in the current i3df format, but needs to be present.";

        [Obsolete(ObsoleteMessage), I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture DiffuseTexture { get; init; } = new Texture();

        [Obsolete(ObsoleteMessage), I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture SpecularTexture { get; init; } = new Texture();

        [Obsolete(ObsoleteMessage), I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture AmbientTexture { get; init; } = new Texture();

        [Obsolete(ObsoleteMessage), I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture NormalTexture { get; init; } = new Texture();

        [Obsolete(ObsoleteMessage), I3df(I3dfAttribute.AttributeType.Texture)]
        public Texture BumpTexture { get; init; } = new Texture();
    }
}