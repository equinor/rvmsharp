namespace CadRevealComposer.Operations;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class StidTagMapper
{
    public static TagDataFromStid[] ParseFromJson(string path)
    {
        var json = File.ReadAllText(path);

        return JsonConvert.DeserializeObject<TagDataFromStid[]>(json)!;
    }

    public static void MapToStidTags(IReadOnlyList<CadRevealNode> revealNodes, TagDataFromStid[] tagDataFromStid)
    {

        var tagLookup = tagDataFromStid.ToDictionary(x => x.TagNo.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        var lineTags = tagDataFromStid.Where(x => x.TagCategory == 6).ToArray();

        foreach (CadRevealNode revealNode in revealNodes)
        {
            if (revealNode.Attributes.TryGetValue("Tag", out var pdmsTag))
            {
                if (tagLookup.TryGetValue(pdmsTag, out var stidTag))
                {
                    revealNode.Attributes.Add("PdmsStidTag2", stidTag.TagNo);
                }
            }

            if (tagLookup.TryGetValue(revealNode.Name.Trim(['/']).Trim(), out var value))
            {
                revealNode.Attributes.Add("StidTagEqualsName", value.TagNo);
                revealNode.Attributes.Add("StidTag", value.TagNo);
                revealNode.Attributes.Add("StidTagDescription", value.Description);
            }
            else if (revealNode.Attributes.TryGetValue("Type", out var type) && type == "PIPE")
            {
                var baseLineTag = revealNode.Name.Split("_")[0].TrimStart('/').Trim();
                var matchingLineTags =
                    lineTags.Where(x => x.TagNo.Contains(baseLineTag, StringComparison.OrdinalIgnoreCase)).ToArray();

                if(pdmsTag != null)
                {
                    var pdmsTagWithoutStars = pdmsTag!.Trim('*');
                    var pdmsTagMatchingLineTags = lineTags.Where(x => x.TagNo.Contains(pdmsTagWithoutStars, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (pdmsTagMatchingLineTags.Any())
                    {
                        if (pdmsTagMatchingLineTags.Length == 1)
                        {
                            var val = pdmsTagMatchingLineTags.Single();
                            revealNode.Attributes.Add("PdmsStidTag", val.TagNo);
                            revealNode.Attributes.Add("PdmsStidTagDescription", val.Description);
                        }

                        else
                        {
                            var val = pdmsTagMatchingLineTags.Select(x => x.TagNo).ToArray();
                            revealNode.Attributes.Add("PdmsStidTag", string.Join(",", val));
                            revealNode.Attributes.Add("PdmsStidTagDescription", "Multiple Line Tags");
                        }
                    }
                }
                if (matchingLineTags.Any())
                {
                    if (matchingLineTags.Length == 1)
                    {
                        var val = matchingLineTags.Single();
                        revealNode.Attributes.Add("StidTag", val.TagNo);
                        revealNode.Attributes.Add("StidTagDescription", val.Description);
                    }

                    else
                    {
                        var val = matchingLineTags.Select(x => x.TagNo).ToArray();
                        revealNode.Attributes.Add("StidTag", string.Join(",", val));
                        revealNode.Attributes.Add("StidTagDescription", "Multiple Line Tags");
                    }
                }
            }
        }
    }
}
