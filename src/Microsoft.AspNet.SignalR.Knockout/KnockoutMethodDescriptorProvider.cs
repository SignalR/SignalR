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
        private const string _methodName = "OnKnockoutUpdate";

        private readonly ConcurrentDictionary<HubDescriptor, MethodDescriptor> _methods;
        private readonly IList<ParameterDescriptor> _parameters = new[]
        {
            new ParameterDescriptor
            {
                Name = "diff",
                ParameterType = typeof(JRaw)
            }
        };

        private readonly IMessageBus _bus;
        private readonly IJsonSerializer _serializer;

        public KnockoutMethodDescriptorProvider(IMessageBus bus, IJsonSerializer serializer)
        {
            _methods = new ConcurrentDictionary<HubDescriptor, MethodDescriptor>();

            _bus = bus;
            _serializer = serializer;
        }

        // Seems to only be used to the JavaScript Proxy generation
        // We don't want to it to show up there, so I'm fine with it being empty
        public IEnumerable<MethodDescriptor> GetMethods(HubDescriptor hub)
        {
            return Enumerable.Empty<MethodDescriptor>();
        }

        public bool TryGetMethod(HubDescriptor hub, string method, out MethodDescriptor descriptor, IList<IJsonValue> parameters)
        {
            if (method == _methodName)
            {
                descriptor = _methods.GetOrAdd(hub, CreateOnKnockoutUpdateMethodDescriptor);
            }
            else
            {
                descriptor = null;
            }

            return descriptor != null;
        }

        private MethodDescriptor CreateOnKnockoutUpdateMethodDescriptor(HubDescriptor hub)
        {
            if (typeof(KnockoutHub).IsAssignableFrom(hub.HubType))
            {
                return new MethodDescriptor
                {
                    ReturnType = typeof(Task),
                    Name = _methodName,
                    NameSpecified = false,
                    Invoker = OnKnockoutUpdateInvoker(hub.Name),
                    Hub = hub,
                    Attributes = Enumerable.Empty<Attribute>(),
                    Parameters = _parameters
                };
            }
            return null;
        }

        private Func<IHub, object[], object> OnKnockoutUpdateInvoker(string hubName)
        {
            return (hub, param) =>
            {
                var diff = (JRaw)param[0];
                var sourceId = hub.Context.ConnectionId;

                return _bus.Publish(new Message()
                {
                    Source = sourceId,
                    Key = DependencyResolverExtensions.KoSignalPrefix + hubName,
                    Value = _serializer.GetMessageBuffer(diff)
                });
            };
        }
    }
}
