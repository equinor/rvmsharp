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


    public record SectorTreeNode(SectorTreeNode[] Children, Sector Self)
    {
        public bool IsLeaf => !Children.Any();
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
        foreach(Sector sector in sectors.Where(x => x.ParentId == -1))
        {
            var treeNode = new SectorTreeNode(MakeTreeRecursive(sector, sectors), Self: sector);
            roots.Add(treeNode);
        }

        return roots.ToArray();
    }

    public static SectorTreeNode[] MakeTreeRecursive(Sector parent, Sector[] datas)
    {
        var children = new List<SectorTreeNode>();
        foreach (var item in datas.Where(x => x.ParentId == parent.Id))
        {
            children.Add(new SectorTreeNode(MakeTreeRecursive(item, datas), item));
        }
        return children.ToArray();
    }
}
