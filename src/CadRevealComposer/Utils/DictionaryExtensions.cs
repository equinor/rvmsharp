namespace CadRevealComposer.Utils
{
    using System.Collections.Generic;

    public static class DictionaryExtensions
    {
        /// <summary>
        /// Get the value assigned to the Key if the key exists, if not it returns null.
        /// </summary>
        public static TValue? GetValueOrNull<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key)
            where TKey : notnull
            where TValue : class // Remark: This does not work on non-reference types, so we just ignore it.
        {
            return self.TryGetValue(key, out var value) ? value : null;
        }
    }
}