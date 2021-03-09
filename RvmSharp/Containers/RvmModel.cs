namespace RvmSharp.Containers
{
    using Primitives;
    using System.Collections.Generic;

    public class RvmModel
    {
        public readonly uint Version;
        public readonly string Project;
        public readonly string Name;
        public readonly List<RvmGroup> children = new List<RvmGroup>();
        private readonly List<RvmPrimitive> primitives = new List<RvmPrimitive>();
        private readonly List<RvmColor> colors = new List<RvmColor>();

        public RvmModel(uint version, string project, string name)
        {
            Version = version;
            Project = project;
            Name = name;
        }

        internal void AddChild(RvmGroup rvmGroup)
        {
            children.Add(rvmGroup);
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