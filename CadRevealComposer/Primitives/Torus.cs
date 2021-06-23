namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record Torus(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.Normal)]
            [property: JsonProperty("normal")] Vector3 Normal,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            [property: JsonProperty("radius")] float Radius,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            [property: JsonProperty("tube_radius")]
            float TubeRadius)
        : APrimitive(CommonPrimitiveProperties);
}