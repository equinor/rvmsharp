namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record OpenExtrudedRingSegment(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: JsonProperty("center_axis")] float[] CenterAxis,
        [property: JsonProperty("height")] float Height,
        [property: JsonProperty("inner_radius")]
        float InnerRadius,
        [property: JsonProperty("outer_radius")]
        float OuterRadius,
        [property: JsonProperty("rotation_angle")]
        float RotationAngle,
        [property: JsonProperty("arc_angle")]
        float ArcAngle
    ) : APrimitive(CommonPrimitiveProperties);
}