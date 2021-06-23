namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record OpenEccentricCone(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
        [property: JsonProperty("center_axis")]
        Vector3 CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
        [property: JsonProperty("height")] float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("radius_a")] float RadiusA,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("radius_b")] float RadiusB,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
        [property: JsonProperty("cap_normal")] Vector3 CapNormal
    ) : APrimitive(CommonPrimitiveProperties);
}