using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.CodeAnalysis;

namespace Mop.Hierarchy.Model
{
    public class PDMSEntry : IEquatable<PDMSEntry>
    {
        public long Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public virtual ICollection<NodePDMSEntry> NodePDMSEntry { get; set; }

        public bool Equals([AllowNull] PDMSEntry other)
        {
            //Check whether the compared object is null. 
            if (ReferenceEquals(other, null)) return false;

            //Check whether the compared object references the same data. 
            if (ReferenceEquals(this, other)) return true;

            return (Key.Equals(other.Key) && Value.Equals(other.Value));
        }

        public override int GetHashCode()
        {
            //Get hash code for the Name field if it is not null. 
            int hashEntryKey = Key == null ? 0 : Key.GetHashCode();

            //Get hash code for the Code field. 
            int hashEntryValue = Value == null ? 0 : Value.GetHashCode();

            //Calculate the hash code for the product. 
            return hashEntryKey ^ hashEntryValue;
        }

        public void RawInsert(SQLiteCommand command)
        {
            command.CommandText = "INSERT INTO PDMSEntries (Id, Key, Value) VALUES (@Id, @Key, @Value);";
            command.Parameters.AddRange(new[] {
                        new SQLiteParameter("@Id", Id),
                        new SQLiteParameter("@Key", Key),
                        new SQLiteParameter("@Value", Value)});
            command.ExecuteNonQuery();
        }
    }
}
