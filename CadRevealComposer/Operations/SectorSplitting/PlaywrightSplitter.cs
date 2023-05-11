namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Utils;

public class PlaywrightSplitter : ISectorSplitter
{
    private const float TargetExtent = 5.0f;

    public IEnumerable<InternalSector> SplitIntoSectors(APrimitive[] allGeometries)
    {
        var allNodes = SplittingUtils.ConvertPrimitivesToNodes(allGeometries);

        // Random place in the HA area on Huldra, since choosing the center does not yield any decent results
        var targetCenter = new Vector3(111f, 300f, 22f);

        var targetBbMax = targetCenter + new Vector3(TargetExtent);
        var targetBbMin = targetCenter - new Vector3(TargetExtent);

        var nodesInsideTargetBoundingBox = allNodes
            .Where(n =>
            {
                var nodeCenter = n.BoundingBox.Center;

                return nodeCenter.X > targetBbMin.X
                    && nodeCenter.Y > targetBbMin.Y
                    && nodeCenter.Z > targetBbMin.Z
                    && nodeCenter.X < targetBbMax.X
                    && nodeCenter.Y < targetBbMax.Y
                    && nodeCenter.Z < targetBbMax.Z;
            })
            .ToArray();

        if (nodesInsideTargetBoundingBox.Length <= 0)
        {
            throw new Exception("Did not find any nodes inside the target when splitting for Playwright testing");
        }

        var targetGeometries = nodesInsideTargetBoundingBox.SelectMany(n => n.Geometries).ToArray();
        var targetGeometriesBoundingBox = targetGeometries.CalculateBoundingBox();

        uint sectorId = 0;

        yield return new InternalSector(
            sectorId,
            null,
            0,
            $"{sectorId}",
            targetGeometries.Min(x => x.AxisAlignedBoundingBox.Diagonal),
            targetGeometries.Max(x => x.AxisAlignedBoundingBox.Diagonal),
            targetGeometries,
            targetGeometriesBoundingBox!,
            targetGeometriesBoundingBox
        );
    }
}
