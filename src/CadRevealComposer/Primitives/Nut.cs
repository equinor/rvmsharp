namespace CadRevealComposer.Primitives
{
    using System.Numerics;

    public record Nut(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
         Vector3 CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
         float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]

        float Radius,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]

        float RotationAngle
    ) : APrimitive(CommonPrimitiveProperties);
}