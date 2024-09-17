namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldPartOptimizers;

using System.Numerics;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealFbxProvider.BatchUtils.ScaffoldPartOptimizers;

public class ScaffoldPartOptimizerTestPartB : ScaffoldPartOptimizerTest
{
    public override List<Vector3> GetVerticesTruth()
    {
        return [new Vector3(3.0f, 0.0f, 0.0f), new Vector3(0.0f, 4.0f, 0.0f), new Vector3(0.0f, 0.0f, 5.0f)];
    }

    public override List<uint> GetIndicesTruth()
    {
        return [2, 0, 1];
    }

    public override string Name
    {
        get { return "Part A test optimizer"; }
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
                basePrimitive,
                new Mesh(GetVerticesTruth().ToArray(), GetIndicesTruth().ToArray(), mesh.Error),
                0,
                requestChildPartInstanceId
            )
        ];
    }

    public override string[] GetPartNameTriggerKeywords()
    {
        return ["Test B", "Another BTest"];
    }
}
