namespace CadRevealComposer.Primitives
{
    using System.Numerics;

    public record OpenCylinder(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
         Vector3 CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
         float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
         float Radius
    ) : APrimitive(CommonPrimitiveProperties);
}