namespace CadRevealRvmProvider.Tessellation;

using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using CadRevealComposer.Utils.MeshTools;
using Operations;
using RvmSharp.Primitives;
using RvmSharp.Tessellation;
using System.Diagnostics;
using System.Drawing;

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
        static TriangleMesh TessellateAndCreateTriangleMesh(ProtoMesh p, float simplifierThreshold)
        {
            simplifierThreshold = 0.05f;

            var rvmMesh = Tessellate(p.RvmPrimitive);
            var mesh = ConvertRvmMesh(rvmMesh);
            (mesh, bool success) = Simplify.SimplifyMeshLossy(mesh, simplifierThreshold);
            return new TriangleMesh(mesh, p.TreeIndex, Color.LightSkyBlue, p.AxisAlignedBoundingBox);
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
        var meshes = facetGroupInstanced
            .Concat(pyramidsInstanced)
            .AsParallel()
            .Select(
                g =>
                    (
                        InstanceGroup: g,
                        Mesh: Tessellate(g.Key),
                        InstanceId: instanceIdGenerator.GetNextId() /* Must be identical for all instances of this mesh */
                    )
            )
            .Where(g => g.Mesh.Triangles.Length > 0) // ignore empty meshes
            .ToArray();
        var totalCount = meshes.Sum(m => m.InstanceGroup.Count());
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
        var triangleMeshes = facetGroupsNotInstanced
            .Concat(pyramidsNotInstanced)
            .AsParallel()
            .Select(x => TessellateAndCreateTriangleMesh(x, simplifierThreshold))
            .Where(t => t.Mesh.Indices.Length > 0) // ignore empty meshes
            .ToArray();

        Console.WriteLine($"Tessellated {triangleMeshes.Length:N0} triangle meshes in {stopwatch.Elapsed}.");

        using (new TeamCityLogBlock("Mesh Reduction Stats"))
        {
            Console.WriteLine(
                $"""
                    Before Total Vertices: {Simplify.SimplificationBefore, 10}
                    After total Vertices:  {Simplify.SimplificationAfter, 10}
                    Percent of Before Verts: {(Simplify.SimplificationAfter / (float)Simplify.SimplificationBefore):P2}
                    """
            );
            Console.WriteLine("");
            Console.WriteLine(
                $"""
                    Before Total Triangles: {Simplify.SimplificationBeforeTriangleCount, 10}
                    After total Triangles:  {Simplify.SimplificationAfterTriangleCount, 10}
                    Percent of Before Tris: {(Simplify.SimplificationAfterTriangleCount / (float)Simplify.SimplificationBeforeTriangleCount):P2}
                    """
            );
        }

        return instancedMeshes.Cast<APrimitive>().Concat(triangleMeshes).ToArray();
    }

    public static RvmMesh Tessellate(RvmPrimitive primitive)
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
                Console.WriteLine($"WARNING: Could not tessellate facet group! Polygon count: {f.Polygons.Length}");
            }
            else if (primitive is RvmPyramid)
            {
                Console.WriteLine("WARNING: Could not tessellate pyramid!: " + primitive);
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
