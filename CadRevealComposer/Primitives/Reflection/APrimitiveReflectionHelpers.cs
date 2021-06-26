namespace CadRevealComposer.Primitives.Reflection
{
    using Primitives;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;

    public static class APrimitiveReflectionHelpers
    {
        private static Dictionary<Type, Dictionary<I3dfAttribute.AttributeType, PropertyInfo[]>>?
            _attributeLookupCache;

        private static Dictionary<Type, Dictionary<I3dfAttribute.AttributeType, PropertyInfo[]>>
            CachedAttributeLookup => _attributeLookupCache ??= CreatePrimitiveAttributeLookup();

        public static Dictionary<Type, Dictionary<I3dfAttribute.AttributeType, PropertyInfo[]>>
            CreatePrimitiveAttributeLookup()
        {
            // Group all properties by Kind and Attribute, for fast lookup.
            var aPrimitiveDerivingTypes =
                typeof(APrimitive).Assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(APrimitive)) && !t.IsAbstract);

            var propertiesByType =
                aPrimitiveDerivingTypes
                    .SelectMany(pt => pt.GetProperties().Select(p => (PrimitiveType: pt, Property: p)))
                    .Select(pair =>
                        (PrimitiveType: pair.PrimitiveType,
                            Attributes: pair.Property.GetCustomAttributes(inherit: true).OfType<I3dfAttribute>()
                                .FirstOrDefault(),
                            Property: pair.Property))
                    .Where(triple => triple.Attributes != null)
                    .GroupBy(triple => triple.PrimitiveType);

            var propertiesByTypeAndThenByAttributeKind =
                propertiesByType
                    .ToDictionary(
                        x => x.Key,
                        x =>
                            x.GroupBy(x1 => x1.Attributes!.Kind)
                                .ToDictionary(
                                    x2 => x2.Key,
                                    x2 =>
                                        x2.Select(x3 => x3.Property).ToArray()));

            return propertiesByTypeAndThenByAttributeKind;
        }

        public static ImmutableSortedSet<T> GetDistinctValuesOfAllPropertiesMatchingKind<T>(
            IEnumerable<APrimitive> primitive,
            I3dfAttribute.AttributeType attributeKind, IComparer<T>? comparer = null)
        {
            var data = primitive
                .AsParallel()
                .SelectMany(y => GetAllValuesOfAttributeKind<T>(y, attributeKind))
                .ToImmutableSortedSet(comparer);

            return data;
        }

        private static IEnumerable<T> GetAllValuesOfAttributeKind<T>(APrimitive primitive,
            I3dfAttribute.AttributeType attributeKind)
        {
            var type = primitive.GetType();

            if (!CachedAttributeLookup[type].TryGetValue(attributeKind, out var propertyInfos))
            {
                yield break;
            }

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                var value = propertyInfo.GetValue(primitive);
                if (value is T tValue)
                {
                    yield return tValue;
                }

                throw new Exception(
                    $"Unexpected non {typeof(T)} value (was {value}) matching attributeKind {attributeKind} on property {propertyInfo.Name} of primitive: {primitive}");
            }
        }
    }
}