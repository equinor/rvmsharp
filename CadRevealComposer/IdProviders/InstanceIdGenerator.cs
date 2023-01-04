namespace CadRevealComposer.IdProviders;


// Sequential Id generator for instance meshes

// Separate empty implementations SequentialIdGenerator are used to
// avoid a TreeIndexGenerator being sent in where a InstanceIdGenerator or a NodeIdGenerator should be used.
public class InstanceIdGenerator : SequentialIdGenerator
{
    
}
