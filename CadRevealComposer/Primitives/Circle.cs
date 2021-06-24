namespace CadRevealComposer.Primitives
{
    using System.Numerics;

    public record Circle(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.Normal)]
            Vector3 Normal,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
            float Radius)
        : APrimitive(CommonPrimitiveProperties);
}