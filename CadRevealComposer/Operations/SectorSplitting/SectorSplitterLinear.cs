namespace CadRevealComposer.Operations.SectorSplitting;

using CadRevealComposer.Configuration;
using IdProviders;
using Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

public class SectorSplitterLinear : ISectorSplitter
{
    public IEnumerable<InternalSector> SplitIntoSectors(
        Node[] allNodes,
        uint parentId,
        string parentPath,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        var boundingBox = allNodes.CalculateBoundingBox();

        var bbMax = boundingBox.Max;
        var bbMin = boundingBox.Min;

        int sectorSideSize = 10; // Size of box, assume cubes

        var xSize = bbMax.X - bbMin.X;
        var ySize = bbMax.Y - bbMin.Y;
        var zSize = bbMax.Z - bbMin.Z;

        var numberOfBoxesOnX = (int)(xSize / sectorSideSize) + 1;
        var numberOfBoxesOnY = (int)(ySize / sectorSideSize) + 1;
        var numberOfBoxesOnZ = (int)(zSize / sectorSideSize) + 1;

        var xDict = new Dictionary<int, Dictionary<int, Dictionary<int, List<Node>>>>();

        for (int x = 0; x < numberOfBoxesOnX; x++)
        {
            xDict.Add(x, new Dictionary<int, Dictionary<int, List<Node>>>());

            for (int y = 0; y < numberOfBoxesOnY; y++)
            {
                var yDict = xDict[x];
                yDict.Add(y, new Dictionary<int, List<Node>>());

                for (int z = 0; z < numberOfBoxesOnZ; z++)
                {
                    var zDict = yDict[y];
                    zDict.Add(z, new List<Node>());
                }
            }
        }

        foreach (var node in allNodes)
        {
            var center = (node.BoundingBox.Max + node.BoundingBox.Min) / 2.0f;

            var xMapped = (int)((center.X - bbMin.X) / sectorSideSize);
            var yMapped = (int)((center.Y - bbMin.Y) / sectorSideSize);
            var zMapped = (int)((center.Z - bbMin.Z) / sectorSideSize);

            xDict[xMapped][yMapped][zMapped].Add(node);
        }

        for (int x = 0; x < numberOfBoxesOnX; x++)
        {
            for (int y = 0; y < numberOfBoxesOnY; y++)
            {
                for (int z = 0; z < numberOfBoxesOnZ; z++)
                {
                    var nodes = xDict[x][y][z];
                    var geometries = xDict[x][y][z].SelectMany(n => n.Geometries).ToArray();

                    if (geometries.Length == 0)
                        continue;

                    var sectorId = (uint)sectorIdGenerator.GetNextId();

                    yield return CreateSector(
                        nodes.ToArray(),
                        sectorId,
                        parentId,
                        parentPath,
                        1,
                        nodes.CalculateBoundingBox()
                    );
                }
            }
        }
    }

    private InternalSector CreateRootSector(uint sectorId, string path, BoundingBox subtreeBoundingBox)
    {
        return new InternalSector(sectorId, null, 0, path, 0, 0, Array.Empty<APrimitive>(), subtreeBoundingBox, null);
    }

    private InternalSector CreateSector(
        Node[] nodes,
        uint sectorId,
        uint? parentSectorId,
        string parentPath,
        int depth,
        BoundingBox subtreeBoundingBox
    )
    {
        var path = $"{parentPath}/{sectorId}";

        var minDiagonal = nodes.Any() ? nodes.Min(n => n.Diagonal) : 0;
        var maxDiagonal = nodes.Any() ? nodes.Max(n => n.Diagonal) : 0;
        var geometries = nodes.SelectMany(n => n.Geometries).ToArray();
        var geometryBoundingBox = geometries.CalculateBoundingBox();

        var tooFewInstancesHandler = new TooFewInstancesHandler();
        geometries = tooFewInstancesHandler.ConvertInstancesWhenTooFew(geometries);

        return new InternalSector(
            sectorId,
            parentSectorId,
            depth,
            path,
            minDiagonal,
            maxDiagonal,
            geometries,
            subtreeBoundingBox,
            geometryBoundingBox
        );
    }
}
