namespace RvmSharp.Primitives
{
    using Containers;
    using System.Collections.Generic;
    using System.Numerics;


    public record RvmNode(uint Version, string Name, Vector3 Translation, uint MaterialId) : RvmGroup(Version)
    {
        // ReSharper disable once CollectionNeverQueried.Global
        public readonly Dictionary<string, string> Attributes = new();
        public readonly List<RvmGroup> Children = new List<RvmGroup>();

        internal void AddChild(RvmGroup rvmGroup)
        {
            Children.Add(rvmGroup);
        }
    }
}