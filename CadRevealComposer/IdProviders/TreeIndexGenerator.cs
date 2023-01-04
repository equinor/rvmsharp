namespace CadRevealComposer.IdProviders;

// Separate empty implementations SequentialIdGenerator are used to
// avoid a TreeIndexGenerator being sent in where a InstanceIdGenerator or a NodeIdGenerator should be used.
public class TreeIndexGenerator : SequentialIdGenerator
{
    // TODO: The tree index generator should probably be something else. (To actually be a tree?)
    //      For now its just a sequential generator.
}