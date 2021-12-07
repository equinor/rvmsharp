namespace CadRevealComposer.Faces
{
    using System;
    using System.Drawing;

    [Flags]
    public enum FaceFlags : byte
    {
        None = 0, // DO NOT USE this one
        PositiveXVisible = 0b0000_0001,
        PositiveYVisible = 0b0000_0010,
        PositiveZVisible = 0b0000_0100,
        NegativeXVisible = 0b0000_1000,
        NegativeYVisible = 0b0001_0000,
        NegativeZVisible = 0b0010_0000,
        // 0b0100_0000 is reserved and left out to trigger an error if used
        Multiple = 0b1000_0000
    }

    /// <summary>
    /// F3D face, base element
    /// </summary>
    public record Face
    {
        public Face(FaceFlags faceFlags, byte repetitions, ulong index, Color? color)
        {
            Color = color;
            FaceFlags = faceFlags;
            Repetitions = repetitions;
            Index = index;
        }

        /// <summary>
        /// Flags define which side of the cell is visible and whenever this is face strip (multiple) or a single face
        /// </summary>
        public FaceFlags FaceFlags { get; }

        /// <summary>
        /// Repeat visible sides in direction of axis specified by CompressFlags in Node.
        /// Default axis for repetitions are positive Y, X and X (for X, Y and Z respectively), but can be overridden
        /// by CompressFlags. Active only when FaceFlags.Multiple is set, ignored otherwise.
        /// </summary>
        public byte Repetitions { get; }

        /// <summary>
        /// Index in 3D grid array (X + Y * width + Z * width * depth)
        /// </summary>
        public ulong Index { get; }

        /// <summary>
        /// Optional color, must be set if CompressFlags.HasColorOnEachCell is set on the parent node
        /// </summary>
        public Color? Color { get; }
    }
}