namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record ClosedSphericalSegment(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: JsonProperty("normal")]
            float[] Normal,
            [property: JsonProperty("height")] float Height,
            [property: JsonProperty("radius")] float Radius)
        : APrimitive(CommonPrimitiveProperties);
}