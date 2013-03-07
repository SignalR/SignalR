using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using Owin;
using Xunit;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Server.Hubs
{
    public class HubPipelineFacts : HostedTest
    {
        [Fact]
        public void BuildNegotiateCanAddValuesToNegotiate()
        {
            using (var host = new MemoryHost())
            {
                var valueForKey = "Bar";

                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    var module = new AddOrModifyNegotiateProperty("Foo", valueForKey);

                    app.MapHubs("/signalr", configuration);

                    configuration.Resolver.Resolve<IHubPipeline>().AddModule(module);
                });

                host.Get("http://foo/signalr/negotiate").Then(result =>
                {
                    var raw = JsonConvert.DeserializeObject<CustomNegotiationResponse>(result.ReadAsString());

                    Assert.NotNull(raw.Foo);
                    Assert.Equal(raw.Foo, valueForKey);
                }).Wait();
            }
        }

        [Fact]
        public void BuildNegotiateCanModifyValuesOfNegotiate()
        {
            using (var host = new MemoryHost())
            {
                var valueForKey = "Bar";

                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    var module = new AddOrModifyNegotiateProperty("ConnectionId", valueForKey);

                    app.MapHubs("/signalr", configuration);

                    configuration.Resolver.Resolve<IHubPipeline>().AddModule(module);
                });

                host.Get("http://foo/signalr/negotiate").Then(result =>
                {
                    var raw = JsonConvert.DeserializeObject<NegotiationResponse>(result.ReadAsString());

                    Assert.NotNull(raw.ConnectionId);
                    Assert.Equal(raw.ConnectionId, valueForKey);
                }).Wait();
            }
        }

        [Fact]
        public void BuildNegotiateCanRemoveValuesOfNegotiate()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    var module = new RemoveNegotiateProperty("ConnectionId");

                    app.MapHubs("/signalr", configuration);

                    configuration.Resolver.Resolve<IHubPipeline>().AddModule(module);
                });

                host.Get("http://foo/signalr/negotiate").Then(result =>
                {
                    var raw = JsonConvert.DeserializeObject<NegotiationResponse>(result.ReadAsString());

                    Assert.Null(raw.ConnectionId);
                }).Wait();
            }
        }

        private class AddOrModifyNegotiateProperty : HubPipelineModule
        {
            private string _key;
            private object _valueForKey;

            public AddOrModifyNegotiateProperty(string key, object valueForKey)
            {
                _key = key;
                _valueForKey = valueForKey;
            }

            public override Func<HostContext, Dictionary<string, object>, Task> BuildNegotiate(Func<HostContext, Dictionary<string, object>, Task> negotiate)
            {
                return (context, response) =>
                {
                    response[_key] = _valueForKey;

                    return negotiate(context, response);
                };
            }
        }

        private class RemoveNegotiateProperty : HubPipelineModule
        {
            private string _keyToRemove;

            public RemoveNegotiateProperty(string keyToRemove)
            {
                _keyToRemove = keyToRemove;
            }

            public override Func<HostContext, Dictionary<string, object>, Task> BuildNegotiate(Func<HostContext, Dictionary<string, object>, Task> negotiate)
            {
                return (context, response) =>
                {
                    response.Remove(_keyToRemove);

                    return negotiate(context, response);
                };
            }
        }

        private class CustomNegotiationResponse : NegotiationResponse
        {
            public object Foo { get; set; }
        }
    }
}
