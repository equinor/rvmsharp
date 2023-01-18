namespace CadRevealRvmProvider.Tessellation;

using Operations;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Primitives;
using CadRevealFbxProvider.BatchUtils;
using RvmSharp.Tessellation;
using RvmSharp.Primitives;
using System;
using System.Diagnostics;
using System.Linq;

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
        InstanceIdGenerator instanceIdGenerator)
    {
        static TriangleMesh TessellateAndCreateTriangleMesh(ProtoMesh p)
        {
            var mesh = Tessellate(p.RvmPrimitive);
            return new TriangleMesh(ConvertRvmMesh(mesh), p.TreeIndex, p.Color, p.AxisAlignedBoundingBox);
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
            .GroupBy(result => (RvmPrimitive)result.Template,
                x => (ProtoMesh: (ProtoMesh)((RvmFacetGroupWithProtoMesh)x.FacetGroup).ProtoMesh, x.Transform))
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
            .Select(g => (InstanceGroup: g, Mesh: Tessellate(g.Key), InstanceId: instanceIdGenerator.GetNextId() /* Must be identical for all instances of this mesh */ ))
            .Where(g => g.Mesh.Triangles.Length > 0) // ignore empty meshes
            .ToArray();
        var totalCount = meshes.Sum(m => m.InstanceGroup.Count());
        Console.WriteLine(
            $"Tessellated {meshes.Length:N0} meshes for {totalCount:N0} instanced meshes in {stopwatch.Elapsed}");

        var instancedMeshes = meshes
            .SelectMany(group => group.InstanceGroup.Select(item => new InstancedMesh(
                InstanceId: group.InstanceId,
                ConvertRvmMesh(group.Mesh),
                item.Transform,
                item.ProtoMesh.TreeIndex,
                item.ProtoMesh.Color,
                item.ProtoMesh.AxisAlignedBoundingBox)))
            .ToArray();

        // tessellate and create TriangleMesh objects
        stopwatch.Restart();
        var triangleMeshes = facetGroupsNotInstanced
            .Concat(pyramidsNotInstanced)
            .AsParallel()
            .Select(TessellateAndCreateTriangleMesh)
            .Where(t => t.Mesh.Triangles.Length > 0) // ignore empty meshes
            .ToArray();
        Console.WriteLine($"Tessellated {triangleMeshes.Length:N0} triangle meshes in {stopwatch.Elapsed}");

        return instancedMeshes
            .Cast<APrimitive>()
            .Concat(triangleMeshes)
            .ToArray();
    }

    public static RvmMesh Tessellate(RvmPrimitive primitive)
    {
        RvmMesh mesh = RvmMesh.Empty;
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
                Console.WriteLine("WARNING: Could not tessellate pyramid!");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        return mesh;
    }


}
