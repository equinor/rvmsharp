namespace CadRevealRvmProvider.Tessellation;

using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using Operations;
using RvmSharp.Primitives;
using RvmSharp.Tessellation;
using System.Diagnostics;
using System.Numerics;

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
            TessellationLogObject tessellationLogObject
        )
        {
            Mesh mesh = TessellateAndSimplifyMesh(
                simplifierThreshold,
                p.RvmPrimitive,
                simplificationLogObject,
                tessellationLogObject
            );

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
        var instanceTessellationLogObject = new TessellationLogObject("Failed instance tessellations");
        var instanceSimplificationLogObject = new SimplificationLogObject();

        var meshes = facetGroupInstanced
            .Concat(pyramidsInstanced)
            .AsParallel()
            .Select(g =>
            {
                var allTransforms = g.Select(x => x.Transform).ToArray();
                Mesh mesh = SimplifyInstancedGroup(
                    simplifierThreshold,
                    g.Key,
                    allTransforms,
                    instanceSimplificationLogObject,
                    instanceTessellationLogObject
                );
                return (
                    InstanceGroup: g,
                    Mesh: mesh,
                    InstanceId: instanceIdGenerator.GetNextId() /* Must be identical for all instances of this mesh */
                );
            })
            .Where(g => g.Mesh.TriangleCount > 0) // ignore empty meshes
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
                                group.Mesh,
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

        var meshSimplificiationLogObject = new SimplificationLogObject();
        var meshTessellationLogObject = new TessellationLogObject("Failed mesh tessellations");
        var triangleMeshes = facetGroupsNotInstanced
            .Concat(pyramidsNotInstanced)
            .AsParallel()
            .Select(
                x =>
                    TessellateAndCreateTriangleMesh(
                        x,
                        simplifierThreshold,
                        meshSimplificiationLogObject,
                        meshTessellationLogObject
                    )
            )
            .Where(t => t.Mesh.Indices.Length > 0) // ignore empty meshes
            .ToArray();

        meshTessellationLogObject.LogFailedTessellations();
        Console.WriteLine($"Tessellated {triangleMeshes.Length:N0} triangle meshes in {stopwatch.Elapsed}.");

        LogSimplifications(meshSimplificiationLogObject, instanceSimplificationLogObject);

        return instancedMeshes.Cast<APrimitive>().Concat(triangleMeshes).ToArray();
    }

    private static void LogSimplifications(
        SimplificationLogObject meshLogObject,
        SimplificationLogObject instancedLogObject
    )
    {
        using (new TeamCityLogBlock("Mesh Reduction Stats"))
        {
            Console.WriteLine(
                $"""
                 Before Total Vertices of Triangle Meshes: {meshLogObject.SimplificationBeforeVertexCount, 10}
                 After total Vertices of Triangle Meshes:  {meshLogObject.SimplificationAfterVertexCount, 10}
                 Percent of Before Vertices of Triangle Meshes: {(meshLogObject.SimplificationAfterVertexCount / (float)meshLogObject.SimplificationBeforeVertexCount):P2}
                 """
            );
            Console.WriteLine("");
            Console.WriteLine(
                $"""
                 Before Total Triangles of Triangle Meshes: {meshLogObject.SimplificationBeforeTriangleCount, 10}
                 After total Triangles of Triangle Meshes:  {meshLogObject.SimplificationAfterTriangleCount, 10}
                 Percent of Before Triangles of Triangle Meshes: {(meshLogObject.SimplificationAfterTriangleCount / (float)meshLogObject.SimplificationBeforeTriangleCount):P2}
                 """
            );
            Console.WriteLine("");
            Console.WriteLine(
                $"""
                 Before Total Vertices of Instanced Meshes: {instancedLogObject.SimplificationBeforeVertexCount, 10}
                 After total Vertices of Instanced Meshes:  {instancedLogObject.SimplificationAfterVertexCount, 10}
                 Percent of Before Vertices of Instanced Meshes: {(instancedLogObject.SimplificationAfterVertexCount / (float)instancedLogObject.SimplificationBeforeVertexCount):P2}
                 """
            );
            Console.WriteLine("");
            Console.WriteLine(
                $"""
                 Before Total Triangles of Instanced Meshes: {instancedLogObject.SimplificationBeforeTriangleCount, 10}
                 After total Triangles of Instanced Meshes:  {instancedLogObject.SimplificationAfterTriangleCount, 10}
                 Percent of Before Triangles of Instanced Meshes: {(instancedLogObject.SimplificationAfterTriangleCount / (float)instancedLogObject.SimplificationBeforeTriangleCount):P2}
                 """
            );
            Console.WriteLine("");
            Console.WriteLine($"Number of failed simplifications of mesh: {meshLogObject.FailedOptimizations}");
            Console.WriteLine(
                $"Number of failed simplifications of instance: {instancedLogObject.FailedOptimizations}"
            );
        }
    }

    private static Mesh SimplifyInstancedGroup(
        float simplifierThreshold,
        RvmPrimitive primitive,
        Matrix4x4[] transforms,
        SimplificationLogObject instanceSimplificationLogObject,
        TessellationLogObject instanceTessellationLogObject
    )
    {
        // Scale the mesh up to the largest use in each dimension, and simplify based on that size.
        // This should make the simplification loss as small as possible and avoids simplifying a small template which is scaled up in its instances (magnifying the error).
        var allScales = transforms.Select(transform =>
        {
            Matrix4x4.Decompose(transform, out var scale, out _, out _);
            return scale;
        });

        Matrix4x4.Decompose(primitive.Matrix, out var originalScale, out _, out _);
        var maxScale = allScales.Aggregate(Vector3.Max);
        var primitiveToTessellate = primitive with { Matrix = primitive.Matrix * Matrix4x4.CreateScale(maxScale) };
        Mesh mesh = TessellateAndSimplifyMesh(
            simplifierThreshold,
            primitiveToTessellate,
            instanceSimplificationLogObject,
            instanceTessellationLogObject,
            transforms.Length
        );
        mesh.Apply(Matrix4x4.CreateScale(originalScale / maxScale));

        return mesh;
    }

    private static Mesh TessellateAndSimplifyMesh(
        float simplifierThreshold,
        RvmPrimitive primitiveToTessellate,
        SimplificationLogObject simplificationLogObject,
        TessellationLogObject tessellationLogObject,
        int numberOfInstances = 1
    )
    {
        var tessellated = Tessellate(primitiveToTessellate, tessellationLogObject);
        var mesh = ConvertRvmMesh(tessellated);
        if (simplifierThreshold > 0.0f)
        {
            Interlocked.Add(
                ref simplificationLogObject.SimplificationBeforeVertexCount,
                mesh.Vertices.Length * numberOfInstances
            );
            Interlocked.Add(
                ref simplificationLogObject.SimplificationBeforeTriangleCount,
                mesh.TriangleCount * numberOfInstances
            );
            mesh = Simplify.SimplifyMeshLossy(mesh, simplificationLogObject, simplifierThreshold);
            Interlocked.Add(
                ref simplificationLogObject.SimplificationAfterVertexCount,
                mesh.Vertices.Length * numberOfInstances
            );
            Interlocked.Add(
                ref simplificationLogObject.SimplificationAfterTriangleCount,
                mesh.TriangleCount * numberOfInstances
            );
        }

        return mesh;
    }

    public static RvmMesh Tessellate(RvmPrimitive primitive, TessellationLogObject logObject)
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
