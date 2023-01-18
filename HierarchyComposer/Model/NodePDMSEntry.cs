namespace HierarchyComposer.Model;

using Microsoft.Data.Sqlite;

public class NodePDMSEntry
{
    public uint NodeId { get; set; }
    public Node Node { get; set; } = null!;

    public long PDMSEntryId { get; set; }
    public PDMSEntry PDMSEntry { get; set; } = null!;

    public void RawInsert(SqliteCommand command)
    {
        command.CommandText = "INSERT INTO NodeToPDMSEntry (NodeId, PDMSEntryId) VALUES (@NodeId, @PDMSEntryId);";
        command.Parameters.Clear();
        command.Parameters.AddRange(new[] {
            new SqliteParameter("@NodeId", NodeId),
            new SqliteParameter("@PDMSEntryId", PDMSEntryId)});
        command.ExecuteNonQuery();
    }
}