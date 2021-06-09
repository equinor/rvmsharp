namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record ExtrudedRing(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: JsonProperty("center_axis")] float[] CenterAxis,
        [property: JsonProperty("height")] float Height,
        [property: JsonProperty("inner_radius")]
        float InnerRadius,
        [property: JsonProperty("outer_radius")]
        float OuterRadius
    ) : APrimitive(CommonPrimitiveProperties);
}