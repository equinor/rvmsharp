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
                new FacesGrid(new GridParameters(3, 3, 3, Vector3.Zero, 5f),
                    new []
                    {
                        new Node(CompressFlags.None, 1, 1, Color.Red,
                            new []
                            {
                                new Face(FaceFlags.PositiveXVisible | FaceFlags.NegativeXVisible |
                                         FaceFlags.PositiveYVisible | FaceFlags.NegativeYVisible |
                                         FaceFlags.PositiveZVisible | FaceFlags.NegativeZVisible,
                                    0, 0, Color.Blue),
                                new Face(FaceFlags.PositiveXVisible | FaceFlags.NegativeXVisible |
                                         FaceFlags.PositiveYVisible | FaceFlags.NegativeYVisible |
                                         FaceFlags.PositiveZVisible | FaceFlags.NegativeZVisible,
                                    0, 1, Color.Green)
                            })
                    }));
            using var outputStream = File.Create(@"E:\gush\projects\cognite\reveal-master\examples\public\primitives\sector_0.f3d");
            F3dWriter.WriteSector(f3d, outputStream);
        }

    }
}