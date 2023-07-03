namespace CadRevealComposer.IdProviders;

using System.Diagnostics;

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
        var id = GetNextId(); // Always start TreeIndexes at 1 as the database uses Zero non-existing.
        Debug.Assert(id == 0);
    }
}
