namespace CadRevealComposer.Tests.Utils
{
    using NUnit.Framework;
    using Operations;
    using RvmSharp.Primitives;
    using System;
    using System.Numerics;

    // ReSharper disable once UnusedMember.Global
    public class HierarchyComposerConverterTests
    {
        [Test]
        public void ConvertToHierarchyNodes_GivenRevealNodes_ConvertsWithoutCrashing()
        {
            var arrangedJson = @"{ ""myField"":""myValue"" }";

            var node2 = new CadRevealNode()
            {
                TreeIndex = 2,
                Children = Array.Empty<CadRevealNode>(),
                BoundingBoxAxisAligned = new RvmBoundingBox(-Vector3.One, Vector3.One),
                Group = new RvmNode(2, "NodeName", Vector3.Zero, 0)
                {
                    Attributes = { { "RefNo", "=123/322" }, { "Tag", "VG23-0001" } }
                },
                NodeId = 1337,
                Parent = null,
                RvmGeometries = Array.Empty<RvmPrimitive>(),
                OptionalDiagnosticInfo = arrangedJson
            };

            var node1 = new CadRevealNode()
            {
                TreeIndex = 1,
                Children = new[] { node2 },
                BoundingBoxAxisAligned = new RvmBoundingBox(-Vector3.One, Vector3.One),
                Group = new RvmNode(2, "RootNode", Vector3.Zero, 0)
                {
                    Attributes = { { "RefNo", "=123/321" }, { "Tag", "23L0001" } }
                },
                NodeId = 9001,
                Parent = null,
                RvmGeometries = Array.Empty<RvmPrimitive>(),
                OptionalDiagnosticInfo = arrangedJson
            };

            node2.Parent = node1;

            var nodes = new[]
            {
                node1,
                node2
            };

            var hierarchyNodes = HierarchyComposerConverter.ConvertToHierarchyNodes(nodes);

            Assert.That(hierarchyNodes, Has.Exactly(2).Items);
            var firstNode = hierarchyNodes[0];

            Assert.That(firstNode.Name, Is.EqualTo("RootNode"));
            Assert.That(firstNode.RefNoDb, Is.EqualTo(123));
            Assert.That(firstNode.RefNoSequence, Is.EqualTo(321));
            Assert.That(firstNode.PDMSData, Contains.Key("Tag").WithValue("23L0001"));
            Assert.That(firstNode.PDMSData, Does.Not.ContainKey("RefNo"), "Expecting RefNo to be filtered out of the PDMS data as it is redundant");
            Assert.That(firstNode.OptionalDiagnosticInfo, Is.EqualTo(arrangedJson));
        }

        [Test]
        public void ConvertToHierarchyNodes_GivenRevealNodes_CrashesIfTreeIndexIsOutOfRange()
        {


            var node1 = new CadRevealNode()
            {
                TreeIndex = uint.MaxValue + 1L,
                Children = Array.Empty<CadRevealNode>(),
                BoundingBoxAxisAligned = new RvmBoundingBox(-Vector3.One, Vector3.One),
                Group = new RvmNode(2, "RootNode", Vector3.Zero, 0)
                {
                    Attributes = { { "RefNo", "=123/321" }, { "Tag", "23L0001" } }
                },
                NodeId = 9001,
                Parent = null,
                RvmGeometries = Array.Empty<RvmPrimitive>()
            };

            var nodes = new[]
            {
                node1
            };

            Assert.That(() => HierarchyComposerConverter.ConvertToHierarchyNodes(nodes), Throws.Exception.Message.StartsWith("input was higher than the max uint32 value"));
        }
    }
}