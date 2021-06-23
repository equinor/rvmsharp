namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using System.Numerics;

    public record Box(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: I3df(I3dfAttribute.AttributeType.Normal)]
        [property: JsonProperty("normal")] Vector3 Normal,
        [property: I3df(I3dfAttribute.AttributeType.Delta)]
        [property: JsonProperty("delta_x")] float DeltaX,
        [property: I3df(I3dfAttribute.AttributeType.Delta)]
        [property: JsonProperty("delta_y")] float DeltaY,
        [property: I3df(I3dfAttribute.AttributeType.Delta)]
        [property: JsonProperty("delta_z")] float DeltaZ,
        [property: I3df(I3dfAttribute.AttributeType.Angle)]
        [property: JsonProperty("rotation_angle")]
        float RotationAngle
    ) : APrimitive(CommonPrimitiveProperties);
}