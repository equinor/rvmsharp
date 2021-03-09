using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace rvmsharp.Rvm
{
    public class RvmGroup
    {
        public readonly List<RvmGroup> Children = new List<RvmGroup>();
        public readonly List<RvmPrimitive> Primitives = new List<RvmPrimitive>();
        public readonly uint Version;
        public readonly string Name;
        public readonly Vector3 Translation;
        public readonly uint MaterialId;
        public readonly Dictionary<string, string> Attributes = new();

        public RvmGroup(uint version, string name, Vector3 translation, uint materialId)
        {
            this.Version = version;
            this.Name = name;
            Translation = translation;
            MaterialId = materialId;
        }

        internal void AddChild(RvmGroup rvmGroup)
        {
            Children.Add(rvmGroup);
        }

        internal void AddPrimitive(RvmPrimitive rvmPrimitive)
        {
            Primitives.Add(rvmPrimitive);
        }

    }
}