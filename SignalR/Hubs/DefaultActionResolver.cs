using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SignalR.Infrastructure;

namespace SignalR.Hubs {
    public class DefaultActionResolver : IActionResolver {
        public ActionInfo ResolveAction(Type hubType, string actionName, object[] parameters) {
            // Get all methods
            var candidates = ReflectionHelper.GetExportedHubMethods(hubType)
                                 .Where(m => m.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase))
                                 .Where(m => ParametersAreCompatible(m.GetParameters(), parameters))
                                 .ToList();

            switch (candidates.Count) {
                case 1:
                    var method = candidates.Single();
                    var args = GetParameters(method.GetParameters(), parameters);
                    return new ActionInfo {
                        Method = method,
                        Arguments = args
                    };
                default:
                    break;
            }

            return null;
        }

        private bool ParametersAreCompatible(ParameterInfo[] parameterInfos, object[] parameters) {
            return (parameterInfos.Length == 0 && parameters == null) ||
                   (parameterInfos.Length == parameters.Length);
        }

        private object[] GetParameters(ParameterInfo[] parameterInfos, object[] parameters) {
            return parameterInfos.OrderBy(p => p.Position)
                                 .Select(p => Bind(parameters[p.Position], p.ParameterType))
                                 .ToArray();
        }

        private object Bind(IDictionary<string, object> dictionaryValue, Type type) {
            object obj = Activator.CreateInstance(type);
            foreach (var property in type.GetProperties()) {
                object value;
                if (dictionaryValue.TryGetValue(property.Name, out value)) {
                    property.SetValue(obj, Bind(value, property.PropertyType), null);
                }
            }
            return obj;
        }

        private object Bind(object value, Type type) {
            var dictionaryValue = value as IDictionary<string, object>;
            if (dictionaryValue != null) {
                return Bind(new Dictionary<string, object>(dictionaryValue, StringComparer.OrdinalIgnoreCase), type);
            }
            return Convert.ChangeType(value, type);
        }
    }
}