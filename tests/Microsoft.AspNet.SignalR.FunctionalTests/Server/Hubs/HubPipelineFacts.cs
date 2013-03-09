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
        
        private class AddOrModifyNegotiateProperty : HubPipelineModule
        {
            private string _key;
            private object _valueForKey;

            public AddOrModifyNegotiateProperty(string key, object valueForKey)
            {
                _key = key;
                _valueForKey = valueForKey;
            }
            public override Action<Dictionary<string, object>> BuildNegotiate(Action<Dictionary<string, object>> negotiate)
            {
                return (response) =>
                {
                    response[_key] = _valueForKey;

                    negotiate(response);
                };
            }
        }
        
        private class CustomNegotiationResponse : NegotiationResponse
        {
            public object Foo { get; set; }
        }
    }
}
