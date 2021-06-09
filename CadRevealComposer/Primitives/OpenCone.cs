namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record OpenCone(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: JsonProperty("center_axis")]
        float[] CenterAxis,
        [property: JsonProperty("height")] float Height,
        [property: JsonProperty("radius_a")] float RadiusA,
        [property: JsonProperty("radius_b")] float RadiusB
    ) : APrimitive(CommonPrimitiveProperties);
}