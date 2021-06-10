namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record ClosedEccentricCone(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: JsonProperty("center_axis")]
        float[] CenterAxis,
        [property: JsonProperty("height")] float Height,
        [property: JsonProperty("radius_a")] float RadiusA,
        [property: JsonProperty("radius_b")] float RadiusB,
        [property: JsonProperty("cap_normal")] float[] CapNormal
    ) : APrimitive(CommonPrimitiveProperties);
}