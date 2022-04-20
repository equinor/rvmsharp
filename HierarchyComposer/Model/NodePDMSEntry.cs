namespace HierarchyComposer.Model;

using System.Data.SQLite;

public class NodePDMSEntry
{
    public uint NodeId { get; set; }
    public Node Node { get; set; } = null!;

    public long PDMSEntryId { get; set; }
    public PDMSEntry PDMSEntry { get; set; } = null!;

    public void RawInsert(SQLiteCommand command)
    {
        command.CommandText = "INSERT INTO NodeToPDMSEntry (NodeId, PDMSEntryId) VALUES (@NodeId, @PDMSEntryId);";
        command.Parameters.AddRange(new[] {
            new SQLiteParameter("@NodeId", NodeId),
            new SQLiteParameter("@PDMSEntryId", PDMSEntryId)});
        command.ExecuteNonQuery();
    }
}