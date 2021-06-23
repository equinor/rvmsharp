namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record ClosedGeneralCone(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
        [property: JsonProperty("center_axis")]
        Vector3 CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
        [property: JsonProperty("height")] float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("radius_a")] float RadiusA,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
        [property: JsonProperty("radius_b")] float RadiusB,
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