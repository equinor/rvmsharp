namespace CadRevealComposer.Utils;

using System;

public class SimplificationLogObject
{
    public int SimplificationBeforeVertexCount;
    public int SimplificationAfterVertexCount;
    public int SimplificationBeforeTriangleCount;
    public int SimplificationAfterTriangleCount;
    public int FailedOptimizations;

    public static void LogSimplifications(
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
}
