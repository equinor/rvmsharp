namespace CadRevealRvmProvider.Tessellation;

using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using Operations;
using RvmSharp.Primitives;
using RvmSharp.Tessellation;
using System.Diagnostics;

public class RvmTessellator
{
    public static Mesh ConvertRvmMesh(RvmMesh rvmMesh)
    {
        // Reveal does not use normals, so they are discarded here.
        // Because it does not use normals, we can remove duplicate vertices optimizing it slightly
        return new Mesh(rvmMesh.Vertices, rvmMesh.Triangles, rvmMesh.Error);
    }

    public static APrimitive[] TessellateAndOutputInstanceMeshes(
        RvmFacetGroupMatcher.Result[] facetGroupInstancingResult,
        RvmPyramidInstancer.Result[] pyramidInstancingResult,
        InstanceIdGenerator instanceIdGenerator,
        float simplifierThreshold
    )
    {
        static TriangleMesh TessellateAndCreateTriangleMesh(
            ProtoMesh p,
            float simplifierThreshold,
            SimplificationLogObject simplificationLogObject,
            RvmTessellatorLogObject tessellationLogObject
        )
        {
            var rvmMesh = Tessellate(p.RvmPrimitive, tessellationLogObject);
            var mesh = ConvertRvmMesh(rvmMesh);

            if (simplifierThreshold > 0.0f)
            {
                Interlocked.Add(ref simplificationLogObject.SimplificationBeforeVertexCount, mesh.Vertices.Length);
                Interlocked.Add(ref simplificationLogObject.SimplificationBeforeTriangleCount, mesh.TriangleCount);
                mesh = Simplify.SimplifyMeshLossy(mesh, simplificationLogObject, simplifierThreshold);
                Interlocked.Add(ref simplificationLogObject.SimplificationAfterVertexCount, mesh.Vertices.Length);
                Interlocked.Add(ref simplificationLogObject.SimplificationAfterTriangleCount, mesh.TriangleCount);
            }

            return new TriangleMesh(mesh, p.TreeIndex, p.Color, p.AxisAlignedBoundingBox);
        }

        var facetGroupsNotInstanced = facetGroupInstancingResult
            .OfType<RvmFacetGroupMatcher.NotInstancedResult>()
            .Select(result => ((RvmFacetGroupWithProtoMesh)result.FacetGroup).ProtoMesh)
            .Cast<ProtoMesh>()
            .ToArray();

        var pyramidsNotInstanced = pyramidInstancingResult
            .OfType<RvmPyramidInstancer.NotInstancedResult>()
            .Select(result => result.Pyramid)
            .Cast<ProtoMesh>()
            .ToArray();

        var facetGroupInstanced = facetGroupInstancingResult
            .OfType<RvmFacetGroupMatcher.InstancedResult>()
            .GroupBy(
                result => (RvmPrimitive)result.Template,
                x => (ProtoMesh: (ProtoMesh)((RvmFacetGroupWithProtoMesh)x.FacetGroup).ProtoMesh, x.Transform)
            )
            .ToArray();

        var pyramidsInstanced = pyramidInstancingResult
            .OfType<RvmPyramidInstancer.InstancedResult>()
            .GroupBy(result => (RvmPrimitive)result.Template, x => (ProtoMesh: (ProtoMesh)x.Pyramid, x.Transform))
            .ToArray();

        // tessellate instanced geometries
        var stopwatch = Stopwatch.StartNew();
        var instanceTessellationLogObject = new RvmTessellatorLogObject("Failed instance tessellations");
        var meshes = facetGroupInstanced
            .Concat(pyramidsInstanced)
            .AsParallel()
            .Select(
                g =>
                    (
                        InstanceGroup: g,
                        Mesh: Tessellate(g.Key, instanceTessellationLogObject),
                        InstanceId: instanceIdGenerator.GetNextId() /* Must be identical for all instances of this mesh */
                    )
            )
            .Where(g => g.Mesh.Triangles.Length > 0) // ignore empty meshes
            .ToArray();
        var totalCount = meshes.Sum(m => m.InstanceGroup.Count());
        instanceTessellationLogObject.LogFailedTessellations();

        Console.WriteLine(
            $"Tessellated {meshes.Length:N0} meshes for {totalCount:N0} instanced meshes in {stopwatch.Elapsed}"
        );

        var instancedMeshes = meshes
            .SelectMany(
                group =>
                    group.InstanceGroup.Select(
                        item =>
                            new InstancedMesh(
                                InstanceId: group.InstanceId,
                                ConvertRvmMesh(group.Mesh),
                                item.Transform,
                                item.ProtoMesh.TreeIndex,
                                item.ProtoMesh.Color,
                                item.ProtoMesh.AxisAlignedBoundingBox
                            )
                    )
            )
            .ToArray();

        // tessellate and create TriangleMesh objects
        stopwatch.Restart();
        var logObject = new SimplificationLogObject();
        var meshTessellationLogObject = new RvmTessellatorLogObject("Failed mesh tessellations");
        var triangleMeshes = facetGroupsNotInstanced
            .Concat(pyramidsNotInstanced)
            .AsParallel()
            .Select(x => TessellateAndCreateTriangleMesh(x, simplifierThreshold, logObject, meshTessellationLogObject))
            .Where(t => t.Mesh.Indices.Length > 0) // ignore empty meshes
            .ToArray();

        meshTessellationLogObject.LogFailedTessellations();
        Console.WriteLine($"Tessellated {triangleMeshes.Length:N0} triangle meshes in {stopwatch.Elapsed}.");

        using (new TeamCityLogBlock("Mesh Reduction Stats"))
        {
            Console.WriteLine(
                $"""
                    Before Total Vertices: {logObject.SimplificationBeforeVertexCount, 10}
                    After total Vertices:  {logObject.SimplificationAfterVertexCount, 10}
                    Percent of Before Verts: {(logObject.SimplificationAfterVertexCount / (float)logObject.SimplificationBeforeVertexCount):P2}
                    """
            );
            Console.WriteLine("");
            Console.WriteLine(
                $"""
                    Before Total Triangles: {logObject.SimplificationBeforeTriangleCount, 10}
                    After total Triangles:  {logObject.SimplificationAfterTriangleCount, 10}
                    Percent of Before Tris: {(logObject.SimplificationAfterTriangleCount / (float)logObject.SimplificationBeforeTriangleCount):P2}
                    """
            );
            Console.WriteLine("");
            Console.WriteLine($"Number of failed simplifications of mesh: {logObject.FailedOptimizations}");
        }

        return instancedMeshes.Cast<APrimitive>().Concat(triangleMeshes).ToArray();
    }

    public static RvmMesh Tessellate(RvmPrimitive primitive, RvmTessellatorLogObject logObject)
    {
        RvmMesh mesh;
        try
        {
            mesh = TessellatorBridge.Tessellate(primitive, 0f) ?? RvmMesh.Empty;
        }
        catch
        {
            mesh = RvmMesh.Empty;
        }

        if (mesh.Vertices.Length == 0)
        {
            if (primitive is RvmFacetGroup f)
            {
                logObject.AddFailedFacetGroup(f.Polygons.Length);
            }
            else if (primitive is RvmPyramid)
            {
                logObject.AddFailedPyramid();
            }
            else
            {
                throw new NotImplementedException(
                    $"Could not tessellate primitive of type {primitive.GetType()}. \n{primitive}. This is unexpected. If this needs to be handled add a case for it"
                );
            }
        }

        return mesh;
    }
}
