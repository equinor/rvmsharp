﻿namespace HierarchyComposer.Extensions
{
    using Model;
    using System.Collections.Generic;

    public static class KeyExtensions
    {
        public static string GetGroupKey(this AABB aabb)
        {
            return $"{aabb.min.x:0.00},{aabb.min.y:0.00},{aabb.min.z:0.00}" +
                $",{aabb.max.x:0.00},{aabb.max.y:0.00},{aabb.max.z:0.00}";
        }

        public static string GetGroupKey(this KeyValuePair<string, string> pdmsEntry)
        {
            return $"{pdmsEntry.Key}:{pdmsEntry.Value}";
        }
    }
}