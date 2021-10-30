namespace CadRevealComposer.Faces
{
    using System.Numerics;

    public record FacesGrid(GridParameters GridParameters, Node[] Nodes);

    public record SectorFaces
    {
        public SectorFaces(ulong sectorId, ulong? parentSectorId, Vector3 bboxMin, Vector3 bboxMax, FacesGrid? sectorContents)
        {
            SectorId = sectorId;
            ParentSectorId = parentSectorId;
            BboxMin = bboxMin;
            BboxMax = bboxMax;
            SectorContents = sectorContents;
        }

        public ulong SectorId { get; }
        public ulong? ParentSectorId { get; }

        /// <summary>
        /// Bounds used by sector culler in Reveal viewer
        /// </summary>
        public Vector3 BboxMin { get; }

        /// <summary>
        /// Bounds used by sector culler in Reveal viewer
        /// </summary>
        public Vector3 BboxMax { get; }

        public FacesGrid? SectorContents { get; }
    }
}