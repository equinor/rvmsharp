namespace CadRevealComposer.Tests.Utils;

using System.Numerics;
using CadRevealComposer.Operations;
using Primitives;

// ReSharper disable once UnusedMember.Global
public class HierarchyComposerConverterTests
{
    [Test]
    public void ConvertToHierarchyNodes_GivenRevealNodes_ConvertsWithoutCrashing()
    {
        const string arrangedJson = @"{ ""myField"":""myValue"" }";

        var node2 = new CadRevealNode()
        {
            TreeIndex = 2,
            Children = Array.Empty<CadRevealNode>(),
            BoundingBoxAxisAligned = new BoundingBox(-Vector3.One, Vector3.One),
            Name = "NodeName",
            Attributes = { { "RefNo", "=123/322" }, { "Tag", "VG23-0001" } },
            Parent = null,
            Geometries = Array.Empty<APrimitive>(),
            OptionalDiagnosticInfo = arrangedJson,
        };

        var node1 = new CadRevealNode()
        {
            TreeIndex = 1,
            Children = new[] { node2 },
            BoundingBoxAxisAligned = new BoundingBox(-Vector3.One, Vector3.One),
            Name = "RootNode",
            Attributes = { { "RefNo", "=123/321" }, { "Tag", "23L0001" } },
            Parent = null,
            Geometries = Array.Empty<APrimitive>(),
            OptionalDiagnosticInfo = arrangedJson,
        };

        node2.Parent = node1;

        var nodes = new[] { node1, node2 };

        var hierarchyNodes = HierarchyComposerConverter.ConvertToHierarchyNodes(nodes);

        Assert.That(hierarchyNodes, Has.Exactly(2).Items);
        var firstNode = hierarchyNodes[0];

        Assert.That(firstNode.Name, Is.EqualTo("RootNode"));
        Assert.That(firstNode.RefNoDb, Is.EqualTo(123));
        Assert.That(firstNode.RefNoSequence, Is.EqualTo(321));
        Assert.That(firstNode.PDMSData, Contains.Key("Tag").WithValue("23L0001"));
        Assert.That(
            firstNode.PDMSData,
            Does.Not.ContainKey("RefNo"),
            "Expecting RefNo to be filtered out of the PDMS data as it is redundant"
        );
        Assert.That(firstNode.OptionalDiagnosticInfo, Is.EqualTo(arrangedJson));
    }
}
