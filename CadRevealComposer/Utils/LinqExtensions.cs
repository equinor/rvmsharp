namespace CadRevealComposer.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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
        /// Remove any "null" values, and change the type of the <see cref="ParallelQuery{T}"/> from T? to T!
        /// </summary>
        [Pure]
        public static ParallelQuery<T> WhereNotNull<T>(this ParallelQuery<T?> parallelQuery) where T : class
        {
            return parallelQuery.Where(e => e is not null)!;
        }

        /// <summary>
        /// Remove any "null" values, and change the type type of the <see cref="IEnumerable{T}"/> from <see cref="Nullable{T}"/> to T!
        /// </summary>
        /// <remarks>This both does a where and a select, and might have a slight performance impact.</remarks>
        [Pure]
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : struct
        {
            return enumerable.Where(e => e != null).Select(e => e!.Value);
        }

        /// <summary>
        /// Remove any "null" values, and change the type of the <see cref="ParallelQuery{T}"/> from T? to T!
        /// </summary>
        [Pure]
        public static ParallelQuery<T> WhereNotNull<T>(this ParallelQuery<T?> parallelQuery) where T : struct
        {
            return parallelQuery.Where(e => e != null).Select(e => e!.Value);
        }


        [Pure]
        public static ImmutableSortedSet<T> ToImmutableSortedSetFast<T>(this IEnumerable<T> enumerable, IComparer<T>? comparer = null)
        {
            // This is twice as fast as "ToImmutableSortedSet". No known issues.
            var builder = ImmutableSortedSet.CreateBuilder<T>();

            if (comparer != null)
                builder.KeyComparer = comparer;

            foreach (T e in enumerable)
            {
                builder.Add(e);
            }

            return builder.ToImmutable();
        }
    }
}