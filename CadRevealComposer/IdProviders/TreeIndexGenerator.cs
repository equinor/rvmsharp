namespace CadRevealComposer.IdProviders;

/// <summary>
/// A Sequential Id Generator for TreeIndexes, Indexes here start from 1
///
///
/// Its just a sequential generator for now. It assumes that all nodes are added depth first!
/// Example tree with indexes (root is left, leaf is right):
/// 1
///   2
///     3
///     4
///   5
///     6
///       7
/// 8
/// </summary>
public class TreeIndexGenerator() : SequentialIdGenerator(firstIdReturned: 1)
{
    /// <summary>
    /// The maximal TreeIndex supported in Reveal
    /// Avoid having higher TreeIndex than this.
    /// </summary>
    public const uint MaxTreeIndex = MaxSafeIdForReveal;
}
