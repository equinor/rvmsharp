﻿namespace CadRevealComposer.Operations.SectorSplitting;

using System;
using System.Collections.Generic;
using System.Linq;
using Primitives;
using Utils;

public static class PrioritySplittingUtils
{
    private static readonly string[] PrioritizedDisciplines = ["PIPE"];

    public static void SetPriorityForPrioritySplittingWithMutation(IReadOnlyList<CadRevealNode> nodes)
    {
        var disciplineFilteredNodes = FilterByDisciplineAndAddDisciplineMetadata(nodes);
        var tagAndDisciplineFilteredNodes = FilterByIfTagExists(disciplineFilteredNodes);
        SetPriorityOnNodesAndChildren(tagAndDisciplineFilteredNodes);
    }

    private static IEnumerable<CadRevealNode> FilterByDisciplineAndAddDisciplineMetadata(
        IEnumerable<CadRevealNode> nodes
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

    private static IEnumerable<CadRevealNode> FilterByIfTagExists(IEnumerable<CadRevealNode> nodes) =>
        nodes.Where(node => node.Attributes.ContainsKey("Tag"));

    private static void SetPriorityOnNodesAndChildren(IEnumerable<CadRevealNode> nodes)
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
