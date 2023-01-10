namespace CadRevealComposer.IdProviders;



/// <summary>
/// Sequential Id generator for instance meshes.
/// NOTE: Every Instance of the same Mesh should use the same InstanceId! Generate this only once per instance
/// </summary>
public class InstanceIdGenerator : SequentialIdGenerator
{
    // Separate empty implementations SequentialIdGenerator are used to
    // avoid a TreeIndexGenerator being sent in where a InstanceIdGenerator or a NodeIdGenerator should be used.
}
