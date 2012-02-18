﻿using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public class DefaultActionResolver : IActionResolver
    {
        public ActionInfo ResolveAction(Type hubType, string actionName, object[] parameters)
        {
            // Get all methods
            var candidates = ReflectionHelper.GetExportedHubMethods(hubType)
                                 .Where(m => m.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase))
                                 .Where(m => ParametersAreCompatible(m.GetParameters(), parameters))
                                 .ToList();

            switch (candidates.Count)
            {
                case 1:
                    var method = candidates.Single();
                    var args = GetParameters(method.GetParameters(), parameters);
                    return new ActionInfo
                    {
                        Method = method,
                        Arguments = args
                    };
                default:
                    break;
            }

            return null;
        }

        private bool ParametersAreCompatible(ParameterInfo[] parameterInfos, object[] parameters)
        {
            return (parameterInfos.Length == 0 && parameters == null) ||
                   (parameterInfos.Length == parameters.Length);
        }

        private object[] GetParameters(ParameterInfo[] parameterInfos, object[] parameters)
        {
            return parameterInfos.OrderBy(p => p.Position)
                                 .Select(p => Bind(parameters[p.Position], p.ParameterType))
                                 .ToArray();
        }

        private object Bind(object value, Type type)
        {
            if (value == null)
            {
                return null;
            }

            if (value.GetType() == type)
            {
                return value;
            }

            if (type == typeof(Guid))
            {
            	return new Guid(value.ToString());
            }

            return JsonConvert.DeserializeObject(value.ToString(), type);
        }
    }
}