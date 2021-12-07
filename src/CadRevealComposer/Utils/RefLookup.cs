namespace CadRevealComposer.Utils
{
    using System.Collections.Generic;

    /// <summary>
    /// This is a hack to allow look up in dictionaries bypassing Equal comparator.
    /// In case we want to write duplicate meshes as "triangle meshes" in peripheral file.
    /// </summary>
    public record RefLookup<T>(T? Value)
    {
        public virtual bool Equals(RefLookup<T>? other)
        {
            return other != null && ReferenceEquals(Value, other.Value);
        }

        public override int GetHashCode()
        {
            return Value != null ? EqualityComparer<T?>.Default.GetHashCode(Value) : 0;
        }
    }
}