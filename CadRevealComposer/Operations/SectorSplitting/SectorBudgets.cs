namespace CadRevealComposer.Operations.SectorSplitting;

/// <summary>
/// Shared budget constants for sector splitting. These represent client-side GPU/rendering
/// constraints and are independent of the spatial splitting strategy (octree vs K-D tree).
/// </summary>
public static class SectorBudgets
{
    /// <summary>
    /// Maximum estimated byte size per sector GLB file. Controls download chunk size.
    /// </summary>
    public const long EstimatedByteSizeBudget = 2_000_000; // bytes

    /// <summary>
    /// Maximum estimated triangle count per sector. Controls GPU rasterization cost.
    /// </summary>
    public const long EstimatedTrianglesBudget = 300_000; // triangles

    /// <summary>
    /// Maximum non-mesh primitive count per sector. Controls draw-call overhead.
    /// </summary>
    public const long EstimatedPrimitiveBudget = 5_000; // count

    /// <summary>
    /// If fewer than this many nodes remain after the current one, allow budget overrun
    /// rather than creating a tiny sector with very few nodes.
    /// </summary>
    public const int MinRemainingNodesToEnforceBudget = 10;
}
