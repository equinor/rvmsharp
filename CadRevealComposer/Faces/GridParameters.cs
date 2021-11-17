namespace CadRevealComposer.Faces
{
    using System.Numerics;

    public record GridParameters
    {
        public GridParameters(uint gridSizeX, uint gridSizeY, uint gridSizeZ, Vector3 gridOrigin, float gridIncrement)
        {
            GridSizeX = gridSizeX;
            GridSizeY = gridSizeY;
            GridSizeZ = gridSizeZ;
            GridOrigin = gridOrigin;
            GridIncrement = gridIncrement;
        }

        /// <summary>
        /// Count of grid sides in X axis (2 for 1 voxel, 3 for 2 etc.) min value is 2
        /// </summary>
        public uint GridSizeX { get; }

        /// <summary>
        /// Count of grid sides in X axis (2 for 1 voxel, 3 for 2 etc.) min value is 2
        /// </summary>
        public uint GridSizeY { get; }

        /// <summary>
        /// Count of grid sides in X axis (2 for 1 voxel, 3 for 2 etc.) min value is 2
        /// </summary>
        public uint GridSizeZ { get; }

        /// <summary>
        /// Center of the first voxel
        /// </summary>
        public Vector3 GridOrigin { get; init; }

        /// <summary>
        /// Distance between two voxel centers in one of the axis
        /// </summary>
        public float GridIncrement { get; }
    }
}