// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.SignalR.Json;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class ReflectedMethodDescriptorProvider : IMethodDescriptorProvider
    {
        private readonly ConcurrentDictionary<string, IDictionary<string, IEnumerable<MethodDescriptor>>> _methods;
        private readonly ConcurrentDictionary<string, IEnumerable<MethodDescriptor>> _executableMethods;

        public ReflectedMethodDescriptorProvider()
        {
            _methods = new ConcurrentDictionary<string, IDictionary<string, IEnumerable<MethodDescriptor>>>(StringComparer.OrdinalIgnoreCase);
            _executableMethods = new ConcurrentDictionary<string, IEnumerable<MethodDescriptor>>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<MethodDescriptor> GetMethods(HubDescriptor hub)
        {
            return FetchMethodsFor(hub)
                .SelectMany(kv => kv.Value)
                .ToList();
        }

        /// <summary>
        /// Retrieves an existing dictionary of all available methods for a given hub from cache.
        /// If cache entry does not exist - it is created automatically by BuildMethodCacheFor.
        /// </summary>
        /// <param name="hub"></param>
        /// <returns></returns>
        private IDictionary<string, IEnumerable<MethodDescriptor>> FetchMethodsFor(HubDescriptor hub)
        {
            return _methods.GetOrAdd(
                hub.Name,
                key => BuildMethodCacheFor(hub));
        }

        /// <summary>
        /// Builds a dictionary of all possible methods on a given hub.
        /// Single entry contains a collection of available overloads for a given method name (key).
        /// This dictionary is being cached afterwards.
        /// </summary>
        /// <param name="hub">Hub to build cache for</param>
        /// <returns>Dictionary of available methods</returns>
        private static IDictionary<string, IEnumerable<MethodDescriptor>> BuildMethodCacheFor(HubDescriptor hub)
        {
            return ReflectionHelper.GetExportedHubMethods(hub.HubType)
                .GroupBy(GetMethodName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key,
                              group => group.Select(oload =>
                                  new MethodDescriptor
                                  {
                                      ReturnType = oload.ReturnType,
                                      Name = group.Key,
                                      NameSpecified = (GetMethodAttributeName(oload) != null),
                                      Invoker = new HubMethodDispatcher(oload).Execute,
                                      Hub = hub,
                                      Attributes = oload.GetCustomAttributes(typeof(Attribute), inherit: true).Cast<Attribute>(),
                                      Parameters = oload.GetParameters()
                                          .Select(p => new ParameterDescriptor
                                              {
                                                  Name = p.Name,
                                                  ParameterType = p.ParameterType,
                                                  IsOptional = p.IsOptional,
                                                  DefaultValue = p.DefaultValue,
                                                  IsParameterArray = p.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0,
                                              })
                                          .ToList()
                                  }),
                              StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Searches the specified <paramref name="hub">Hub</paramref> for the specified <paramref name="method"/>.
        /// </summary>
        /// <remarks>
        /// In the case that there are multiple overloads of the specified <paramref name="method"/>, the <paramref name="parameters">parameter set</paramref> helps determine exactly which instance of the overload should be resolved. 
        /// If there are multiple overloads found with the same number of matching parameters, none of the methods will be returned because it is not possible to determine which overload of the method was intended to be resolved.
        /// </remarks>
        /// <param name="hub">Hub to search for the specified <paramref name="method"/> on.</param>
        /// <param name="method">The method name to search for.</param>
        /// <param name="descriptor">If successful, the <see cref="MethodDescriptor"/> that was resolved.</param>
        /// <param name="parameters">The set of parameters that will be used to help locate a specific overload of the specified <paramref name="method"/>.</param>
        /// <returns>True if the method matching the name/parameter set is found on the hub, otherwise false.</returns>
        public bool TryGetMethod(HubDescriptor hub, string method, out MethodDescriptor descriptor, IList<IJsonValue> parameters)
        {
            string hubMethodKey = BuildHubExecutableMethodCacheKey(hub, method, parameters);
            IEnumerable<MethodDescriptor> overloads;
            descriptor = null;

            if (!_executableMethods.TryGetValue(hubMethodKey, out overloads))
            {
                if (FetchMethodsFor(hub).TryGetValue(method, out overloads))
                {
                    if (overloads != null)
                    {
                        // If executable method overloads was found, cache it for future lookups (NOTE: we don't cache null instances because it could be a surface area for DoS attack by supplying random method names to flood the cache)
                        _executableMethods.TryAdd(hubMethodKey, overloads);
                    }
                }
            }

            if (overloads != null)
            {
                var matches = overloads.Where(o => o.Matches(parameters)).ToList();


                if (matches.Count == 1)
                    descriptor = matches[0];

                //support overloading, choose the best match to parameters which has less extra parameters in method, and parameter type match. 
                if (matches.Count > 1)
                {

                    if (parameters.Count > 0)
                    {
                        List<MethodDescriptor> paramCanConvertMatches = new List<MethodDescriptor>();

                        foreach (var match in matches)
                        {
                            bool canConvert = true;
                            for (int i = 0; i < parameters.Count; i++)
                            {
                                if (!parameters[i].CanConvertTo(match.Parameters[i].ParameterType))
                                {
                                    canConvert = false;
                                }
                            }

                            if (canConvert == true)
                                paramCanConvertMatches.Add(match);
                        }


                        //one for parameters type match 
                        if (paramCanConvertMatches.Count == 1)
                            descriptor = paramCanConvertMatches[0];

                        if (paramCanConvertMatches.Count > 1)
                        {
                            int leastParamsMatch = 0;

                            //multiple mataches for least paramters in matches, so check parameter type match                              
                            for (int i = 0; i < paramCanConvertMatches.Count; i++)
                            {
                                if ((paramCanConvertMatches[i].Parameters.Count == parameters.Count))
                                {
                                    if ((!paramCanConvertMatches[i].Parameters[parameters.Count - 1].IsParameterArray) && (!paramCanConvertMatches[i].Parameters[parameters.Count - 1].IsOptional))
                                    {
                                        leastParamsMatch = i;
                                        descriptor = paramCanConvertMatches[i];
                                        break;
                                    }

                                    if (paramCanConvertMatches[i].Parameters[parameters.Count - 1].IsOptional)
                                    {
                                        leastParamsMatch = i;
                                    }

                                    // Parameter Array match last
                                    if (paramCanConvertMatches[i].Parameters[parameters.Count - 1].IsParameterArray)
                                    {
                                        if (!paramCanConvertMatches[leastParamsMatch].Parameters[parameters.Count - 1].IsOptional)
                                        {
                                            leastParamsMatch = i;
                                        }
                                    }
                                }

                                if ((paramCanConvertMatches[i].Parameters.Count > parameters.Count))
                                {
                                    //multiple mataches for the last parameter in parameters type match, so mataches should have extra parameter  
                                    //match the IsOptional for the extra parameter
                                    if (paramCanConvertMatches[i].Parameters[parameters.Count].IsOptional)
                                    {
                                        if (paramCanConvertMatches[i].Parameters.Count < paramCanConvertMatches[leastParamsMatch].Parameters.Count)
                                        {
                                            leastParamsMatch = i;
                                        }
                                    }

                                    // Parameter Array match last
                                    if (paramCanConvertMatches[i].Parameters[parameters.Count].IsParameterArray)
                                    {
                                        if (!paramCanConvertMatches[leastParamsMatch].Parameters[parameters.Count - 1].IsOptional)
                                        {
                                            leastParamsMatch = i;
                                        }
                                    }
                                }
                            }

                            descriptor = paramCanConvertMatches[leastParamsMatch];
                        }

                    }
                    else
                    {
                        foreach (var match in matches)
                        {
                            //match the IsOptional if parameters count is 0
                            if (match.Parameters[0].IsOptional)
                            {
                                descriptor = match;
                                break;
                            }
                            else
                            {
                                descriptor = match;
                            }
                        }
                    }
                }
            }

            return descriptor != null;
        }

        private static string BuildHubExecutableMethodCacheKey(HubDescriptor hub, string method, IList<IJsonValue> parameters)
        {
            string normalizedParameterCountKeyPart;

            if (parameters != null)
            {
                normalizedParameterCountKeyPart = parameters.Count.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                // NOTE: we normalize a null parameter array to be the same as an empty (i.e. Length == 0) parameter array
                normalizedParameterCountKeyPart = "0";
            }

            // NOTE: we always normalize to all uppercase since method names are case insensitive and could theoretically come in diff. variations per call
            string normalizedMethodName = method.ToUpperInvariant();

            string methodKey = hub.Name + "::" + normalizedMethodName + "(" + normalizedParameterCountKeyPart + ")";

            return methodKey;
        }

        private static string GetMethodName(MethodInfo method)
        {
            return GetMethodAttributeName(method) ?? method.Name;
        }

        private static string GetMethodAttributeName(MethodInfo method)
        {
            return ReflectionHelper.GetAttributeValue<HubMethodNameAttribute, string>(method, a => a.MethodName);
        }
    }
}
