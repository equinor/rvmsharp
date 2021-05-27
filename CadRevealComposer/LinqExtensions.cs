namespace CadRevealComposer
{
    using System.Collections.Generic;
    using System.Linq;

    public static class LinqExtensions
    {
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : class
        {
            return enumerable.Where(e => e is not null)!;
        }
    }
}