namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldPartOptimizers;
using System.Numerics;
using CadRevealComposer.Tessellation;

public class ScaffoldPartOptimizerTestPartA : ScaffoldPartOptimizerTest
{
    public override List<Vector3> GetVerticesTruth()
    {
        return
        [
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f)
        ];
    }

    public override List<uint> GetIndicesTruth()
    {
        return [1, 0, 2];
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
        return [ "Test A" ];
    }
}
