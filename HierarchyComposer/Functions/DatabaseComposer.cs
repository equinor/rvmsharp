using Equinor.MeshOptimizationPipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mop.Hierarchy.Extensions;
using Mop.Hierarchy.Model;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace Mop.Hierarchy.Functions
{
    public class DatabaseComposer
    {
        private readonly ILogger Logger;

        public DatabaseComposer(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<DatabaseComposer>();
        }

        public void ComposeDatabase(string inputPath, string outputDatabase)
        {
            var optionsBuilder = new DbContextOptionsBuilder<HierarchyContext>();
            optionsBuilder.UseSqlite($"Data Source={outputDatabase};");
            CreateEmptyDatabase(optionsBuilder.Options);

            var jsons = Directory.GetFiles(inputPath, "*.json");
            var jsonNodes = MOPTimer.RunAndMeasure("Loading hierarchy", Logger,
                () => jsons.SelectMany(j => DeserializeJsonFromFileAsStream<HierarchyNode[]>(j)).Where(j => j.NodeId != 0).ToArray());
            var jsonNodesWithoutPDMS = jsonNodes.Where(n => n.PDMSData == null || n.PDMSData.Count == 0).ToArray();
            foreach (var jsonNode in jsonNodesWithoutPDMS)
            {
                // Adding information node to reduce query complexity on the hierarchy service, so that every node has at least one PDMS value
                jsonNode.PDMSData = new Dictionary<string, string> { { "Info:", $"No E3D data available for selected part." } };
            }
            var jsonPdmsKeyValuePairs = MOPTimer.RunAndMeasure("Collecting PDMS data", Logger,
                () => jsonNodes.Where(n => n.PDMSData != null).SelectMany(n => n.PDMSData).ToArray());
            var jsonAabbs = jsonNodes.Where(jn => jn.AABB != null).Select(jn => jn.AABB);

            Logger.LogInformation("Creating database model entries");
            long pdmsEntryIdCounter = 0;
            var pdmsEntries = jsonPdmsKeyValuePairs.GroupBy(kvp => kvp.GetKey()).ToDictionary(g => g.Key, g => new PDMSEntry { Id = ++pdmsEntryIdCounter, Key = g.First().Key, Value = g.First().Value });
            var aabbIdCounter = 0;
            var aabbs = jsonAabbs.GroupBy(b => b.GetKey()).ToDictionary(g => g.Key, g => g.First().ToAABB(++aabbIdCounter));
            
            var nodes = jsonNodes.Select(jn => new Node
                {
                    Id = jn.NodeId,
                    RefNoDb = jn.RefNoDb,
                    RefNoSequence = jn.RefNoSequence,
                    Name = jn.Name,
                    HasMesh = jn.MeshGhost != null,
                    NodePDMSEntry = jn.PDMSData == null ? null : jn.PDMSData.Select(kvp => new NodePDMSEntry { NodeId = jn.NodeId, PDMSEntryId = pdmsEntries[kvp.GetKey()].Id }).ToList(),
                    AABB = jn.AABB == null ? null : aabbs[jn.AABB.GetKey()]
                }).ToDictionary(n => n.Id, n => n);

            foreach (var jsonNode in jsonNodes)
            {
                nodes[jsonNode.NodeId].TopNode = nodes[jsonNode.TopNodeId];
                if (jsonNode.ParentId.HasValue == false)
                    continue;
                nodes[jsonNode.NodeId].Parent = nodes[jsonNode.ParentId.Value];
            }

            var nodePdmsEntries = nodes.Values.Where(n => n.NodePDMSEntry != null).SelectMany(n => n.NodePDMSEntry);

            var sqliteComposeTimer = MOPTimer.Create("Populating database and building index", Logger);

            using var connection = new SQLiteConnection($"Data Source={outputDatabase}");
            connection.Open();
            using var cmd = new SQLiteCommand(connection);

            MOPTimer.RunAndMeasure("Insert PDMSEntries", Logger, () =>
            {
                using var transaction = connection.BeginTransaction();
                foreach (var pdmsEntry in pdmsEntries.Values)
                    pdmsEntry.RawInsert(cmd);

                transaction.Commit();
            });

            MOPTimer.RunAndMeasure("Insert NodePDMSEntries", Logger, () =>
            {
                using var transaction = connection.BeginTransaction();
                foreach (var nodePdmsEntry in nodePdmsEntries)
                    nodePdmsEntry.RawInsert(cmd);

                transaction.Commit();
            });

            MOPTimer.RunAndMeasure("Insert AABBs", Logger, () =>
            {
                using var transaction = connection.BeginTransaction();
                foreach (var aabb in aabbs.Values)
                    aabb.RawInsert(cmd);

                transaction.Commit();
            });


            MOPTimer.RunAndMeasure("Insert Nodes", Logger, () =>
            {
                using var transaction = connection.BeginTransaction();
                foreach (var node in nodes.Values)
                    node.RawInsert(cmd);

                transaction.Commit();
            });

            MOPTimer.RunAndMeasure("Creating indexes", Logger, () =>
                {
                    using var transaction = connection.BeginTransaction();
                    cmd.CommandText = "CREATE INDEX PDMSEntries_Value_index ON PDMSEntries (Value)"; // key index will just slow things down
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "CREATE INDEX PDMSEntries_Value_nocase_index ON PDMSEntries (Value collate nocase)";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "CREATE INDEX PDMSEntries_Key_index ON PDMSEntries (Key)";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "CREATE INDEX Nodes_Name_index ON Nodes (Name)";
                    cmd.CommandText = "CREATE INDEX Nodes_RefNo_Index ON Nodes (RefNoDb, RefNoSequence)";
                    cmd.ExecuteNonQuery();

                    transaction.Commit();
                });

            sqliteComposeTimer.LogCompleteion();
        }

        private static void CreateEmptyDatabase(DbContextOptions options)
        {
            using var context = new HierarchyContext(options);
            if (!context.Database.EnsureCreated())
                throw new Exception($"Could not create database");
        }

        private T DeserializeJsonFromFileAsStream<T>(string path)
        {
            Logger.LogInformation("Reading " + path);
            var serializer = new JsonSerializer();
            using var streamReader = new StreamReader(File.OpenRead(path));
            using var jsonReader = new JsonTextReader(streamReader);
            return serializer.Deserialize<T>(jsonReader);
        }
    }
}
