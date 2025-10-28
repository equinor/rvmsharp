namespace CadRevealComposer.Operations.SectorSplitting;

/// <summary>
/// Indicates the reason why a sector was created or split.
/// </summary>
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
}

/// <summary>
/// Encapsulates diagnostic information for sector splitting analysis.
/// This record groups related splitting metrics to reduce parameter bloat.
/// </summary>
public record SectorDiagnostics(
    SplitReason SplitReason,
    int PrimitiveCount,
    int MeshCount,
    int InstanceMeshCount,
    BudgetInfo? BudgetInfo = null
);

/// <summary>
/// Contains detailed information about budget constraints when a sector split occurred due to budget exceeded.
/// </summary>
public record BudgetInfo(
    long? ByteSizeBudget = null,
    long? ByteSizeUsed = null,
    long? PrimitiveCountBudget = null,
    long? PrimitiveCountUsed = null,
    long? TriangleCountBudget = null,
    long? TriangleCountUsed = null
);
