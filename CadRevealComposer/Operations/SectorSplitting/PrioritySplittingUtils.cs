namespace CadRevealComposer.Operations.SectorSplitting;

using System;
using System.Collections.Generic;
using System.Linq;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;

public static class PrioritySplittingUtils
{
    private static readonly string[] PrioritizedDisciplines = ["PIPE"];

    public static void SetPriorityForHighlightSplittingWithMutation(IReadOnlyList<CadRevealNode> nodes)
    {
        var disciplineFilteredNodes = FilterByDisciplineAndAddDisciplineMetadata(nodes).ToArray();

        // TODO
        // Are we going to use the custom STID mapper, or should we wait for a more official solution?
        // Notes: Uncommenting code below requires that a relevant file exists on build server
        var tagMappingAndDisciplineFilteredNodes = disciplineFilteredNodes; // StidTagMapper.FilterNodesWithTag(disciplineFilteredNodes);

        var tagAndDisciplineFilteredNodes = FilterByIfTagExists(tagMappingAndDisciplineFilteredNodes).ToArray();
        SetPriorityOnNodesAndChildren(tagAndDisciplineFilteredNodes);
    }

    private static IEnumerable<CadRevealNode> FilterByDisciplineAndAddDisciplineMetadata(
        IReadOnlyList<CadRevealNode> nodes
    )
    {
        foreach (var node in nodes)
        {
            var discipline = node.Attributes.GetValueOrNull("Discipline");

            if (!PrioritizedDisciplines.Contains(discipline))
                continue;

            var children = CadRevealNode.GetAllNodesFlat(node);
            foreach (var child in children)
            {
                child.Geometries = child.Geometries.Select(g => g with { Discipline = discipline }).ToArray();
            }

            yield return node;
        }
    }

    private static IEnumerable<CadRevealNode> FilterByIfTagExists(CadRevealNode[] nodes)
    {
        foreach (var node in nodes)
        {
            var tag = node.Attributes.GetValueOrNull("Tag");

            if (tag != null)
            {
                yield return node;
            }
        }
    }

    private static void SetPriorityOnNodesAndChildren(CadRevealNode[] nodes)
    {
        foreach (var node in nodes)
        {
            var allChildren = CadRevealNode.GetAllNodesFlat(node);
            foreach (var child in allChildren)
            {
                child.Geometries = child.Geometries.Select(g => g with { Priority = 1 }).ToArray();
            }
        }
    }

    public static Node[] ConvertPrimitiveGroupsToNodes(IEnumerable<IGrouping<uint, APrimitive>> geometryGroups)
    {
        float sizeCutoff = 0.1f;

        return geometryGroups
            .Select(g =>
            {
                var allGeometries = g.Select(x => x).ToArray();
                var orderedBySizeDescending = allGeometries.OrderByDescending(x => x.AxisAlignedBoundingBox.Diagonal);
                APrimitive[] geometries;
                if (orderedBySizeDescending.First().AxisAlignedBoundingBox.Diagonal < sizeCutoff)
                {
                    geometries = allGeometries;
                }
                else
                {
                    geometries = allGeometries.Where(x => x.AxisAlignedBoundingBox.Diagonal > sizeCutoff).ToArray();
                }
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
