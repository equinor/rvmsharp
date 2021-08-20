using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SQLite;

namespace Mop.Hierarchy.Model
{
    public class Node
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public uint Id { get; set; }

        public int? RefNoDb { get; set; }
        public int? RefNoSequence { get; set; }

        public string Name { get; set; }
        public bool HasMesh { get; set; }

        [ForeignKey("ParentId")]
        public virtual Node Parent { get; set; }

        [ForeignKey("TopNodeId")]
        public virtual Node TopNode { get; set; }

        public ICollection<NodePDMSEntry> NodePDMSEntry { get; set; }

        public AABB AABB { get; set; }

        public void RawInsert(SQLiteCommand command)
        {
            command.CommandText = "INSERT INTO Nodes (Id, RefNoDb, RefNoSequence,  Name, HasMesh, ParentId, TopNodeId, AABBId) VALUES (@Id, @RefNoDb, @RefNoSequence, @Name, @HasMesh, @ParentId, @TopNodeId, @AABBId);";
            command.Parameters.AddRange(new[] {
                    new SQLiteParameter("@Id", Id),
                    new SQLiteParameter("@RefNoDb", RefNoDb),
                    new SQLiteParameter("@RefNoSequence", RefNoSequence),
                    new SQLiteParameter("@Name", Name),
                    new SQLiteParameter("@HasMesh", HasMesh),
                    new SQLiteParameter("@ParentId", Parent?.Id ?? 0),
                    new SQLiteParameter("@TopNodeId", TopNode?.Id ?? 0),
                    new SQLiteParameter("@AABBId", AABB?.Id ?? 0),
                    });
            command.ExecuteNonQuery();
        }
    }
}
