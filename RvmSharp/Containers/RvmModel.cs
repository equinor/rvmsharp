namespace RvmSharp.Containers
{
    using Primitives;
    using System.Collections.Generic;

    public class RvmModel
    {
        public readonly uint Version;
        public readonly string Project;
        public readonly string Name;
        public readonly List<RvmNode> children = new List<RvmNode>();
        private readonly List<RvmPrimitive> primitives = new List<RvmPrimitive>();
        private readonly List<RvmColor> colors = new List<RvmColor>();

        public RvmModel(uint version, string project, string name)
        {
            Version = version;
            Project = project;
            Name = name;
        }

        internal void AddChild(RvmNode rvmNode)
        {
            children.Add(rvmNode);
        }

        internal void AddPrimitive(RvmPrimitive rvmPrimitive)
        {
            primitives.Add(rvmPrimitive);
        }

        internal void AddColor(RvmColor rvmColor)
        {
            colors.Add(rvmColor);
        }
    }
}