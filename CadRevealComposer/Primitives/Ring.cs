namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record Ring(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: JsonProperty("inner_radius")]
        float InnerRadius,
        [property: JsonProperty("outer_radius")]
        float OuterRadius
    ) : APrimitive(CommonPrimitiveProperties);
}