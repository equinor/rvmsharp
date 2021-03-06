using System.Collections.Generic;
using System.Numerics;

namespace rvmsharp.Rvm
{
    public class RvmGroup
    {
        public readonly List<RvmGroup> children = new List<RvmGroup>();
        public readonly List<RvmPrimitive> primitives = new List<RvmPrimitive>();
        private uint version;
        private string name;
        private readonly Vector3 _translation;
        private readonly uint _materialId;

        public RvmGroup(uint version, string name, Vector3 translation, uint materialId)
        {
            this.version = version;
            this.name = name;
            _translation = translation;
            _materialId = materialId;
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