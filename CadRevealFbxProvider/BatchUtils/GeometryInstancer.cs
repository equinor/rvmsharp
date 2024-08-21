namespace CadRevealFbxProvider.BatchUtils;

using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

public class GeometryInstancer
{
    public static void Invoke(List<CadRevealNode> nodes)
    {
        var instanceIDsAlreadyDone = new List<(ulong id, Mesh mesh)?>();

        foreach (CadRevealNode node in nodes)
        {
            for (int i = 0; i < node.Geometries.Length; i++)
            {
                APrimitive primitive = node.Geometries[i];
                if (primitive is not InstancedMesh instance)
                {
                    continue;
                }

                var instanceAlreadyDone = instanceIDsAlreadyDone.Find(element =>
                    (element != null) && (element.Value.id == instance.InstanceId)
                );

                if (instanceAlreadyDone != null)
                {
                    node.Geometries[i] = new InstancedMesh(
                        instance.InstanceId,
                        instanceAlreadyDone.Value.mesh,
                        instance.InstanceMatrix,
                        instance.TreeIndex,
                        instance.Color,
                        instance.AxisAlignedBoundingBox
                    );
                }
                else
                {
                    instanceIDsAlreadyDone.Add((instance.InstanceId, instance.TemplateMesh));
                }
            }
        }
    }
}
