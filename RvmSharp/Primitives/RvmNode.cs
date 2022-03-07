namespace RvmSharp.Primitives
{
    using Containers;
    using System.Collections.Generic;
    using System.Numerics;

    public class RvmNode : RvmGroup
    {
        public readonly List<RvmGroup> Children = new List<RvmGroup>();
        public string Name;
        public readonly Vector3 Translation;
        public readonly uint MaterialId;
        public readonly Dictionary<string, string> Attributes = new();

        public RvmNode(uint version, string name, Vector3 translation, uint materialId) : base(version)
        {
            Name = name;
            Translation = translation;
            MaterialId = materialId;
        }

        internal void AddChild(RvmGroup rvmGroup)
        {
            Children.Add(rvmGroup);
        }
    }
}