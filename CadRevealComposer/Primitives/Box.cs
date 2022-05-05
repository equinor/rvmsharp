namespace CadRevealComposer.Primitives;

using System.Numerics;

public record Box(
    CommonPrimitiveProperties CommonPrimitiveProperties,
    [property: I3df(I3dfAttribute.AttributeType.Normal)]
    Vector3 Normal,
    [property: I3df(I3dfAttribute.AttributeType.Delta)]
    float DeltaX,
    [property: I3df(I3dfAttribute.AttributeType.Delta)]
    float DeltaY,
    [property: I3df(I3dfAttribute.AttributeType.Delta)]
    float DeltaZ,
    [property: I3df(I3dfAttribute.AttributeType.Angle)]

    float RotationAngle,
    Matrix4x4 Matrix
) : APrimitive(CommonPrimitiveProperties);