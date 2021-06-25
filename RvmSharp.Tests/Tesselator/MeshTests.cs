namespace RvmSharp.Tests.Tesselator
{
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using System.Numerics;
    using Tessellation;

    [TestFixture]
    public class MeshTests
    {
        static Mesh GenerateNewStandardMeshForTest()
        {
            var boundingBoxUnused = new RvmBoundingBox(Vector3.Zero, Vector3.Zero);
            // Arbitrary mesh
            int toleranceUnusedForPyramid = -1;
            return TessellatorBridge.Tessellate(
                new RvmPyramid(2,
                    Matrix4x4.Identity,
                    boundingBoxUnused,
                    1,
                    2,
                    3,
                    2,
                    0,
                    1,
                    1),
                toleranceUnusedForPyramid);
        }


        [Test]
        public void Mesh_Equals_ByContent()
        {
            var initialMesh = GenerateNewStandardMeshForTest();
            Assert.That(initialMesh, Is.EqualTo(initialMesh));
            Assert.That(initialMesh.GetHashCode(), Is.EqualTo(initialMesh.GetHashCode()));

            var newIdenticalMesh = GenerateNewStandardMeshForTest();
            Assert.That(initialMesh, Is.EqualTo(newIdenticalMesh));
            Assert.That(initialMesh.GetHashCode(), Is.EqualTo(newIdenticalMesh.GetHashCode()));

            // Modify the (previously identical) mesh
            newIdenticalMesh.Apply(Matrix4x4.CreateScale(new Vector3(1, 2, 3)));
            Assert.That(initialMesh, Is.Not.EqualTo(newIdenticalMesh));
            Assert.That(initialMesh.GetHashCode(), Is.Not.EqualTo(newIdenticalMesh.GetHashCode()));
        }
    }
}