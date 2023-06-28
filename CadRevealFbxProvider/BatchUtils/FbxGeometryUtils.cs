namespace CadRevealFbxProvider.BatchUtils;

public static class FbxGeometryUtils
{
    /// <summary>
    /// Gets all geometry pointers in the file below the given Fbx node (recursively including self)
    /// </summary>
    /// <param name="node">The start node</param>
    /// <returns>A list of all nodes, with no guarantee of ordering</returns>
    public static IEnumerable<IntPtr> GetAllGeomPointersRecursive(FbxNode node)
    {
        var nodeGeometryPtr = FbxMeshWrapper.GetMeshGeometryPtr(node);
        yield return nodeGeometryPtr;

        var childCount = FbxNodeWrapper.GetChildCount(node);
        for (var i = 0; i < childCount; i++)
        {
            FbxNode child = FbxNodeWrapper.GetChild(i, node);
            var allChildPointers = GetAllGeomPointersRecursive(child);
            foreach (IntPtr geomPointer in allChildPointers)
            {
                yield return geomPointer;
            }
        }
    }

    /// <summary>
    /// Gets all geometry pointers in the Fbx hierarchy with > 1 uses, so you can decide to reuse-instances or not
    /// </summary>
    /// <param name="node">Root node to start from</param>
    /// <returns>A set of pointers to geometries with multiple uses.</returns>
    public static HashSet<IntPtr> GetAllGeomPointersWithTwoOrMoreUses(FbxNode node)
    {
        // TODO in the future consider maybe having a smarter limit, such as "total savings by instancing" or similar.
        // TODO-cont: This would avoid simple meshes having instances as the overhead of runtime instancing is very high,
        // TODO-cont2: so we want to maximize memory savings.
        const int minUses = 2;
        var pointersWithXUses = FbxGeometryUtils
            .GetAllGeomPointersRecursive(node)
            .GroupBy(ptr => ptr) // Group identical pointers
            .Where(pointerGroup => pointerGroup.Count() >= minUses)
            .Select(pointerGroup => pointerGroup.First());
        return pointersWithXUses.ToHashSet();
    }
}
