﻿namespace HierarchyComposer.Functions;

using Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class DatabaseComposer
{
    private readonly ILogger _logger;

    public DatabaseComposer(ILogger<DatabaseComposer>? logger = null)
    {
        _logger = logger ?? NullLogger<DatabaseComposer>.Instance;
    }

    // ReSharper disable once CognitiveComplexity
    public void ComposeDatabase(IReadOnlyList<HierarchyNode> inputNodes, string outputDatabaseFullPath)
    {
        if (File.Exists(outputDatabaseFullPath))
            File.Delete(outputDatabaseFullPath);

        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = outputDatabaseFullPath,
            Pooling = false, // We do not need pooling yet, and the tests fail as the database is not fully closed until the app exits when pooling is enabled.
            Mode = SqliteOpenMode.ReadWriteCreate,
        };
        var connectionString = connectionStringBuilder.ToString();

        var optionsBuilder = new DbContextOptionsBuilder<HierarchyContext>();
        optionsBuilder.UseSqlite(connectionString);
        CreateEmptyDatabase(optionsBuilder.Options);

        var jsonNodesWithoutPdms = inputNodes.Where(n => !n.PDMSData.Any()).ToArray();
        foreach (var jsonNode in jsonNodesWithoutPdms)
        {
            // Adding information node to reduce query complexity on the hierarchy service, so that every node has at least one PDMS value
            jsonNode.PDMSData["Info:"] = "No E3D data available for selected part.";
        }

        var jsonPdmsKeyValuePairs = MopTimer.RunAndMeasure(
            "Collecting PDMS data",
            _logger,
            () => inputNodes.SelectMany(n => n.PDMSData).ToArray()
        );
        var jsonAabbs = inputNodes.Where(jn => jn.AABB != null).Select(jn => jn.AABB!);

        _logger.LogInformation("Creating database model entries");
        long pdmsEntryIdCounter = 0;

        var pdmsEntries = jsonPdmsKeyValuePairs
            .GroupBy(kvp => kvp.GetGroupKey())
            .ToDictionary(
                keySelector: g => g.Key,
                elementSelector: g =>
                    new PDMSEntry()
                    {
                        Id = ++pdmsEntryIdCounter,
                        Key = g.First().Key,
                        Value = g.First().Value
                    }
            );

        var aabbIdCounter = 0;
        var aabbs = jsonAabbs
            .GroupBy(b => b.GetGroupKey())
            .ToDictionary(keySelector: g => g.Key, elementSelector: g => g.First().CopyWithNewId(++aabbIdCounter));

        var nodes = inputNodes
            .Select(
                inputNode =>
                    new Node
                    {
                        Id = inputNode.NodeId,
                        EndId = inputNode.EndId,
                        RefNoPrefix = inputNode.RefNoPrefix,
                        RefNoDb = inputNode.RefNoDb,
                        RefNoSequence = inputNode.RefNoSequence,
                        Name = inputNode.Name,
                        HasMesh = inputNode.HasMesh,
                        NodePDMSEntry = inputNode.PDMSData
                            .Select(
                                kvp =>
                                    new NodePDMSEntry
                                    {
                                        NodeId = inputNode.NodeId,
                                        PDMSEntryId = pdmsEntries[kvp.GetGroupKey()].Id
                                    }
                            )
                            .ToList(),
                        AABB = inputNode.AABB == null ? null : aabbs[inputNode.AABB.GetGroupKey()],
                        DiagnosticInfo = inputNode.OptionalDiagnosticInfo
                    }
            )
            .ToDictionary(n => n.Id, n => n);

        foreach (var jsonNode in inputNodes)
        {
            nodes[jsonNode.NodeId].TopNode = nodes[jsonNode.TopNodeId];
            if (!jsonNode.ParentId.HasValue)
                continue;
            nodes[jsonNode.NodeId].Parent = nodes[jsonNode.ParentId.Value];
        }

        var nodePdmsEntries = nodes.Values.Where(n => n.NodePDMSEntry != null).SelectMany(n => n.NodePDMSEntry!);

        var sqliteComposeTimer = MopTimer.Create("Populating database and building index", _logger);

        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        // ReSharper disable AccessToDisposedClosure
        MopTimer.RunAndMeasure(
            "Insert PDMSEntries",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();

                using var cmd = new SQLiteCommand(connection);
                PDMSEntry.RawInsertBatch(cmd, pdmsEntries.Values);

                transaction.Commit();
            }
        );

        MopTimer.RunAndMeasure(
            "Insert NodePDMSEntries",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();

                using var cmd = new SQLiteCommand(connection);
                NodePDMSEntry.RawInsertBatch(cmd, nodePdmsEntries);

                transaction.Commit();
            }
        );

        MopTimer.RunAndMeasure(
            "Insert AABBs",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();
                using var cmd = new SQLiteCommand(connection);

                // Manually creating a special R-Tree table to speed up queries on the AABB table, specifically
                // finding AABBs based on a location. The sqlite rtree module auto-creates spatial indexes.
                cmd.CommandText =
                    "CREATE VIRTUAL TABLE AABBs USING rtree(Id, min_x, max_x, min_y, max_y, min_z, max_z)";
                cmd.ExecuteNonQuery();

                AABB.RawInsertBatch(cmd, aabbs.Values);

                transaction.Commit();
            }
        );

        MopTimer.RunAndMeasure(
            "Insert Nodes",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();
                using var cmd = new SQLiteCommand(connection);
                Node.RawInsertBatch(cmd, nodes.Values);

                transaction.Commit();
            }
        );

        MopTimer.RunAndMeasure(
            "Creating indexes",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();
                using var cmd = new SQLiteCommand(connection);
                cmd.CommandText = "CREATE INDEX PDMSEntries_Value_index ON PDMSEntries (Value)"; // key index will just slow things down
                cmd.ExecuteNonQuery();
                cmd.CommandText = "CREATE INDEX PDMSEntries_Value_nocase_index ON PDMSEntries (Value collate nocase)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "CREATE INDEX PDMSEntries_Key_index ON PDMSEntries (Key)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "CREATE INDEX Nodes_Name_index ON Nodes (Name)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "CREATE INDEX Nodes_RefNo_Index ON Nodes (RefNoPrefix, RefNoDb, RefNoSequence)";
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }
        );

        MopTimer.RunAndMeasure(
            "Optimizing Database",
            _logger,
            () =>
            {
                // Run Sqlite Optimizing methods once. This may be superstition. The operations are usually quick (<1 second).
                using var cmd = new SQLiteCommand(connection);
                // Analyze the database. Actual performance gains of this on a "fresh database" have not been checked.
                cmd.CommandText = "pragma analyze";
                cmd.ExecuteNonQuery();
                // Optimize the database. Actual performance gains of this have not been checked.
                cmd.CommandText = "pragma optimize";
                cmd.ExecuteNonQuery();
            }
        );

        MopTimer.RunAndMeasure(
            "VACUUM Database",
            _logger,
            () =>
            {
#if DEBUG
                // Ignore in debug mode to run faster
                return;
#else
                // Vacuum completely recreates the database but removes all "Extra Data" from it.
                // Its a quite slow operation but might fix the "First query is super slow issue" on the hierarchy service.
                using var vacuumCmds = new SQLiteCommand(connection);

                vacuumCmds.CommandText = "PRAGMA page_count";
                var pageCountBeforeVacuum = (Int64)vacuumCmds.ExecuteScalar();
                var timer = Stopwatch.StartNew();
                // Vacuum the database. This is quite slow!
                vacuumCmds.CommandText = "VACUUM";
                vacuumCmds.ExecuteNonQuery();
                vacuumCmds.CommandText = "PRAGMA page_count";
                var pageCountAfterVacuum = (Int64)vacuumCmds.ExecuteScalar();

                // Disable auto_vacuum explicitly as we expect no more data to be written to the database after this.
                vacuumCmds.CommandText = "PRAGMA auto_vacuum = NONE";
                vacuumCmds.ExecuteNonQuery();

                // Analyze only a subset of the data when doing optimize queries.
                // See more at:  https://sqlite.org/pragma.html#pragma_analysis_limit
                // Recommended values are between 100-1000.
                vacuumCmds.CommandText = "PRAGMA analysis_limit = 1000";
                vacuumCmds.ExecuteNonQuery();

                // FUTURE: Consider if we should disable VACUUM in dev builds if its too slow, its not really needed there.
                Console.WriteLine(
                    $"VACUUM finished in {timer.Elapsed}. Reduced size from {pageCountBeforeVacuum} to {pageCountAfterVacuum}"
                );
#endif
            }
        );

        // ReSharper restore AccessToDisposedClosure
        sqliteComposeTimer.LogCompletion();
    }

    private static void CreateEmptyDatabase(DbContextOptions options)
    {
        using var context = new HierarchyContext(options);
        if (!context.Database.EnsureCreated())
            throw new Exception($"Could not create database");
    }
}
