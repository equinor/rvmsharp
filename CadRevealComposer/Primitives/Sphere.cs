namespace CadRevealComposer.Primitives;

public record Sphere(
    CommonPrimitiveProperties CommonPrimitiveProperties,
    [property: I3df(I3dfAttribute.AttributeType.Radius)]
    float Radius
) : APrimitive(CommonPrimitiveProperties);