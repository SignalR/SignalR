using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public class ReflectedMethodDescriptorProvider : IMethodDescriptorProvider
    {
        private readonly ConcurrentDictionary<string, IDictionary<string, IEnumerable<MethodDescriptor>>> _methods;
        private readonly ConcurrentDictionary<string, MethodDescriptor> _executableMethods;

        public ReflectedMethodDescriptorProvider()
        {
            _methods = new ConcurrentDictionary<string, IDictionary<string, IEnumerable<MethodDescriptor>>>(StringComparer.OrdinalIgnoreCase);
            _executableMethods = new ConcurrentDictionary<string, MethodDescriptor>(StringComparer.OrdinalIgnoreCase);
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
        private IDictionary<string, IEnumerable<MethodDescriptor>> BuildMethodCacheFor(HubDescriptor hub)
        {
            return ReflectionHelper.GetExportedHubMethods(hub.Type)
                .GroupBy(GetMethodName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key,
                              group => group.Select(oload =>
                                  new MethodDescriptor
                                  {
                                      ReturnType = oload.ReturnType,
                                      Name = group.Key,
                                      Invoker = oload.Invoke,
                                      Parameters = oload.GetParameters()
                                          .Select(p => new ParameterDescriptor
                                              {
                                                  Name = p.Name,
                                                  Type = p.ParameterType,
                                              })
                                          .ToList()
                                  }),
                              StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetMethod(HubDescriptor hub, string method, out MethodDescriptor descriptor, params JToken[] parameters)
        {
            string hubMethodKey = hub.Name + "::" + method;

            if(!_executableMethods.TryGetValue(hubMethodKey, out descriptor))
            {
                IEnumerable<MethodDescriptor> overloads;

                if(FetchMethodsFor(hub).TryGetValue(method, out overloads))
                {
                    var matches = overloads.Where(o => o.Matches(parameters)).ToList();

                    // If only one match is found, that is the "executable" version, otherwise none of the methods can be returned because we don't know which one was actually being targeted
                    descriptor =  matches.Count == 1 ? matches[0] : null;
                }
                else
                {
                    descriptor = null;
                }

                // If an executable method was found, cache it for future lookups (NOTE: we don't cache null instances because it could be a surface area for DoS attack by supplying random method names to flood the cache)
                if(descriptor != null)
                {
                    _executableMethods.TryAdd(hubMethodKey, descriptor);
                }
            }

            return descriptor != null;
        }

        private static string GetMethodName(MethodInfo method)
        {
            return ReflectionHelper.GetAttributeValue<HubMethodNameAttribute, string>(method, a => a.MethodName)
                   ?? method.Name;
        }
    }
}