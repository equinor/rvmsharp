namespace CadRevealRvmProvider.Tessellation;

using CadRevealComposer.Tessellation;
using CadRevealComposer.Primitives;
using RvmSharp.Tessellation;
using RvmSharp.Primitives;
using System;
using System.Numerics;
using System.Diagnostics;
using System.Linq;

public class RvmTesselator
{
    public static Mesh ConvertRvmMesh(RvmMesh rvmMesh)
    {
        return new Mesh(rvmMesh.Vertices, rvmMesh.Normals, rvmMesh.Triangles, rvmMesh.Error);
    }

    public static APrimitive[] TessellateAndOutputInstanceMeshes(
        RvmFacetGroupMatcher.Result[] facetGroupInstancingResult)
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


        var facetGroupInstanced = facetGroupInstancingResult
            .OfType<RvmFacetGroupMatcher.InstancedResult>()
            .GroupBy(result => (RvmPrimitive)result.Template,
                x => (ProtoMesh: (ProtoMesh)((RvmFacetGroupWithProtoMesh)x.FacetGroup).ProtoMesh, x.Transform))
            .ToArray();

        // tessellate instanced geometries
        var stopwatch = Stopwatch.StartNew();
        var meshes = facetGroupInstanced
            .AsParallel()
            .Select(g => (InstanceGroup: g, Mesh: Tessellate(g.Key)))
            .Where(g => g.Mesh.Triangles.Length > 0) // ignore empty meshes
            .ToArray();
        var totalCount = meshes.Sum(m => m.InstanceGroup.Count());
        Console.WriteLine(
            $"Tessellated {meshes.Length:N0} meshes for {totalCount:N0} instanced meshes in {stopwatch.Elapsed}");

        var instancedMeshes = meshes
            .SelectMany((group, index) => group.InstanceGroup.Select(item => new InstancedMesh(
                InstanceId: index,
                ConvertRvmMesh(group.Mesh),
                item.Transform,
                item.ProtoMesh.TreeIndex,
                item.ProtoMesh.Color,
                item.ProtoMesh.AxisAlignedBoundingBox)))
            .ToArray();

        // tessellate and create TriangleMesh objects
        stopwatch.Restart();
        var triangleMeshes = facetGroupsNotInstanced
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

    private static RvmMesh Tessellate(RvmPrimitive primitive)
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
                Console.WriteLine("WARNING: Could not tessellate pyramid!");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        return mesh;
    }

    /// <summary>
    /// Sole purpose is to keep the <see cref="ProtoMeshFromFacetGroup"/> through processing of facet group instancing.
    /// </summary>
    public record RvmFacetGroupWithProtoMesh(
            ProtoMeshFromFacetGroup ProtoMesh,
            uint Version,
            Matrix4x4 Matrix,
            RvmBoundingBox BoundingBoxLocal,
            RvmFacetGroup.RvmPolygon[] Polygons)
        : RvmFacetGroup(Version, Matrix, BoundingBoxLocal, Polygons);
}