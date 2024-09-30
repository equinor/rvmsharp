namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldPartOptimizers;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealFbxProvider.BatchUtils.ScaffoldPartOptimizers;

public class ScaffoldPartOptimizerTestPartC : ScaffoldPartOptimizerTest
{
    public override List<List<Vector3>> GetVerticesTruth()
    {
        return
        [
            [],
            [new Vector3(3.0f, 0.0f, 0.0f), new Vector3(0.0f, 4.0f, 0.0f), new Vector3(0.0f, 0.0f, 5.0f)],
            [new Vector3(3.0f, 0.0f, 0.0f), new Vector3(0.0f, 4.0f, 0.0f), new Vector3(0.0f, 0.0f, 5.0f)],
            [],
            [new Vector3(3.0f, 0.0f, 0.0f), new Vector3(0.0f, 4.0f, 0.0f), new Vector3(0.0f, 0.0f, 5.0f)],
            [new Vector3(3.0f, 0.0f, 0.0f), new Vector3(0.0f, 4.0f, 0.0f), new Vector3(0.0f, 0.0f, 5.0f)]
        ];
    }

    public override List<List<uint>> GetIndicesTruth()
    {
        return
        [
            [],
            [2, 0, 1],
            [2, 0, 1],
            [],
            [2, 0, 1],
            [2, 0, 1]
        ];
    }

    public override string Name
    {
        get { return "Part C test optimizer"; }
    }

    public override IScaffoldOptimizerResult[] Optimize(
        APrimitive basePrimitive,
        Mesh mesh,
        Func<ulong, int, ulong> requestChildPartInstanceId
    )
    {
        return
        [
            new ScaffoldOptimizerResult(
                new Circle(
                    Matrix4x4.Identity,
                    new Vector3(0, 0, 1),
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            ),
            new ScaffoldOptimizerResult(
                basePrimitive,
                new Mesh(GetVerticesTruth()[1].ToArray(), GetIndicesTruth()[1].ToArray(), mesh.Error),
                0,
                requestChildPartInstanceId
            ),
            new ScaffoldOptimizerResult(
                basePrimitive,
                new Mesh(GetVerticesTruth()[2].ToArray(), GetIndicesTruth()[2].ToArray(), mesh.Error),
                1,
                requestChildPartInstanceId
            ),
            new ScaffoldOptimizerResult(
                new Circle(
                    Matrix4x4.Identity,
                    new Vector3(0, 0, 1),
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            ),
            new ScaffoldOptimizerResult(
                new TriangleMesh(
                    new Mesh(GetVerticesTruth()[4].ToArray(), GetIndicesTruth()[4].ToArray(), mesh.Error),
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            ),
            new ScaffoldOptimizerResult(
                basePrimitive,
                new Mesh(GetVerticesTruth()[5].ToArray(), GetIndicesTruth()[5].ToArray(), mesh.Error),
                2,
                requestChildPartInstanceId
            )
        ];
    }

    public override string[] GetPartNameTriggerKeywords()
    {
        return ["Test C"];
    }
}
