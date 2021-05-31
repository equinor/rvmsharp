namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;

    public record OpenCylinder(
        ulong NodeId,
        ulong TreeIndex,
        int[] Color,
        float Diagonal,
        float CenterX,
        float CenterY,
        float CenterZ,
        [property: JsonProperty("center_axis")] float[] CenterAxis,
        [property: JsonProperty("height")] float Height,
        [property: JsonProperty("radius")] float Radius
    ) : APrimitive(NodeId, TreeIndex, Color,Diagonal,CenterX,CenterY,CenterZ);
}