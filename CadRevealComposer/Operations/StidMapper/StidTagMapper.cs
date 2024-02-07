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
        //.Where(x => x.TagCategoryDescription != "Administrative").
        // Where(x => x.DisciplineCode != null).Where(x => x.PoNo != null)

        var tagLookup = tagDataFromStid.ToDictionary(x => x.TagNo.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        //var lineTags = tagDataFromStid.Where(x => x.TagCategory == 6).ToArray();



        // MISS: 57GT0061
        var test2 = new List<CadRevealNode>();
        var hits = new List<TagDataFromStid>();

        var test = revealNodes.Where(x => x.Attributes.Values.Any(x => x.Contains("77DE029"))).ToArray();

        foreach (CadRevealNode revealNode in revealNodes)
        {
            // REMOVE STUDY AND TEMP

            //var suffix = revealNode.Name.Split('_').Last();
            //if (suffix.Equals("STUDY") || suffix.Equals("TEMP"))
            //    continue;

            if (revealNode.Attributes.TryGetValue("Tag", out var pdmsTag))
            {
                // Trim pdmsTag, remove -S ending

                string tryFixPdmsTag = pdmsTag.Trim();
                if (tryFixPdmsTag.EndsWith("-S"))
                {
                    tryFixPdmsTag = tryFixPdmsTag.Substring(0, tryFixPdmsTag.LastIndexOf("-S"));
                }

                if (tagLookup.TryGetValue(tryFixPdmsTag, out var stidTag)) // Trim pdmsTag
                {
                    revealNode.Attributes.Add("PdmsStidTag2", stidTag.TagNo);
                    hits.Add(stidTag);
                }
            }

            //if (tagLookup.TryGetValue(revealNode.Name.Trim(['/']).Trim(), out var value))
            //{
            //    revealNode.Attributes.Add("StidTagEqualsName", value.TagNo);
            //    revealNode.Attributes.Add("StidTag", value.TagNo);
            //    revealNode.Attributes.Add("StidTagDescription", value.Description);
            //}
            //else if (revealNode.Attributes.TryGetValue("Type", out var type) && type == "PIPE")
            //{
            //    var baseLineTag = revealNode.Name.Split("_")[0].TrimStart('/').Trim();
            //    var matchingLineTags =
            //        lineTags.Where(x => x.TagNo.Contains(baseLineTag, StringComparison.OrdinalIgnoreCase)).ToArray();

            //    if(pdmsTag != null)
            //    {
            //        var pdmsTagWithoutStars = pdmsTag!.Trim('*');
            //        var pdmsTagMatchingLineTags = lineTags.Where(x => x.TagNo.Contains(pdmsTagWithoutStars, StringComparison.OrdinalIgnoreCase)).ToArray();
            //        if (pdmsTagMatchingLineTags.Any())
            //        {
            //            if (pdmsTagMatchingLineTags.Length == 1)
            //            {
            //                var val = pdmsTagMatchingLineTags.Single();
            //                revealNode.Attributes.Add("PdmsStidTag", val.TagNo);
            //                revealNode.Attributes.Add("PdmsStidTagDescription", val.Description);
            //            }

            //            else
            //            {
            //                var val = pdmsTagMatchingLineTags.Select(x => x.TagNo).ToArray();
            //                revealNode.Attributes.Add("PdmsStidTag", string.Join(",", val));
            //                revealNode.Attributes.Add("PdmsStidTagDescription", "Multiple Line Tags");
            //            }
            //        }
            //    }
            //    if (matchingLineTags.Any())
            //    {
            //        if (matchingLineTags.Length == 1)
            //        {
            //            var val = matchingLineTags.Single();
            //            revealNode.Attributes.Add("StidTag", val.TagNo);
            //            revealNode.Attributes.Add("StidTagDescription", val.Description);
            //        }

            //        else
            //        {
            //            var val = matchingLineTags.Select(x => x.TagNo).ToArray();
            //            revealNode.Attributes.Add("StidTag", string.Join(",", val));
            //            revealNode.Attributes.Add("StidTagDescription", "Multiple Line Tags");
            //        }
            //    }
            //}
        }

        var hitsDistinct = hits.Distinct().ToArray();

        var multipleHits = hits.GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .SelectMany(x => x.Select(c => c.TagNo))
            .ToArray();

        var misses = tagDataFromStid.Except(hits).ToArray();
        Console.WriteLine($"Found {misses.Length} misses");
    }
}
