namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record ClosedEllipsoidSegment(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.Normal)]

            Vector3 Normal,
            [property: I3df(I3dfAttribute.AttributeType.Height)]
             float Height,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
             float HorizontalRadius,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
             float VerticalRadius)
        : APrimitive(CommonPrimitiveProperties);
}