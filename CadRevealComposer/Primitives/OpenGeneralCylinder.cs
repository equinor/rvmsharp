namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record OpenGeneralCylinder(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]

        Vector3 CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
         float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
         float Radius,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
         float RotationAngle,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
         float ArcAngle,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
         float SlopeA,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
         float SlopeB,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
         float ZangleA,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
         float ZangleB
    ) : APrimitive(CommonPrimitiveProperties);
}