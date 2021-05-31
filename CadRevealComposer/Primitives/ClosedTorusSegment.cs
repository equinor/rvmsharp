namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record ClosedTorusSegment(
            ulong NodeId,
            ulong TreeIndex,
            int[] Color,
            float Diagonal,
            float CenterX,
            float CenterY,
            float CenterZ,
            [property: JsonProperty("normal")] float[] Normal,
            [property: JsonProperty("radius")] float Radius,
            [property: JsonProperty("tube_radius")]
            float TubeRadius,
            [property: JsonProperty("rotation_angle")]
            float RotationAngle,
            [property: JsonProperty("arc_angle")] float ArcAngle
        )
        : APrimitive(NodeId: NodeId, TreeIndex: TreeIndex, Color, Diagonal, CenterX, CenterY, CenterZ);
}