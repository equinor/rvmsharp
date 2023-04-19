namespace CadRevealComposer.IdProviders;

public class TreeIndexGenerator : SequentialIdGenerator
{
    // Separate empty implementations SequentialIdGenerator are used to
    // avoid a TreeIndexGenerator being sent in where a InstanceIdGenerator or a NodeIdGenerator should be used.
    
    // Its just a sequential generator for now. It assumes that all nodes are added depth first!
    // Example tree with indexes (root is left, leaf is right):
    // 1
    //   2
    //     3
    //     4
    //   5
    //     6
    //       7
    // 8
}