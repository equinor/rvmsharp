namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer;

using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

///
/// <summary>
/// A CadRevealNode contains a list of primitives, where each primitive can be of a different type such as
/// TriangleMesh, Cone, Circle, etc. One special type of primitive is the InstancedMesh, which represents a
/// geometric object being instanced. That is, multiple objects are drawn at different locations but all
/// share the same instance in memory.
///
/// When an InstancedMesh with the same instance ID appears within a list of primitives or across different
/// nodes, we need to ensure that all those nodes have the same Mesh object attached, meaning they share the
/// same reference.
///
/// This class iterates through all nodes and primitives provided to Invoke(). When an InstancedMesh object is
/// encountered within the primitive lists, the instance ID and reference of its mesh are registered. If an
/// instanced primitive with an already registered instance ID is found, this class replaces the mesh reference
/// of that instance with the one from the registered instance. In this way, we always ensure that all instances
/// with the same instance ID refer to the same mesh in memory.
/// </summary>
///
public static class GeometryInstancer
{
    public static void Invoke(List<CadRevealNode> nodes)
    {
        var instanceIDsAlreadyDone = new Dictionary<ulong, Mesh>();

        foreach (CadRevealNode node in nodes)
        {
            for (int i = 0; i < node.Geometries.Length; i++)
            {
                APrimitive primitive = node.Geometries[i];
                if (primitive is not InstancedMesh instance)
                {
                    continue;
                }

                if (instanceIDsAlreadyDone.TryGetValue(instance.InstanceId, out Mesh? locatedMesh))
                {
                    node.Geometries[i] = new InstancedMesh(
                        instance.InstanceId,
                        locatedMesh,
                        instance.InstanceMatrix,
                        instance.TreeIndex,
                        instance.Color,
                        instance.AxisAlignedBoundingBox
                    );
                }
                else
                {
                    instanceIDsAlreadyDone.Add(instance.InstanceId, instance.TemplateMesh);
                }
            }
        }
    }
}
