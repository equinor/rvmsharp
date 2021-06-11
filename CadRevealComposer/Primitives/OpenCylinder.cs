namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record OpenCylinder(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
        [property: JsonProperty("center_axis")] float[] CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
        [property: JsonProperty("height")] float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("radius")] float Radius
    ) : APrimitive(CommonPrimitiveProperties);
}