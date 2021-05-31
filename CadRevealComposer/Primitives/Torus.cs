namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record Torus(
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
            float TubeRadius)
        : APrimitive(NodeId: NodeId, TreeIndex: TreeIndex, Color: Color, Diagonal: Diagonal, CenterX: CenterX, CenterY: CenterY, CenterZ: CenterZ);
}