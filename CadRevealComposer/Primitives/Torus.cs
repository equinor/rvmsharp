namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record Torus(
            CommonPrimitiveProperties CommonPrimitiveProperties,
            [property: I3df(I3dfAttribute.AttributeType.Normal)]
             Vector3 Normal,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]
             float Radius,
            [property: I3df(I3dfAttribute.AttributeType.Radius)]

            float TubeRadius)
        : APrimitive(CommonPrimitiveProperties);
}