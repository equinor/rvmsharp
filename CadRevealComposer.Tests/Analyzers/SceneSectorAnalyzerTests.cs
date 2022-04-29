namespace CadRevealComposer.Tests.Analyzers;

using CadRevealComposer.Analyzers;
using CadRevealComposer.Tests.Utils;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


    [Test]
    public void AnalyzeSectors_Returns_SumOfSectorCosts_ByPath()
    {
        SceneSectorAnalyzer.SectorTreeNode[] result = SceneSectorAnalyzer.GenerateSectorTree(scene.Sectors);
        Assert.That(result, Has.One.Items);
    }

    [Test]
    public void SectorTreeNode_IsLeaf_WhenItHasZeroChildren()
    {
        var treeNode = new SceneSectorAnalyzer.SectorTreeNode(
            Children: Array.Empty<SceneSectorAnalyzer.SectorTreeNode>(),
            new Sector());
        Assert.That(treeNode.IsLeaf, Is.True);
        var treeNode2 = new SceneSectorAnalyzer.SectorTreeNode(Children: new[] { treeNode }, new Sector());
        Assert.That(treeNode2.IsLeaf, Is.False);
    }

}
