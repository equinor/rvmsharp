namespace CadRevealFbxProvider.Tests;

using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using System.Numerics;

[TestFixture]
public class FbxNodeToCadRevealNodeConverterTests
{
    private static readonly string TestFile = new("TestSamples\\cube_and_instanced_cube_with_parent.fbx");

    [Test]
    public void CubeAndInstancedCubeParentedToBaseMeshAllWithTransforms_ConvertRecursive_VerifyCorrectTransformations()
    {
        using var fbxImporter = new FbxImporter();
        var fbxRootNode = fbxImporter.LoadFile(TestFile);

        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIdGenerator = new InstanceIdGenerator();
        var nodeNameFiltering = new NodeNameFiltering(new NodeNameExcludeRegex(null));

        var rootNode = FbxNodeToCadRevealNodeConverter.ConvertRecursive(
            fbxRootNode,
            treeIndexGenerator,
            instanceIdGenerator,
            nodeNameFiltering,
            null
        );

        Assert.That(rootNode, Is.Not.Null);
        Assert.That(rootNode.Children, Has.Length.EqualTo(1));

        var baseObject = rootNode.Children[0];
        AssertCadRevealNode<TriangleMesh>(baseObject, "Base",
            new BoundingBox(new Vector3(-1.5f, -1.5f, -1.5f), new Vector3(0.5f, 0.5f, 0.5f)));

        Assert.That(baseObject.Children, Has.Length.EqualTo(2));

        var cube = baseObject.Children[0];
        AssertCadRevealNode<InstancedMesh>(cube, "Cube",
            new BoundingBox(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f)));

        var instanced = baseObject.Children[1];
        AssertCadRevealNode<InstancedMesh>(instanced, "Instanced",
            new BoundingBox(new Vector3(-0.25f, -0.25f, -1.25f), new Vector3(0.25f, 0.25f, -0.75f)));
        return;

        void AssertCadRevealNode<T>(CadRevealNode? node, string name, BoundingBox expectedBoundingBox)
        {
            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo(name));
            Assert.That(node.BoundingBoxAxisAligned!.Same(expectedBoundingBox));
            Assert.That(node.Geometries, Has.Length.EqualTo(1));
            Assert.That(node.Geometries.First(), Is.InstanceOf<T>());
        }
    }
}
