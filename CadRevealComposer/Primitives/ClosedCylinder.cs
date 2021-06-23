namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record ClosedCylinder(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.Normal)]
            [property: JsonProperty("center_axis")]
            Vector3 CenterAxis,
            [property: I3df(I3dfAttribute.AttributeType.Height)]
            [property: JsonProperty("height")] float Height,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            [property: JsonProperty("radius")] float Radius)
        : APrimitive(CommonPrimitiveProperties);
}