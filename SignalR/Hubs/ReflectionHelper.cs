using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SignalR.Hubs
{
    public static class ReflectionHelper
    {
        private static readonly Type[] _excludeTypes = new[] { typeof(Hub), typeof(object) };
        private static readonly Type[] _excludeInterfaces = new[] { typeof(IHub), typeof(IDisconnect), typeof(IConnected) };

        public static IEnumerable<MethodInfo> GetExportedHubMethods(Type type)
        {
            if (!typeof(IHub).IsAssignableFrom(type))
            {
                return Enumerable.Empty<MethodInfo>();
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var getMethods = properties.Select(p => p.GetGetMethod());
            var setMethods = properties.Select(p => p.GetSetMethod());
            var allPropertyMethods = getMethods.Concat(setMethods);
            var allInterfaceMethods = _excludeInterfaces.SelectMany(i => GetInterfaceMethods(type, i));
            var allExcludes = allPropertyMethods.Concat(allInterfaceMethods);

            var actualMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            return actualMethods.Except(allExcludes)
                                .Where(m => !_excludeTypes.Contains(m.DeclaringType));

        }

        private static IEnumerable<MethodInfo> GetInterfaceMethods(Type type, Type iface)
        {
            if (!iface.IsAssignableFrom(type))
            {
                return Enumerable.Empty<MethodInfo>();
            }

            return type.GetInterfaceMap(iface).TargetMethods;
        }

        public static TResult GetAttributeValue<TAttribute, TResult>(ICustomAttributeProvider source, Func<TAttribute, TResult> valueGetter)
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
