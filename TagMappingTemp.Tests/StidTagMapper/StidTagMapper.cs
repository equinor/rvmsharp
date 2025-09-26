namespace CadRevealComposer.Operations;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using CsvHelper;
using CsvHelper.Configuration;

using Newtonsoft.Json;


public static class StidTagMapper
{
    [Test]
    public static void CompareProsessedPdmsNodesWithStidTags()
    {
        string pdmsNodesPath = @"/Users/KAG/Documents/Repos/rvmsharp/Uncommited data/troll_b_echo_tags.json";
        string stidTagsPath = @"/Users/KAG/Documents/Repos/rvmsharp/Uncommited data/troll_b_stid_tags.csv";

        var pdmsNodes = ParseFromJson(pdmsNodesPath);
        var stidTags = ParseFromCsv(stidTagsPath);

        var nodes = FilterPdmsNodesByStidTags(pdmsNodes, stidTags);
        Console.WriteLine(nodes.Length);
    }

    private static PdmsNode[] ParseFromJson(string path)
    {
        var json = File.ReadAllText(path);

        return JsonConvert.DeserializeObject<PdmsNode[]>(json)!;
    }
    private static StidTag[] ParseFromCsv(string path)
    {
        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture){Delimiter = ";"}))
        {
            return csv.GetRecords<StidTag>().ToArray();
        }
    }

    private class ResultNode
    {
        public required PdmsNode PdmsNode { get; set; }
        public required StidTag StidTag { get; set; }
        public required string MatchType { get; set; }
    }


    private static ResultNode[] FilterPdmsNodesByStidTags(
        PdmsNode[] pdmsNodes, StidTag[] stidTags)
    {

        var resultNodes = new List<ResultNode>();

        var stidTagLookup = stidTags.ToDictionary(x => x.TAG_NO.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        var lineStidTags = stidTags.Where(x => x.TAG_CATEGORY == 6).ToArray(); // 6 is a line tag in STID (multiple stid tags per pdms tag). I.e they use wildcards.

        foreach (PdmsNode pdmsNode in pdmsNodes)
        {
            if (pdmsNode.PdmsTag != null)
            {
                if (stidTagLookup.TryGetValue(pdmsNode.PdmsTag, out var stidTag))
                {
                    resultNodes.Add(new ResultNode(){PdmsNode = pdmsNode, StidTag = stidTag, MatchType = "PdmsTag"});
                    continue;
                }
            }

            if (stidTagLookup.TryGetValue(pdmsNode.NodeName.Trim(['/']).Trim(), out var value))
            {
                resultNodes.Add(new ResultNode(){PdmsNode = pdmsNode, StidTag = value, MatchType = "PdmsName"});
                continue;
            }

        //     if (pdmsNode.Attributes.TryGetValue("Type", out var type) && type == "PIPE")
        //     {
        //         var baseLineTag = pdmsNode.Name.Split("_")[0].TrimStart('/').Trim();
        //         var matchingLineTags = lineStidTags
        //             .Where(x => x.TagNo.Contains(baseLineTag, StringComparison.OrdinalIgnoreCase))
        //             .ToArray();
        //
        //         if (pdmsTag != null)
        //         {
        //             var pdmsTagWithoutStars = pdmsTag!.Trim('*');
        //             var pdmsTagMatchingLineTags = lineStidTags
        //                 .Where(x => x.TagNo.Contains(pdmsTagWithoutStars, StringComparison.OrdinalIgnoreCase))
        //                 .ToArray();
        //             if (pdmsTagMatchingLineTags.Any())
        //             {
        //                 if (pdmsTagMatchingLineTags.Length == 1)
        //                 {
        //                     resultNodes.Add(pdmsNode);
        //                     continue;
        //                 }
        //                 else
        //                 {
        //                     resultNodes.Add(pdmsNode);
        //                     continue;
        //                 }
        //             }
        //         }
        //         if (matchingLineTags.Any())
        //         {
        //             resultNodes.Add(pdmsNode);
        //             continue;
        //         }
        //     }
        }

        return resultNodes.ToArray();
    }
}
