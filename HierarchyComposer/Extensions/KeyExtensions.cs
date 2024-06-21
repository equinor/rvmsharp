﻿namespace HierarchyComposer.Extensions;

using System.Collections.Generic;
using Model;

public static class KeyExtensions
{
    public static string GetGroupKey(this AABB aabb)
    {
        // Intentionally reduces precision of the group key to allow for more deduplication
        return $"{aabb.min.x:0.00},{aabb.min.y:0.00},{aabb.min.z:0.00}"
            + $",{aabb.max.x:0.00},{aabb.max.y:0.00},{aabb.max.z:0.00}";
    }

    public static string GetGroupKey(this KeyValuePair<string, string> pdmsEntry)
    {
        return $"{pdmsEntry.Key}:{pdmsEntry.Value}";
    }
}
