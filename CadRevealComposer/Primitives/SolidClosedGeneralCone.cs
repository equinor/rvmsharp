﻿namespace CadRevealComposer.Primitives
{
    using System.Numerics;

    public record SolidClosedGeneralCone(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]

        Vector3 CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
         float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
         float RadiusA,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
         float RadiusB,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
         float Thickness,
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