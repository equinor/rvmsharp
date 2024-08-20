namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldPartOptimizers;
using System.Numerics;
using CadRevealComposer.Tessellation;

public class ScaffoldPartOptimizerTestPartB : ScaffoldPartOptimizerTest
{
    public override List<Vector3> GetVerticesTruth()
    {
        return
        [
            new Vector3(3.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 4.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 5.0f)
        ];
    }

    public override List<uint> GetIndicesTruth()
    {
        return [2, 0, 1];
    }

    public override string GetName()
    {
        return "Part A test optimizer";
    }
    public override Mesh[] Optimize(Mesh mesh)
    {
        return [new Mesh(GetVerticesTruth().ToArray(), GetIndicesTruth().ToArray(), mesh.Error)];
    }

    public override string[] GetPartNameTriggerKeywords()
    {
        return [ "Test B", "Another BTest" ];
    }
}
