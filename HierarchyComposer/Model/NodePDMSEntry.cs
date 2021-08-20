using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;

namespace Mop.Hierarchy.Model
{
    public class NodePDMSEntry
    {
        public uint NodeId { get; set; }
        public Node Node { get; set; }

        public long PDMSEntryId { get; set; }
        public PDMSEntry PDMSEntry { get; set; }

        public void RawInsert(SQLiteCommand command)
        {
            command.CommandText = "INSERT INTO NodeToPDMSEntry (NodeId, PDMSEntryId) VALUES (@NodeId, @PDMSEntryId);";
            command.Parameters.AddRange(new[] {
                    new SQLiteParameter("@NodeId", NodeId),
                    new SQLiteParameter("@PDMSEntryId", PDMSEntryId)});
            command.ExecuteNonQuery();
        }
    }
}
