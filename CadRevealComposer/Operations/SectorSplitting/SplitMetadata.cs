namespace CadRevealComposer.Operations.SectorSplitting;

using System.Text.Json.Serialization;

/// <summary>
/// Indicates the reason why a sector was created or split.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SplitReason
{
    /// <summary>
    /// No specific reason (default value).
    /// </summary>
    None,

    /// <summary>
    /// This is the root sector of the tree.
    /// </summary>
    Root,

    /// <summary>
    /// Split occurred because the byte size budget was exceeded.
    /// </summary>
    BudgetByteSize,

    /// <summary>
    /// Split occurred because the primitive count budget was exceeded.
    /// </summary>
    BudgetPrimitiveCount,

    /// <summary>
    /// Split occurred because the triangle count budget was exceeded.
    /// </summary>
    BudgetTriangleCount,

    /// <summary>
    /// Split occurred because multiple budgets were exceeded simultaneously.
    /// </summary>
    BudgetMultiple,

    /// <summary>
    /// Split occurred because the sector size exceeded the minimum diagonal threshold.
    /// </summary>
    SizeThreshold,

    /// <summary>
    /// This sector contains outlier nodes that are spatially distant from the main geometry.
    /// </summary>
    Outlier,

    /// <summary>
    /// This is a priority sector, organized by discipline and tree index for efficient highlighting.
    /// </summary>
    Priority,

    /// <summary>
    /// This is a leaf sector that contains all remaining geometry without hitting any constraints.
    /// </summary>
    Leaf,

    /// <summary>
    /// Split occurred due to spatial subdivision (octree voxel-based splitting).
    /// </summary>
    Spatial,

    /// <summary>
    /// Sector created at early recursion depth before budget checking begins.
    /// </summary>
    EarlyDepth,
}

/// <summary>
/// Encapsulates diagnostic information for sector splitting analysis.
/// This record groups related splitting metrics to reduce parameter bloat.
/// </summary>
public record SplittingStats(
    [property: JsonPropertyName("splitReason")] SplitReason SplitReason,
    [property: JsonPropertyName("primitiveCount")] int PrimitiveCount,
    [property: JsonPropertyName("meshCount")] int MeshCount,
    [property: JsonPropertyName("instanceMeshCount")] int InstanceMeshCount,
    [property: JsonPropertyName("budgetInfo")] BudgetInfo? BudgetInfo = null
);

/// <summary>
/// Contains detailed information about budget constraints when a sector split occurred due to budget exceeded.
/// </summary>
public record BudgetInfo(
    [property: JsonPropertyName("byteSizeBudget")] long? ByteSizeBudget = null,
    [property: JsonPropertyName("byteSizeUsed")] long? ByteSizeUsed = null,
    [property: JsonPropertyName("primitiveCountBudget")] long? PrimitiveCountBudget = null,
    [property: JsonPropertyName("primitiveCountUsed")] long? PrimitiveCountUsed = null,
    [property: JsonPropertyName("triangleCountBudget")] long? TriangleCountBudget = null,
    [property: JsonPropertyName("triangleCountUsed")] long? TriangleCountUsed = null
);
