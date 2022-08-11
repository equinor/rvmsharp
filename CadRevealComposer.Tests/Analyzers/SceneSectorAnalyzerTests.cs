namespace CadRevealComposer.Tests.Analyzers;

using CadRevealComposer.Analyzers;
using CadRevealComposer.Tests.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

[TestFixture]
public class SceneSectorAnalyzerTests
{
    Scene scene;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        scene = TestSampleLoader.LoadTestJson<Scene>("scene_hda_v8.json");
    }

    [Test]
    public void AnalyzeSectors_Returns_SectorCount_AsResult()
    {
        SceneSectorAnalyzer.SectorAnalysisResult result = SceneSectorAnalyzer.AnalyzeSectorsInScene(scene);
        Assert.That(result.SectorCount, Is.EqualTo(119));
    }

    [Test]
    public void AnalyzeSectors_Returns_MaxSectorDepth_AsResult()
    {
        SceneSectorAnalyzer.SectorAnalysisResult result = SceneSectorAnalyzer.AnalyzeSectorsInScene(scene);
        Assert.That(result.MaxSectorDepth, Is.EqualTo(5));
    }


    [Test]
    public void AnalyzeSectors_Returns_AverageSectorCost_AsResult()
    {
        SceneSectorAnalyzer.SectorAnalysisResult result = SceneSectorAnalyzer.AnalyzeSectorsInScene(scene);
        Assert.That(result.AverageEstimatedTriangleCount, Is.EqualTo(50928.4).Within(0.1));
    }

    [Test]
    public void AnalyzeSectors_Returns_AverageDrawcallCount_AsResult()
    {
        SceneSectorAnalyzer.SectorAnalysisResult result = SceneSectorAnalyzer.AnalyzeSectorsInScene(scene);
        Assert.That(result.AverageEstimatedDrawcallCount, Is.EqualTo(184.6).Within(0.1));
    }


    public record SectorCsvData(string Path, long SumEstimatedTriangleCount, long SumDownloadSize, string Parts, int Depth);

    [Test]
    public void AnalyzeSectors_Returns_SumOfSectorCosts_ByPath()
    {
        (string sectorId, Sector sector)[][] results = SceneSectorAnalyzer.CalculateMinimumCostForLeafs(scene.Sectors);

        var output = new List<string>();
        var rows = new List<SectorCsvData>();
        foreach (var result in results)
        {
            var path = string.Join("/", result.Select(x => x.sectorId));
            var sumEstimatedTriangleCount = result.Sum(x=> x.sector.EstimatedTriangleCount);
            var downloadSizeSum = result.Sum(x=> (x.sector.IndexFile.DownloadSize + x.sector.FacesFile.DownloadSize));
            var math = string.Join(",", result.Select(x => $"{x.sector.EstimatedTriangleCount,10:N0}"));
            var line = $"Cost of Path: {path,-30}: {sumEstimatedTriangleCount,12:N0}, " +
                $"Parts: {string.Join(",", result.Select(x => $"{x.sector.EstimatedTriangleCount,10:N0}"))}," +
                $" Depth {result.Length}," +
                $" DownloadSizeSum: {downloadSizeSum,12:N0}";
            rows.Add(new SectorCsvData(path, sumEstimatedTriangleCount, downloadSizeSum, math, result.Length));
            output.Add(line);
        }

        File.WriteAllText(@"C:\Users\VES\Documents\Projects\RvmSharp\TestData\sceneDebug.txt", string.Join(Environment.NewLine, output));
        File.WriteAllText(@"C:\Users\VES\Documents\Projects\RvmSharp\TestData\sceneDebug.csv", WriteCsvToString(rows));

        Assert.That(results, Has.Exactly(103).Items);

    }

    public record SectorBoxCsvData(string Id, string Depth, string TriangleCount, string DrawCallCount, string DownloadSizeIndex, string DownloadSizeFaces, string BoxMinX, string BoxMinY, string BoxMinZ, string BoxMaxX, string BoxMaxY, string BoxMaxZ);

    [Test]
    public void AnalyzeSectors_Returns_BoxOfSectorCosts_ByPath()
    {
        (string sectorId, Sector sector)[] results = SceneSectorAnalyzer.CalculateAllSectorsList(scene.Sectors);

        var rows = new List<SectorBoxCsvData>();
        foreach (var result in results)
        {
            rows.Add(new SectorBoxCsvData(
                result.sectorId,
                result.sector.Depth.ToString(),
                result.sector.EstimatedTriangleCount.ToString(),
                result.sector.EstimatedDrawCallCount.ToString(),
                result.sector.IndexFile.DownloadSize.ToString(),
                result.sector.FacesFile.DownloadSize.ToString(),
                result.sector.BoundingBox.Min.X.ToString().Replace(",", "."),
                result.sector.BoundingBox.Min.Y.ToString().Replace(",", "."),
                result.sector.BoundingBox.Min.Z.ToString().Replace(",", "."),
                result.sector.BoundingBox.Max.X.ToString().Replace(",", "."),
                result.sector.BoundingBox.Max.Y.ToString().Replace(",", "."),
                result.sector.BoundingBox.Max.Z.ToString().Replace(",", ".")));
        }

        //File.WriteAllText(@"C:\Users\VES\Documents\Projects\RvmSharp\TestData\sceneDebug.txt", string.Join(Environment.NewLine, output));
        File.WriteAllText(@"C:\Users\VES\Documents\Projects\RvmSharp\TestData\sceneDebugBoxes.csv", WriteBoxCsvToString(rows));

        Assert.That(true);

    }
    private string WriteBoxCsvToString<T>(IEnumerable<T> rows) where T : class
    {
        var sb = new StringBuilder();

        var properties = typeof(SectorBoxCsvData).GetProperties();
        sb.AppendLine(string.Join(';', properties.Select(x => x.Name).ToArray()));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(";", properties.Select(x => x.GetValue(row))));
        }

        return sb.ToString();
    }


    private string WriteCsvToString<T>(IEnumerable<T> rows) where T : class
    {
        var sb = new StringBuilder();

        var properties = typeof(SectorCsvData).GetProperties();
        sb.AppendLine(string.Join(';', properties.Select(x => x.Name).ToArray()));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(";", properties.Select(x => x.GetValue(row))));
        }

        return sb.ToString();
    }

    [Test]
    public void SectorTreeNode_IsLeaf_WhenItHasZeroChildren()
    {
        var treeNode = new SceneSectorAnalyzer.SectorTreeNode(
            Children: Array.Empty<SceneSectorAnalyzer.SectorTreeNode>(),
            new Sector(),
            Parent: null);
        Assert.That(treeNode.IsLeaf, Is.True);
        var treeNode2 = new SceneSectorAnalyzer.SectorTreeNode(Children: new[] { treeNode }, new Sector(), Parent: null);
        Assert.That(treeNode2.IsLeaf, Is.False);
    }
}
