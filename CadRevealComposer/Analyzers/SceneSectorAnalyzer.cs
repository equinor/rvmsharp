namespace CadRevealComposer.Analyzers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SceneSectorAnalyzer
{
    public record SectorAnalysisResult(int SectorCount, long MaxSectorDepth, double AverageEstimatedTriangleCount, double AverageEstimatedDrawcallCount);

    public static SectorAnalysisResult AnalyzeSectorsInScene(Scene scene)
    {
        var sectors = scene.Sectors.ToImmutableArray();
        var maxDepth = sectors.Max(x => x.Depth);
        var averageEstimatedTriangleCount = sectors.Average(x => x.EstimatedTriangleCount);
        var averageDrawcallCount = sectors.Average(x => x.EstimatedDrawCallCount);

        return new SectorAnalysisResult(
            SectorCount: scene.Sectors.Length,
            MaxSectorDepth: maxDepth,
            AverageEstimatedTriangleCount: averageEstimatedTriangleCount,
            AverageEstimatedDrawcallCount: averageDrawcallCount);
    }
    public record SceneAnalysisContent(long Id, string Path, string FileName, long? Parent, long Depth,
        bool IsLeaf,
        float MinX, float MinY, float MinZ,
        float MaxX, float MaxY, float MaxZ,
        string InsideParent,
        long NodeDownloadSize,
        long AgrDownloadSize,
        long AgrEstDrawCalls,
        long AgrEstTriangleCount); 
    public static IEnumerable<SceneAnalysisContent> SceneAnalyisAsStringBuilderCsv(Scene scene)
    {
        
        var content = new List<SceneAnalysisContent>();
        var currentCsvIndex = 2;    //Skip header + not 0-based index

        foreach (var sector in scene.Sectors)
        {
            var aggregated = new AggregatedSize() { DownloadSize = sector.DownloadSize, EstimatedDrawCalls = sector.EstimatedDrawCallCount, EstimatedTriangleCount = sector.EstimatedTriangleCount };
            var bbox = sector.BoundingBox;
            if (sector.Depth > 0)
            {
                var parentSector = scene.Sectors.Where(ss => ss.Id == sector.ParentId).FirstOrDefault();
                if (parentSector != null && parentSector.Id != sector.Id)
                {
                    aggregated.DownloadSize+= parentSector.DownloadSize;
                    aggregated.EstimatedDrawCalls+= parentSector.EstimatedDrawCallCount;
                    aggregated.EstimatedTriangleCount+= parentSector.EstimatedTriangleCount;
                    SceneParentRecursiveSize(scene.Sectors, parentSector.ParentId,ref aggregated);
                }

            }
            var hasChildren = scene.Sectors.Where(s => s.ParentId == sector.Id).Any();
            var min = sector.BoundingBox.Min;
            var max = sector.BoundingBox.Max;
            var boxMin = $"{min.X};{min.Y};{min.Z}";
            var boxMax = $"{max.X};{max.Y};{max.Z}";
            var parentIndex = Array.FindIndex<Sector>(scene.Sectors, ss => ss.Id == sector.ParentId);
            if (parentIndex != -1)
                parentIndex += 2; //Skip header + not 0-based index
            var checkForSectorWithinParent = parentIndex != -1 ? $"\"=AND(g{currentCsvIndex}>=g{parentIndex};h{currentCsvIndex}>=h{parentIndex};i{currentCsvIndex}>=i{parentIndex};j{currentCsvIndex}<=j{parentIndex};k{currentCsvIndex}<=k{parentIndex};l{currentCsvIndex}<=l{parentIndex})\"" : "";
            content.Add(new SceneAnalysisContent(
                Id: sector.Id,
                Path: sector.Path,
                FileName: sector.SectorFileName,
                Parent: sector.ParentId,
                Depth:sector.Depth,
                NodeDownloadSize:sector.DownloadSize,
                IsLeaf:!hasChildren,
                MinX: min.X,
                MinY: min.Y,
                MinZ: min.Z,
                MaxX: max.X,
                MaxY: max.Y,
                MaxZ: max.Z,
                InsideParent:checkForSectorWithinParent,
                AgrDownloadSize: aggregated.DownloadSize,
                AgrEstDrawCalls: aggregated.EstimatedDrawCalls,
                AgrEstTriangleCount: aggregated.EstimatedTriangleCount

                ));
            currentCsvIndex++;
        }
        return content;
    }
    private class AggregatedSize
    {
        public long DownloadSize;
        public long EstimatedDrawCalls;
        public long EstimatedTriangleCount;
    }

private static void SceneParentRecursiveSize(Sector[] sectors, long? parentId, ref AggregatedSize aggregated)
    {
        //var size = 0L;
        var parentSector = sectors.Where(ss => ss.Id == parentId).FirstOrDefault();
        if (parentSector != null)
        {
            //size += parentSector.DownloadSize;
            aggregated.DownloadSize += parentSector.DownloadSize;
            aggregated.EstimatedDrawCalls += parentSector.EstimatedDrawCallCount;
            aggregated.EstimatedTriangleCount += parentSector.EstimatedTriangleCount;

            if (parentSector.Depth > 0)
               SceneParentRecursiveSize(sectors, parentSector.ParentId, ref aggregated);
//            return size;
        }


//        return 0L;
    }

    public record SectorTreeNode(SectorTreeNode[] Children, Sector Self, Sector? Parent)
    {
        public bool IsLeaf => !Children.Any();

        public IEnumerable<SectorTreeNode> FindLeafs()
        {
            if (this.IsLeaf)
            {
                yield return this;
                yield break;
            }

            foreach (var child in Children)
            {
                if (child.IsLeaf)
                {
                    yield return child;
                }
                else
                {
                    foreach (var item in child.FindLeafs())
                    {
                        yield return item;
                    }
                }
            }
        }

        public IEnumerable<SectorTreeNode> TraverseDepthFirst()
        {
            yield return this;

            foreach (var child in Children)
            {
                foreach (var node in child.TraverseDepthFirst())
                {
                    yield return node;
                }
            }
        }
    }

    public static (string sectorId, Sector sector)[] CalculateAllSectorsList(Sector[] data)
    {
        var sectorTree = GenerateSectorTree(data);

        var allSectorsById = data.ToDictionary(k => k.Id.ToString(), v => v);

        var allSectorsList = allSectorsById.Select(sec => (sec.Key, sec.Value));

        //var costPerLeaf = leafs.Select(leaf => calculateMinimumCostForNode(leaf.Self, allSectorsById));
        return allSectorsList.ToArray();
    }

    public static (string sectorId, Sector sector)[][] CalculateMinimumCostForLeafs(Sector[] data)
    {
        var sectorTree = GenerateSectorTree(data);

        var allSectorsById = data.ToDictionary(k => k.Id, v => v);

        var leafs = sectorTree.SelectMany(x => x.FindLeafs());

        var costPerLeaf = leafs.Select(leaf => calculateMinimumCostForNode(leaf.Self, allSectorsById));
        return costPerLeaf.ToArray();
    }

    public static (string path, Sector sector)[] calculateMinimumCostForNode(Sector leafSector, Dictionary<long, Sector> allSectorsById)
    {
        var cost = new List<(string, Sector)>();
        cost.Add((leafSector.Id.ToString(), leafSector));
        var parentId = leafSector.ParentId ?? -1;
        while (parentId != -1)
        {
            var parentSector = allSectorsById[parentId];

            cost.Add((parentSector.Id.ToString(), parentSector));
            parentId = parentSector.ParentId ?? -1;
        }
        cost.Reverse();
        return cost.ToArray();
    }


    /// <summary>
    /// Generate a tree based on a list of sectors.
    /// This returns the "Root" nodes (can be multiple),
    /// and you have to traverse the tree yourself
    /// </summary>
    /// <param name="sectors"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static SectorTreeNode[] GenerateSectorTree(Sector[] sectors)
    {
        var roots = new List<SectorTreeNode>();
        foreach (Sector sector in sectors.Where(x => x.ParentId == -1 || x.ParentId==null)) //For v9, ParentId is null
        {
            var treeNode = new SectorTreeNode(MakeTreeRecursive(sector, sectors), Self: sector, Parent: null);
            roots.Add(treeNode);
        }

        return roots.ToArray();
    }

    public static SectorTreeNode[] MakeTreeRecursive(Sector parent, Sector[] datas)
    {
        var children = new List<SectorTreeNode>();
        foreach (var item in datas.Where(x => x.ParentId == parent.Id))
        {
            children.Add(new SectorTreeNode(MakeTreeRecursive(item, datas), item, Parent: parent));
        }
        return children.ToArray();
    }
}
