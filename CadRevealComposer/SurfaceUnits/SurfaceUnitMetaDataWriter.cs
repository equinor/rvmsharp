namespace CadRevealComposer.SurfaceUnits;

using Utils;
using CsvHelper;
using CsvHelper.Configuration;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

public static class SurfaceUnitMetaDataWriter
{
    public static void AddMetaData(IEnumerable<CadRevealNode> allNodes, string filePath)
    {
        var rvmNodes = allNodes
            .Where(n => n.Attributes.ContainsKey("RefNo")).ToDictionary((k) => k.Attributes["RefNo"], (v) => v);


        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";", };
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<SurfaceUnit>().ToList();

        Console.WriteLine("Found " + rvmNodes.Count + " nodes with RefNo attribute.");
        Console.WriteLine("Found " + records.Count + " records in file.");
        int count = 0;

        Console.WriteLine("Adding Surface Unit Metadata to nodes from file " + filePath);
        foreach (var record in records)
        {
            try
            {
                var node = rvmNodes.TryGetValue(record.E3DReference, out var nodeValue) ? nodeValue : null;
                if (node == null)
                {
                    continue;
                }

                node.Attributes.Add("SurfaceUnit", record.SurfaceUnit1);
                node.Attributes.Add($"SU-{record.SurfaceUnit1}", "true");

                if (!String.IsNullOrEmpty(record.SurfaceUnit2))
                {
                    node.Attributes.Add($"SU-{record.SurfaceUnit2}", "true");
                }

                if (!String.IsNullOrEmpty(record.SurfaceUnit3))
                {
                    node.Attributes.Add($"SU-{record.SurfaceUnit3}", "true");
                }

                if (!String.IsNullOrEmpty(record.SurfaceUnit4))
                {
                    node.Attributes.Add($"SU-{record.SurfaceUnit4}", "true");
                }

                if (!String.IsNullOrEmpty(record.SurfaceUnit5))
                {
                    node.Attributes.Add($"SU-{record.SurfaceUnit5}", "true");
                }

                if (!String.IsNullOrEmpty(record.SurfaceUnit6))
                {
                    node.Attributes.Add($"SU-{record.SurfaceUnit6}", "true");
                }

                if (!String.IsNullOrEmpty(record.SurfaceUnit7))
                {
                    node.Attributes.Add($"SU-{record.SurfaceUnit7}", "true");
                }

                if (!String.IsNullOrEmpty(record.SurfaceUnit8))
                {
                    node.Attributes.Add($"SU-{record.SurfaceUnit8}", "true");
                }

                if (!String.IsNullOrEmpty(record.SurfaceUnit9))
                {
                    node.Attributes.Add($"SU-{record.SurfaceUnit9}", "true");
                }

                if (!String.IsNullOrEmpty(record.SurfaceUnit10))
                {
                    node.Attributes.Add($"SU-{record.SurfaceUnit10}", "true");
                }

                count++;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error adding metadata to node " + record.E3DReference + ": " + e.Message);
            }
        }

        Console.WriteLine("Added Surface Unit Metadata to {0} nodes.", count);
    }
}