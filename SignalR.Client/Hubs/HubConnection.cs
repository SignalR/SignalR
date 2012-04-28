using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalR.Client.Transports;

namespace SignalR.Client.Hubs
{
    public class HubConnection : Connection
    {
        private readonly Dictionary<string, HubProxy> _hubs = new Dictionary<string, HubProxy>(StringComparer.OrdinalIgnoreCase);

        public HubConnection(string url)
            : base(GetUrl(url))
        {
        }

        public override Task Start(IClientTransport transport)
        {
            Sending += OnConnectionSending;
            return base.Start(transport);
        }

        public override void Stop()
        {
            Sending -= OnConnectionSending;
            base.Stop();
        }

        protected override void OnReceived(JToken message)
        {
            var invocation = message.ToObject<HubInvocation>();
            HubProxy hubProxy;
            if (_hubs.TryGetValue(invocation.Hub, out hubProxy))
            {
                if (invocation.State != null)
                {
                    foreach (var state in invocation.State)
                    {
                        hubProxy[state.Key] = state.Value;
                    }
                }

                hubProxy.InvokeEvent(invocation.Method, invocation.Args);
            }

            base.OnReceived(message);
        }

        /// <summary>
        /// Creates a proxy to the <see cref="Hub"/> with the specified name.
        /// </summary>
        /// <param name="hubName">The name of the hub.</param>
        /// <returns>A <see cref="IHubProxy"/></returns>
        public IHubProxy CreateProxy(string hubName)
        {
            HubProxy hubProxy;
            if (!_hubs.TryGetValue(hubName, out hubProxy))
            {
                hubProxy = new HubProxy(this, hubName);
                _hubs[hubName] = hubProxy;
            }
            return hubProxy;
        }

        private string OnConnectionSending()
        {
            var data = _hubs.Select(p => new HubRegistrationData
            {
                Name = p.Key
            });

            return JsonConvert.SerializeObject(data);
        }

        private static string GetUrl(string url)
        {
            if (!url.EndsWith("/"))
            {
                url += "/";
            }
            return url + "signalr";
        }
    }
}
