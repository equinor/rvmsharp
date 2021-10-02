namespace RvmSharp.Containers
{
    using Primitives;
    using System;
    using System.Collections.Generic;

    public record RvmModel
    {
        public uint Version { get; }
        public string Project  { get; }
        public string Name { get; }

        public IReadOnlyList<RvmNode> Children => _children;

        private readonly List<RvmNode> _children = new List<RvmNode>();
        private readonly List<RvmPrimitive> _primitives = new List<RvmPrimitive>();
        private readonly List<RvmColor> _colors = new List<RvmColor>();

        public RvmModel(uint version, string project, string name,
            IEnumerable<RvmNode> children,
            IEnumerable<RvmPrimitive> primitives,
            IEnumerable<RvmColor> colors)
        {
            Version = version;
            Project = project;
            Name = name;
            _children.AddRange(children);
            _primitives.AddRange(primitives);
            _colors.AddRange(colors);
        }

        internal void AddChild(RvmNode rvmNode)
        {
            _children.Add(rvmNode);
        }

        internal void AddPrimitive(RvmPrimitive rvmPrimitive)
        {
            _primitives.Add(rvmPrimitive);
        }

        internal void AddColor(RvmColor rvmColor)
        {
            _colors.Add(rvmColor);
        }
    }
}