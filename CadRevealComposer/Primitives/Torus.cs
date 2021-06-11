namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record Torus(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.Normal)]
            [property: JsonProperty("normal")] float[] Normal,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            [property: JsonProperty("radius")] float Radius,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            [property: JsonProperty("tube_radius")]
            float TubeRadius)
        : APrimitive(CommonPrimitiveProperties);
}