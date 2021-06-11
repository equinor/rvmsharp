namespace CadRevealComposer.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    public static class LinqExtensions
    {
        /// <summary>
        /// Remove any "null" values, and change the type of the <see cref="IEnumerable{T}"/> from T? to T!
        /// </summary>
        [Pure]
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : class
        {
            return enumerable.Where(e => e is not null)!;
        }
        
        /// <summary>
        /// Remove any "null" values, and change the type type of the <see cref="IEnumerable{T}"/> from <see cref="Nullable{T}"/> to T!
        /// </summary>
        /// <remarks>This both does a where and a select, and might have a slight performance impact.</remarks>
        [Pure]
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : struct
        {
            return enumerable.Where(e => e != null).Select(e => e!.Value)!;
        }
    }
}