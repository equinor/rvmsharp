namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record OpenSphericalSegment(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.Normal)]
            [property: JsonProperty("normal")]
            float[] Normal,
            [property: I3df(I3dfAttribute.AttributeType.Height)]
            [property: JsonProperty("height")] float Height,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            [property: JsonProperty("radius")] float Radius)
        : APrimitive(CommonPrimitiveProperties);
}