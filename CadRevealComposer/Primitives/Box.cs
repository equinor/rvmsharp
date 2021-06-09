namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record Box(
        CommonPrimitiveProperties CommonPrimitiveProperties,
        [property: JsonProperty("normal")] float[] Normal,
        [property: JsonProperty("delta_x")] float DeltaX,
        [property: JsonProperty("delta_y")] float DeltaY,
        [property: JsonProperty("delta_z")] float DeltaZ,
        [property: JsonProperty("rotation_angle")]
        float RotationAngle
    ) : APrimitive(CommonPrimitiveProperties);
}