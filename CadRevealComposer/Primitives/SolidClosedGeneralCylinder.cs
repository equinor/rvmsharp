namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record SolidClosedGeneralCylinder(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
        [property: JsonProperty("center_axis")]
        float[] CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
        [property: JsonProperty("height")] float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("radius")] float Radius,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("thickness")] float Thickness,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
        [property: JsonProperty("rotation_angle")] float RotationAngle,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
        [property: JsonProperty("arc_angle")] float ArcAngle,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
        [property: JsonProperty("slope_a")] float SlopeA,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
        [property: JsonProperty("slope_b")] float SlopeB,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
        [property: JsonProperty("zangle_a")] float ZangleA,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
        [property: JsonProperty("zangle_b")] float ZangleB
    ) : APrimitive(CommonPrimitiveProperties);
}