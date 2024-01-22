namespace HierarchyComposer.Extensions;

using Model;
using System.Collections.Generic;

public static class KeyExtensions
{
    public static string GetGroupKey(this AABB aabb)
    {
        // Intentionally reduces precision of the group key to allow for more deduplication
        return $"{aabb.Min.X:0.00},{aabb.Min.Y:0.00},{aabb.Min.Z:0.00}"
            + $",{aabb.Max.X:0.00},{aabb.Max.Y:0.00},{aabb.Max.Z:0.00}";
    }

    public static string GetGroupKey(this KeyValuePair<string, string> pdmsEntry)
    {
        return $"{pdmsEntry.Key}:{pdmsEntry.Value}";
    }
}
