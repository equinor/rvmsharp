namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record Ellipsoid(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.Normal)]
            [property: JsonProperty("normal")]
            Vector3 Normal,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            [property: JsonProperty("horizontal_radius")] float HorizontalRadius,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            [property: JsonProperty("vertical_radius")] float VerticalRadius)
        : APrimitive(CommonPrimitiveProperties);
}