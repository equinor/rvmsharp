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
    /// Split occurred because one or more budgets (byte size, primitive count, or triangle count) were exceeded.
    /// </summary>
    Budget,

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
