namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record ClosedGeneralCone(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: JsonProperty("center_axis")]
        float[] CenterAxis,
        [property: JsonProperty("height")] float Height,
        [property: JsonProperty("radius_a")] float RadiusA,
        [property: JsonProperty("radius_b")] float RadiusB,
        [property: JsonProperty("rotation_angle")] float RotationAngle,
        [property: JsonProperty("arc_angle")] float ArcAngle,
        [property: JsonProperty("slope_a")] float SlopeA,
        [property: JsonProperty("slope_b")] float SlopeB,
        [property: JsonProperty("zangle_a")] float ZangleA,
        [property: JsonProperty("zangle_b")] float ZangleB
    ) : APrimitive(CommonPrimitiveProperties);
}