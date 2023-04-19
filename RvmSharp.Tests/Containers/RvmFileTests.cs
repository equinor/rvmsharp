namespace RvmSharp.Tests.Containers;

using NUnit.Framework;
using RvmSharp.Containers;
using RvmSharp.Primitives;
using System.Collections.Generic;
using System.Numerics;

public class RvmFileTests
{
    [Test]
    public void AttachAttributes_WhenRvmFileHasDuplicatedNodes_IgnoresThem()
    {
        var groups = new RvmNode[]
        {
            new RvmNode(2, "Root", Vector3.Zero, 0)
            {
                Children =
                {
                    new RvmNode(2, "Duplicate", Vector3.Zero, 0),
                    new RvmNode(2, "Duplicate", Vector3.Zero, 0)
                }
            }
        };
        var root = new PdmsTextParser.PdmsNode(
            "Root",
            new Dictionary<string, string>() { { "RefNo", "=123/321" } },
            null,
            new List<PdmsTextParser.PdmsNode>()
        );
        var duplicate = new PdmsTextParser.PdmsNode(
            "Duplicate",
            new Dictionary<string, string>() { { "Imported", "True" } },
            root,
            new List<PdmsTextParser.PdmsNode>()
        );
        root.Children.Add(duplicate);

        var attributeNodes = new[] { root };

        // This would throw with a
        Assert.DoesNotThrow(() => RvmFile.AttachAttributes(attributeNodes, groups));

        Assert.That(groups[0].Attributes, Has.One.Items);
        Assert.That((groups[0].Children[0] as RvmNode)!.Attributes, Is.Empty);
        Assert.That((groups[0].Children[1] as RvmNode)!.Attributes, Is.Empty);
    }

    [Test]
    public void AttachAttributes_WhenRvmFileDoesNotHaveDuplicatedNodesButMissingAttributes_SkipsMissingAttributes()
    {
        var groups = new RvmNode[]
        {
            new RvmNode(2, "Root", Vector3.Zero, 0)
            {
                Children = { new RvmNode(2, "One", Vector3.Zero, 0), new RvmNode(2, "Two", Vector3.Zero, 0) }
            }
        };
        var root = new PdmsTextParser.PdmsNode(
            "Root",
            new Dictionary<string, string>() { { "RefNo", "=123/321" } },
            null,
            new List<PdmsTextParser.PdmsNode>()
        );
        var two = new PdmsTextParser.PdmsNode(
            "Two",
            new Dictionary<string, string>() { { "Imported", "True" } },
            root,
            new List<PdmsTextParser.PdmsNode>()
        );
        root.Children.Add(two);

        var attributeNodes = new[] { root };

        // This would throw with a
        Assert.DoesNotThrow(() => RvmFile.AttachAttributes(attributeNodes, groups));

        Assert.That(groups[0].Attributes, Has.One.Items);
        Assert.That((groups[0].Children[0] as RvmNode)!.Attributes, Is.Empty);
        Assert.That((groups[0].Children[1] as RvmNode)!.Attributes, Has.One.Items);
    }
}
