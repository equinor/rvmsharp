namespace CadRevealComposer.Operations.SectorSplitting;

using System;
using System.Collections.Generic;
using System.Linq;
using Primitives;
using Utils;

public static class PrioritySplittingUtils
{
    /// <summary>
    /// These disciplines have limited amount of data and high highlighting value.
    /// </summary>
    private static readonly string[] PrioritizedDisciplines = ["PIPE", "ELEC", "SAFE", "INST", "TELE"];

    public static void SetPriorityForPrioritySplittingWithMutation(IReadOnlyList<CadRevealNode> nodes)
    {
        foreach (var node in nodes)
        {
            var discipline = node.Attributes.GetValueOrNull("Discipline");
            var hasTag = TagExists(node);

            if (!HasPrioritizedDiscipline(node, discipline))
                continue;

            foreach (var child in CadRevealNode.GetAllNodesFlat(node))
            {
                child.Geometries = child
                    .Geometries.Select(g => g with { Discipline = discipline, Priority = hasTag ? 1 : g.Priority })
                    .ToArray();
            }
        }
    }

    private static bool TagExists(CadRevealNode node) => node.Attributes.ContainsKey("Tag");

    private static bool HasPrioritizedDiscipline(CadRevealNode node, string? discipline) =>
        PrioritizedDisciplines.Contains(discipline);

    public static Node[] ConvertPrimitiveGroupsToNodes(IEnumerable<IGrouping<uint, APrimitive>> geometryGroups)
    {
        const float sizeCutoff = 0.10f; // Arbitrary value

        return geometryGroups
            .Select(g =>
            {
                var allGeometries = g.Select(x => x).ToArray();

                // The idea here is: If the largest geometry is smaller than the size cutoff, we include all geometries because we
                // believe the sum of the parts may represent a larger "whole"
                // else we only include the larger geometries
                var geometries =
                    allGeometries.Max(x => x.AxisAlignedBoundingBox.Diagonal) < sizeCutoff
                        ? allGeometries
                        : allGeometries.Where(x => x.AxisAlignedBoundingBox.Diagonal > sizeCutoff).ToArray();

                var boundingBox = geometries.CalculateBoundingBox();
                if (boundingBox == null)
                {
                    throw new Exception("Unexpected error, the bounding box should not have been null.");
                }

                return new Node(
                    g.Key,
                    geometries,
                    geometries.Sum(DrawCallEstimator.EstimateByteSize),
                    EstimatedTriangleCount: DrawCallEstimator.Estimate(geometries).EstimatedTriangleCount,
                    boundingBox
                );
            })
            .ToArray();
    }
}
