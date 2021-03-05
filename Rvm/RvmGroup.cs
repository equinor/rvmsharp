using System.Collections.Generic;

namespace rvmsharp.Rvm
{
    public class RvmGroup
    {
        private List<RvmGroup> children = new List<RvmGroup>();
        private List<RvmPrimitive> primitives = new List<RvmPrimitive>();
        private uint version;
        private string name;

        public RvmGroup(uint version, string name)
        {
            this.version = version;
            this.name = name;
        }

        internal void AddChild(RvmGroup rvmGroup)
        {
            children.Add(rvmGroup);
        }

        internal void AddPrimitive(RvmPrimitive rvmPrimitive)
        {
            primitives.Add(rvmPrimitive);
        }
    }
}