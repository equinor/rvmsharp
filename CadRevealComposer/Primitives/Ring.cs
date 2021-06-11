namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record Ring(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
        [property: JsonProperty("normal")] float[] Normal,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("inner_radius")]
        float InnerRadius,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("outer_radius")]
        float OuterRadius
    ) : APrimitive(CommonPrimitiveProperties);
}