using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SignalR.Hubs;

namespace SignalR.Infrastructure
{
    internal static class ReflectionHelper
    {
        private static readonly Type[] _excludeTypes = new[] { typeof(IHub), typeof(Hub), typeof(object) };

        internal static IEnumerable<MethodInfo> GetExportedHubMethods(Type type)
        {
            if (!typeof(IHub).IsAssignableFrom(type))
            {
                return Enumerable.Empty<MethodInfo>();
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var getMethods = properties.Select(p => p.GetGetMethod());
            var setMethods = properties.Select(p => p.GetSetMethod());
            var allPropertyMethods = getMethods.Concat(setMethods);

            var actualMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            return actualMethods.Except(allPropertyMethods)
                                .Where(m => !_excludeTypes.Contains(m.DeclaringType));

        }

        internal static TResult GetAttributeValue<TAttribute, TResult>(ICustomAttributeProvider source, Func<TAttribute, TResult> valueGetter)
            where TAttribute : Attribute
        {
            var attributes = source.GetCustomAttributes(typeof(TAttribute), false)
                .Cast<TAttribute>()
                .ToList();
            if (attributes.Any())
            {
                return valueGetter(attributes[0]);
            }
            return default(TResult);
        }

    }
}
