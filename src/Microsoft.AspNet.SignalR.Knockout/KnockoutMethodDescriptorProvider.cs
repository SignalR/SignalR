// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Knockout
{
    public class KnockoutMethodDescriptorProvider : IMethodDescriptorProvider
    {
        private readonly IList<MethodDescriptorBuilder> _builders;

        private readonly ConcurrentDictionary<HubDescriptor, IDictionary<string,  MethodDescriptor>> _methods;

        private readonly IMessageBus _bus;
        private readonly IJsonSerializer _serializer;

        public KnockoutMethodDescriptorProvider(IMessageBus bus, IJsonSerializer serializer)
        {
            _builders = new List<MethodDescriptorBuilder>()
            {
                new MethodDescriptorBuilder()
                {
                    Name = "OnKnockoutUpdate",
                    Parameters =  new[]
                    {
                        new ParameterDescriptor
                        {
                            Name = "diff",
                            ParameterType = typeof(JRaw)
                        }
                    },
                    InvokerBuilder = BuildOnKnockoutUpdateInvoker
                },
                new MethodDescriptorBuilder()
                {
                    Name = "GetKnockoutState",
                    Parameters =  new ParameterDescriptor[0],
                    InvokerBuilder = BuildGetKnockoutStateInvoker
                }
            };

            _methods = new ConcurrentDictionary<HubDescriptor, IDictionary<string, MethodDescriptor>>();

            _bus = bus;
            _serializer = serializer;
        }

        // Seems to only be used to the JavaScript Proxy generation
        // We don't want to it to show up there, so I'm fine with it being empty
        public IEnumerable<MethodDescriptor> GetMethods(HubDescriptor hub)
        {
            return Enumerable.Empty<MethodDescriptor>();
        }

        public bool TryGetMethod(HubDescriptor hub,
                                 string method,
                                 out MethodDescriptor descriptor,
                                 IList<IJsonValue> parameters)
        {
            IDictionary<string, MethodDescriptor> descriptors = _methods.GetOrAdd(hub, CreateMethodDescriptors);

            if (descriptors != null)
            {
                descriptor = descriptors[method];
            }
            else
            {
                descriptor = null;
            }

            return descriptor != null;
        }

        private IDictionary<string, MethodDescriptor> CreateMethodDescriptors(HubDescriptor hub)
        {
            if (typeof(KnockoutHub).IsAssignableFrom(hub.HubType))
            {
                return _builders.Select(b => b.CreateMethodDescriptor(hub)).ToDictionary(d => d.Name);
            }
            else
            {
                return null;
            }
        }

        private Func<IHub, object[], object> BuildOnKnockoutUpdateInvoker(string hubName)
        {
            var signal = DependencyResolverExtensions.KoSignalPrefix + hubName;

            return (hub, args) =>
            {
                var diff = (JRaw)args[0];
                var sourceId = hub.Context.ConnectionId;

                return _bus.Publish(new Message()
                {
                    Source = sourceId,
                    Key = signal,
                    Value = _serializer.GetMessageBuffer(diff)
                });
            };
        }

        private Func<IHub, object[], object> BuildGetKnockoutStateInvoker(string hubName)
        {
            var signal = DependencyResolverExtensions.KoSignalPrefix + hubName;

            return (hub, args) =>
            {
                var sourceId = hub.Context.ConnectionId;

                return _bus.Publish(new Message()
                {
                    Source = sourceId,
                    Key = signal,
                    CommandId = DependencyResolverExtensions.GetStateCommand
                });
            };
        }

        private class MethodDescriptorBuilder
        {
            public string Name;
            public IList<ParameterDescriptor> Parameters;
            public Func<string, Func<IHub, object[], object>> InvokerBuilder;

            public MethodDescriptor CreateMethodDescriptor(HubDescriptor hub)
            {
                return new MethodDescriptor
                {
                    ReturnType = typeof(Task),
                    Name = Name,
                    NameSpecified = false,
                    Invoker = InvokerBuilder(hub.Name),
                    Hub = hub,
                    Attributes = Enumerable.Empty<Attribute>(),
                    Parameters = Parameters
                };
            }
        }
    }
}
