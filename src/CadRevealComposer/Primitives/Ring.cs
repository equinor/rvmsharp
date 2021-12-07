namespace CadRevealComposer.Primitives
{
    using System.Numerics;

    public record Ring(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
         Vector3 Normal,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]

        float InnerRadius,
        [property: I3df(I3dfAttribute.AttributeType.Radius)]

        float OuterRadius
    ) : APrimitive(CommonPrimitiveProperties);
}