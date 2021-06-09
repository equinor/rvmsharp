namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record ClosedEllipsoidSegment(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: JsonProperty("normal")]
            float[] Normal,
            [property: JsonProperty("height")] float Height,
            [property: JsonProperty("horizontal_radius")] float HorizontalRadius,
            [property: JsonProperty("vertical_radius")] float VerticalRadius)
        : APrimitive(CommonPrimitiveProperties);
}