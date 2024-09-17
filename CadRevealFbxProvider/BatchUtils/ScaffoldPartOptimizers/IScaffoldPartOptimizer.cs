namespace CadRevealFbxProvider.BatchUtils.ScaffoldPartOptimizers;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

public interface IScaffoldPartOptimizer
{
    public string Name { get; }
    public IScaffoldOptimizerResult[] Optimize(
        APrimitive basePrimitive, // Primitive that contains the mesh
        Mesh mesh, // The mesh
        Func<ulong, int, ulong> requestChildPartInstanceId // Function will be called when the optimizer needs an instance ID parameters are (index of child mesh, instance ID of base primitive)
    );
    public string[] GetPartNameTriggerKeywords();
}
