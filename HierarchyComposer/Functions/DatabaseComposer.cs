// Keep existing using statements
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HierarchyComposer.Functions;

using Extensions;
using Model;

public class DatabaseComposer
{
    private readonly ILogger _logger;

    // Define a batch size for database insertions. Tune as needed.
    private const int DatabaseBatchSize = 5000; // Example value

    public DatabaseComposer(ILogger<DatabaseComposer>? logger = null)
    {
        _logger = logger ?? NullLogger<DatabaseComposer>.Instance;
    }

    // ReSharper disable once CognitiveComplexity

    // Method to check and write current memory usage to the console (Kept from original)
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
        CheckMemoryUsage("Start of ComposeDatabase");
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

        // Create Schema using EF Core (as before)
        var optionsBuilder = new DbContextOptionsBuilder<HierarchyContext>();
        optionsBuilder.UseSqlite(connectionString);
        CreateEmptyDatabase(optionsBuilder.Options);

        // --- Preprocessing (Largely unchanged, necessary for unique IDs before Node creation) ---
        _logger.LogInformation("Preprocessing input nodes...");

        var jsonNodesWithoutPdms = inputNodes.Where(n => !n.PDMSData.Any()).ToArray(); // ToArray is needed to materialize for modification
        foreach (var jsonNode in jsonNodesWithoutPdms)
        {
            // Adding information node to reduce query complexity on the hierarchy service, so that every node has at least one PDMS value
            jsonNode.PDMSData["Info:"] = "No E3D data available for selected part.";
        }

        CheckMemoryUsage("After adding Info PDMS data");

        // Collect all key-value pairs - ** Still a potential memory peak here **
        var jsonPdmsKeyValuePairs = MopTimer.RunAndMeasure(
            "Collecting PDMS data",
            _logger,
            () => inputNodes.SelectMany(n => n.PDMSData).ToList() // ToList to avoid multiple enumerations
        );

        CheckMemoryUsage("After collecting PDMS data");

        // Collect all AABBs - ** Still a potential memory peak here **
        var jsonAabbs = MopTimer.RunAndMeasure(
            "Collecting AABB data",
            _logger,
            () => inputNodes.Where(jn => jn.AABB != null).Select(jn => jn.AABB!).ToList() // ToList to avoid multiple enumerations
        );

        CheckMemoryUsage("After collecting AABB data");

        // Write// Write the length of PDMS, AABB, and inputNodes data to the console
        Console.WriteLine($"Input Nodes Count: {inputNodes.Count}, PDMS Data Count: {jsonPdmsKeyValuePairs.Count}, AABB Data Count: {jsonAabbs.Count}");

        _logger.LogInformation("Deduplicating PDMS entries and AABBs...");
        int pdmsEntryIdCounter = 0;
        int aabbIdCounter = 0;

        // Deduplicate PDMSEntries - ** Memory peak for the dictionary itself **
        var pdmsEntries = MopTimer.RunAndMeasure(
            "Grouping PDMSEntries",
            _logger,
             () => jsonPdmsKeyValuePairs
                .GroupBy(kvp => kvp.GetGroupKey())
                .ToDictionary(
                    keySelector: g => g.Key,
                    elementSelector: g => new PDMSEntry()
                    {
                        Id = ++pdmsEntryIdCounter,
                        Key = g.First().Key,
                        Value = g.First().Value
                    }
                )
         );
        // Hint to GC that the intermediate list might be collectible
        jsonPdmsKeyValuePairs = null;
        CheckMemoryUsage("After creating pdmsEntries dictionary");

        // Deduplicate AABBs - ** Memory peak for the dictionary itself **
        var aabbs = MopTimer.RunAndMeasure(
            "Grouping AABBs",
            _logger,
            () => jsonAabbs
                .GroupBy(b => b.GetGroupKey())
                .ToDictionary(keySelector: g => g.Key, elementSelector: g => g.First().CopyWithNewId(++aabbIdCounter))
        );
        // Hint to GC
        jsonAabbs = null;
        CheckMemoryUsage("After creating aabbs dictionary");


        _logger.LogInformation("Starting database population...");
        var sqliteComposeTimer = MopTimer.Create("Populating database and building index", _logger);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // --- Batch Insertions ---

        // Batch Insert PDMSEntries
        MopTimer.RunAndMeasure("Insert PDMSEntries", _logger, () =>
            BatchInsertHelper(connection, pdmsEntries.Values, PDMSEntry.RawInsertBatch, DatabaseBatchSize, "PDMSEntries", _logger)
        );
        CheckMemoryUsage("After inserting PDMSEntries");

        // Batch Insert AABBs (Create R-Tree table first)
        MopTimer.RunAndMeasure("Create R-Tree Table and Insert AABBs", _logger, () =>
        {
            using (var transaction = connection.BeginTransaction())
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                // Manually creating a special R-Tree table
                cmd.CommandText =
                    "CREATE VIRTUAL TABLE AABBs USING rtree(Id, min_x, max_x, min_y, max_y, min_z, max_z)";
                cmd.ExecuteNonQuery();
                transaction.Commit(); // Commit schema change before data insertion batching
            }
            // Now batch insert data
            BatchInsertHelper(connection, aabbs.Values, AABB.RawInsertBatch, DatabaseBatchSize, "AABBs", _logger);
        });
        CheckMemoryUsage("After inserting AABBs");

        // --- Batch Process and Insert Nodes and NodePDMSEntries ---
        _logger.LogInformation("Processing and inserting Nodes and NodePDMSEntries in batches...");
        var nodeProcessingTimer = Stopwatch.StartNew();
        int totalNodes = inputNodes.Count;
        int numNodeBatches = (totalNodes + DatabaseBatchSize - 1) / DatabaseBatchSize;

        for (int i = 0; i < numNodeBatches; i++)
        {
            var batchStartIndex = i * DatabaseBatchSize;
            var currentBatchSize = Math.Min(DatabaseBatchSize, totalNodes - batchStartIndex);
            // Use Skip/Take for simplicity, though List index access might be slightly faster if inputNodes is List
            var inputNodesBatch = inputNodes.Skip(batchStartIndex).Take(currentBatchSize);

            var nodesBatch = new List<Node>(currentBatchSize);
            var nodePdmsEntriesBatch = new List<NodePDMSEntry>(); // Capacity is variable, start default

            // Process the batch
            foreach (var inputNode in inputNodesBatch)
            {
                // Create Node object
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
                    // AABB lookup from the pre-calculated dictionary
                    AABB = inputNode.AABB == null ? null : aabbs[inputNode.AABB.GetGroupKey()],
                    DiagnosticInfo = inputNode.OptionalDiagnosticInfo
                    // NodePDMSEntry relationship is handled via the separate table/list
                };
                nodesBatch.Add(newNode);

                // Create corresponding NodePDMSEntry objects for this node
                if (inputNode.PDMSData.Any())
                {
                    foreach (var kvp in inputNode.PDMSData)
                    {
                        // Lookup PDMSEntry Id from the pre-calculated dictionary
                        if (pdmsEntries.TryGetValue(kvp.GetGroupKey(), out var pdmsEntry))
                        {
                            nodePdmsEntriesBatch.Add(new NodePDMSEntry
                            {
                                NodeId = inputNode.NodeId,
                                PDMSEntryId = pdmsEntry.Id
                            });
                        }
                        else
                        {
                            // Should not happen if preprocessing was correct, but log if it does
                            _logger.LogWarning("Could not find pre-calculated PDMSEntry for Node {NodeId}, Key: {Key}, Value: {Value}",
                                inputNode.NodeId, kvp.Key, kvp.Value);
                        }
                    }
                }
            } // End foreach inputNode in batch

            // Insert the collected batches for Nodes and NodePDMSEntries
            if (nodesBatch.Any())
            {
                BatchInsertHelper(connection, nodesBatch, Node.RawInsertBatch, nodesBatch.Count, $"Nodes (Batch {i + 1}/{numNodeBatches})", _logger, isInnerBatch: true);
            }
            if (nodePdmsEntriesBatch.Any())
            {
                 BatchInsertHelper(connection, nodePdmsEntriesBatch, NodePDMSEntry.RawInsertBatch, nodePdmsEntriesBatch.Count, $"NodePDMSEntries (Batch {i + 1}/{numNodeBatches})", _logger, isInnerBatch: true);
            }

            _logger.LogDebug("Processed node batch {BatchNum}/{TotalBatches}", i + 1, numNodeBatches);
            CheckMemoryUsage($"After processing node batch {i + 1}");

        } // End for each batch

        nodeProcessingTimer.Stop();
        _logger.LogInformation("Finished processing and inserting {TotalNodes} Nodes and related entries in {ElapsedSeconds:0.00} seconds.", totalNodes, nodeProcessingTimer.Elapsed.TotalSeconds);


        // --- Index Creation and Optimization (Unchanged) ---
        MopTimer.RunAndMeasure(
            "Creating indexes",
            _logger,
            () =>
            {
                _logger.LogInformation("Creating standard indexes...");
                using var transaction = connection.BeginTransaction();
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction; // Ensure command uses the transaction
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
                _logger.LogInformation("Standard indexes created.");
            }
        );
        CheckMemoryUsage("After creating indexes");

        MopTimer.RunAndMeasure(
            "Optimizing Database",
            _logger,
            () =>
            {
                _logger.LogInformation("Running PRAGMA analyze/optimize...");
                using var cmd = connection.CreateCommand();
                cmd.CommandTimeout = 300; // Increase timeout for potentially long operations
                cmd.CommandText = "pragma analyze";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "pragma optimize";
                cmd.ExecuteNonQuery();
                 _logger.LogInformation("PRAGMA analyze/optimize finished.");
            }
        );
        CheckMemoryUsage("After optimizing database");

        MopTimer.RunAndMeasure(
            "VACUUM Database",
            _logger,
            () =>
            {
#if DEBUG
                _logger.LogInformation("Skipping VACUUM in DEBUG mode.");
                return;
#else
                _logger.LogInformation("Starting VACUUM...");
                using var vacuumCmds = connection.CreateCommand();
                vacuumCmds.CommandTimeout = 1200; // Vacuum can take a very long time, increase timeout significantly

                vacuumCmds.CommandText = "PRAGMA page_count";
                Int64 pageCountBeforeVacuum = 0;
                try { pageCountBeforeVacuum = (Int64?)vacuumCmds.ExecuteScalar() ?? 0; } catch { /* Ignore */ }

                var timer = Stopwatch.StartNew();
                vacuumCmds.CommandText = "VACUUM";
                vacuumCmds.ExecuteNonQuery();
                timer.Stop();

                Int64 pageCountAfterVacuum = 0;
                try {
                    vacuumCmds.CommandText = "PRAGMA page_count";
                    pageCountAfterVacuum = (Int64?)vacuumCmds.ExecuteScalar() ?? 0;
                } catch { /* Ignore */ }


                _logger.LogInformation(
                    "VACUUM finished in {Elapsed}. Page count before: {PageCountBefore}, after: {PageCountAfter}",
                    timer.Elapsed, pageCountBeforeVacuum, pageCountAfterVacuum
                );

                try {
                    // Set other pragmas after vacuum
                    vacuumCmds.CommandText = "PRAGMA auto_vacuum = NONE";
                    vacuumCmds.ExecuteNonQuery();
                    vacuumCmds.CommandText = "PRAGMA analysis_limit = 1000";
                    vacuumCmds.ExecuteNonQuery();
                } catch (Exception ex) {
                     _logger.LogWarning(ex, "Failed to set PRAGMAs after VACUUM.");
                }
#endif
            }
        );

        CheckMemoryUsage("End of ComposeDatabase");

        sqliteComposeTimer.LogCompletion();
    }


    /// <summary>
    /// Helper method to insert items in batches using a provided raw insert action.
    /// </summary>
    private static void BatchInsertHelper<T>(
        SqliteConnection connection,
        IEnumerable<T> allItems,
        Action<SqliteCommand, IEnumerable<T>> insertMethod, // e.g., PDMSEntry.RawInsertBatch
        int batchSize,
        string itemNamePlural, // For logging
        ILogger logger,
        bool isInnerBatch = false) // Flag to adjust logging verbosity
    {
        if (allItems == null) return;

        var timer = Stopwatch.StartNew();
        int totalCount = 0;
        int batchCount = 0;

        var batch = new List<T>(Math.Min(batchSize, 1024)); // Pre-allocate list reasonably

        foreach (var item in allItems)
        {
            batch.Add(item);
            totalCount++;
            if (batch.Count >= batchSize)
            {
                InsertBatchInternal(connection, batch, insertMethod, itemNamePlural, ++batchCount, logger, isInnerBatch);
                batch.Clear(); // Clear for the next batch
            }
        }

        // Insert any remaining items in the last partial batch
        if (batch.Count > 0)
        {
            InsertBatchInternal(connection, batch, insertMethod, itemNamePlural, ++batchCount, logger, isInnerBatch);
        }
        timer.Stop();

        // Reduce log noise for inner batches (Nodes/NodePDMSEntries)
        if (!isInnerBatch)
        {
             logger.LogInformation("Finished inserting {TotalCount} {ItemNamePlural} in {BatchCount} batches in {ElapsedSeconds:0.00} seconds.", totalCount, itemNamePlural, batchCount, timer.Elapsed.TotalSeconds);
        } else if (timer.Elapsed.TotalSeconds > 1) // Log inner batches only if they take significant time
        {
             logger.LogDebug("Finished inserting {TotalCount} {ItemNamePlural} in {ElapsedSeconds:0.00} seconds.", totalCount, itemNamePlural, timer.Elapsed.TotalSeconds);
        }
    }

    /// <summary>
    /// Internal helper to execute the insert action for a single batch within a transaction.
    /// </summary>
    private static void InsertBatchInternal<T>(
        SqliteConnection connection,
        List<T> batchItems,
        Action<SqliteCommand, IEnumerable<T>> insertMethod,
        string itemNamePlural, // For logging context in case of error
        int batchNum,
        ILogger logger,
        bool isInnerBatch)
    {
        if (batchItems.Count == 0) return;

        var logLevel = isInnerBatch ? LogLevel.Trace : LogLevel.Debug; // Less verbose for inner batches
        logger.Log(logLevel, "Inserting batch {BatchNum} of {ItemNamePlural} ({ItemCount} items)...", batchNum, itemNamePlural, batchItems.Count);

        using var transaction = connection.BeginTransaction();
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction; // Associate command with transaction
        try
        {
            insertMethod(cmd, batchItems); // Call the specific RawInsertBatch method provided
            transaction.Commit();
        }
        catch (Exception ex)
        {
            // Log error with batch context
            logger.LogError(ex, "Failed to insert batch {BatchNum} for {ItemNamePlural}. Error: {Message}", batchNum, itemNamePlural, ex.Message);
            // Rollback is implicit due to transaction dispose on exception, but rethrow to halt process
            throw;
        }
    }


    // AddTreeIndexToSectorToDatabase method remains unchanged as it uses a different pattern
    public static void AddTreeIndexToSectorToDatabase(
        IReadOnlyList<(uint TreeIndex, uint SectorId)> treeIndexToSectorId,
        DirectoryInfo outputDirectory
    )
    {
        var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
        // Consider adding batching here too if treeIndexToSectorId can be very large
        const int SectorBatchSize = 50000; // Example batch size for this method

        using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            connection.Open();

            using (var createTableCommand = connection.CreateCommand())
            {
                 // Use IF NOT EXISTS for resilience if method is called multiple times
                createTableCommand.CommandText =
                    "CREATE TABLE IF NOT EXISTS PrioritizedSectors (TreeIndex INTEGER NOT NULL, PrioritizedSectorId INTEGER NOT NULL, PRIMARY KEY (TreeIndex, PrioritizedSectorId)) WITHOUT ROWID; ";
                createTableCommand.ExecuteNonQuery();
            }


            using (var command = connection.CreateCommand())
            {
                // Use parameterized query for safety and efficiency
                command.CommandText =
                    "INSERT OR IGNORE INTO PrioritizedSectors (TreeIndex, PrioritizedSectorId) VALUES ($TreeIndex, $PrioritizedSectorId)"; // Use INSERT OR IGNORE to handle duplicates gracefully

                var treeIndexParameter = command.CreateParameter();
                treeIndexParameter.ParameterName = "$TreeIndex";
                var prioritizedSectorIdParameter = command.CreateParameter();
                prioritizedSectorIdParameter.ParameterName = "$PrioritizedSectorId"; // Corrected name

                command.Parameters.AddRange(new [] {treeIndexParameter, prioritizedSectorIdParameter}); // Use array initializer

                int itemCount = 0;
                var distinctItems = treeIndexToSectorId.Distinct(); // Process distinct items

                using (var transaction = connection.BeginTransaction())
                {
                    command.Transaction = transaction; // Assign transaction to command once

                    foreach (var pair in distinctItems)
                    {
                        treeIndexParameter.Value = pair.TreeIndex;
                        prioritizedSectorIdParameter.Value = pair.SectorId;
                        command.ExecuteNonQuery();
                        itemCount++;

                        // Commit periodically in batches
                        if (itemCount % SectorBatchSize == 0)
                        {
                             Console.WriteLine($"Committing PrioritizedSectors batch at item {itemCount}..."); // Simple progress indicator
                            transaction.Commit();
                            transaction.Dispose(); // Dispose old transaction
                            var newTransaction = connection.BeginTransaction(); // Start new transaction
                            command.Transaction = newTransaction; // Assign new transaction
                        }
                    }
                     // Commit any remaining items in the final batch
                    transaction.Commit();
                } // Final transaction is disposed here
            }
        }
         Console.WriteLine($"Finished inserting/updating {treeIndexToSectorId.Count} PrioritizedSectors entries.");
    }

    // CreateEmptyDatabase method remains unchanged
    private static void CreateEmptyDatabase(DbContextOptions options)
    {
        using var context = new HierarchyContext(options);
        // EnsureCreated is generally fine for creating schema once.
        if (!context.Database.EnsureCreated())
        {
            // Consider logging error here
            throw new Exception($"Could not create database schema using EF Core EnsureCreated.");
        }
    }
}
