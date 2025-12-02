namespace CadRevealRvmProvider.Tests;

using CadRevealComposer;

[TestFixture]
public class RvmProviderTests
{
    [TestFixture]
    public class AddMetadataForSurfaceUnits
    {
        [Test]
        public void WhenRvmContainsSurfaceUnits_MetadataIsAdded()
        {
            var rootNode = new CadRevealNode()
            {
                Name = "/A00-AREA",
                Children = [],
                TreeIndex = 1,
                Parent = null,
            };
            var branchNode = new CadRevealNode()
            {
                Name = "/A00-AREA/BRANCH",
                Children = [],
                TreeIndex = 2,
                Parent = rootNode,
            };
            var branchNode2 = new CadRevealNode()
            {
                Name = "/A00-AREA/BRANCH/DECK-1",
                Children = [],
                TreeIndex = 3,
                Parent = branchNode,
            };
            var surfaceUnitVolumeNode1 = new CadRevealNode()
            {
                Name = "/1B41",
                Children = [],
                TreeIndex = 4,
                Parent = branchNode2,
            };

            var surfaceUnitVolumeNode2 = new CadRevealNode()
            {
                Name = "/2C52",
                Children = [],
                TreeIndex = 5,
                Parent = branchNode2,
            };

            rootNode.Children = [branchNode];
            branchNode.Children = [branchNode2];
            branchNode2.Children = [surfaceUnitVolumeNode1, surfaceUnitVolumeNode2];

            CadRevealNode[] nodes = [rootNode, branchNode, branchNode2, surfaceUnitVolumeNode1, surfaceUnitVolumeNode2];

            RvmProvider.AddMetadataForSurfaceUnits(nodes);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(surfaceUnitVolumeNode1.Attributes.ContainsKey("IsSurfaceUnitVolume"), Is.True);
                Assert.That(surfaceUnitVolumeNode1.Attributes["IsSurfaceUnitVolume"], Is.EqualTo("true"));
                Assert.That(surfaceUnitVolumeNode1.Attributes.ContainsKey("SurfaceUnitVolume"), Is.True);
                Assert.That(surfaceUnitVolumeNode1.Attributes["SurfaceUnitVolume"], Is.EqualTo("1B41"));

                Assert.That(surfaceUnitVolumeNode2.Attributes.ContainsKey("IsSurfaceUnitVolume"), Is.True);
                Assert.That(surfaceUnitVolumeNode2.Attributes["IsSurfaceUnitVolume"], Is.EqualTo("true"));
                Assert.That(surfaceUnitVolumeNode2.Attributes.ContainsKey("SurfaceUnitVolume"), Is.True);
                Assert.That(surfaceUnitVolumeNode2.Attributes["SurfaceUnitVolume"], Is.EqualTo("2C52"));
            }
        }
    }
}
