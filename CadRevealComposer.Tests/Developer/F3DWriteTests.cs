namespace CadRevealComposer.Tests.Developer
{
    using Faces;
    using NUnit.Framework;
    using System.Drawing;
    using System.IO;
    using System.Numerics;
    using Writers;

    [TestFixture]
    public class F3DWriteTests
    {
        [Test]
        public void SimpleCube()
        {
            var f3d = new SectorFaces(0, null, Vector3.Zero, Vector3.One * 30,
                new FacesGrid(new GridParameters(2, 2, 2, Vector3.Zero, 5f),
                    new []
                    {
                        new Node(CompressFlags.HasColorOnEachCell, 1, 1, null,
                            new []
                            {
                                new Face(FaceFlags.PositiveXVisible | FaceFlags.PositiveYVisible,
                                    0, 0, Color.Blue)
                            })
                    }));
            using var outputStream = File.Create(@"E:\gush\projects\echo\echo-web\reveal-master\examples\public\primitives\sector_0.f3d");
            F3dWriter.WriteSector(f3d, outputStream);
        }

    }
}