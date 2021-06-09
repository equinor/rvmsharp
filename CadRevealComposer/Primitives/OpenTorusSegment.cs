namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record OpenTorusSegment(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: JsonProperty("normal")] float[] Normal,
            [property: JsonProperty("radius")] float Radius,
            [property: JsonProperty("tube_radius")]
            float TubeRadius,
            [property: JsonProperty("rotation_angle")]
            float RotationAngle,
            [property: JsonProperty("arc_angle")] float ArcAngle)
        : APrimitive(CommonPrimitiveProperties);
}