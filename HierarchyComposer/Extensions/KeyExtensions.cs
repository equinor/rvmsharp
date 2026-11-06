namespace HierarchyComposer.Extensions;

using Model;

public static class KeyExtensions
{
    public static string GetGroupKey(this AabbItem aabbTable)
    {
        // Intentionally reduces precision of the group key to allow for more deduplication
        return $"{aabbTable.Min.X:0.00},{aabbTable.Min.Y:0.00},{aabbTable.Min.Z:0.00}"
            + $",{aabbTable.Max.X:0.00},{aabbTable.Max.Y:0.00},{aabbTable.Max.Z:0.00}";
    }
}
