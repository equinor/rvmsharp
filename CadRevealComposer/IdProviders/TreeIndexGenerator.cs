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
public class TreeIndexGenerator : SequentialIdGenerator
{
    public TreeIndexGenerator()
    {
        // "Pre-increment" the first id (which is zero) to avoid that being used anywhere else.
        _ = GetNextId(); // This returns zero, but we discard it.
    }
}
