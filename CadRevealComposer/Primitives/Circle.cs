namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record Circle(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.Normal)]
            [property: JsonProperty("normal")]
            float[] Normal,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            [property: JsonProperty("radius")] float Radius)
        : APrimitive(CommonPrimitiveProperties);
}