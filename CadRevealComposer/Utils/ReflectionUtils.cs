namespace CadRevealComposer.Utils
{
    using Primitives;
    using System.Collections.Generic;
    using System.Linq;

    public static class ReflectionUtils
    {
        public static bool HasProperty<T>(this object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName);
            return propertyInfo != null && propertyInfo.PropertyType == typeof(T);
        }

        public static T? GetProperty<T>(this object obj, string propertyName)
        {
            return (T?)obj.GetType().GetProperty(propertyName)?.GetValue(obj);
        }
        
        public static IEnumerable<T?> CollectProperties<T, TG>(this IEnumerable<TG> collection, params string[] propertyNames) where TG : notnull
        {
            return propertyNames.SelectMany(
                propertyName => collection
                    .Where(x => x.HasProperty<T>(propertyName))
                    .Select(x => x.GetProperty<T>(propertyName)));
        }

        public static IEnumerable<T?> CollectProperties<T>(this IEnumerable<APrimitive> collection, I3dfAttribute.AttributeType type)
        {
            return collection
                .AsParallel().SelectMany(x => (x.GetType().GetProperties()
                    .Where(p => p.GetCustomAttributes(true).Any(a => a is I3dfAttribute attr && attr.Type == type))
                    .Select(p => (x, p))))
                    .Select(x => x.x.GetProperty<T>(x.p.Name));
        }
    }
}