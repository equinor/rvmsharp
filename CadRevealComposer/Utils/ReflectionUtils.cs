namespace CadRevealComposer.Utils
{
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
    }
}