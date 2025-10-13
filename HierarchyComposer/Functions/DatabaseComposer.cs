namespace HierarchyComposer.Functions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Model;

public class PdmsKeyValuePairCaseInsensitiveValue(string key, string value)
    : IEquatable<PdmsKeyValuePairCaseInsensitiveValue>
{
    // ReSharper disable MemberCanBePrivate.Global // Used in Dictionary key comparison
    public readonly string Key = key;
    public readonly string Value = value;

    // ReSharper restore MemberCanBePrivate.Global
    public bool Equals(PdmsKeyValuePairCaseInsensitiveValue? other)
    {
        if (other == null)
            return false;
        return Key == other.Key && string.Equals(Value, other.Value, StringComparison.InvariantCultureIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value.ToLowerInvariant());
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as PdmsKeyValuePairCaseInsensitiveValue);
    }
}

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
            ForeignKeys = false, // We ignore foreign keys so we can populate the database as fast as possible by filling one table at a time. (FKs has no impact on read-performance)
        };
        var connectionString = connectionStringBuilder.ToString();

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
        int pdmsEntryIdCounter = 0;

        var pdmsEntries = jsonPdmsKeyValuePairs
            .Select(x => new PdmsKeyValuePairCaseInsensitiveValue(x.Key, x.Value))
            .Distinct()
            .OrderBy(x => x.Key)
            .ThenBy(x => x.Value)
            .ToDictionary(
                keySelector: g => new PdmsKeyValuePairCaseInsensitiveValue(g.Key, g.Value),
                elementSelector: g => new PdmsEntry
                {
                    Id = ++pdmsEntryIdCounter,
                    Key = g.Key,
                    Value = g.Value,
                }
            );

        var aabbIdCounter = 0;

        var aabbs = jsonAabbs
            .GroupBy(b => b.GetGroupKey())
            .ToDictionary(keySelector: g => g.Key, elementSelector: g => g.First() with { Id = ++aabbIdCounter });

        var nodes = inputNodes
            .Select(inputNode => new Node
            {
                Id = inputNode.NodeId,
                EndId = inputNode.EndId,
                RefNoPrefix = inputNode.RefNoPrefix,
                RefNoDb = inputNode.RefNoDb,
                RefNoSequence = inputNode.RefNoSequence,
                Name = inputNode.Name,
                HasMesh = inputNode.HasMesh,
                ParentId = inputNode.ParentId,
                TopNodeId = inputNode.TopNodeId,
                AABBId = inputNode.AABB == null ? null : aabbs[inputNode.AABB.GetGroupKey()].Id,
                DiagnosticInfo = inputNode.OptionalDiagnosticInfo,
            })
            .ToDictionary(n => n.Id, n => n);

        var nodePdmsEntries = inputNodes
            .SelectMany(node =>
                node.PDMSData.Select(
                    (
                        pdmsKvp => new NodePdmsEntryMapItem()
                        {
                            NodeId = node.NodeId,
                            PdmsEntryId = pdmsEntries[
                                new PdmsKeyValuePairCaseInsensitiveValue(pdmsKvp.Key, pdmsKvp.Value)
                            ].Id,
                        }
                    )
                )
            )
            .ToArray();

        var sqliteComposeTimer = MopTimer.Create("Populating database and building index", _logger);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        _logger.LogInformation("Opened database. Sqlite Version: {Version}", connection.ServerVersion);

        // Optimize SQLite for write speed before inserting anything
        using (var pragmaCmd = connection.CreateCommand())
        {
            pragmaCmd.CommandText = "PRAGMA synchronous = OFF;";
            pragmaCmd.ExecuteNonQuery();
            pragmaCmd.CommandText = "PRAGMA journal_mode = MEMORY;";
            pragmaCmd.ExecuteNonQuery();
            pragmaCmd.CommandText = "PRAGMA locking_mode = EXCLUSIVE;";
            pragmaCmd.ExecuteNonQuery();
            pragmaCmd.CommandText = "PRAGMA temp_store = MEMORY;";
            pragmaCmd.ExecuteNonQuery();
            pragmaCmd.CommandText = "PRAGMA cache_size = -100000;"; // 100 MB cache
            pragmaCmd.ExecuteNonQuery();
        }

        MopTimer.RunAndMeasure(
            "Insert Nodes",
            _logger,
            () =>
            {
                using var tableCmd = connection.CreateCommand();
                NodeTable.CreateTable(tableCmd);
                using var transaction = connection.BeginTransaction();
                using var batchCmd = connection.CreateCommand();
                NodeTable.RawInsertBatch(batchCmd, nodes.Values);
                transaction.Commit();

                // We want to free up memory as soon as possible. Not profiled.
                nodes.Clear();
                GC.Collect();
            }
        );

        MopTimer.RunAndMeasure(
            "Insert PDMSEntries",
            _logger,
            () =>
            {
                using var tableCmd = connection.CreateCommand();
                PDMSEntryTable.CreateTable(tableCmd);

                using var transaction = connection.BeginTransaction();

                using var cmd = connection.CreateCommand();
                PDMSEntryTable.RawInsertBatch(cmd, pdmsEntries.Values.ToArray());

                transaction.Commit();

                // We want to free up memory as soon as possible. Not profiled.
                pdmsEntries.Clear();
            }
        );
        MopTimer.RunAndMeasure(
            "Insert NodePDMSEntries",
            _logger,
            () =>
            {
                using var tableCmd = connection.CreateCommand();
                NodePdmsEntryTable.CreateTable(tableCmd);

                NodePdmsEntryTable.RawInsertBatch(connection, nodePdmsEntries);
            }
        );
        nodePdmsEntries = [];
        GC.Collect();

        MopTimer.RunAndMeasure(
            "Insert AABBs",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();
                using var cmd = connection.CreateCommand();

                // Manually creating a special R-Tree table to speed up queries on the AABB table, specifically
                // finding AABBs based on a location. The sqlite rtree module auto-creates spatial indexes.
                AabbTable.CreateTable(cmd);

                AabbTable.RawInsertBatch(cmd, aabbs.Values);

                transaction.Commit();
            }
        );

        MopTimer.RunAndMeasure(
            "Creating additional indexes",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();
                using var cmd = connection.CreateCommand();
                PDMSEntryTable.CreateIndexes(cmd);
                NodeTable.CreateIndexes(cmd);
                NodePdmsEntryTable.CreateIndexes(cmd);
                transaction.Commit();
            }
        );

        MopTimer.RunAndMeasure(
            "Optimizing Database",
            _logger,
            () =>
            {
                // Run Sqlite Optimizing methods once. This may be superstition. The operations are usually quick (<1 second).
                using var cmd = connection.CreateCommand();
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
                using var vacuumCmds = connection.CreateCommand();

                vacuumCmds.CommandText = "PRAGMA page_count";
                var pageCountBeforeVacuum = (Int64)vacuumCmds.ExecuteScalar()!;
                var timer = Stopwatch.StartNew();
                // Vacuum the database. This is quite slow!
                vacuumCmds.CommandText = "VACUUM";
                vacuumCmds.ExecuteNonQuery();
                vacuumCmds.CommandText = "PRAGMA page_count";
                var pageCountAfterVacuum = (Int64)vacuumCmds.ExecuteScalar()!;

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

    public static void AddTreeIndexToSectorToDatabase(
        IReadOnlyList<(uint TreeIndex, uint SectorId)> treeIndexToSectorId,
        DirectoryInfo outputDirectory
    )
    {
        var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
        using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText =
                "CREATE TABLE PrioritizedSectors (TreeIndex INTEGER NOT NULL, PrioritizedSectorId INTEGER NOT NULL, PRIMARY KEY (TreeIndex, PrioritizedSectorId)) WITHOUT ROWID; ";
            createTableCommand.ExecuteNonQuery();

            var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO PrioritizedSectors (TreeIndex, PrioritizedSectorId) VALUES ($TreeIndex, $PrioritizedSectorId)";

            var treeIndexParameter = command.CreateParameter();
            treeIndexParameter.ParameterName = "$TreeIndex";
            var prioritizedSectorIdParameter = command.CreateParameter();
            prioritizedSectorIdParameter.ParameterName = $"PrioritizedSectorId";

            command.Parameters.AddRange([treeIndexParameter, prioritizedSectorIdParameter]);

            var transaction = connection.BeginTransaction();
            command.Transaction = transaction;

            foreach (var pair in treeIndexToSectorId.Distinct())
            {
                treeIndexParameter.Value = pair.TreeIndex;
                prioritizedSectorIdParameter.Value = pair.SectorId;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }
}
