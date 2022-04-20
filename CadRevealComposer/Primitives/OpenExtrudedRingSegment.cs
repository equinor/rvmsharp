namespace CadRevealComposer.Primitives;

using System.Numerics;

public record OpenExtrudedRingSegment(
    CommonPrimitiveProperties CommonPrimitiveProperties,
    [property: I3df(I3dfAttribute.AttributeType.Normal)]
    Vector3 CenterAxis,
    [property: I3df(I3dfAttribute.AttributeType.Height)]
    float Height,
    [property: I3df(I3dfAttribute.AttributeType.Radius)]

    float InnerRadius,
    [property: I3df(I3dfAttribute.AttributeType.Radius)]

    float OuterRadius,
    [property: I3df(I3dfAttribute.AttributeType.Angle)]

    float RotationAngle,
    [property: I3df(I3dfAttribute.AttributeType.Angle)]

    float ArcAngle
) : APrimitive(CommonPrimitiveProperties);