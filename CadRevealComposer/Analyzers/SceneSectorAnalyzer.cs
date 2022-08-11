namespace CadRevealComposer.Analyzers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        foreach (Sector sector in sectors.Where(x => x.ParentId == -1))
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
