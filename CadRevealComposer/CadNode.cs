namespace CadRevealComposer
{
    using RvmSharp.Containers;

    public class CadNode
    {
        public ulong NodeId;
        public ulong TreeIndex;
        // TODO support Store, Model, File and maybe not RVM
        public RvmGroup Group; // PDMS inside, children inside
        public CadNode Parent;
        public CadNode[] Children;
        // Bounding box
        // Depth
        // Subtree size
    }
}
