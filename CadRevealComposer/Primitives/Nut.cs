namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record Nut(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
        [property: JsonProperty("center_axis")] float[] CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
        [property: JsonProperty("height")] float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("radius")]
        float Radius,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
        [property: JsonProperty("rotation_angle")]
        float RotationAngle
    ) : APrimitive(CommonPrimitiveProperties);
}