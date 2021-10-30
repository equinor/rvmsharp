namespace CadRevealComposer.Faces
{
    using System;
    using System.Drawing;

    [Flags]
    public enum CompressFlags : byte
    {
        PositiveXRepeatZ = 0b0000_0001,
        PositiveYRepeatZ = 0b0000_0010,
        PositiveZRepeatY = 0b0000_0100,
        NegativeXRepeatZ = 0b0000_1000,
        NegativeYRepeatZ = 0b0001_0000,
        NegativeZRepeatY = 0b0010_0000,
        HasColorOnEachCell = 0b0100_0000,
        IndexIsLong = 0b1000_0000
    }

    public record Node
    {
        public Node(CompressFlags compressFlags, ulong nodeId, ulong treeIndex, Color? color, Face[] faces)
        {
            CompressFlags = compressFlags;
            NodeId = nodeId;
            TreeIndex = treeIndex;
            Color = color;
            Faces = faces;
        }

        public CompressFlags CompressFlags { get; }

        public ulong NodeId { get; }

        public ulong TreeIndex { get; }

        /// <summary>
        /// Optional color, must be set if CompressFlags.HasColorOnEachCell is NOT set
        /// </summary>
        public Color? Color { get; }

        public Face[] Faces { get; }
    }
}