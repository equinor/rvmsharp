namespace CadRevealComposer.Tests.Developer
{
    using NUnit.Framework;

    [TestFixture]
    public class CustomScene
    {
        //private const string ExportPath = "/Users/GUSH/projects/echo/echo-web/Echo3DWeb-master/EchoReflectApi/EchoReflect.Api/AppData/demomodel";
        //private const string Mesh2CtmPath = "/Users/GUSH/projects/rvmsharp/rvmsharp/tools/OpenCTM/mesh2ctm.osx";

        [Test]
        public void ExportSingleFacetGroup()
        {
            /*var facetGroup = TestSampleLoader.LoadTestJson<RvmFacetGroup>("m1.json");
            var adjustedFacetGroup = facetGroup; // TODO:
            PeripheralFileExporter.ExportInstancedMeshesToObjFile(new DirectoryInfo(ExportPath), 0, new []{})
            var node = new RvmNode(1, "testGroup", Vector3.Zero, 0);
            node.Children.Add(facetGroup);
            var rvmModel = new RvmModel(1, "test project", "test model",
                new []{node},
                Array.Empty<RvmPrimitive>(),
                Array.Empty<RvmColor>());

            var p = new CadRevealComposerRunner.Parameters(new ProjectId(1), new ModelId(1), new RevisionId(1), false,
                true, true);
            var geometries = new APrimitive[]
            {
                new InstancedMesh(new CommonPrimitiveProperties(
                        1, 0,
                        Vector3.Zero, Quaternion.Identity, Vector3.One,
                        10, new RvmBoundingBox(Vector3.One * -10, Vector3.One * 10),
                        Color.Blue, (Vector3.UnitZ, 0)),
                    0,
                    0, 10,
                    0, 0, 0,
                    0, 0, 0,
                    1, 1, 1)
            };
            // TODO: write CTM file
            var si = (new[] { new SceneCreator.SectorInfo(
                0, null, 0, "0", "", new []{""}, 10, 1, 10, geometries, new RvmBoundingBox(Vector3.One * -10, Vector3.One * 10)
                ) }).ToImmutableArray();
            SceneCreator.WriteSceneFile(si, p, new DirectoryInfo(ExportPath), 10);

            var rvmStore = new RvmStore();
            rvmStore.RvmFiles.Add(new RvmFile(new RvmFile.RvmHeader(1, "", "", "", "", ""),
                rvmModel));
            CadRevealComposerRunner.ProcessRvmStore(rvmStore,
                new DirectoryInfo(ExportPath),
                new CadRevealComposerRunner.Parameters(
                    new ProjectId(1),
                    new ModelId(1),
                    new RevisionId(1),
                    false,
                    true,
                    true),
                new CadRevealComposerRunner.ToolsParameters(
                    Mesh2CtmPath,
                    "",
                    false));*/
        }
    }
}