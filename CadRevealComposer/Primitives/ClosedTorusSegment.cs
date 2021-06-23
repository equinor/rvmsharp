namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record ClosedTorusSegment(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.Normal)]
            [property: JsonProperty("normal")] Vector3 Normal,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            [property: JsonProperty("radius")] float Radius,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            [property: JsonProperty("tube_radius")]
            float TubeRadius,
            [property: I3df(I3dfAttribute.AttributeType.Angle)]
            [property: JsonProperty("rotation_angle")]
            float RotationAngle,
            [property: I3df(I3dfAttribute.AttributeType.Angle)]
            [property: JsonProperty("arc_angle")] float ArcAngle
        )
        : APrimitive(CommonPrimitiveProperties);
}