namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record ClosedEccentricCone(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]

        Vector3 CenterAxis,
        [property: I3df(I3dfAttribute.AttributeType.Height)]
         float Height,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
         float RadiusA,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]
         float RadiusB,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
         Vector3 CapNormal
    ) : APrimitive(CommonPrimitiveProperties);
}