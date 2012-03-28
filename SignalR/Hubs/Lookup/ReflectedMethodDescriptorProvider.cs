using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SignalR.Hubs.Extensions;
using SignalR.Hubs.Lookup.Descriptors;
using SignalR.Infrastructure;

namespace SignalR.Hubs.Lookup
{
    using System.Reflection;

    public class ReflectedMethodDescriptorProvider : IMethodDescriptorProvider
    {
        private readonly ConcurrentDictionary<string, IDictionary<string, IEnumerable<MethodDescriptor>>> _methods;

        public ReflectedMethodDescriptorProvider()
        {
            _methods = new ConcurrentDictionary<string, IDictionary<string, IEnumerable<MethodDescriptor>>>();
        }

        public IEnumerable<MethodDescriptor> GetMethods(HubDescriptor hub)
        {
            return FetchMethodsFor(hub)
                .SelectMany(kv => kv.Value)
                .ToList();
        }

        private IDictionary<string, IEnumerable<MethodDescriptor>> FetchMethodsFor(HubDescriptor hub)
        {
            return _methods.GetOrAdd(
                hub.Name, 
                key => ReflectionHelper.GetExportedHubMethods(hub.Type)
                    .Select(m => 
                        {
                            var descriptor = new MethodDescriptor 
                                {
                                    ReturnType = m.ReturnType,
                                    Name = GetMethodName(m),
                                    Parameters = m.GetParameters()
                                        .Select(p => new ParameterDescriptor 
                                            {
                                                Name = p.Name,
                                                Type = p.ParameterType
                                            })
                                        .ToList()                   
                                };

                            descriptor.Invoker = (target, parameters) => m.Invoke(target, descriptor.Adjust(parameters));
                            return descriptor;
                        })
                    .GroupBy(d => d.Name)
                    .ToDictionary(a => a.Key.ToLowerInvariant(), 
                                  a => a.AsEnumerable()));
        }

        public bool TryGetMethod(HubDescriptor hub, string method, out MethodDescriptor descriptor, params object[] parameters)
        {
            IEnumerable<MethodDescriptor> overloads;

            if(FetchMethodsFor(hub).TryGetValue(method.ToLowerInvariant(), out overloads))
            {
                var matches = overloads.Where(o => o.Matches(parameters)).ToList();
                if(matches.Count == 1)
                {
                    descriptor = matches.First();
                    return true;
                }
            }

            descriptor = null;
            return false;
        }

        private static string GetMethodName(MethodInfo method)
        {
            return ReflectionHelper.GetAttributeValue<HubMethodNameAttribute, string>(method, a => a.MethodName)
                   ?? method.Name;
        }
    }
}