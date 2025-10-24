namespace CadRevealComposer.Operations.SectorSplitting;

using System;
using System.Collections.Generic;
using System.Linq;
using IdProviders;
using Primitives;
using Utils;

public class SectorSplitterSingle : ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors(
        APrimitive[] allGeometries,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        yield return CreateRootSector(sectorIdGenerator.GetNextId(), allGeometries);
    }

    private static InternalSector CreateRootSector(uint sectorId, APrimitive[] geometries)
    {
        var bb = geometries.CalculateBoundingBox();
        if (bb == null)
        {
            throw new Exception("The bounding box of the root sector should never be null");
        }

        var primitiveCount = geometries.Count(x => x is not (InstancedMesh or TriangleMesh));
        var meshCount = geometries.Count(x => x is TriangleMesh);
        var instanceMeshCount = geometries.Count(x => x is InstancedMesh);

        return new InternalSector(
            sectorId,
            ParentSectorId: null,
            Depth: 0,
            Path: $"{sectorId}",
            MinNodeDiagonal: geometries.Min(x => x.AxisAlignedBoundingBox.Diagonal),
            MaxNodeDiagonal: geometries.Max(x => x.AxisAlignedBoundingBox.Diagonal),
            Geometries: geometries,
            SubtreeBoundingBox: bb,
            GeometryBoundingBox: null,
            IsPrioritizedSector: false,
            SplitReason: SplitReason.Root,
            PrimitiveCount: primitiveCount,
            MeshCount: meshCount,
            InstanceMeshCount: instanceMeshCount
        );
    }
}
