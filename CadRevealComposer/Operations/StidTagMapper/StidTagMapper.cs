namespace CadRevealComposer.Operations;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public static class StidTagMapper
{
    public static CadRevealNode[] FilterNodesWithTag(CadRevealNode[] nodes)
    {
        // string filename = "troa_tags.json";
        string path = @"\\ws1611\AppResources\TrollA\Tags_temp2024-08-08\troa_tags.json";

        var tagDataFromStid = ParseFromJson(path);
        return FilterNodesByStidTags(nodes, tagDataFromStid);
    }

    private static TagDataFromStid[] ParseFromJson(string path)
    {
        var json = File.ReadAllText(path);

        return JsonConvert.DeserializeObject<TagDataFromStid[]>(json)!;
    }

    private static CadRevealNode[] FilterNodesByStidTags(
        IReadOnlyList<CadRevealNode> revealNodes,
        TagDataFromStid[] tagDataFromStid
    )
    {
        var acceptedNodes = new List<CadRevealNode>();

        var tagLookup = tagDataFromStid.ToDictionary(x => x.TagNo.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        var lineTags = tagDataFromStid.Where(x => x.TagCategory == 6).ToArray();

        foreach (CadRevealNode revealNode in revealNodes)
        {
            if (revealNode.Attributes.TryGetValue("Tag", out var pdmsTag))
            {
                if (tagLookup.TryGetValue(pdmsTag, out var stidTag))
                {
                    acceptedNodes.Add(revealNode);
                    continue;
                }
            }

            if (tagLookup.TryGetValue(revealNode.Name.Trim(['/']).Trim(), out var value))
            {
                acceptedNodes.Add(revealNode);
                continue;
            }

            if (revealNode.Attributes.TryGetValue("Type", out var type) && type == "PIPE")
            {
                var baseLineTag = revealNode.Name.Split("_")[0].TrimStart('/').Trim();
                var matchingLineTags = lineTags
                    .Where(x => x.TagNo.Contains(baseLineTag, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (pdmsTag != null)
                {
                    var pdmsTagWithoutStars = pdmsTag!.Trim('*');
                    var pdmsTagMatchingLineTags = lineTags
                        .Where(x => x.TagNo.Contains(pdmsTagWithoutStars, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    if (pdmsTagMatchingLineTags.Any())
                    {
                        if (pdmsTagMatchingLineTags.Length == 1)
                        {
                            acceptedNodes.Add(revealNode);
                            continue;
                        }
                        else
                        {
                            acceptedNodes.Add(revealNode);
                            continue;
                        }
                    }
                }
                if (matchingLineTags.Any())
                {
                    acceptedNodes.Add(revealNode);
                    continue;
                }
            }
        }

        return acceptedNodes.ToArray();
    }
}
