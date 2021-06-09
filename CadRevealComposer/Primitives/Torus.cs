namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record Torus(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: JsonProperty("normal")] float[] Normal,
            [property: JsonProperty("radius")] float Radius,
            [property: JsonProperty("tube_radius")]
            float TubeRadius)
        : APrimitive(CommonPrimitiveProperties);
}