namespace HierarchyComposer.Functions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Model;

public class DatabaseComposer
{
    private readonly ILogger _logger;

    public DatabaseComposer(ILogger<DatabaseComposer>? logger = null)
    {
        _logger = logger ?? NullLogger<DatabaseComposer>.Instance;
    }

    // ReSharper disable once CognitiveComplexity

    // Method to check and write current memory usage to the console
    static void CheckMemoryUsage(string currentLine)
    {
        // Get the current process
        Process currentProcess = Process.GetCurrentProcess();

        // Get the physical memory usage (in bytes)
        long totalBytesOfMemoryUsed = currentProcess.WorkingSet64;

        // Convert to megabytes for easier reading
        double megabytesUsed = totalBytesOfMemoryUsed / (1024.0 * 1024.0);

        // Write the memory usage to the console
        Console.WriteLine($"Memory usage (MB): {megabytesUsed:N2} at line {currentLine}");
    }

    public void ComposeDatabase(IReadOnlyList<HierarchyNode> inputNodes, string outputDatabaseFullPath)
    {
        CheckMemoryUsage("44");
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
            () => inputNodes.SelectMany(n => n.PDMSData)
        );
        var jsonAabbs = inputNodes.Where(jn => jn.AABB != null).Select(jn => jn.AABB!);

        _logger.LogInformation("Creating database model entries");
        long pdmsEntryIdCounter = 0;

        CheckMemoryUsage("78");

        var pdmsEntries = jsonPdmsKeyValuePairs
            .GroupBy(kvp => kvp.GetGroupKey())
            .ToDictionary(
                keySelector: g => g.Key,
                elementSelector: g => new PDMSEntry()
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

        CheckMemoryUsage("97");



// --- Optimized Iterative Construction ---

// 1. Estimate capacity if possible to reduce dictionary reallocations
        int initialCapacity = 0;
        var nodes = new Dictionary<long, Node>(initialCapacity);
// 2. Iterate through input nodes one by one
        foreach (var inputNode in inputNodes)
        {
            // 3. Create the main Node object
            var newNode = new Node
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
                // Initialize list here, potentially with capacity
                NodePDMSEntry = null, // Initialize as null or empty list
                // AABB lookup (same logic as before)
                AABB = inputNode.AABB == null ? null : aabbs[inputNode.AABB.GetGroupKey()],
                DiagnosticInfo = inputNode.OptionalDiagnosticInfo
            };

            // 4. Process NodePDMSEntry efficiently
            if (inputNode.PDMSData.Count > 0) // Check if there's data
            {
                // Create the list *once* per node, with capacity if available
                var pdmsEntryList = new List<NodePDMSEntry>(inputNode.PDMSData.Count);

                foreach (var kvp in inputNode.PDMSData)
                {
                    // Perform lookup and create entry directly
                    pdmsEntryList.Add(new NodePDMSEntry
                    {
                        NodeId = inputNode.NodeId, // Use the already accessed NodeId
                        PDMSEntryId = pdmsEntries[kvp.GetGroupKey()].Id
                    });
                }
                newNode.NodePDMSEntry = pdmsEntryList; // Assign the populated list
            }
            else
            {
                // Ensure the list is initialized if null wasn't intended, e.g.:
                newNode.NodePDMSEntry = new List<NodePDMSEntry>(); // Assign empty list if PDMSData is null/empty
            }


            // 5. Add the fully constructed node to the dictionary
            // Use Add for potentially better performance if keys are guaranteed unique,
            // or use the indexer nodes[newNode.Id] = newNode; if duplicates might need overwriting (though ToDictionary implies unique keys).
            nodes.Add(newNode.Id, newNode);
        }

// 'nodes' dictionary is now populated.

        CheckMemoryUsage("'nodes' dictionary is now populated. Line 160");





        //
        // var nodes = inputNodes
        //     .Select(inputNode => new Node
        //     {
        //         Id = inputNode.NodeId,
        //         EndId = inputNode.EndId,
        //         RefNoPrefix = inputNode.RefNoPrefix,
        //         RefNoDb = inputNode.RefNoDb,
        //         RefNoSequence = inputNode.RefNoSequence,
        //         Name = inputNode.Name,
        //         HasMesh = inputNode.HasMesh,
        //         ParentId = inputNode.ParentId,
        //         TopNodeId = inputNode.TopNodeId,
        //         NodePDMSEntry = inputNode.PDMSData.Select(kvp => new NodePDMSEntry
        //         {
        //             NodeId = inputNode.NodeId,
        //             PDMSEntryId = pdmsEntries[kvp.GetGroupKey()].Id
        //         }),
        //         AABB = inputNode.AABB == null ? null : aabbs[inputNode.AABB.GetGroupKey()],
        //         DiagnosticInfo = inputNode.OptionalDiagnosticInfo
        //     })
        //     .ToDictionary(n => n.Id, n => n);

        CheckMemoryUsage("166");

        var nodePdmsEntries = nodes.Values.Where(n => n.NodePDMSEntry != null).SelectMany(n => n.NodePDMSEntry!);

        var sqliteComposeTimer = MopTimer.Create("Populating database and building index", _logger);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        CheckMemoryUsage("175");

        // ReSharper disable AccessToDisposedClosure
        MopTimer.RunAndMeasure(
            "Insert PDMSEntries",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();

                using var cmd = connection.CreateCommand();
                PDMSEntry.RawInsertBatch(cmd, pdmsEntries.Values);

                transaction.Commit();
            }
        );

        CheckMemoryUsage("192");

        MopTimer.RunAndMeasure(
            "Insert NodePDMSEntries",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();

                using var cmd = connection.CreateCommand();
                NodePDMSEntry.RawInsertBatch(cmd, nodePdmsEntries);

                transaction.Commit();
            }
        );

        CheckMemoryUsage("208");

        MopTimer.RunAndMeasure(
            "Insert AABBs",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();
                using var cmd = connection.CreateCommand();

                // Manually creating a special R-Tree table to speed up queries on the AABB table, specifically
                // finding AABBs based on a location. The sqlite rtree module auto-creates spatial indexes.
                cmd.CommandText =
                    "CREATE VIRTUAL TABLE AABBs USING rtree(Id, min_x, max_x, min_y, max_y, min_z, max_z)";
                cmd.ExecuteNonQuery();

                AABB.RawInsertBatch(cmd, aabbs.Values);

                transaction.Commit();
            }
        );

        CheckMemoryUsage("230");

        MopTimer.RunAndMeasure(
            "Insert Nodes",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();
                using var cmd = connection.CreateCommand();
                Node.RawInsertBatch(cmd, nodes.Values);

                transaction.Commit();
            }
        );

        CheckMemoryUsage("245");

        MopTimer.RunAndMeasure(
            "Creating indexes",
            _logger,
            () =>
            {
                using var transaction = connection.BeginTransaction();
                using var cmd = connection.CreateCommand();
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

        CheckMemoryUsage("268");

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

        CheckMemoryUsage("286");

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

        CheckMemoryUsage("Last line");

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

    private static void CreateEmptyDatabase(DbContextOptions options)
    {
        using var context = new HierarchyContext(options);
        if (!context.Database.EnsureCreated())
            throw new Exception($"Could not create database");
    }
}
