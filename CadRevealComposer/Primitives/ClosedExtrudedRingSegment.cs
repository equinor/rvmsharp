namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record ClosedExtrudedRingSegment(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
        [property: JsonProperty("center_axis")] 
        float[] CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
        [property: JsonProperty("height")] 
        float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("inner_radius")]
        float InnerRadius,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("outer_radius")]
        float OuterRadius,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
        [property: JsonProperty("rotation_angle")]
        float RotationAngle,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
        [property: JsonProperty("arc_angle")]
        float ArcAngle
    ) : APrimitive(CommonPrimitiveProperties);
}