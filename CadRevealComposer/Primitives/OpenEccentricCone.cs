namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record OpenEccentricCone(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
        [property: JsonProperty("center_axis")]
        float[] CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
        [property: JsonProperty("height")] float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("radius_a")] float RadiusA,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("radius_b")] float RadiusB,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
        [property: JsonProperty("cap_normal")] float[] CapNormal
    ) : APrimitive(CommonPrimitiveProperties);
}